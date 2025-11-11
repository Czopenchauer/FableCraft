using FableCraft.Application.NarrativeEngine.Plugins;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class WriterAgent : AgentBase
{
    public override string Name { get; }

    public override string Description { get; }

    protected override string BuildInstruction(NarrativeContext context)
    {
        throw new NotImplementedException();
    }

    public override ChatCompletionAgent BuildAgent(Kernel kernel, NarrativeContext context)
    {
        var writerKernel = kernel.Clone();
        // var criticPlugin = new CriticPlugin(context, writerKernel);
        // writerKernel.Plugins.Add(KernelPluginFactory.CreateFromObject(criticPlugin));

        return base.BuildAgent(writerKernel, context);
    }
}