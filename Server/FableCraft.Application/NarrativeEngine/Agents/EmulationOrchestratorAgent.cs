using System.Text;

using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Application.NarrativeEngine.Plugins;
using FableCraft.Application.NarrativeEngine.Plugins.Impl;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using Serilog;

namespace FableCraft.Application.NarrativeEngine.Agents;

/// <summary>
///     Agent that orchestrates character emulations within a single beat.
///     Uses CharacterEmulationPlugin as a tool to call characters.
/// </summary>
internal sealed class EmulationOrchestratorAgent : BaseAgent
{
    private readonly IAgentKernel _agentKernel;
    private readonly ILogger _logger;
    private readonly IPluginFactory _pluginFactory;

    public EmulationOrchestratorAgent(
        IAgentKernel agentKernel,
        ILogger logger,
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        KernelBuilderFactory kernelBuilderFactory,
        IPluginFactory pluginFactory) : base(dbContextFactory, kernelBuilderFactory)
    {
        _agentKernel = agentKernel;
        _logger = logger;
        _pluginFactory = pluginFactory;
    }

    public async Task<string> OrchestrateBeatAsync(
        GenerationContext context,
        string beatInput,
        CancellationToken cancellationToken)
    {
        _logger.Information("Starting beat orchestration: {beat}", beatInput);

        var previousCounts = SnapshotEmulationCounts(context);

        var kernelBuilder = await GetKernelBuilder(context);
        var skKernelBuilder = kernelBuilder.Create();

        var callerContext = new CallerContext(
            nameof(EmulationOrchestratorAgent),
            context.AdventureId,
            context.NewSceneId);

        await _pluginFactory.AddPluginAsync<CharacterEmulationPlugin>(skKernelBuilder, context, callerContext);
        await _pluginFactory.AddPluginAsync<CharacterStatePlugin>(skKernelBuilder, context, callerContext);
        await _pluginFactory.AddPluginAsync<WorldKnowledgePlugin>(skKernelBuilder, context, callerContext);

        var kernel = skKernelBuilder.Build();

        var systemPrompt = await GetPromptAsync(context);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);
        chatHistory.AddUserMessage(beatInput);

        await _agentKernel.SendRequestAsync(
            chatHistory,
            response => response,
            kernelBuilder.GetDefaultFunctionPromptExecutionSettings(),
            nameof(EmulationOrchestratorAgent),
            kernel,
            cancellationToken);

        return BuildBeatResponse(context, previousCounts);
    }

    private static Dictionary<string, int> SnapshotEmulationCounts(GenerationContext context)
    {
        lock (context)
        {
            return context.CharacterEmulationOutputs
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count);
        }
    }

    private static string BuildBeatResponse(GenerationContext context, Dictionary<string, int> previousCounts)
    {
        var newOutputs = new List<CharacterEmulationOutput>();

        lock (context)
        {
            foreach (var (characterName, outputs) in context.CharacterEmulationOutputs)
            {
                var previousCount = previousCounts.GetValueOrDefault(characterName, 0);
                var newOnes = outputs.Skip(previousCount);
                newOutputs.AddRange(newOnes);
            }
        }

        newOutputs = newOutputs.OrderBy(o => o.SequenceNumber).ToList();

        if (newOutputs.Count == 0)
        {
            return "No character emulations were executed.";
        }

        var sb = new StringBuilder();
        var executionOrder = string.Join(", ", newOutputs.Select(o => o.CharacterName));
        sb.AppendLine($"Execution order: {executionOrder}");

        foreach (var output in newOutputs)
        {
            sb.AppendLine();
            sb.AppendLine($"[{output.CharacterName}]");
            sb.AppendLine($"Situation given: {output.Stimulus}");
            sb.AppendLine($"Query given: {output.Query}");
            sb.AppendLine();
            sb.AppendLine(output.Response);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    protected override AgentName GetAgentName() => AgentName.EmulationOrchestratorAgent;
}
