using System.Diagnostics.CodeAnalysis;

using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Plugins;
using FableCraft.Infrastructure.Clients;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.Orchestration.Sequential;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;

using Serilog;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.NarrativeEngine.Orchestration;

internal sealed class SceneGenerationOrchestrator
{
    private readonly IKernelBuilder _kernelBuilder;
    private readonly IRagSearch _ragSearch;

    private readonly ILogger _logger;

    public SceneGenerationOrchestrator(IKernelBuilder kernelBuilder, IRagSearch ragSearch, ILogger logger)
    {
        _kernelBuilder = kernelBuilder;
        _ragSearch = ragSearch;
        _logger = logger;
    }

    [Experimental("SKEXP0110")]
    public async Task GenerateSceneAsync(Guid adventureId, CancellationToken cancellationToken)
    {
        var narrativeContext = new NarrativeContext();
        var kernel = _kernelBuilder.WithBase();
        var kgPlugin = new KnowledgeGraphPlugin(_ragSearch, adventureId.ToString());
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(kgPlugin));
        var kernelWithKg = kernel.Build();

        var trackerAgent = new TrackerAgent().BuildAgent(kernelWithKg, narrativeContext);
        var narrativeAgent = new NarrativeAgent(_logger).BuildAgent(kernelWithKg, narrativeContext);
        var writerAgent = new WriterAgent(_logger).BuildAgent(kernelWithKg, narrativeContext);
        var formatterAgent = new FormatterAgent().BuildAgent(kernelWithKg, narrativeContext);
        var orchestrator = new SequentialOrchestration(trackerAgent, narrativeAgent, writerAgent, formatterAgent);

        var runtime = new InProcessRuntime();
        try
        {
            await runtime.StartAsync(cancellationToken);
            var result = await orchestrator.InvokeAsync("", runtime, cancellationToken);
        }
        finally
        {
            await runtime.StopAsync(cancellationToken);
        }
    }
}