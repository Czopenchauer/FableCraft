using FableCraft.Application.NarrativeEngine.Models;

namespace FableCraft.Application.NarrativeEngine.Workflow;

internal interface IProcessor
{
    Task Invoke(GenerationContext context, CancellationToken cancellationToken);
}