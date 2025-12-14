using System.IO.Hashing;
using System.Net;
using System.Text;
using System.Threading.RateLimiting;

using FableCraft.Application.NarrativeEngine;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;
using FableCraft.Infrastructure.Queue;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using Polly;
using Polly.Retry;

using Adventure = FableCraft.Infrastructure.Persistence.Entities.Adventure.Adventure;

namespace FableCraft.Application.AdventureGeneration;

public class AddAdventureToKnowledgeGraphCommand : IMessage
{
    public required Guid AdventureId { get; set; }
}

internal class AddAdventureToKnowledgeGraphCommandHandler(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IRagBuilder ragProcessor,
    SceneGenerationOrchestrator sceneGenerationOrchestrator)
    : IMessageHandler<AddAdventureToKnowledgeGraphCommand>
{
    private readonly ResiliencePipeline _ioResiliencePipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(500),
            BackoffType = DelayBackoffType.Exponential,
            ShouldHandle = new PredicateBuilder()
                .Handle<IOException>()
        })
        .Build();

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
                ex.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.TooManyRequests)
        })
        .Build();

    public async Task HandleAsync(AddAdventureToKnowledgeGraphCommand message, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(Path.Combine(StartupExtensions.DataDirectory, message.AdventureId.ToString())))
        {
            Directory.CreateDirectory(StartupExtensions.DataDirectory);
        }
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Adventure adventure = await dbContext.Adventures
            .Include(x => x.MainCharacter)
            .Include(x => x.Lorebook)
            .Include(x => x.Scenes)
            .ThenInclude(scene => scene.CharacterActions)
            .SingleAsync(x => x.Id == message.AdventureId, cancellationToken);
        if (adventure.RagProcessingStatus is not (ProcessingStatus.Pending or ProcessingStatus.Failed)
            && adventure.SceneGenerationStatus is not (ProcessingStatus.Pending or ProcessingStatus.Failed))
        {
            return;
        }

        var filesToCommit = new List<(Chunk Chunk, string Content)>();

        var existingLorebookChunks = await dbContext.Chunks
            .AsNoTracking()
            .Where(x => adventure.Lorebook.Select(y => y.Id).Contains(x.EntityId))
            .ToListAsync(cancellationToken);

        foreach (LorebookEntry lorebookEntry in adventure.Lorebook.OrderBy(x => x.Priority))
        {
            Chunk? existingChunk = existingLorebookChunks.SingleOrDefault(y => y.EntityId == lorebookEntry.Id);
            if (existingChunk is null)
            {
                var lorebookBytes = Encoding.UTF8.GetBytes(lorebookEntry.Content + adventure.Id);
                var lorebookHash = XxHash64.HashToUInt64(lorebookBytes);
                var lorebookName = $"{lorebookHash:x16}";
                var lorebookPath = @$"{StartupExtensions.DataDirectory}\{adventure.Id}\{lorebookName}.{lorebookEntry.ContentType.ToString()}";

                var newLorebookChunk = new Chunk
                {
                    EntityId = lorebookEntry.Id,
                    Name = lorebookName,
                    Path = lorebookPath,
                    ContentType = lorebookEntry.ContentType.ToString(),
                    KnowledgeGraphNodeId = null,
                    ContentHash = lorebookHash,
                    AdventureId = message.AdventureId
                };
                filesToCommit.Add((newLorebookChunk, lorebookEntry.Content));
            }
        }

        var existingCharacterChunks = await dbContext.Chunks
            .AsNoTracking()
            .Where(x => x.EntityId == adventure.MainCharacter.Id)
            .ToListAsync(cancellationToken);

        var characterContent = $"""
                                Name: {adventure.MainCharacter.Name}

                                {adventure.MainCharacter.Description}
                                """;

        var characterBytes = Encoding.UTF8.GetBytes(characterContent);
        var characterHash = XxHash64.HashToUInt64(characterBytes);
        Chunk? existingCharacterChunk = existingCharacterChunks.FirstOrDefault(x => x.ContentHash == characterHash);

        if (existingCharacterChunk is null)
        {
            var characterName = $"{characterHash:x16}";
            var characterPath = @$"{StartupExtensions.DataDirectory}\{adventure.Id}\{characterName}.{nameof(ContentType.txt)}";

            var newCharacterChunk = new Chunk
            {
                EntityId = adventure.MainCharacter.Id,
                Name = characterName,
                Path = characterPath,
                ContentType = nameof(ContentType.txt),
                KnowledgeGraphNodeId = null,
                ContentHash = characterHash,
                AdventureId = message.AdventureId
            };
            filesToCommit.Add((newCharacterChunk, characterContent));
        }

        if (filesToCommit.Count > 0 && adventure.RagProcessingStatus is not ProcessingStatus.Completed)
        {
            await dbContext.Adventures.Where(x => x.Id == adventure.Id)
                .ExecuteUpdateAsync(x => x.SetProperty(a => a.RagProcessingStatus, ProcessingStatus.InProgress),
                    cancellationToken);
            IExecutionStrategy strategy = dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    await _ioResiliencePipeline.ExecuteAsync(async ct =>
                        {
                            Directory.CreateDirectory(@$"{StartupExtensions.DataDirectory}\{adventure.Id}");

                            await Task.WhenAll(filesToCommit.Select(x =>
                                File.WriteAllTextAsync(x.Chunk.Path, x.Content, ct)));
                        },
                        cancellationToken);

                    await Task.WhenAll(filesToCommit.Select(x =>
                        File.WriteAllTextAsync(x.Chunk.Path, x.Content, cancellationToken)));

                    var addResult = await _resiliencePipeline.ExecuteAsync(async ct =>
                            await ragProcessor.AddDataAsync(
                                filesToCommit.Select(x => x.Chunk.Path).ToList(),
                                message.AdventureId.ToString(),
                                ct),
                        cancellationToken);

                    foreach ((Chunk chunk, _) in filesToCommit)
                    {
                        chunk.KnowledgeGraphNodeId = addResult[chunk.Name];
                        dbContext.Chunks.Add(chunk);
                    }

                    await _resiliencePipeline.ExecuteAsync(async ct =>
                            await ragProcessor.CognifyAsync(message.AdventureId.ToString(), ct),
                        cancellationToken);

                    // await _resiliencePipeline.ExecuteAsync(async ct =>
                    //         await ragProcessor.MemifyAsync(message.AdventureId.ToString(), ct),
                    //     cancellationToken);

                    adventure.RagProcessingStatus = ProcessingStatus.Completed;
                    await dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    adventure.RagProcessingStatus = ProcessingStatus.Failed;
                    dbContext.Adventures.Update(adventure);
                    await dbContext.SaveChangesAsync(cancellationToken);
                    throw;
                }
            });
        }

        await dbContext.Adventures.Where(x => x.Id == adventure.Id)
            .ExecuteUpdateAsync(x => x.SetProperty(a => a.SceneGenerationStatus, ProcessingStatus.InProgress),
                cancellationToken);
        IExecutionStrategy executionStrategy = dbContext.Database.CreateExecutionStrategy();
        await executionStrategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await sceneGenerationOrchestrator.GenerateSceneAsync(message.AdventureId, string.Empty, cancellationToken);
                adventure.SceneGenerationStatus = ProcessingStatus.Completed;
                dbContext.Adventures.Update(adventure);
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                adventure.SceneGenerationStatus = ProcessingStatus.Failed;
                dbContext.Adventures.Update(adventure);
                await dbContext.SaveChangesAsync(cancellationToken);
                throw;
            }
        });
    }
}