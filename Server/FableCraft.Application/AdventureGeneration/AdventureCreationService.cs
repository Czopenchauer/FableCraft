using FableCraft.Application.Exceptions;
using FableCraft.Application.Model.Adventure;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;
using FableCraft.Infrastructure.Queue;

using Microsoft.EntityFrameworkCore;

using Npgsql;

using Serilog;

namespace FableCraft.Application.AdventureGeneration;

public class AdventureCreationStatus
{
    public required Guid AdventureId { get; init; }

    public required string RagProcessing { get; init; }

    public required string SceneGeneration { get; init; }
}

public interface IAdventureCreationService
{
    Task<AdventureCreationStatus> CreateAdventureAsync(AdventureDto adventureDto, CancellationToken cancellationToken);

    Task<AdventureCreationStatus> GetAdventureCreationStatusAsync(Guid worldId, CancellationToken cancellationToken);

    Task DeleteAdventureAsync(Guid adventureId, CancellationToken cancellationToken);

    Task<IEnumerable<AdventureListItemDto>> GetAllAdventuresAsync(CancellationToken cancellationToken);
}

internal class AdventureCreationService : IAdventureCreationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger _logger;
    private readonly IMessageDispatcher _messageDispatcher;
    private readonly TimeProvider _timeProvider;

    public AdventureCreationService(
        ApplicationDbContext dbContext,
        IMessageDispatcher messageDispatcher,
        TimeProvider timeProvider,
        ILogger logger)
    {
        _dbContext = dbContext;
        _messageDispatcher = messageDispatcher;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<AdventureCreationStatus> CreateAdventureAsync(
        AdventureDto adventureDto,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var tracker = await _dbContext.TrackerDefinitions.SingleAsync(x => x.Id == adventureDto.TrackerDefinitionId, cancellationToken);

        var worldbookId = adventureDto.WorldbookId
            ?? throw new InvalidOperationException("WorldbookId is required. All adventures must be created from an indexed worldbook.");

        var worldbook = await _dbContext.Worldbooks
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == worldbookId, cancellationToken);
        if (worldbook is null)
        {
            throw new InvalidOperationException($"Worldbook {worldbookId} not found.");
        }

        if (worldbook.IndexingStatus != IndexingStatus.Indexed)
        {
            throw new InvalidOperationException(
                $"Worldbook {worldbookId} has not been indexed. " +
                "Run POST /api/Worldbook/{id}/index before creating adventures.");
        }

        var adventure = new Adventure
        {
            Name = adventureDto.Name,
            CreatedAt = now,
            FirstSceneGuidance = adventureDto.FirstSceneDescription,
            LastPlayedAt = null,
            AdventureStartTime = adventureDto.ReferenceTime,
            WorldbookId = worldbookId,
            MainCharacter = new MainCharacter
            {
                Name = adventureDto.MainCharacter.Name,
                Description = adventureDto.MainCharacter.Description
            },
            Lorebook = adventureDto.ExtraLoreEntries.Select(x => new LorebookEntry
            {
                Title = x.Title,
                Description = x.Title,
                Content = x.Content,
                Category = x.Category,
                ContentType = ContentType.txt,
                Priority = 0,
            }).ToList(),
            TrackerStructure = tracker.Structure,
            AgentLlmPresets = adventureDto.AgentLlmPresets.Select(p => new AdventureAgentLlmPreset
                {
                    LlmPresetId = p.LlmPresetId,
                    AgentName = p.AgentName
                })
                .ToList(),
            PromptPath = adventureDto.PromptPath
        };

        try
        {
            await _dbContext.Adventures.AddAsync(adventure, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is NpgsqlException { SqlState: "23505" })
        {
            _logger.Error(ex, "Adventure with name {Name} already exists.", adventureDto.Name);
            throw new InvalidOperationException($"An adventure with the name '{adventureDto.Name}' already exists.", ex);
        }

        await _messageDispatcher.PublishAsync(new AddAdventureToKnowledgeGraphCommand { AdventureId = adventure.Id },
            cancellationToken);

        return await GetAdventureCreationStatusAsync(adventure.Id, cancellationToken);
    }

    public async Task<AdventureCreationStatus> GetAdventureCreationStatusAsync(Guid adventureId,
        CancellationToken cancellationToken)
    {
        var adventure = await _dbContext.Adventures
            .FirstOrDefaultAsync(w => w.Id == adventureId, cancellationToken);

        if (adventure == null)
        {
            throw new AdventureNotFoundException(adventureId);
        }

        return new AdventureCreationStatus
        {
            AdventureId = adventureId,
            RagProcessing = adventure.RagProcessingStatus.ToString(),
            SceneGeneration = adventure.SceneGenerationStatus.ToString()
        };
    }

    public async Task DeleteAdventureAsync(Guid adventureId, CancellationToken cancellationToken)
    {
        var adventure = await _dbContext.Adventures
            .Include(w => w.MainCharacter)
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
            throw new NotImplementedException();
        }
        catch (Exception ex)
        {
            _logger.Warning(ex,
                "Failed to delete adventure {adventureId} volume. Continuing with database cleanup.",
                adventureId);
        }

        await _dbContext.Chunks.Where(x => x.AdventureId == adventureId).ExecuteDeleteAsync(cancellationToken);
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