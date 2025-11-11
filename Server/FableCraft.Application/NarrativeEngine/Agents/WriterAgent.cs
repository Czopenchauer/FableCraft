using FableCraft.Application.NarrativeEngine.Plugins;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

using Serilog;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class WriterAgent : AgentBase
{
    private readonly ILogger _logger;

    public WriterAgent(ILogger logger)
    {
        _logger = logger;
    }

    protected override string Name { get; } = nameof(WriterAgent);

    protected override string Description { get; }

    protected override string BuildInstruction(NarrativeContext context)
    {
        throw new NotImplementedException();
    }

    public override ChatCompletionAgent BuildAgent(Kernel kernel, NarrativeContext context)
    {
        var writerKernel = kernel.Clone();
        var criticPlugin = new CriticPlugin(context, writerKernel, _logger);
        var characterPlugin = new CharacterPlugin(context, writerKernel, _logger);
        writerKernel.Plugins.Add(KernelPluginFactory.CreateFromObject(characterPlugin));
        writerKernel.Plugins.Add(KernelPluginFactory.CreateFromObject(criticPlugin));

        return base.BuildAgent(writerKernel, context);
    }
}