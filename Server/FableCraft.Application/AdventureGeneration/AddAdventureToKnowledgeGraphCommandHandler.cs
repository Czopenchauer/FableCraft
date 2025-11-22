using FableCraft.Application.NarrativeEngine.WelcomeScene;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Queue;
using FableCraft.Infrastructure.Rag;

using Microsoft.EntityFrameworkCore;

namespace FableCraft.Application.AdventureGeneration;

public class AddAdventureToKnowledgeGraphCommand : IMessage
{
    public Guid AdventureId { get; init; }
}

internal class AddAdventureToKnowledgeGraphCommandHandler(
    IMessageDispatcher messageDispatcher,
    ApplicationDbContext dbContext,
    IRagProcessor ragProcessor)
    : IMessageHandler<AddAdventureToKnowledgeGraphCommand>
{
    public async Task HandleAsync(AddAdventureToKnowledgeGraphCommand message, CancellationToken cancellationToken)
    {
        Adventure adventure = await dbContext.Adventures
            .Include(x => x.MainCharacter)
            .Include(x => x.Lorebook)
            .Include(x => x.Scenes).ThenInclude(scene => scene.CharacterActions)
            .SingleAsync(x => x.Id == message.AdventureId, cancellationToken);

        await ragProcessor.Add(adventure.Id, adventure.Lorebook.OrderBy(x => x.Priority).ToArray(), cancellationToken);
        await ragProcessor.Add(adventure.Id, [adventure.MainCharacter], cancellationToken);
        await ragProcessor.Add(adventure.Id, adventure.Scenes.OrderBy(x => x.SequenceNumber).ToArray(), cancellationToken);

        if (adventure.Scenes.Count == 0)
        {
            await messageDispatcher.PublishAsync(new AdventureCreatedEvent
                {
                    AdventureId = adventure.Id
                },
                cancellationToken);
        }
        else
        {
            await dbContext.Adventures.Where(x => x.Id == adventure.Id)
                .ExecuteUpdateAsync(x => x.SetProperty(y => y.ProcessingStatus, y => ProcessingStatus.Completed), cancellationToken);
        }
    }
}