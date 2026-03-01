using System.ComponentModel;

using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Infrastructure;

using Microsoft.SemanticKernel;

using Serilog;

namespace FableCraft.Application.NarrativeEngine.Plugins.Impl;

/// <summary>
///     Plugin that exposes beat orchestration functionality as a Semantic Kernel function.
/// </summary>
internal sealed class OrchestrateEmulationPlugin : PluginBase
{
    private readonly EmulationOrchestratorAgent _orchestrator;
    private readonly ILogger _logger;

    public OrchestrateEmulationPlugin(EmulationOrchestratorAgent orchestrator, ILogger logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    [KernelFunction("orchestrate_beat")]
    [Description(
        "Execute emulations for a single beat. Builds sanitized situations for each character, calls their emulations in the correct order, and forwards observables between them.")]
    public async Task<string> OrchestrateBeatAsync(
        [Description("Beat input describing the scene context, action resolution, and characters to emulate")]
        string input
    )
    {
        ProcessExecutionContext.SceneId.Value = CallerContext!.SceneId;
        ProcessExecutionContext.AdventureId.Value = CallerContext.AdventureId;

        _logger.Information("Orchestrating beat");

        try
        {
            return await _orchestrator.OrchestrateBeatAsync(
                Context!,
                input,
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during beat orchestration");
            throw;
        }
    }
}
