using System.Text.Json;

using FableCraft.Application.Exceptions;
using FableCraft.Application.Model;
using FableCraft.Application.Model.Adventure;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Persistence;
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
    private readonly IRagBuilder _ragBuilder;
    private readonly TimeProvider _timeProvider;

    public AdventureCreationService(
        ApplicationDbContext dbContext,
        IMessageDispatcher messageDispatcher,
        TimeProvider timeProvider,
        ILogger logger,
        IRagBuilder ragBuilder)
    {
        _dbContext = dbContext;
        _messageDispatcher = messageDispatcher;
        _timeProvider = timeProvider;
        _logger = logger;
        _ragBuilder = ragBuilder;
    }

    public async Task<AdventureCreationStatus> CreateAdventureAsync(
        AdventureDto adventureDto,
        CancellationToken cancellationToken)
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };
        var trackerStructure = JsonSerializer.Deserialize<TrackerStructure>(adventureDto.TrackerStructure, options);

        if (trackerStructure is null)
        {
            throw new InvalidOperationException("Failed to deserialize tracker structure.");
        }

        var adventure = new Adventure
        {
            Name = adventureDto.Name,
            CreatedAt = now,
            FirstSceneGuidance = adventureDto.FirstSceneDescription,
            LastPlayedAt = null,
            AuthorNotes = adventureDto.AuthorNotes,
            AdventureStartTime = adventureDto.ReferenceTime,
            MainCharacter = new MainCharacter
            {
                Name = adventureDto.Character.Name,
                Description = adventureDto.Character.Description
            },
            Lorebook = adventureDto.Lorebook.Select(entry => new LorebookEntry
                {
                    Description = entry.Description,
                    Content = entry.Content,
                    Category = entry.Category,
                    ContentType = entry.ContentType,
                    Priority = entry.Order
                })
                .ToList(),
            TrackerStructure = trackerStructure!
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
        Adventure? adventure = await _dbContext.Adventures
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

        try
        {
            await _ragBuilder.DeleteDatasetAsync(adventure.Id.ToString(), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error(ex,
                "Failed to delete adventure {adventureId} from knowledge graph.",
                adventureId);
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
                LastPlayed = a.LastPlayedAt
            })
            .ToListAsync(cancellationToken);

        return adventures;
    }
}