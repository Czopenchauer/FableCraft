using FableCraft.Infrastructure.Queue;

using Serilog;

namespace FableCraft.Application.NarrativeEngine.WelcomeScene;

internal class AdventureCreatedEvent : IMessage
{
    public Guid AdventureId { get; init; }
}

internal class AdventureCreatedEventHandler(
    IGameService gameService,
    ILogger logger) : IMessageHandler<AdventureCreatedEvent>
{
    public async Task HandleAsync(AdventureCreatedEvent message, CancellationToken cancellationToken = default)
    {
        try
        {
            await gameService.GenerateFirstSceneAsync(message.AdventureId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to generate first scene for adventure {AdventureId}", message.AdventureId);
            throw;
        }
    }
}