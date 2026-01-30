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
            var creationRequests = new List<ChunkCreationRequest>();
            foreach (var scene in scenesToCommit)
            {
                var lorebookRequests = scene.Lorebooks.Select(x =>
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