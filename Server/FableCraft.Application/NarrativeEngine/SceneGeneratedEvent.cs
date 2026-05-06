using System.Text.Json;

using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;
using FableCraft.Infrastructure.Queue;

using Microsoft.EntityFrameworkCore;

using Serilog;

namespace FableCraft.Application.NarrativeEngine;

public sealed class SceneGeneratedEvent : IMessage
{
    public required Guid SceneId { get; init; }

    public required Guid AdventureId { get; set; }
}

internal sealed class SceneGeneratedEventHandler : IMessageHandler<SceneGeneratedEvent>
{
    private const int MinScenesToCommit = 1;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger _logger;
    private readonly IRagChunkService _ragChunkService;
    private readonly IRagClientFactory _ragClientFactory;

    public SceneGeneratedEventHandler(
        ApplicationDbContext dbContext,
        IRagChunkService ragChunkService,
        ILogger logger, IRagClientFactory ragClientFactory)
    {
        _dbContext = dbContext;
        _ragChunkService = ragChunkService;
        _logger = logger;
        _ragClientFactory = ragClientFactory;
    }

    public async Task HandleAsync(SceneGeneratedEvent message, CancellationToken cancellationToken)
    {
        var currentScene = await _dbContext.Scenes
            .Select(x =>
                new
                {
                    x.Id,
                    x.SequenceNumber
                })
            .SingleAsync(x => x.Id == message.SceneId, cancellationToken);

        var candidateSceneIds = await _dbContext.Scenes
            .Where(x => x.AdventureId == message.AdventureId
                        && x.SequenceNumber < currentScene.SequenceNumber
                        && x.CommitStatus == CommitStatus.Uncommited)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (candidateSceneIds.Count < MinScenesToCommit)
        {
            _logger.Information("Not enough scenes to commit for adventure {AdventureId}. Current uncommitted scenes count: {ScenesCount}",
                message.AdventureId,
                candidateSceneIds.Count);
            return;
        }

        var lockedCount = await _dbContext.Scenes
            .Where(x => candidateSceneIds.Contains(x.Id) && x.CommitStatus == CommitStatus.Uncommited)
            .ExecuteUpdateAsync(x => x.SetProperty(s => s.CommitStatus, CommitStatus.Lock), cancellationToken);

        if (lockedCount == 0)
        {
            _logger.Information("Scenes already locked by another process for adventure {AdventureId}", message.AdventureId);
            return;
        }

        var scenesToCommit = await _dbContext.Scenes
            .AsSplitQuery()
            .Include(x => x.Lorebooks)
            .Include(x => x.CharacterActions)
            .Include(x => x.CharacterStates)
            .Include(x => x.CharacterSceneRewrites)
            .Where(x => candidateSceneIds.Contains(x.Id) && x.CommitStatus == CommitStatus.Lock)
            .ToListAsync(cancellationToken);

        if (scenesToCommit.Count == 0)
        {
            _logger.Warning("No scenes to commit after locking for adventure {AdventureId}.", message.AdventureId);
            return;
        }

        _logger.Information("Committing {ScenesCount} scenes for adventure {AdventureId}",
            scenesToCommit.Count,
            message.AdventureId);
        try
        {
            var characters = await _dbContext.Characters
                .Where(x => x.AdventureId == message.AdventureId)
                .ToListAsync(cancellationToken);

            // Get adventure character names for filtering private activities
            var adventureCharacterNames = await GetAdventureCharacterNamesAsync(message.AdventureId, cancellationToken);

            var creationRequests = new List<ChunkCreationRequest>();
            foreach (var scene in scenesToCommit)
            {
                // Filter out private activities (witnessed only by adventure characters)
                var lorebooksToCommit = FilterPrivateActivities(scene.Lorebooks, adventureCharacterNames);

                var lorebookRequests = lorebooksToCommit.Select(x =>
                    new ChunkCreationRequest(x.Id,
                        $"""
                         {x.Title}
                         {x.Content}
                         {x.Category}
                         """,
                        x.ContentType,
                        [RagClientExtensions.GetWorldDatasetName()]));
                creationRequests.AddRange(lorebookRequests);

                var sceneContent = $"""
                                    Character is: {scene.Metadata.Tracker!.MainCharacter!.MainCharacter!.Name}
                                    Time: {scene.Metadata.Tracker.Scene!.Time}
                                    Location: {scene.Metadata.Tracker.Scene!.Location}
                                    Weather: {scene.Metadata.Tracker.Scene!.Weather}
                                    Characters on scene: {string.Join(", ", scene.Metadata.Tracker.Scene.CharactersPresent)}

                                    {scene.NarrativeText}
                                    """;
                var sceneRequest = new ChunkCreationRequest(scene.Id, sceneContent, ContentType.txt, [RagClientExtensions.GetMainCharacterDatasetName()]);
                creationRequests.Add(sceneRequest);

                scene.CharacterSceneRewrites
                    .ForEach(x =>
                    {
                        var content = $"""
                                       Character: {characters.Single(y => y.Id == x.CharacterId).Name}
                                       Time: {x.SceneTracker.Time}
                                       Location: {x.SceneTracker.Location}
                                       Weather: {x.SceneTracker.Weather}
                                       Characters on scene: {string.Join(", ", x.SceneTracker.CharactersPresent)}

                                       {x.Content}
                                       """;
                        creationRequests.Add(new ChunkCreationRequest(x.Id,
                            content,
                            ContentType.txt,
                            [RagClientExtensions.GetCharacterDatasetName(x.CharacterId)]));
                    });
            }

            var allDatasets = characters
                .Select(x => RagClientExtensions.GetCharacterDatasetName(x.Id))
                .Append(RagClientExtensions.GetMainCharacterDatasetName())
                .Append(RagClientExtensions.GetWorldDatasetName()).ToArray();
            foreach (var character in characters)
            {
                creationRequests.Add(new ChunkCreationRequest(character.Id,
                    RagClientExtensions.GetCharacterDescription(character),
                    ContentType.txt,
                    allDatasets));
            }

            var mainCharacter = await _dbContext.MainCharacters
                .SingleAsync(x => x.AdventureId == message.AdventureId, cancellationToken);

            creationRequests.Add(new ChunkCreationRequest(mainCharacter.Id,
                RagClientExtensions.GetMainCharacterDescription(mainCharacter),
                ContentType.txt,
                allDatasets));

            var strategy = _dbContext.Database.CreateExecutionStrategy();
            var ragBuilder = await _ragClientFactory.CreateBuildClientForAdventure(message.AdventureId, cancellationToken);
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                var chunks = await _ragChunkService.CreateChunk(creationRequests, message.AdventureId, cancellationToken);
                await _ragChunkService.CommitChunksToRagAsync(ragBuilder, chunks, cancellationToken);
                await _ragChunkService.CognifyDatasetsAsync(ragBuilder, allDatasets, cancellationToken: cancellationToken);

                foreach (var scene in scenesToCommit)
                {
                    scene.CommitStatus = CommitStatus.Commited;
                }

                var existingChunks = await _dbContext.Chunks.Where(x => x.AdventureId == message.AdventureId).ToListAsync(cancellationToken);
                var newChunks = chunks.Except(existingChunks, new ChunkComparer()).ToList();
                _dbContext.AddRange(newChunks);
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(CancellationToken.None);
            });
        }
        catch (Exception e)
        {
            await _dbContext.Scenes
                .Where(x => scenesToCommit.Select(y => y.Id).Contains(x.Id))
                .ExecuteUpdateAsync(x => x.SetProperty(s => s.CommitStatus, CommitStatus.Uncommited), cancellationToken);
            _logger.Error(e, "Error while committing scenes for adventure {AdventureId}", message.AdventureId);
            throw;
        }
    }

    /// <summary>
    ///     Gets the names of all adventure characters (main character + arc_important + significant).
    /// </summary>
    private async Task<HashSet<string>> GetAdventureCharacterNamesAsync(
        Guid adventureId,
        CancellationToken cancellationToken)
    {
        var adventure = await _dbContext.Adventures
            .Include(a => a.MainCharacter)
            .Include(a => a.Characters)
            .FirstOrDefaultAsync(a => a.Id == adventureId, cancellationToken);

        if (adventure is null)
        {
            return [];
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            adventure.MainCharacter.Name
        };

        foreach (var character in adventure.Characters)
        {
            if (character.Importance == CharacterImportance.ArcImportance
                || character.Importance == CharacterImportance.Significant)
            {
                names.Add(character.Name);
            }
        }

        return names;
    }

    /// <summary>
    ///     Filters out Activity lorebooks that were only witnessed by adventure characters.
    /// </summary>
    private static List<LorebookEntry> FilterPrivateActivities(
        List<LorebookEntry> lorebooks,
        HashSet<string> adventureCharacterNames)
    {
        var result = new List<LorebookEntry>();

        foreach (var lorebook in lorebooks)
        {
            if (lorebook.Category != nameof(LorebookCategory.Activity))
            {
                result.Add(lorebook);
                continue;
            }

            try
            {
                var activity = JsonSerializer.Deserialize<ActivityExtraction>(lorebook.Content);
                if (activity is null)
                {
                    result.Add(lorebook);
                    continue;
                }

                var witnesses = activity.Witnesses ?? [];
                var hasExternalWitness = witnesses.Any(
                    witness => !adventureCharacterNames.Contains(witness));

                if (hasExternalWitness || witnesses.Length == 0)
                {
                    result.Add(lorebook);
                }
                // else: private activity, skip it
            }
            catch
            {
                result.Add(lorebook);
            }
        }

        return result;
    }

    private class ChunkComparer : IEqualityComparer<Chunk>
    {
        public bool Equals(Chunk? x, Chunk? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null)
            {
                return false;
            }

            if (y is null)
            {
                return false;
            }

            if (x.GetType() != y.GetType())
            {
                return false;
            }

            return Nullable.Equals(x.AdventureId, y.AdventureId)
                   && x.Name == y.Name
                   && x.ContentHash == y.ContentHash
                   && x.DatasetName == y.DatasetName
                   && x.KnowledgeGraphNodeId == y.KnowledgeGraphNodeId;
        }

        public int GetHashCode(Chunk obj)
        {
            return HashCode.Combine(obj.AdventureId, obj.Name, obj.ContentHash, obj.DatasetName, obj.KnowledgeGraphNodeId);
        }
    }
}