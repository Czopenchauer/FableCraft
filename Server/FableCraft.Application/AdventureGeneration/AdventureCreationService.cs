using System.Text.Json;

using FableCraft.Application.Exceptions;
using FableCraft.Application.Model;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Queue;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using OpenAI.Chat;

using Polly;
using Polly.Retry;

using Serilog;

using ChatMessageContent = Microsoft.SemanticKernel.ChatMessageContent;
using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.AdventureGeneration;

public class AdventureCreationStatus
{
    public required Guid AdventureId { get; init; }

    public required Dictionary<string, string> ComponentStatuses { get; init; }
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
    private readonly IOptions<AdventureCreationConfig> _config;
    private readonly ApplicationDbContext _dbContext;
    private readonly IKernelBuilder _kernelBuilder;
    private readonly ILogger _logger;
    private readonly IMessageDispatcher _messageDispatcher;
    private readonly IRagBuilder _ragBuilder;
    private readonly TimeProvider _timeProvider;

    public AdventureCreationService(
        ApplicationDbContext dbContext,
        IMessageDispatcher messageDispatcher,
        TimeProvider timeProvider,
        IOptions<AdventureCreationConfig> config,
        IKernelBuilder kernelBuilder,
        ILogger logger,
        IRagBuilder ragBuilder)
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
        var categories = _config.Value.Lorebooks.Select(x => new AvailableLorebookDto
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
        DateTimeOffset now = _timeProvider.GetUtcNow();

        var adventure = new Adventure
        {
            Name = adventureDto.Name,
            CreatedAt = now,
            FirstSceneGuidance = adventureDto.FirstSceneDescription,
            LastPlayedAt = null,
            AuthorNotes = adventureDto.AuthorNotes,
            MainCharacter = new MainCharacter
            {
                Name = adventureDto.Character.Name,
                Description = adventureDto.Character.Description,
            },
            Lorebook = adventureDto.Lorebook.Select(entry => new LorebookEntry
                {
                    Description = entry.Description,
                    Content = entry.Content,
                    Category = entry.Category
                })
                .ToList(),
            TrackerStructure = null
        };

        _dbContext.Adventures.Add(adventure);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _messageDispatcher.PublishAsync(new AddAdventureToKnowledgeGraphCommand { AdventureId = adventure.Id },
            cancellationToken);

        return await GetAdventureCreationStatusAsync(adventure.Id, cancellationToken);
    }

