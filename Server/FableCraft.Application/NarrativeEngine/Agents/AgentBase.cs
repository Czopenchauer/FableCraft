using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal abstract class AgentBase
{
    protected abstract string Name { get; }

    protected abstract string Description { get; }

    protected abstract string BuildInstruction(NarrativeContext context);

    public virtual ChatCompletionAgent BuildAgent(Kernel kernel, NarrativeContext context)
    {
        return new ChatCompletionAgent
        {
            Name = Name,
            Description = Description,
            Instructions = BuildInstruction(context),
            Kernel = kernel.Clone(),
        };
    }
}