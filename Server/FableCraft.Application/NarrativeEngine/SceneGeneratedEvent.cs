using FableCraft.Infrastructure;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;
using FableCraft.Infrastructure.Queue;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

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

        var scenesToCommit = await _dbContext.Scenes
            .Include(x => x.Lorebooks)
            .Include(x => x.CharacterActions)
            .Include(x => x.CharacterStates)
            .Include(x => x.CharacterSceneRewrites)
            .Where(x => x.AdventureId == message.AdventureId && x.SequenceNumber < currentScene.SequenceNumber && x.CommitStatus == CommitStatus.Uncommited)
            .ToListAsync(cancellationToken);

        if (scenesToCommit.Count < MinScenesToCommit)
        {
            _logger.Information("Not enough scenes to commit for adventure {AdventureId}. Current committed scenes count: {ScenesCount}",
                message.AdventureId,
                scenesToCommit.Count);
            return;
        }

        _logger.Information("Committing {ScenesCount} scenes for adventure {AdventureId}",
            scenesToCommit.Count,
            message.AdventureId);
        var fileToCommit = new List<FileToWrite>();
        try
        {
            await _dbContext.Scenes
                .Where(x => scenesToCommit.Select(y => y.Id).Contains(x.Id))
                .ExecuteUpdateAsync(x => x.SetProperty(s => s.CommitStatus, CommitStatus.Lock), cancellationToken);

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
            foreach (Scene scene in scenesToCommit.OrderBy(x => x.SequenceNumber))
            {
                var existingLorebookChunks = await _dbContext.Chunks
                    .AsNoTracking()
                    .Where(x => scene.Lorebooks.Select(y => y.Id).Contains(x.EntityId))
                    .ToListAsync(cancellationToken);

                foreach (LorebookEntry sceneLorebook in scene.Lorebooks)
                {
                    Chunk? lorebookChunk = existingLorebookChunks.SingleOrDefault(y => y.EntityId == sceneLorebook.Id);
                    if (lorebookChunk is null)
                    {
                        var contentType = Enum.Parse<ContentType>(sceneLorebook.ContentType.ToString());
                        Chunk newLorebookChunk = _ragChunkService.CreateChunk(
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
                                    Main character is: {scene.Metadata.Tracker!.MainCharacter!.MainCharacter!.Name}
                                    Time: {scene.Metadata.Tracker.Story!.Time}
                                    Location: {scene.Metadata.Tracker.Story!.Location}
                                    Weather: {scene.Metadata.Tracker.Story!.Weather}
                                    Characters on scene: {string.Join(", ", scene.Metadata.Tracker.Story.CharactersPresent)}

                                    {scene.GetSceneWithSelectedAction()}
                                    """;

                var hash = _ragChunkService.ComputeHash(sceneContent);
                Chunk? existingChunk = existingSceneChunks.FirstOrDefault(x => x.EntityId == scene.Id && x.ContentHash == hash);

                if (existingChunk == null)
                {
                    Chunk chunk = _ragChunkService.CreateChunk(
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
                                       Time: {x.StoryTracker.Time}
                                       Location: {x.StoryTracker.Location}
                                       Weather: {x.StoryTracker.Weather}
                                       Characters on scene: {string.Join(", ", x.StoryTracker.CharactersPresent)}

                                       {x.Content}
                                       """;

                        var contentHash = _ragChunkService.ComputeHash(content);
                        Chunk? rewriteChunk = existingRewriteChunks.FirstOrDefault(z => z.EntityId == x.Id && z.ContentHash == contentHash);

                        if (rewriteChunk == null)
                        {
                            Chunk chunk = _ragChunkService.CreateChunk(
                                scene.Id,
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
                FileToWrite description = ProcessCharacterState(
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

            Scene lastScene = scenesToCommit.OrderByDescending(x => x.SequenceNumber).First();
            Chunk existingMainCharacterChunk = await _dbContext.Chunks
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

            IExecutionStrategy strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                await _ragChunkService.WriteFilesAsync(fileToCommit, cancellationToken);
                await _ragChunkService.UpdateExistingChunksAsync(fileToCommit, cancellationToken);
                await _ragChunkService.CommitChunksToRagAsync(fileToCommit, cancellationToken);
                await _ragChunkService.CognifyDatasetsAsync(
                    [RagClientExtensions.GetWorldDatasetName(message.AdventureId)],
                    cancellationToken: cancellationToken);
                await _ragChunkService.CognifyDatasetsAsync(
                    [RagClientExtensions.GetMainCharacterDatasetName(message.AdventureId)],
                    cancellationToken: cancellationToken);
                
                foreach (Character character in characters)
                {
                    await _ragChunkService.CognifyDatasetsAsync(
                        [RagClientExtensions.GetCharacterDatasetName(message.AdventureId, character.Id)],
                        cancellationToken: cancellationToken);
                }

                foreach (Scene scene in scenesToCommit)
                {
                    scene.CommitStatus = CommitStatus.Commited;
                }

                foreach (FileToWrite file in fileToCommit)
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

        Chunk? existingChunk = existingChunks.FirstOrDefault(x => hashes.Contains(x.ContentHash));
        string[] datasets =
        [
            RagClientExtensions.GetCharacterDatasetName(adventureId, characterId),
            RagClientExtensions.GetMainCharacterDatasetName(adventureId)
        ];

        if (existingChunk == null)
        {
            var name = $"{hash:x16}";
            var path = _ragChunkService.GetChunkPath(adventureId, hash, contentType);

            var chunk = new Chunk
            {
                EntityId = latestState.Id,
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