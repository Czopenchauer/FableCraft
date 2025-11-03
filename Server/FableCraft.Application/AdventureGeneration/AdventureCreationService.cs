using System.Text.Json;

using FableCraft.Application.Exceptions;
using FableCraft.Application.Model;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Queue;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using Polly;
using Polly.Retry;

using Serilog;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.AdventureGeneration;

public enum Status
{
    Pending,
    Completed,
    InProgress,
    Failed
}

public static class StatusExtensions
{
    public static Status ToStatus(this ProcessingStatus processingStatus) =>
        processingStatus switch
        {
            ProcessingStatus.Pending => Status.Pending,
            ProcessingStatus.Completed => Status.Completed,
            ProcessingStatus.InProgress => Status.InProgress,
            ProcessingStatus.Failed => Status.Failed,
            _ => throw new ArgumentOutOfRangeException(nameof(processingStatus), processingStatus, null)
        };
}

public class AdventureCreationStatus
{
    public AdventureCreationStatus(Adventure adventure)
    {
        AdventureId = adventure.Id;

        var statusDict = new Dictionary<string, string>();
        foreach (var entry in adventure.Lorebook)
        {
            statusDict.Add(entry.Category, entry.ProcessingStatus.ToStatus().ToString());
        }

        statusDict.Add(nameof(Character), adventure.Character.ProcessingStatus.ToString());
        statusDict.Add(nameof(Adventure), adventure.ProcessingStatus.ToString());
        ComponentStatuses = statusDict;
    }

    public Guid AdventureId { get; }

    public Dictionary<string, string> ComponentStatuses { get; }
}

public interface IAdventureCreationService
{
    AvailableLorebookDto[] GetSupportedLorebook();

    Task<string> GenerateLorebookAsync(
        LorebookEntryDto[] lorebooks,
        string category,
        CancellationToken cancellationToken,
        string? additionalInstruction = null);

    Task<AdventureCreationStatus> CreateAdventureAsync(AdventureDto adventureDto, CancellationToken cancellationToken);

    Task<AdventureCreationStatus> GetAdventureCreationStatusAsync(Guid worldId, CancellationToken cancellationToken);

    Task<AdventureCreationStatus> RetryKnowledgeGraphProcessingAsync(Guid adventureId, CancellationToken cancellationToken);

    Task DeleteAdventureAsync(Guid adventureId, CancellationToken cancellationToken);

    Task<IEnumerable<AdventureListItemDto>> GetAllAdventuresAsync(CancellationToken cancellationToken);
}

