using System.Diagnostics.CodeAnalysis;

using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Plugins;
using FableCraft.Infrastructure.Clients;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration.Sequential;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.NarrativeEngine.Orchestration;

internal sealed class SceneGenerationOrchestrator
{
    private readonly IKernelBuilder _kernelBuilder;
    private readonly IRagSearch _ragSearch;
    private readonly IEnumerable<IAgent> _agents;

    public SceneGenerationOrchestrator(IKernelBuilder kernelBuilder, IRagSearch ragSearch, IEnumerable<IAgent> agents)
    {
        _kernelBuilder = kernelBuilder;
        _ragSearch = ragSearch;
        _agents = agents;
    }

    [Experimental("SKEXP0110")]
    public async Task GenerateSceneAsync(string adventureId, CancellationToken cancellationToken)
    {
        var narrativeContext = new NarrativeContext();
        var kernel = _kernelBuilder.WithBase();
        var kgPlugin = new KnowledgeGraphPlugin(_ragSearch, adventureId);
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(kgPlugin));
        var kernelWithKg = kernel.Build();

        var agents = _agents.Select(x => x.BuildAgent(kernelWithKg, narrativeContext)).ToArray<Agent>();
        var orchestrator = new SequentialOrchestration(agents);

        InProcessRuntime runtime = new InProcessRuntime();
        await runtime.StartAsync(cancellationToken);
        var result = await orchestrator.InvokeAsync("", runtime, cancellationToken);
    }
}