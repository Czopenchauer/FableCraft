using System.Diagnostics;

using FableCraft.Application.KnowledgeGraph;
using FableCraft.Application.NarrativeEngine.WelcomeScene;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Queue;

using Microsoft.EntityFrameworkCore;

namespace FableCraft.Application.AdventureGeneration;

public class AddAdventureToKnowledgeGraphCommand : IMessage
{
    public Guid AdventureId { get; init; }
}

internal class AddAdventureToKnowledgeGraphCommandHandler(
    IMessageDispatcher messageDispatcher,
    ApplicationDbContext dbContext,
    KnowledgeGraphService knowledgeGraphService)
    : IMessageHandler<AddAdventureToKnowledgeGraphCommand>
{
    private const int MaxChunkSize = 128;

    public async Task HandleAsync(AddAdventureToKnowledgeGraphCommand message, CancellationToken cancellationToken)
    {
        var adventure = await dbContext.Adventures
            .Include(x => x.Character)
            .Include(x => x.Lorebook)
            .Include(x => x.Scenes)
            .SingleAsync(x => x.Id == message.AdventureId, cancellationToken: cancellationToken);

        var existingCharacterChunks = await dbContext.Chunks
            .Where(x => x.EntityId == adventure.CharacterId)
            .ToListAsync(cancellationToken: cancellationToken);

        var existingLorebooksChunks = await dbContext.Chunks
            .Where(x => adventure.Lorebook.Select(y => y.Id).Contains(x.EntityId))
            .ToListAsync(cancellationToken: cancellationToken);

        var existingSceneChunks = await dbContext.Chunks
            .Where(x => adventure.Scenes.Select(y => y.Id).Contains(x.EntityId))
            .ToListAsync(cancellationToken: cancellationToken);

        await knowledgeGraphService.ResetFailedChunksAsync(existingCharacterChunks, cancellationToken);
        await knowledgeGraphService.ResetFailedChunksAsync(existingLorebooksChunks, cancellationToken);
        await knowledgeGraphService.ResetFailedChunksAsync(existingSceneChunks, cancellationToken);

        var lorebookToProcess = adventure.Lorebook
            .Where(x => existingLorebooksChunks.All(y => y.EntityId != x.Id))
            .ToList();

        foreach (var lorebook in lorebookToProcess)
        {
            var chunkedText = await knowledgeGraphService.ChunkTextAsync(
                lorebook.Content,
                MaxChunkSize,
                cancellationToken);

            var lorebookChunks = chunkedText.Select((text, idx) => new Chunk
            {
                RawChunk = text,
                EntityId = lorebook.Id,
                ProcessingStatus = ProcessingStatus.Pending,
                Name = lorebook.Category,
                Description = lorebook.Description,
                Order = idx,
            }).ToList();

            await dbContext.Chunks.AddRangeAsync(lorebookChunks, cancellationToken);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            existingLorebooksChunks.AddRange(lorebookChunks);
        }

        Debug.Assert(existingLorebooksChunks.All(x => x.Id != Guid.Empty));
        var lorebookChunksGrouped = existingLorebooksChunks
            .Join(adventure.Lorebook,
                chunk => chunk.EntityId,
                lorebook => lorebook.Id,
                (chunk, lorebook) => new
                {
                    LorebookChunk = lorebook,
                    Chunk = chunk,
                });

        foreach (var chunk in lorebookChunksGrouped.Where(x => string.IsNullOrEmpty(x.Chunk.ContextualizedChunk)))
        {
            var contextualizedChunk = await knowledgeGraphService.ContextualizeChunkAsync(
                chunk.Chunk.RawChunk,
                chunk.LorebookChunk.Content,
                cancellationToken);

            chunk.Chunk.ContextualizedChunk = contextualizedChunk;
            await dbContext.Chunks
                .Where(c => c.Id == chunk.Chunk.Id)
                .ExecuteUpdateAsync(
                    x => x.SetProperty(c => c.ContextualizedChunk, contextualizedChunk),
                    cancellationToken);
        }

        var entireCharacterText = $"Main Character Description: {adventure.Character.Description}\n"
                                  + (string.IsNullOrEmpty(adventure.Character.Background) ? string.Empty : $"Main Character Background: {adventure.Character.Background}");

        if (!existingCharacterChunks.Any())
        {
            var chunkedText = await knowledgeGraphService.ChunkTextAsync(
                entireCharacterText,
                MaxChunkSize,
                cancellationToken);

            var characterChunks = chunkedText.Select((text, idx) => new Chunk
            {
                RawChunk = text,
                EntityId = adventure.CharacterId,
                ProcessingStatus = ProcessingStatus.Pending,
                Name = $"Main Character, {adventure.Character.Name}",
                Description = $"Main Character, {adventure.Character.Name}, Description",
                Order = idx,
            }).ToList();
            foreach (Chunk characterChunk in characterChunks)
            {
                characterChunk.ContextualizedChunk = await knowledgeGraphService.ContextualizeChunkAsync(
                    characterChunk.RawChunk,
                    entireCharacterText,
                    cancellationToken);
            }

            await dbContext.Chunks.AddRangeAsync(characterChunks, cancellationToken);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            existingCharacterChunks.AddRange(characterChunks);
        }

        if (adventure.Scenes.Any())
        {
            var scenesToProcess = adventure.Scenes
                .Where(x => existingSceneChunks.All(y => y.EntityId != x.Id))
                .ToList();

            var order = 0;
            foreach (var scene in scenesToProcess)
            {
                var chunk = await knowledgeGraphService.ChunkTextAsync(
                    scene.NarrativeText,
                    MaxChunkSize,
                    cancellationToken);

                var sceneChunks = chunk.Select((text, idx) =>
                {
                    order += idx;
                    return new Chunk
                    {
                        RawChunk = text,
                        EntityId = scene.Id,
                        ProcessingStatus = ProcessingStatus.Pending,
                        Name = "Scene number " + scene.SequenceNumber,
                        Description = "Scene number " + scene.SequenceNumber,
                        Order = order,
                    };
                }).ToList();

                await dbContext.Chunks.AddRangeAsync(sceneChunks, cancellationToken);
                await dbContext.SaveChangesAsync(CancellationToken.None);
                existingSceneChunks.AddRange(sceneChunks);
            }

            var sceneChunksGrouped = existingSceneChunks
                .Join(adventure.Lorebook,
                    chunk => chunk.EntityId,
                    lorebook => lorebook.Id,
                    (chunk, lorebook) => new
                    {
                        LorebookChunk = lorebook,
                        Chunk = chunk,
                    });

            foreach (var chunk in sceneChunksGrouped.Where(x => string.IsNullOrEmpty(x.Chunk.ContextualizedChunk)))
            {
                var contextualizedChunk = await knowledgeGraphService.ContextualizeChunkAsync(
                    chunk.Chunk.RawChunk,
                    chunk.LorebookChunk.Content,
                    cancellationToken);

                chunk.Chunk.ContextualizedChunk = contextualizedChunk;
                await dbContext.Chunks
                    .Where(c => c.Id == chunk.Chunk.Id)
                    .ExecuteUpdateAsync(
                        x => x.SetProperty(c => c.ContextualizedChunk, contextualizedChunk),
                        cancellationToken);
            }
        }

        await knowledgeGraphService.ProcessChunksAsync(
            adventure.Id,
            lorebookChunksGrouped.Select(x => x.Chunk).ToList(),
            cancellationToken);

        await knowledgeGraphService.ProcessChunksAsync(
            adventure.Id,
            existingCharacterChunks,
            cancellationToken);

        await knowledgeGraphService.ProcessChunksAsync(
            adventure.Id,
            existingSceneChunks,
            cancellationToken);

        await knowledgeGraphService.BuildCommunitiesAsync(adventure.Id.ToString(), cancellationToken);

        await messageDispatcher.PublishAsync(new AdventureCreatedEvent
            {
                AdventureId = adventure.Id
            },
            cancellationToken);
    }
}