internal class AdventureCreationService : IAdventureCreationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMessageDispatcher _messageDispatcher;
    private readonly TimeProvider _timeProvider;
    private readonly IOptions<AdventureCreationConfig> _config;
    private readonly IKernelBuilder _kernelBuilder;
    private readonly ILogger _logger;
    private readonly Infrastructure.Clients.IRagBuilder _ragBuilder;

    public AdventureCreationService(
        ApplicationDbContext dbContext,
        IMessageDispatcher messageDispatcher,
        TimeProvider timeProvider,
        IOptions<AdventureCreationConfig> config,
        IKernelBuilder kernelBuilder,
        ILogger logger,
        Infrastructure.Clients.IRagBuilder ragBuilder)
    {
        _dbContext = dbContext;
        _messageDispatcher = messageDispatcher;
        _timeProvider = timeProvider;
        _config = config;
        _kernelBuilder = kernelBuilder;
        _logger = logger;
        _ragBuilder = ragBuilder;
    }

    public AvailableLorebookDto[] GetSupportedLorebook()
    {
        var categories = _config.Value.Lorebooks.Select(x => new AvailableLorebookDto()
        {
            Category = x.Key,
            Description = x.Value.Description,
            Priority = x.Value.Priority
        }).OrderBy(x => x.Priority).ToArray();

        return categories;
    }

    public async Task<AdventureCreationStatus> CreateAdventureAsync(AdventureDto adventureDto,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();

        var world = new Adventure
        {
            Name = adventureDto.Name,
            CreatedAt = now,
            FirstSceneGuidance = adventureDto.FirstSceneDescription,
            LastPlayedAt = null,
            AuthorNotes = adventureDto.AuthorNotes,
            Character = new Character
            {
                Name = adventureDto.Character.Name,
                Description = adventureDto.Character.Description,
                Background = adventureDto.Character.Background,
                ProcessingStatus = ProcessingStatus.Pending,
            },
            Lorebook = adventureDto.Lorebook.Select(entry => new LorebookEntry
                {
                    Description = entry.Description,
                    Content = entry.Content,
                    Category = entry.Category,
                    ProcessingStatus =
                        ProcessingStatus.Pending,
                })
                .ToList(),
        };

        _dbContext.Adventures.Add(world);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _messageDispatcher.PublishAsync(new AddAdventureToKnowledgeGraphCommand { AdventureId = world.Id },
            cancellationToken);

        return new AdventureCreationStatus(world);
    }

    public async Task<string> GenerateLorebookAsync(
        LorebookEntryDto[] lorebooks,
        string category,
        CancellationToken cancellationToken,
        string? additionalInstruction = null)
    {
        if (!_config.Value.Lorebooks.TryGetValue(category, out var lorebookConfig))
        {
            throw new ArgumentException($"Lorebook type '{category}' is not supported.", category);
        }

        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Prompts", "adventure_generation", lorebookConfig.PromptPath);
            await using var stream = File.OpenRead(path);
            using var reader = new StreamReader(stream);
            var prompt = await reader.ReadToEndAsync(cancellationToken);

            var orderedLorebooks = _config.Value.Lorebooks
                .Where(x => x.Value.Priority <= lorebookConfig.Priority)
                .OrderBy(x => x.Value.Priority)
                .Join(lorebooks,
                    x => x.Key,
                    y => y.Category,
                    (x, y) => new { config = x.Value, lorebookEntry = y })
                .ToArray();

            var establishedWorld = string.Empty;
            if (orderedLorebooks.Length > 0)
            {
                establishedWorld = orderedLorebooks.Aggregate("Already established world:",
                    (current, entry) =>
                        current + $"\n{entry.lorebookEntry.Category}\n{entry.lorebookEntry.Content}\n");
            }

            if (!string.IsNullOrEmpty(additionalInstruction))
            {
                establishedWorld += $"\nInstruction:\n{additionalInstruction}\n";
            }

            ResiliencePipeline pipeline = new ResiliencePipelineBuilder()
                .AddRetry(new RetryStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<InvalidCastException>(),
                    MaxRetryAttempts = 1,
                    Delay = TimeSpan.FromSeconds(5),
                    OnRetry = args =>
                    {
                        _logger.Warning("Attempt {attempt}: Retrying lorebook generation for type {type} due to error: {error}",
                            args.AttemptNumber,
                            category,
                            args.Outcome.Exception?.Message);
                        return default;
                    }
                })
                .Build();

            var chatHistory = new ChatHistory();
            chatHistory.AddUserMessage(prompt);
            chatHistory.AddUserMessage(establishedWorld);
            var kernel = _kernelBuilder.WithBase(_config.Value.LlmModel).Build();
            var promptExecutionSettings = new OpenAIPromptExecutionSettings
            {
                Temperature = _config.Value.Temperature,
                PresencePenalty = _config.Value.PresencePenalty,
                FrequencyPenalty = _config.Value.FrequencyPenalty,
                MaxTokens = _config.Value.MaxTokens,
                TopP = _config.Value.TopP,
            };
            _logger.Debug("Generating lorebook for type {type} with prompt: {prompt}", category, establishedWorld);
            try
            {
                var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
                return await pipeline.ExecuteAsync(async token =>
                           {
                               var result = await chatCompletionService.GetChatMessageContentAsync(chatHistory, promptExecutionSettings, kernel, token);
                               var replyInnerContent = result.InnerContent as OpenAI.Chat.ChatCompletion;
                               _logger.Information("Input usage: {usage}, output usage {output}, total usage {total}",
                                   replyInnerContent?.Usage.InputTokenCount,
                                   replyInnerContent?.Usage.OutputTokenCount,
                                   replyInnerContent?.Usage.TotalTokenCount);
                               _logger.Debug("Generated response: {response}", JsonSerializer.Serialize(result));
                               return result.Content?.RemoveThinkingBlock();
                           },
                           cancellationToken)
                       ?? string.Empty;
            }
            catch (InvalidCastException ex)
            {
                _logger.Error(ex, "Failed to generate lorebook for type {type}", category);
                throw;
            }
        }
        catch (FileNotFoundException e)
        {
            _logger.Error(e, "Lorebook file for type {type} not found", category);
            throw;
        }
    }

    public async Task<AdventureCreationStatus> GetAdventureCreationStatusAsync(Guid worldId,
        CancellationToken cancellationToken)
    {
        var world = await _dbContext.Adventures
            .Include(w => w.Character)
            .Include(w => w.Lorebook)
            .FirstOrDefaultAsync(w => w.Id == worldId, cancellationToken);

        if (world == null)
        {
            throw new AdventureNotFoundException(worldId);
        }

        return new AdventureCreationStatus(world);
    }

    public async Task<AdventureCreationStatus> RetryKnowledgeGraphProcessingAsync(Guid adventureId,
        CancellationToken cancellationToken)
    {
        var adventure = await _dbContext.Adventures
            .Include(w => w.Character)
            .Include(w => w.Lorebook)
            .FirstOrDefaultAsync(w => w.Id == adventureId, cancellationToken);

        if (adventure == null)
        {
            throw new AdventureNotFoundException(adventureId);
        }

        var hasPendingOrFailed = adventure.Character.ProcessingStatus is ProcessingStatus.Pending or ProcessingStatus.Failed
                                 || adventure.Lorebook.Any(x => x.ProcessingStatus is ProcessingStatus.Pending or ProcessingStatus.Failed);

        if (!hasPendingOrFailed)
        {
            _logger.Information("No pending or failed items for adventure {AdventureId}, skipping retry", adventureId);
            return new AdventureCreationStatus(adventure);
        }

        _logger.Information("Retrying knowledge graph processing for adventure {AdventureId}", adventureId);
        await _messageDispatcher.PublishAsync(new AddAdventureToKnowledgeGraphCommand { AdventureId = adventureId },
            cancellationToken);

        return new AdventureCreationStatus(adventure);
    }

    public async Task DeleteAdventureAsync(Guid adventureId, CancellationToken cancellationToken)
    {
        var adventure = await _dbContext.Adventures
            .Include(w => w.Character)
            .Include(w => w.Lorebook)
            .Include(x => x.Scenes)
            .ThenInclude(x => x.CharacterActions)
            .FirstOrDefaultAsync(w => w.Id == adventureId, cancellationToken);

        if (adventure == null)
        {
            throw new AdventureNotFoundException(adventureId);
        }

        try
        {
            var tasks = adventure.Lorebook.Select(x => x.Id).Concat(adventure.Scenes.Select(x => x.Id)).Concat([adventure.CharacterId])
                .Select(id => _ragBuilder.DeleteDataAsync(id.ToString(), cancellationToken));
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.Error(ex,
                "Failed to delete adventure {adventureId} from knowledge graph.",
                adventure.Id);
            throw;
        }

        _dbContext.Adventures.Remove(adventure);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<AdventureListItemDto>> GetAllAdventuresAsync(CancellationToken cancellationToken)
    {
        var adventures = await _dbContext.Adventures
            .Include(a => a.Scenes)
            .OrderByDescending(a => a.LastPlayedAt)
            .Select(a => new AdventureListItemDto
            {
                AdventureId = a.Id,
                Name = a.Name,
                LastScenePreview = a.Scenes
                    .OrderByDescending(s => s.SequenceNumber)
                    .Select(s => s.NarrativeText.Length > 200
                        ? s.NarrativeText.Substring(0, 200)
                        : s.NarrativeText)
                    .FirstOrDefault(),
                Created = a.CreatedAt,
                LastPlayed = a.LastPlayedAt,
            })
            .ToListAsync(cancellationToken);

        return adventures;
    }
}