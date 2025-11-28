using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Queue;

using Microsoft.EntityFrameworkCore;
// ReSharper disable EntityFramework.NPlusOne.IncompleteDataUsage

namespace FableCraft.Application.NarrativeEngine;

internal sealed class SceneGeneratedEvent : IMessage
{
    public required Guid SceneId { get; init; }

    public required Guid AdventureId { get; init; }
}

internal sealed class SceneGeneratedEventHandler : IMessageHandler<SceneGeneratedEvent>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IRagBuilder _ragBuilder;

    public SceneGeneratedEventHandler(ApplicationDbContext dbContext, IRagBuilder ragBuilder)
    {
        _dbContext = dbContext;
        _ragBuilder = ragBuilder;
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
            .Include(x => x.Lorebooks)
            .Include(x => x.CharacterStates)
            .Include(x => x.CharacterActions)
            .Where(x => x.Id == message.SceneId && x.SequenceNumber < currentScene.SequenceNumber && !x.Commited)
            .ToListAsync(cancellationToken: cancellationToken);

        if (scenesToCommit.Count < 5)
        {
            return;
        }

        var addDataBatchRequest = new AddDataBatchRequest
        {
            Content = new List<string>(),
            AdventureId = message.AdventureId.ToString()
        };

        var chunks = new List<Chunk>();
        foreach (var scene in scenesToCommit)
        {
            var existingLorebookChunks = await _dbContext.Chunks
                .Where(x => scene.Lorebooks.Select(y => y.Id).Contains(x.EntityId))
                .ToListAsync(cancellationToken: cancellationToken);

            foreach (LorebookEntry sceneLorebook in scene.Lorebooks)
            {
                var chunk = existingLorebookChunks.FirstOrDefault(y => y.Id == sceneLorebook.Id);
                if (chunk is null)
                {
                    var path = @$"C:\Disc\Dev\_projects\FableCraft\data\{sceneLorebook.Content.GetHashCode()}.{sceneLorebook.ContentType}";
                    chunk = new Chunk
                    {
                        Name = path,
                        RawChunk = sceneLorebook.Content,
                        ContentType = sceneLorebook.ContentType.ToString(),
                        ReferenceTime = scene.Metadata.Tracker.Story.Time,
                        EntityId = sceneLorebook.Id
                    };
                }
                chunks.Add(chunk);
            }
        }
    }
}