    public async Task<string> GenerateLorebookAsync(
        LorebookEntryDto[] lorebooks,
        string category,
        CancellationToken cancellationToken,
        string? additionalInstruction = null)
    {
        if (!_config.Value.Lorebooks.TryGetValue(category, out LorebookConfig? lorebookConfig))
        {
            throw new ArgumentException($"Lorebook type '{category}' is not supported.", category);
        }

        try
        {
            await using FileStream stream = File.OpenRead(lorebookConfig.GetPromptFileName());
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
            Kernel kernel = _kernelBuilder.WithBase(_config.Value.LlmModel).Build();
            var promptExecutionSettings = new OpenAIPromptExecutionSettings
            {
                Temperature = _config.Value.Temperature,
                PresencePenalty = _config.Value.PresencePenalty,
                FrequencyPenalty = _config.Value.FrequencyPenalty,
                MaxTokens = _config.Value.MaxTokens,
                TopP = _config.Value.TopP
            };
            _logger.Debug("Generating lorebook for type {type} with prompt: {prompt}", category, establishedWorld);
            try
            {
                var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
                return await pipeline.ExecuteAsync(async token =>
                           {
                               ChatMessageContent result = await chatCompletionService.GetChatMessageContentAsync(chatHistory, promptExecutionSettings, kernel, token);
                               var replyInnerContent = result.InnerContent as ChatCompletion;
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

    public async Task<AdventureCreationStatus> GetAdventureCreationStatusAsync(Guid adventureId,
        CancellationToken cancellationToken)
    {
        var adventure = await _dbContext.Adventures
            .Include(w => w.MainCharacter)
            .Include(w => w.Lorebook)
            .Include(x => x.Scenes)
            .Select(x => new
            {
                x.Id,
                CharacterId = x.MainCharacter.Id,
                Lorebooks = x.Lorebook.Select(y => new
                {
                    LorebookId = y.Id,
                    y.Category
                }),
                SceneIds = x.Scenes.Select(s => s.Id),
                x.ProcessingStatus
            })
            .FirstOrDefaultAsync(w => w.Id == adventureId, cancellationToken);

        if (adventure == null)
        {
            throw new AdventureNotFoundException(adventureId);
        }

        var lorebookStatuses = await _dbContext.Chunks
            .Where(x => adventure.Lorebooks.Select(y => y.LorebookId).Contains(x.EntityId))
            .Join(_dbContext.LorebookEntries,
                chunk => chunk.EntityId,
                lorebook => lorebook.Id,
                (chunk, lorebook) => new { lorebook.Category, chunk.ProcessingStatus })
            .GroupBy(x => x.Category)
            .Select(g => new
            {
                Category = g.Key,
                Status = g.All(s => s.ProcessingStatus == ProcessingStatus.Completed) ? nameof(ProcessingStatus.Completed) :
                    g.Any(s => s.ProcessingStatus == ProcessingStatus.Failed) ? nameof(ProcessingStatus.Failed) :
                    g.Any(s => s.ProcessingStatus == ProcessingStatus.InProgress) ? nameof(ProcessingStatus.InProgress) :
                    nameof(ProcessingStatus.Pending)
            })
            .ToDictionaryAsync(x => x.Category, x => x.Status, cancellationToken);

        foreach (var category in adventure.Lorebooks.Select(x => x.Category).Except(lorebookStatuses.Keys))
        {
            lorebookStatuses.Add(category, nameof(ProcessingStatus.Pending));
        }

        var characterStatus = await _dbContext.Chunks
                                  .Where(x => x.EntityId == adventure.CharacterId)
                                  .GroupBy(x => x.EntityId)
                                  .Select(g => g.All(s => s.ProcessingStatus == ProcessingStatus.Completed) ? nameof(ProcessingStatus.Completed) :
                                      g.Any(s => s.ProcessingStatus == ProcessingStatus.Failed) ? nameof(ProcessingStatus.Failed) :
                                      g.Any(s => s.ProcessingStatus == ProcessingStatus.InProgress) ? nameof(ProcessingStatus.InProgress) :
                                      nameof(ProcessingStatus.Pending))
                                  .FirstOrDefaultAsync(cancellationToken)
                              ?? nameof(ProcessingStatus.Pending);

        var sceneStatus = adventure.SceneIds.Any()
            ? await _dbContext.Chunks
                .Where(x => adventure.SceneIds.Contains(x.EntityId))
                .GroupBy(x => x.EntityId)
                .Select(g => g.All(s => s.ProcessingStatus == ProcessingStatus.Completed) ? ProcessingStatus.Completed :
                    g.Any(s => s.ProcessingStatus == ProcessingStatus.Failed) ? ProcessingStatus.Failed :
                    g.Any(s => s.ProcessingStatus == ProcessingStatus.InProgress) ? ProcessingStatus.InProgress :
                    ProcessingStatus.Pending)
                .Select(status => status.ToString())
                .ToListAsync(cancellationToken)
            : new List<string>();

        var aggregatedSceneStatus = !sceneStatus.Any() ? string.Empty :
            sceneStatus.All(s => s == nameof(ProcessingStatus.Completed)) ? nameof(ProcessingStatus.Completed) :
            sceneStatus.Any(s => s == nameof(ProcessingStatus.Failed)) ? nameof(ProcessingStatus.Failed) :
            sceneStatus.Any(s => s == nameof(ProcessingStatus.InProgress)) ? nameof(ProcessingStatus.InProgress) :
            nameof(ProcessingStatus.Pending);

        var status = new Dictionary<string, string>(lorebookStatuses)
        {
            ["Character"] = characterStatus
        };

        if (adventure.SceneIds.Any())
        {
            status.Add("Importing scenes", aggregatedSceneStatus);
        }
        else
        {
            status.Add("Creating first scene", adventure.ProcessingStatus.ToString());
        }

        return new AdventureCreationStatus
        {
            AdventureId = adventureId,
            ComponentStatuses = status
        };
    }

    public async Task<AdventureCreationStatus> RetryKnowledgeGraphProcessingAsync(Guid adventureId,
        CancellationToken cancellationToken)
    {
        AdventureCreationStatus adventureStatus = await GetAdventureCreationStatusAsync(adventureId, cancellationToken);

        if (!adventureStatus.ComponentStatuses.ContainsValue(nameof(ProcessingStatus.Failed)))
        {
            _logger.Information("No pending or failed items for adventure {AdventureId}, skipping retry", adventureId);
            return adventureStatus;
        }

        _logger.Information("Retrying knowledge graph processing for adventure {AdventureId}", adventureId);
        await _messageDispatcher.PublishAsync(new AddAdventureToKnowledgeGraphCommand { AdventureId = adventureId },
            cancellationToken);

        return adventureStatus;
    }

    public async Task DeleteAdventureAsync(Guid adventureId, CancellationToken cancellationToken)
    {
        Adventure? adventure = await _dbContext.Adventures
            .Include(w => w.MainCharacter)
            .Include(w => w.Lorebook)
            .Include(x => x.Scenes)
            .ThenInclude(x => x.CharacterActions)
            .FirstOrDefaultAsync(w => w.Id == adventureId, cancellationToken);

        if (adventure == null)
        {
            throw new AdventureNotFoundException(adventureId);
        }

        var ids = adventure.Lorebook.Select(x => x.Id).Concat(adventure.Scenes.Select(x => x.Id)).Concat([adventure.MainCharacter.Id]);
        var chunks = new List<Chunk>();
        foreach (var guides in ids.Chunk(50))
        {
            chunks = await _dbContext.Chunks.Where(x => guides.Contains(x.Id)).ToListAsync(cancellationToken);
            try
            {
                var tasks = chunks.Select(chunk => _ragBuilder.DeleteDataAsync(chunk.EntityId.ToString(), cancellationToken));
                await Task.WhenAll(tasks);
                chunks.Clear();
            }
            catch (Exception ex)
            {
                _logger.Error(ex,
                    "Failed to delete adventure {adventureId} from knowledge graph.",
                    adventure.Id);
                throw;
            }
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
                LastPlayed = a.LastPlayedAt
            })
            .ToListAsync(cancellationToken);

        return adventures;
    }
}