using System.IO.Hashing;
using System.Text;
using System.Threading.RateLimiting;

using FableCraft.Application.NarrativeEngine;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Queue;

using Microsoft.EntityFrameworkCore;

using Polly;
using Polly.Retry;

namespace FableCraft.Application.AdventureGeneration;

public class AddAdventureToKnowledgeGraphCommand : IMessage
{
    public Guid AdventureId { get; init; }
}

internal class AddAdventureToKnowledgeGraphCommandHandler(
    ApplicationDbContext dbContext,
    IRagBuilder ragProcessor,
    SceneGenerationOrchestrator sceneGenerationOrchestrator)
    : IMessageHandler<AddAdventureToKnowledgeGraphCommand>
{
    private const string DataDirectory = @"C:\Disc\Dev\_projects\FableCraft\data";

    private readonly ResiliencePipeline _resiliencePipeline = new ResiliencePipelineBuilder()
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

    public async Task HandleAsync(AddAdventureToKnowledgeGraphCommand message, CancellationToken cancellationToken)
    {
        Adventure adventure = await dbContext.Adventures
            .Include(x => x.MainCharacter)
            .Include(x => x.Lorebook)
            .Include(x => x.Scenes)
            .ThenInclude(scene => scene.CharacterActions)
            .SingleAsync(x => x.Id == message.AdventureId, cancellationToken);

        var filesToCommit = new List<(Chunk Chunk, string Content)>();

        var existingLorebookChunks = await dbContext.Chunks
            .AsNoTracking()
            .Where(x => adventure.Lorebook.Select(y => y.Id).Contains(x.EntityId))
            .ToListAsync(cancellationToken: cancellationToken);

        foreach (LorebookEntry lorebookEntry in adventure.Lorebook.OrderBy(x => x.Priority))
        {
            var existingChunk = existingLorebookChunks.SingleOrDefault(y => y.EntityId == lorebookEntry.Id);
            if (existingChunk is null)
            {
                var lorebookBytes = Encoding.UTF8.GetBytes(lorebookEntry.Content);
                var lorebookHash = XxHash64.HashToUInt64(lorebookBytes);
                var lorebookName = $"{lorebookHash:x16}";
                var lorebookPath = @$"{DataDirectory}\{adventure.Id}\{lorebookName}.{lorebookEntry.ContentType.ToString()}";

                var newLorebookChunk = new Chunk
                {
                    EntityId = lorebookEntry.Id,
                    Name = lorebookName,
                    Path = lorebookPath,
                    ContentType = lorebookEntry.ContentType.ToString(),
                    KnowledgeGraphNodeId = null,
                    ContentHash = lorebookHash
                };
                filesToCommit.Add((newLorebookChunk, lorebookEntry.Content));
            }
        }

        var existingCharacterChunks = await dbContext.Chunks
            .AsNoTracking()
            .Where(x => x.EntityId == adventure.MainCharacter.Id)
            .ToListAsync(cancellationToken: cancellationToken);

        var characterContent = $"""
                                Name: {adventure.MainCharacter.Name}

                                {adventure.MainCharacter.Description}
                                """;

        var characterBytes = Encoding.UTF8.GetBytes(characterContent);
        var characterHash = XxHash64.HashToUInt64(characterBytes);
        var existingCharacterChunk = existingCharacterChunks.FirstOrDefault(x => x.ContentHash == characterHash);

        if (existingCharacterChunk is null)
        {
            var characterName = $"{characterHash:x16}";
            var characterPath = @$"{DataDirectory}\{adventure.Id}\{characterName}.{nameof(ContentType.txt)}";

            var newCharacterChunk = new Chunk
            {
                EntityId = adventure.MainCharacter.Id,
                Name = characterName,
                Path = characterPath,
                ContentType = nameof(ContentType.txt),
                KnowledgeGraphNodeId = null,
                ContentHash = characterHash
            };
            filesToCommit.Add((newCharacterChunk, characterContent));
        }

        if (filesToCommit.Count > 0)
        {
            await dbContext.Adventures.Where(x => x.Id == adventure.Id)
                .ExecuteUpdateAsync(x => x.SetProperty(a => a.ProcessingStatus, ProcessingStatus.InProgress),
                    cancellationToken);
            var strategy = dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
                Directory.CreateDirectory(@$"{DataDirectory}\{adventure.Id}");

                await Task.WhenAll(filesToCommit.Select(x =>
                    File.WriteAllTextAsync(x.Chunk.Path, x.Content, cancellationToken)));

                var addResult = await _resiliencePipeline.ExecuteAsync(async ct =>
                        await ragProcessor.AddDataAsync(
                            filesToCommit.Select(x => x.Chunk.Path).ToList(),
                            message.AdventureId.ToString(),
                            ct),
                    cancellationToken);

                foreach (var (chunk, _) in filesToCommit)
                {
                    chunk.KnowledgeGraphNodeId = addResult[chunk.Name];
                    dbContext.Chunks.Add(chunk);
                }

                await _resiliencePipeline.ExecuteAsync(async ct =>
                        await ragProcessor.CognifyAsync(message.AdventureId.ToString(), ct),
                    cancellationToken);

                await _resiliencePipeline.ExecuteAsync(async ct =>
                        await ragProcessor.MemifyAsync(message.AdventureId.ToString(), ct),
                    cancellationToken);

                await sceneGenerationOrchestrator.GenerateInitialSceneAsync(message.AdventureId, cancellationToken);
                adventure.ProcessingStatus = ProcessingStatus.Completed;
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            });
        }
    }
}