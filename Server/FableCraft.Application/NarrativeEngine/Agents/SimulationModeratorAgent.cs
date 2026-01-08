using System.Text;
using System.Text.Json;

using FableCraft.Application.NarrativeEngine.Agents.Builders;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Application.NarrativeEngine.Plugins.Impl;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using Serilog;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.NarrativeEngine.Agents;

/// <summary>
/// Orchestrates cohort simulation by querying characters via tools.
/// Manages time, facilitates interactions, and ensures all characters produce reflection output.
/// </summary>
internal sealed class SimulationModeratorAgent(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    KernelBuilderFactory kernelBuilderFactory,
    CharacterSimulationAgent characterAgent,
    ILogger logger) : BaseAgent(dbContextFactory, kernelBuilderFactory)
{
    protected override AgentName GetAgentName() => AgentName.SimulationModeratorAgent;

    /// <summary>
    /// Run cohort simulation and return results.
    /// </summary>
    public async Task<CohortSimulationResult> Invoke(
        GenerationContext context,
        CohortSimulationInput input,
        CancellationToken cancellationToken)
    {
        IKernelBuilder kernelBuilder = await GetKernelBuilder(context);

        var systemPrompt = await GetPromptAsync(context);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);
        chatHistory.AddUserMessage(BuildContextMessage(input));
        chatHistory.AddUserMessage(BuildRequestMessage(input));

        Microsoft.SemanticKernel.IKernelBuilder kernel = kernelBuilder.Create();
        var callerContext = new CallerContext(GetType(), context.AdventureId);

        var queryPlugin = new QueryCharacterPlugin(characterAgent, logger);
        await queryPlugin.SetupAsync(context, callerContext, input);
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(queryPlugin));

        Kernel builtKernel = kernel.Build();
        PromptExecutionSettings promptExecutionSettings = kernelBuilder.GetDefaultFunctionPromptExecutionSettings();

        logger.Information(
            "Starting cohort simulation for characters: {Characters}",
            string.Join(", ", input.CohortMembers.Select(m => m.Name)));

        var output = ResponseParser.CreateJsonParser<CohortSimulationOutput>(
            "simulation",
            ignoreNull: true);
        var response = await agentKernel.SendRequestAsync(
            chatHistory,
            output,
            promptExecutionSettings,
            nameof(SimulationModeratorAgent),
            builtKernel,
            cancellationToken);

        var reflections = queryPlugin.GetCompletedReflections();

        var pendingCharacters = queryPlugin.GetPendingReflectionCharacters();
        if (pendingCharacters.Any())
        {
            logger.Warning(
                "Characters did not submit reflections: {Characters}",
                string.Join(", ", pendingCharacters));

            chatHistory.AddUserMessage($"The following characters did not submit reflections: {string.Join(", ", pendingCharacters)}");
            response = await agentKernel.SendRequestAsync(
                chatHistory,
                output,
                promptExecutionSettings,
                nameof(SimulationModeratorAgent),
                builtKernel,
                cancellationToken);

            if (queryPlugin.GetPendingReflectionCharacters().Any())
            {
                throw new InvalidOperationException(
                    $"Characters still missing reflections after retry: {string.Join(", ", queryPlugin.GetPendingReflectionCharacters())}");
            }
        }

        return new CohortSimulationResult
        {
            CharacterReflections = reflections,
            Result = response
        };
    }

    private static string BuildContextMessage(CohortSimulationInput input)
    {
        var sb = new StringBuilder();
        var cohortNames = input.CohortMembers.Select(m => m.Name).ToHashSet();

        sb.AppendLine("## Cohort");
        sb.AppendLine();
        foreach (var member in input.CohortMembers)
        {
            sb.AppendLine($"### {member.Name}");
            sb.AppendLine($"- Location: {member.CharacterTracker?.Location ?? "Unknown"}");
            sb.AppendLine($"- Primary Goal: {ExtractPrimaryGoal(member)}");

            // Extract relationships within cohort
            var cohortRelationships = member.Relationships
                .Where(r => cohortNames.Contains(r.TargetCharacterName))
                .ToList();
            if (cohortRelationships.Count > 0)
            {
                sb.AppendLine("- Relationships within cohort:");
                foreach (var rel in cohortRelationships)
                {
                    sb.AppendLine($"  - {rel.TargetCharacterName}: {rel.Dynamic}");
                }
            }

            sb.AppendLine();
        }

        // Time period
        sb.AppendLine("## Time Period");
        sb.AppendLine($"Simulate to: {input.SimulationPeriod.To}");
        sb.AppendLine();

        // Known interactions
        if (input.KnownInteractions?.Count > 0)
        {
            sb.AppendLine("## Known Interactions");
            sb.AppendLine("These interactions are already confirmed from IntentCheck. Orchestrate them appropriately.");
            sb.AppendLine();
            sb.AppendLine(JsonSerializer.Serialize(input.KnownInteractions, new JsonSerializerOptions { WriteIndented = true }));
            sb.AppendLine();
        }

        sb.AppendLine("## World Events");
        if (input.WorldEvents != null)
        {
            sb.AppendLine(input.WorldEvents.ToJsonString());
        }
        else
        {
            sb.AppendLine("No significant world events.");
        }

        sb.AppendLine();

        sb.AppendLine("## Significant Characters (Available for Interaction)");
        if (input.SignificantCharacters?.Length > 0)
        {
            sb.AppendLine(string.Join(", ", input.SignificantCharacters));
        }
        else
        {
            sb.AppendLine("None available.");
        }

        return sb.ToString();
    }

    private static string BuildRequestMessage(CohortSimulationInput input)
    {
        var characterNames = string.Join(", ", input.CohortMembers.Select(m => m.Name));

        return $"""
                Run the simulation for this cohort: {characterNames}

                Begin.
                """;
    }

    private static string ExtractPrimaryGoal(CharacterContext character)
    {
        return character.CharacterState.Goals.ToJsonString();
    }
}