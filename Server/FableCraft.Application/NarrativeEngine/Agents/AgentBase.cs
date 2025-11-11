using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

using Serilog;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal interface IAgent
{
    ChatCompletionAgent BuildAgent(Kernel kernel, NarrativeContext context);
}

internal abstract class AgentBase : IAgent
{
    public abstract string Name { get; }

    public abstract string Description { get; }

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