using FableCraft.Application.NarrativeEngine.Plugins;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

using Serilog;

#pragma warning disable SKEXP0110 // Semantic Kernel Agents are experimental

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class NarrativeAgent : AgentBase
{
    private readonly ILogger _logger;

    public NarrativeAgent(ILogger logger)
    {
        _logger = logger;
    }

    protected override string Name { get; } = "Story Weaver";

    protected override string Description { get; } = "Narrative Director + Writer";

    protected override string BuildInstruction(NarrativeContext context)
    {
        return "# Role: Story Weaver (Narrative Director + Writer)";
    }

    public override ChatCompletionAgent BuildAgent(Kernel kernel, NarrativeContext context)
    {
        var narrativeKernel = kernel.Clone();
        var loreCrafterPlugin = new LoreCrafterPlugin(context, kernel.Clone(), _logger);
        var characterCrafterPlugin = new CharacterCrafterPlugin(context, kernel.Clone(), _logger);
        narrativeKernel.Plugins.Add(KernelPluginFactory.CreateFromObject(loreCrafterPlugin));
        narrativeKernel.Plugins.Add(KernelPluginFactory.CreateFromObject(characterCrafterPlugin));

        return base.BuildAgent(narrativeKernel, context);
    }
}