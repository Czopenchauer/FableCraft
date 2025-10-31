using FableCraft.Application.Exceptions;
using FableCraft.Application.Model;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Queue;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;

using Serilog;

using IKernelBuilder = FableCraft.Infrastructure.Clients.IKernelBuilder;

namespace FableCraft.Application.AdventureGeneration;

public enum Status
{
    Pending,
    Completed,
    Failed
}

public static class StatusExtensions
{
    public static Status ToStatus(this ProcessingStatus processingStatus) =>
        processingStatus switch
        {
            ProcessingStatus.Pending => Status.Pending,
            ProcessingStatus.Completed => Status.Completed,
            ProcessingStatus.Failed => Status.Failed,
            _ => throw new ArgumentOutOfRangeException(nameof(processingStatus), processingStatus, null)
        };
}

public class AdventureCreationStatus
{
    public AdventureCreationStatus(Adventure adventure)
    {
        AdventureId = adventure.Id;

        var statusDict = new Dictionary<string, Status>
        {
            { "World Description", adventure.ProcessingStatus.ToStatus() },
            { "Character", adventure.Character.ProcessingStatus.ToStatus() }
        };

        foreach (var entry in adventure.Lorebook)
        {
            statusDict.Add(entry.Category, entry.ProcessingStatus.ToStatus());
        }

        ComponentStatuses = statusDict;
    }

    public Guid AdventureId { get; }

    public Dictionary<string, Status> ComponentStatuses { get; }
}

public interface IAdventureCreationService
{
    IReadOnlyDictionary<string, string> GetSupportedLorebook();

    Task<string?> GenerateLorebookAsync(AdventureDto adventureDto, string instruction, string category,
        CancellationToken cancellationToken);

    Task<AdventureCreationStatus> CreateAdventureAsync(AdventureDto adventureDto, CancellationToken cancellationToken);

    Task<AdventureCreationStatus> GetAdventureCreationStatusAsync(Guid worldId, CancellationToken cancellationToken);
}

internal class AdventureCreationService : IAdventureCreationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMessageDispatcher _messageDispatcher;
    private readonly TimeProvider _timeProvider;
    private readonly IOptions<AdventureCreationConfig> _config;
    private readonly IKernelBuilder _kernelBuilder;
    private readonly ILogger _logger;

    public AdventureCreationService(
        ApplicationDbContext dbContext,
        IMessageDispatcher messageDispatcher,
        TimeProvider timeProvider,
        IOptions<AdventureCreationConfig> config,
        IKernelBuilder kernelBuilder,
        ILogger logger)
    {
        _dbContext = dbContext;
        _messageDispatcher = messageDispatcher;
        _timeProvider = timeProvider;
        _config = config;
        _kernelBuilder = kernelBuilder;
        _logger = logger;
    }

    public IReadOnlyDictionary<string, string> GetSupportedLorebook()
    {
        var categories = _config.Value.Lorebooks.ToDictionary(x => x.Key, y => y.Value.Description);

        return categories;
    }

    public async Task<string?> GenerateLorebookAsync(
        AdventureDto adventureDto,
        string instruction,
        string category,
        CancellationToken cancellationToken)
    {
        if (!_config.Value.Lorebooks.TryGetValue(category, out var lorebook))
        {
            return null;
        }

        try
        {
            await using var stream = File.OpenRead(lorebook.PromptPath);
            using var reader = new StreamReader(stream);
            var prompt = await reader.ReadToEndAsync(cancellationToken);
            var kernel = _kernelBuilder.WithBase(_config.Value.LlmModel).Build();
            var promptExecutionSettings = new OpenAIPromptExecutionSettings()
            {
                Temperature = _config.Value.Temperature,
                PresencePenalty = _config.Value.PresencePenalty,
                FrequencyPenalty = _config.Value.FrequencyPenalty,
                MaxTokens = _config.Value.MaxTokens,
                TopP = _config.Value.TopP
            };

            var orderedLorebooks = _config.Value.Lorebooks
                .Where(x => x.Value.Priority <= lorebook.Priority)
                .OrderBy(x => x.Value.Priority)
                .Join(adventureDto.Lorebook,
                    x => x.Key,
                    y => y.Category,
                    (config, lorebookEntry) => (lorebookEntry, config.Value.Priority))
                .OrderBy(x => x.Priority)
                .Aggregate("Already established world:",
                    (current, entry) =>
                        current + $"\nCategory: {entry.lorebookEntry.Category}\n{entry.lorebookEntry.Content}\n");

            var arguments = new KernelArguments(promptExecutionSettings)
            {
                {
                    "history", new[]
                    {
                        new { role = AuthorRole.User, content = "What is my current membership level?" },
                    }
                },
            };

            var function = await kernel.InvokeHandlebarsPromptAsync(prompt, arguments, cancellationToken: cancellationToken);
            _logger.Debug("{prompt}", function.RenderedPrompt);

            return function.GetValue<string>();
        }
        catch (FileNotFoundException e)
        {
            _logger.Error(e, "Lorebook file for type {type} not found", category);
            throw;
        }
    }

    public async Task<AdventureCreationStatus> CreateAdventureAsync(AdventureDto adventureDto,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();

        var world = new Adventure
        {
            Name = adventureDto.Name,
            WorldDescription = adventureDto.WorldDescription,
            CreatedAt = now,
            LastPlayedAt = now,
            ProcessingStatus = ProcessingStatus.Pending,
            Character = new Character
            {
                Name = adventureDto.Character.Name,
                Description = adventureDto.Character.Description,
                Background = adventureDto.Character.Background,
                ProcessingStatus = ProcessingStatus.Pending,
                StatsJson = string.Empty,
            },
            Lorebook = adventureDto.Lorebook.Select(entry => new LorebookEntry
            {
                Description = entry.Description,
                Content = entry.Content,
                Category = entry.Category,
                ProcessingStatus =
                    ProcessingStatus.Pending,
            }).ToList(),
            Scenes = new List<Scene>()
        };

        _dbContext.Adventures.Add(world);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _messageDispatcher.PublishAsync(new AddAdventureToKnowledgeGraphCommand { AdventureId = world.Id },
            cancellationToken);

        return new AdventureCreationStatus(world);
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
}