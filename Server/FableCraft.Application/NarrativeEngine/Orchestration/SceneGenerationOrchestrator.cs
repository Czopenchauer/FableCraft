using System.Diagnostics.CodeAnalysis;

using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Plugins;
using FableCraft.Infrastructure.Clients;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration.Sequential;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;

using Serilog;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.NarrativeEngine.Orchestration;

internal sealed class SceneGenerationOrchestrator
{
    private readonly IKernelBuilder _kernelBuilder;

    private readonly ILogger _logger;
    private readonly IRagSearch _ragSearch;

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
        Microsoft.SemanticKernel.IKernelBuilder kernel = _kernelBuilder.WithBase();
        var kgPlugin = new KnowledgeGraphPlugin(_ragSearch, adventureId.ToString());
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(kgPlugin));
        Kernel kernelWithKg = kernel.Build();

        ChatCompletionAgent trackerAgent = new TrackerAgent().BuildAgent(kernelWithKg, narrativeContext);
        ChatCompletionAgent narrativeAgent = new NarrativeAgent(_logger).BuildAgent(kernelWithKg, narrativeContext);
        ChatCompletionAgent writerAgent = new WriterAgent(_logger).BuildAgent(kernelWithKg, narrativeContext);
        ChatCompletionAgent formatterAgent = new FormatterAgent().BuildAgent(kernelWithKg, narrativeContext);
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