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
            .SingleAsync(x => x.Id == message.AdventureId, cancellationToken: cancellationToken);

        var existingCharacterChunks = await dbContext.Chunks
            .Where(x => x.EntityId == adventure.CharacterId)
            .ToListAsync(cancellationToken: cancellationToken);

        var existingLorebooksChunks = await dbContext.Chunks
            .Where(x => adventure.Lorebook.Select(y => y.Id).Contains(x.EntityId))
            .ToListAsync(cancellationToken: cancellationToken);

        await knowledgeGraphService.ResetFailedChunksAsync(existingCharacterChunks, cancellationToken);
        await knowledgeGraphService.ResetFailedChunksAsync(existingLorebooksChunks, cancellationToken);

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
                Order = idx
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

        var entireCharacterText = $"""
                                   Main Character Description: {adventure.Character.Description}

                                   Main Character Background: {adventure.Character.Background}
                                   """;

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

            await dbContext.Chunks.AddRangeAsync(characterChunks, cancellationToken);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            existingCharacterChunks.AddRange(characterChunks);
        }

        Debug.Assert(existingCharacterChunks.All(x => x.Id != Guid.Empty));
        foreach (var characterChunk in existingCharacterChunks.Where(x => string.IsNullOrEmpty(x.ContextualizedChunk)))
        {
            var contextualizedChunk = await knowledgeGraphService.ContextualizeChunkAsync(
                characterChunk.RawChunk,
                entireCharacterText,
                cancellationToken);

            characterChunk.ContextualizedChunk = contextualizedChunk;
            await dbContext.Chunks
                .Where(c => c.Id == characterChunk.Id)
                .ExecuteUpdateAsync(
                    x => x.SetProperty(c => c.ContextualizedChunk, contextualizedChunk),
                    cancellationToken);
        }

        await knowledgeGraphService.ProcessChunksAsync(
            adventure.Id,
            lorebookChunksGrouped.Select(x => x.Chunk).ToList(),
            cancellationToken);

        await knowledgeGraphService.ProcessChunksAsync(
            adventure.Id,
            existingCharacterChunks,
            cancellationToken);

        await knowledgeGraphService.BuildCommunitiesAsync(adventure.Id.ToString(), cancellationToken);

        await messageDispatcher.PublishAsync(new AdventureCreatedEvent
            {
                AdventureId = adventure.Id
            },
            cancellationToken);
    }
}