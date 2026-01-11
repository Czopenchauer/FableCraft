using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

namespace FableCraft.Application.NarrativeEngine.Workflow;

/// <summary>
///     Processor responsible for updating the scene tracker (time, location, weather, characters present).
///     This must run before other processors that depend on context.NewTracker.Scene.
/// </summary>
internal sealed class SceneTrackerProcessor(SceneTrackerAgent sceneTracker) : IProcessor
{
    public async Task Invoke(GenerationContext context, CancellationToken cancellationToken)
    {
        var result = context.NewTracker?.Scene ?? await sceneTracker.Invoke(context, cancellationToken);
        context.NewTracker ??= new Tracker();
        context.NewTracker.Scene = result;
    }
}