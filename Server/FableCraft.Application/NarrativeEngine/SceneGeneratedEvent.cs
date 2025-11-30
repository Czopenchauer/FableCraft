using System.IO.Hashing;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;

using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;
using FableCraft.Infrastructure.Queue;

using Microsoft.EntityFrameworkCore;

using Polly;
using Polly.Retry;

using FileToWrite = (FableCraft.Infrastructure.Persistence.Entities.Chunk Chunk, string Content);

namespace FableCraft.Application.NarrativeEngine;

internal sealed class SceneGeneratedEvent : IMessage
{
    public required Guid SceneId { get; init; }

    public required Guid AdventureId { get; init; }
}

/// <summary>
/// Commit generated scenes and related data to RAG system. Works on the assumption that "stale" date in KG is fine.
/// </summary>
internal sealed class SceneGeneratedEventHandler : IMessageHandler<SceneGeneratedEvent>
{
    private readonly ApplicationDbContext _dbContext;
    private const int MinScenesToCommit = 2;
    private readonly IRagBuilder _ragBuilder;
    private readonly ResiliencePipeline _resiliencePipeline;

    public SceneGeneratedEventHandler(ApplicationDbContext dbContext, IRagBuilder ragBuilder)
    {
        _dbContext = dbContext;
        _ragBuilder = ragBuilder;
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
                    ex.StatusCode is System.Net.HttpStatusCode.InternalServerError or System.Net.HttpStatusCode.TooManyRequests)
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
            .SingleAsync(x => x.Id == message.SceneId, cancellationToken: cancellationToken);

        // ReSharper disable once EntityFramework.NPlusOne.IncompleteDataQuery
        var scenesToCommit = await _dbContext.Scenes
            .AsNoTracking()
            .Include(x => x.Lorebooks)
            .Include(x => x.CharacterActions)
            .Where(x => x.Id == message.SceneId && x.SequenceNumber < currentScene.SequenceNumber && x.CommitStatus == CommitStatus.Uncommited)
            .ToListAsync(cancellationToken: cancellationToken);

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
            foreach (var scene in scenesToCommit)
            {
                var existingLorebookChunks = await _dbContext.Chunks
                    .AsNoTracking()
                    .Where(x => scene.Lorebooks.Select(y => y.Id).Contains(x.EntityId))
                    .ToListAsync(cancellationToken: cancellationToken);

                // Only add new lorebooks
                foreach (LorebookEntry sceneLorebook in scene.Lorebooks)
                {
                    var lorebookChunk = existingLorebookChunks.SingleOrDefault(y => y.Id == sceneLorebook.Id);
                    if (lorebookChunk is null)
                    {
                        var lorebookBytes = Encoding.UTF8.GetBytes(sceneLorebook.Content);
                        var lorebookHash = XxHash64.HashToUInt64(lorebookBytes);
                        var lorebookName = $"{lorebookHash:x16}";
                        var lorebookPath = @$"{StartupExtensions.DataDirectory}\{scene.AdventureId}\{lorebookName}.{sceneLorebook.ContentType.ToString()}";
                        var newLorebookChunk = new Chunk
                        {
                            EntityId = sceneLorebook.Id,
                            Name = lorebookName,
                            Path = lorebookPath,
                            ContentType = sceneLorebook.ContentType.ToString(),
                            KnowledgeGraphNodeId = null,
                            ContentHash = lorebookHash
                        };
                        fileToCommit.Add((newLorebookChunk, sceneLorebook.Content));
                    }
                }

                var sceneContent = $"""
                                    Time: {scene.Metadata.Tracker.Story.Time}
                                    Location: {scene.Metadata.Tracker.Story.Location}
                                    Weather: {scene.Metadata.Tracker.Story.Weather}
                                    Characters Present: {string.Join(", ", scene.Metadata.Tracker.CharactersPresent)}

                                    {scene.GetSceneWithSelectedAction()}
                                    """;
                var bytes = Encoding.UTF8.GetBytes(sceneContent);
                var hash = XxHash64.HashToUInt64(bytes);
                var name = $"{hash:x16}";
                var path = @$"{StartupExtensions.DataDirectory}\{scene.AdventureId}\{name}.{nameof(ContentType.txt)}";
                var chunk = new Chunk
                {
                    EntityId = scene.Id,
                    Name = name,
                    Path = path,
                    ContentType = sceneContent,
                    KnowledgeGraphNodeId = null,
                    ContentHash = hash
                };
                fileToCommit.Add((chunk, sceneContent));
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
                var charStats = Process(message.AdventureId, existingCharacterChunks, states, x => JsonSerializer.Serialize(x.CharacterStats), ContentType.json);
                fileToCommit.Add(charStats);
                var tracker = Process(message.AdventureId, existingCharacterChunks, states, x => JsonSerializer.Serialize(x.Tracker), ContentType.json);
                fileToCommit.Add(tracker);
            }

            var strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                foreach (var (chunk, _) in fileToCommit)
                {
                    if (!string.IsNullOrEmpty(chunk.KnowledgeGraphNodeId))
                    {
                        _dbContext.Chunks.Update(chunk);
                    }
                    else
                    {
                        _dbContext.Chunks.Add(chunk);
                    }
                }

                await _dbContext.SaveChangesAsync(cancellationToken);

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

                await _resiliencePipeline.ExecuteAsync(async ct =>
                        await _ragBuilder.AddDataAsync(fileToCommit.Select(x => x.Chunk.Path).ToList(), message.AdventureId.ToString(), ct),
                    cancellationToken);

                await _resiliencePipeline.ExecuteAsync(async ct =>
                        await _ragBuilder.CognifyAsync(message.AdventureId.ToString(), ct),
                    cancellationToken);

                await _resiliencePipeline.ExecuteAsync(async ct =>
                        await _ragBuilder.MemifyAsync(message.AdventureId.ToString(), ct),
                    cancellationToken);
                foreach (Scene scene in scenesToCommit)
                {
                    scene.CommitStatus = CommitStatus.Commited;
                }

                _dbContext.Scenes.UpdateRange(scenesToCommit);
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(CancellationToken.None);
            });
        }
        finally
        {
            // Assume commit failed
            await _dbContext.Scenes
                .Where(x => scenesToCommit.Select(y => y.Id).Contains(x.Id))
                .ExecuteUpdateAsync(x => x.SetProperty(s => s.CommitStatus, CommitStatus.Uncommited), cancellationToken);
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
            var bytes = Encoding.UTF8.GetBytes(fieldSelector(x));
            var hash = XxHash64.HashToUInt64(bytes);
            return hash;
        });

        var latestState = states.MaxBy(x => x.SequenceNumber)!;
        var bytes = Encoding.UTF8.GetBytes(fieldSelector(latestState));
        var hash = XxHash64.HashToUInt64(bytes);
        var name = $"{hash:x16}";
        var path = @$"{StartupExtensions.DataDirectory}\{adventureId}\{name}.{contentType.ToString()}";
        var existingChunk = existingChunks.FirstOrDefault(x => hashes.Contains(x.ContentHash));
        var chunk = new Chunk
        {
            EntityId = latestState.CharacterId,
            Name = name,
            Path = path,
            ContentType = contentType.ToString(),
            KnowledgeGraphNodeId = existingChunk?.KnowledgeGraphNodeId,
            ContentHash = hash
        };

        return (chunk, fieldSelector(latestState));
    }
}