using System.IO.Hashing;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;

using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;
using FableCraft.Infrastructure.Queue;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using Polly;
using Polly.Retry;

using Serilog;

using FileToWrite = (FableCraft.Infrastructure.Persistence.Entities.Chunk Chunk, string Content);

namespace FableCraft.Application.NarrativeEngine;

public sealed class SceneGeneratedEvent : IMessage
{
    public required Guid SceneId { get; init; }

    public required Guid AdventureId { get; set; }
}

internal sealed class SceneGeneratedEventHandler : IMessageHandler<SceneGeneratedEvent>
{
    private const int MinScenesToCommit = 2;
    private readonly ApplicationDbContext _dbContext;
    private readonly IRagBuilder _ragBuilder;
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly ILogger _logger;

    public SceneGeneratedEventHandler(ApplicationDbContext dbContext, IRagBuilder ragBuilder, ILogger logger)
    {
        _dbContext = dbContext;
        _ragBuilder = ragBuilder;
        _logger = logger;
        _resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRateLimiter(new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 5,
                Window = TimeSpan.FromSeconds(1),
                QueueLimit = 1000
            }))
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>(ex =>
                    ex.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.TooManyRequests)
            })
            .Build();
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

        var scenesToCommit = await _dbContext.Scenes
            .AsNoTracking()
            .Include(x => x.Lorebooks)
            .Include(x => x.CharacterActions)
            .Include(x => x.CharacterStates)
            .Where(x => x.AdventureId == message.AdventureId && x.SequenceNumber < currentScene.SequenceNumber && x.CommitStatus == CommitStatus.Uncommited)
            .ToListAsync(cancellationToken);

        if (scenesToCommit.Count <= MinScenesToCommit)
        {
            return;
        }

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
            foreach (Scene scene in scenesToCommit.OrderBy(x => x.SequenceNumber))
            {
                var existingLorebookChunks = await _dbContext.Chunks
                    .AsNoTracking()
                    .Where(x => scene.Lorebooks.Select(y => y.Id).Contains(x.EntityId))
                    .ToListAsync(cancellationToken);

                foreach (LorebookEntry sceneLorebook in scene.Lorebooks)
                {
                    var lorebookBytes = Encoding.UTF8.GetBytes(sceneLorebook.Content);
                    var lorebookHash = XxHash64.HashToUInt64(lorebookBytes);
                    var lorebookName = $"{lorebookHash:x16}";
                    var lorebookPath = @$"{StartupExtensions.DataDirectory}\{scene.AdventureId}\{lorebookName}.{sceneLorebook.ContentType.ToString()}";
                    Chunk? lorebookChunk = existingLorebookChunks.SingleOrDefault(y => y.EntityId == sceneLorebook.Id);
                    if (lorebookChunk is null)
                    {
                        var newLorebookChunk = new Chunk
                        {
                            EntityId = sceneLorebook.Id,
                            Name = lorebookName,
                            Path = lorebookPath,
                            ContentType = sceneLorebook.ContentType.ToString(),
                            KnowledgeGraphNodeId = null,
                            ContentHash = lorebookHash,
                            AdventureId = message.AdventureId
                        };
                        fileToCommit.Add((newLorebookChunk, sceneLorebook.Content));
                    }
                }

                var sceneContent = $"""
                                    Main character is: {scene.Metadata.Tracker!.MainCharacter?.Name}
                                    Time: {scene.Metadata.Tracker.Story.Time}
                                    Location: {scene.Metadata.Tracker.Story.Location}
                                    Weather: {scene.Metadata.Tracker.Story.Weather}
                                    Characters on scene: {string.Join(", ", scene.Metadata.Tracker.CharactersPresent)}

                                    {scene.GetSceneWithSelectedAction()}
                                    """;
                var bytes = Encoding.UTF8.GetBytes(sceneContent);
                var hash = XxHash64.HashToUInt64(bytes);
                var existingChunk = existingSceneChunks.FirstOrDefault(x => x.EntityId == scene.Id && x.ContentHash == hash);
                if (existingChunk == null)
                {
                    var name = $"{hash:x16}";
                    var path = @$"{StartupExtensions.DataDirectory}\{scene.AdventureId}\{name}.{nameof(ContentType.txt)}";
                    var chunk = new Chunk
                    {
                        EntityId = scene.Id,
                        Name = name,
                        Path = path,
                        ContentType = nameof(ContentType.txt),
                        KnowledgeGraphNodeId = null,
                        ContentHash = hash,
                        AdventureId = message.AdventureId
                    };
                    fileToCommit.Add((chunk, sceneContent));
                }
                else
                {
                    fileToCommit.Add((existingChunk, sceneContent));
                }
            }

            var characterStatesOnScenes = scenesToCommit
                .SelectMany(x => x.CharacterStates)
                .GroupBy(x => x.CharacterId);
            var existingCharacterChunks = await _dbContext.Chunks
                .Where(x => characterStatesOnScenes.Select(y => y.Key).Contains(x.EntityId))
                .ToListAsync(cancellationToken);
            foreach (var states in characterStatesOnScenes)
            {
                var description = Process(message.AdventureId, existingCharacterChunks, states, x => x.Description, ContentType.txt);
                fileToCommit.Add(description);
                // var charStats = Process(message.AdventureId, existingCharacterChunks, states, x => JsonSerializer.Serialize(x.CharacterStats), ContentType.json);
                // fileToCommit.Add(charStats);
                // var tracker = Process(message.AdventureId, existingCharacterChunks, states, x => JsonSerializer.Serialize(x.Tracker), ContentType.json);
                // fileToCommit.Add(tracker);
            }

            var mainCharacter = await _dbContext.MainCharacters
                .Select(x => new { x.Id, x.Name, x.AdventureId })
                .SingleAsync(x => x.AdventureId == message.AdventureId, cancellationToken: cancellationToken);
            var lastScene = scenesToCommit.OrderByDescending(x => x.SequenceNumber).First();
            var existingMainCharacterChunk = await _dbContext.Chunks
                .AsNoTracking()
                .Where(x => x.EntityId == mainCharacter.Id)
                .SingleAsync(cancellationToken);
            var characterContent = $"""
                                    Name: {mainCharacter.Name}

                                    {lastScene.Metadata.MainCharacterDescription}
                                    """;

            var mainCharacterBytes = Encoding.UTF8.GetBytes(characterContent);
            var mainCharacterHash = XxHash64.HashToUInt64(mainCharacterBytes);
            var mainCharacterName = $"{mainCharacterHash:x16}";
            var mainCharacterPath = @$"{StartupExtensions.DataDirectory}\{message.AdventureId}\{mainCharacterName}.{nameof(ContentType.txt)}";
            existingMainCharacterChunk.Name = mainCharacterName;
            existingMainCharacterChunk.Path = mainCharacterPath;
            existingMainCharacterChunk.ContentHash = mainCharacterHash;
            fileToCommit.Add((existingMainCharacterChunk, characterContent));

            IExecutionStrategy strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                await Task.WhenAll(fileToCommit.Select(x => File.WriteAllTextAsync(x.Chunk.Path, x.Content, cancellationToken)));
                foreach (var (chunk, _) in fileToCommit)
                {
                    if (!string.IsNullOrEmpty(chunk.KnowledgeGraphNodeId))
                    {
                        await _resiliencePipeline.ExecuteAsync(async ct =>
                                await _ragBuilder.UpdateDataAsync(message.AdventureId.ToString(), chunk.KnowledgeGraphNodeId, chunk.Path, ct),
                            cancellationToken);
                    }
                }

                var addResult = await _resiliencePipeline.ExecuteAsync(async ct =>
                        await _ragBuilder.AddDataAsync(
                            fileToCommit.Select(x => x.Chunk.Path).ToList(),
                            message.AdventureId.ToString(),
                            ct),
                    cancellationToken);

                foreach ((Chunk chunk, _) in fileToCommit)
                {
                    chunk.KnowledgeGraphNodeId = addResult[chunk.Name];
                }

                await _resiliencePipeline.ExecuteAsync(async ct =>
                        await _ragBuilder.CognifyAsync(message.AdventureId.ToString(), ct),
                    cancellationToken);

                // await _resiliencePipeline.ExecuteAsync(async ct =>
                //         await _ragBuilder.MemifyAsync(message.AdventureId.ToString(), ct),
                //     cancellationToken);
                foreach (Scene scene in scenesToCommit)
                {
                    scene.CommitStatus = CommitStatus.Commited;
                }

                foreach ((Chunk chunk, _) in fileToCommit)
                {
                    if (chunk.Id != Guid.Empty)
                    {
                        _dbContext.Chunks.Update(chunk);
                    }
                    else
                    {
                        _dbContext.Chunks.Add(chunk);
                    }
                }

                _dbContext.Scenes.UpdateRange(scenesToCommit);
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

    private FileToWrite Process(
        Guid adventureId,
        List<Chunk> existingChunks,
        IGrouping<Guid, Character> states,
        Func<Character, string> fieldSelector,
        ContentType contentType)
    {
        var hashes = states.Select(x =>
        {
            var bytes = Encoding.UTF8.GetBytes(fieldSelector(x) + adventureId);
            var hash = XxHash64.HashToUInt64(bytes);
            return hash;
        });

        Character latestState = states.MaxBy(x => x.SequenceNumber)!;
        var bytes = Encoding.UTF8.GetBytes(fieldSelector(latestState) + adventureId);
        var hash = XxHash64.HashToUInt64(bytes);
        var name = $"{hash:x16}";
        var path = @$"{StartupExtensions.DataDirectory}\{adventureId}\{name}.{contentType.ToString()}";
        Chunk? existingChunk = existingChunks.FirstOrDefault(x => hashes.Contains(x.ContentHash));
        if (existingChunk == null)
        {
            var chunk = new Chunk
            {
                EntityId = latestState.CharacterId,
                Name = name,
                Path = path,
                ContentType = contentType.ToString(),
                KnowledgeGraphNodeId = null,
                ContentHash = hash,
                AdventureId = adventureId
            };
            return (chunk, fieldSelector(latestState));
        }

        existingChunk.Name = name;
        existingChunk.Path = path;
        existingChunk.ContentType = contentType.ToString();
        existingChunk.ContentHash = hash;
        return (existingChunk, fieldSelector(latestState));
    }
}