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

    public SceneGeneratedEventHandler(
        ApplicationDbContext dbContext,
        IRagChunkService ragChunkService,
        ILogger logger)
    {
        _dbContext = dbContext;
        _ragChunkService = ragChunkService;
        _logger = logger;
    }

    public async Task HandleAsync(SceneGeneratedEvent message, CancellationToken cancellationToken)
    {
        _ragChunkService.EnsureDirectoryExists(message.AdventureId);

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
        var fileToCommit = new List<FileToWrite>();
        try
        {

            var existingSceneChunks = await _dbContext.Chunks
                .AsNoTracking()
                .Where(x => scenesToCommit.Select(y => y.Id).Contains(x.EntityId))
                .ToListAsync(cancellationToken);

            var ids = scenesToCommit.SelectMany(y => y.CharacterSceneRewrites.Select(z => z.Id)).ToList();
            var existingRewriteChunks = await _dbContext.Chunks
                .AsNoTracking()
                .Where(x => ids.Contains(x.EntityId))
                .ToListAsync(cancellationToken);
            var characters = await _dbContext.Characters
                .Where(x => x.AdventureId == message.AdventureId)
                .ToListAsync(cancellationToken);
            foreach (var scene in scenesToCommit.OrderBy(x => x.SequenceNumber))
            {
                var existingLorebookChunks = await _dbContext.Chunks
                    .AsNoTracking()
                    .Where(x => scene.Lorebooks.Select(y => y.Id).Contains(x.EntityId))
                    .ToListAsync(cancellationToken);

                foreach (var sceneLorebook in scene.Lorebooks)
                {
                    var lorebookChunk = existingLorebookChunks.SingleOrDefault(y => y.EntityId == sceneLorebook.Id);
                    if (lorebookChunk is null)
                    {
                        var contentType = Enum.Parse<ContentType>(sceneLorebook.ContentType.ToString());
                        var newLorebookChunk = _ragChunkService.CreateChunk(
                            sceneLorebook.Id,
                            message.AdventureId,
                            sceneLorebook.Content,
                            contentType);

                        fileToCommit.Add(new FileToWrite(
                            newLorebookChunk,
                            sceneLorebook.Content,
                            [RagClientExtensions.GetWorldDatasetName(message.AdventureId)]));
                    }
                }

                var sceneContent = $"""
                                    Character is: {scene.Metadata.Tracker!.MainCharacter!.MainCharacter!.Name}
                                    Time: {scene.Metadata.Tracker.Scene!.Time}
                                    Location: {scene.Metadata.Tracker.Scene!.Location}
                                    Weather: {scene.Metadata.Tracker.Scene!.Weather}
                                    Characters on scene: {string.Join(", ", scene.Metadata.Tracker.Scene.CharactersPresent)}

                                    {scene.GetSceneWithSelectedAction()}
                                    """;

                var hash = _ragChunkService.ComputeHash(sceneContent);
                var existingChunk = existingSceneChunks.FirstOrDefault(x => x.EntityId == scene.Id && x.ContentHash == hash);

                if (existingChunk == null)
                {
                    var chunk = _ragChunkService.CreateChunk(
                        scene.Id,
                        message.AdventureId,
                        sceneContent,
                        ContentType.txt);

                    fileToCommit.Add(new FileToWrite(
                        chunk,
                        sceneContent,
                        [RagClientExtensions.GetMainCharacterDatasetName(message.AdventureId)]));
                }
                else
                {
                    fileToCommit.Add(new FileToWrite(
                        existingChunk,
                        sceneContent,
                        [RagClientExtensions.GetMainCharacterDatasetName(message.AdventureId)]));
                }

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

                        var contentHash = _ragChunkService.ComputeHash(content);
                        var rewriteChunk = existingRewriteChunks.FirstOrDefault(z => z.EntityId == x.Id && z.ContentHash == contentHash);

                        if (rewriteChunk == null)
                        {
                            var chunk = _ragChunkService.CreateChunk(
                                x.Id,
                                message.AdventureId,
                                content,
                                ContentType.txt);

                            fileToCommit.Add(new FileToWrite(
                                chunk,
                                content,
                                [RagClientExtensions.GetCharacterDatasetName(message.AdventureId, x.CharacterId)]));
                        }
                        else
                        {
                            fileToCommit.Add(new FileToWrite(
                                rewriteChunk,
                                content,
                                [RagClientExtensions.GetCharacterDatasetName(message.AdventureId, x.CharacterId)]));
                        }
                    });
            }

            var characterStatesOnScenes = scenesToCommit
                .SelectMany(x => x.CharacterStates)
                .GroupBy(x => x.CharacterId);
            var existingCharacterChunks = await _dbContext.Chunks
                .Where(x => characterStatesOnScenes.Select(y => y.Key).Contains(x.EntityId))
                .ToListAsync(cancellationToken);

            foreach (var states in characterStatesOnScenes)
            {
                var newestState = states.OrderByDescending(y => y.SequenceNumber).First();
                var description = ProcessCharacterState(
                    message.AdventureId,
                    newestState.CharacterId,
                    existingCharacterChunks.Where(x => x.EntityId == newestState.CharacterId).ToList(),
                    states,
                    x => x.Description,
                    ContentType.txt);
                fileToCommit.Add(description);
            }

            var mainCharacter = await _dbContext.MainCharacters
                .Select(x => new { x.Id, x.Name, x.AdventureId })
                .SingleAsync(x => x.AdventureId == message.AdventureId, cancellationToken);

            var lastScene = scenesToCommit.OrderByDescending(x => x.SequenceNumber).First();
            var existingMainCharacterChunk = await _dbContext.Chunks
                .Where(x => x.EntityId == mainCharacter.Id)
                .SingleAsync(cancellationToken);

            var characterContent = $"""
                                    Name: {mainCharacter.Name}

                                    {lastScene.Metadata.Tracker!.MainCharacter!.MainCharacterDescription}
                                    """;

            var mainCharacterHash = _ragChunkService.ComputeHash(characterContent);
            var mainCharacterName = $"{mainCharacterHash:x16}";
            var mainCharacterPath = _ragChunkService.GetChunkPath(message.AdventureId, mainCharacterHash, ContentType.txt);

            existingMainCharacterChunk.Name = mainCharacterName;
            existingMainCharacterChunk.Path = mainCharacterPath;
            existingMainCharacterChunk.ContentHash = mainCharacterHash;

            var mainMcDescroptionDatasets = characters.Select(x => RagClientExtensions.GetCharacterDatasetName(message.AdventureId, x.Id)).ToList();
            mainMcDescroptionDatasets.Add(RagClientExtensions.GetMainCharacterDatasetName(message.AdventureId));
            fileToCommit.Add(new FileToWrite(
                existingMainCharacterChunk,
                characterContent,
                mainMcDescroptionDatasets.ToArray()));

            var strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                await _ragChunkService.WriteFilesAsync(fileToCommit, cancellationToken);
                await _ragChunkService.UpdateExistingChunksAsync(fileToCommit, cancellationToken);
                await _ragChunkService.CommitChunksToRagAsync(fileToCommit, cancellationToken);
                var datasetsToCognify = new HashSet<string>
                {
                    RagClientExtensions.GetWorldDatasetName(message.AdventureId),
                    RagClientExtensions.GetMainCharacterDatasetName(message.AdventureId)
                };
                foreach (var character in characters)
                {
                    datasetsToCognify.Add(RagClientExtensions.GetCharacterDatasetName(message.AdventureId, character.Id));
                }
                await _ragChunkService.CognifyDatasetsAsync(datasetsToCognify, cancellationToken: cancellationToken);

                foreach (var scene in scenesToCommit)
                {
                    scene.CommitStatus = CommitStatus.Commited;
                }

                foreach (var file in fileToCommit)
                {
                    if (file.Chunk.Id != Guid.Empty)
                    {
                        _dbContext.Chunks.Update(file.Chunk);
                    }
                    else
                    {
                        _dbContext.Chunks.Add(file.Chunk);
                    }
                }

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

    private FileToWrite ProcessCharacterState(
        Guid adventureId,
        Guid characterId,
        List<Chunk> existingChunks,
        IEnumerable<CharacterState> states,
        Func<CharacterState, string> fieldSelector,
        ContentType contentType)
    {
        var statesList = states.ToList();
        var hashes = statesList.Select(x => _ragChunkService.ComputeHash(fieldSelector(x))).ToList();

        var latestState = statesList.MaxBy(x => x.SequenceNumber)!;
        var latestContent = fieldSelector(latestState);
        var hash = _ragChunkService.ComputeHash(latestContent);

        var existingChunk = existingChunks.FirstOrDefault(x => hashes.Contains(x.ContentHash));
        string[] datasets =
        [
            RagClientExtensions.GetCharacterDatasetName(adventureId, characterId),
            RagClientExtensions.GetMainCharacterDatasetName(adventureId),
            RagClientExtensions.GetWorldDatasetName(adventureId)
        ];

        if (existingChunk == null)
        {
            var name = $"{hash:x16}";
            var path = _ragChunkService.GetChunkPath(adventureId, hash, contentType);

            var chunk = new Chunk
            {
                EntityId = characterId,
                Name = name,
                Path = path,
                ContentType = contentType.ToString(),
                ContentHash = hash,
                AdventureId = adventureId,
                ChunkLocation = null
            };
            return new FileToWrite(chunk, latestContent, datasets);
        }

        var updatedName = $"{hash:x16}";
        existingChunk.Name = updatedName;
        existingChunk.Path = _ragChunkService.GetChunkPath(adventureId, hash, contentType);
        existingChunk.ContentType = contentType.ToString();
        existingChunk.ContentHash = hash;

        return new FileToWrite(existingChunk, latestContent, datasets);
    }
}