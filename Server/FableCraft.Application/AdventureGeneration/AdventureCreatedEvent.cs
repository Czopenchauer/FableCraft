using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Queue;

using Microsoft.EntityFrameworkCore;

using Serilog;

namespace FableCraft.Application.AdventureGeneration;

internal class AdventureCreatedEvent : IMessage
{
    public Guid AdventureId { get; init; }
}

internal class AdventureCreatedEventHandler(
    ApplicationDbContext dbContext,
    ILogger logger,
    IKernelBuilder kernelBuilder) : IMessageHandler<AdventureCreatedEvent>
{
    public async Task HandleAsync(AdventureCreatedEvent message, CancellationToken cancellationToken = default)
    {
        var adventure = await dbContext.Adventures.FirstOrDefaultAsync(x => x.Id == message.AdventureId, cancellationToken: cancellationToken);
        if (adventure is null)
        {
            logger.Debug("Adventure with ID {AdventureId} not found", message.AdventureId);
            return;
        }

        var kernel = kernelBuilder.WithBase();
        
        
    }
}