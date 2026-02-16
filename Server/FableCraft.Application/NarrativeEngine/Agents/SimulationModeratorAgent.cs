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

namespace FableCraft.Application.NarrativeEngine.Agents;

/// <summary>
///     Orchestrates cohort simulation by querying characters via tools.
///     Manages time, facilitates interactions, then directly queries each character for their reflection.
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
    ///     Run cohort simulation and return results.
    ///     Idempotent: if called again after a reflection failure, resumes from saved state.
    /// </summary>
    public async Task<CohortSimulationResult> Invoke(
        GenerationContext context,
        CohortSimulationInput input,
        CancellationToken cancellationToken)
    {
        var kernelBuilder = await GetKernelBuilder(context);

        if (context.CohortSimulationState != null)
        {
            logger.Information(
                "Resuming cohort simulation from saved state. {Collected} reflections collected, {Pending} pending.",
                context.CohortSimulationState.CollectedReflections.Count,
                context.CohortSimulationState.PendingCharacters.Count);

            return await CollectReflectionsAsync(context, kernelBuilder, cancellationToken);
        }

        var systemPrompt = await GetPromptAsync(context);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);
        chatHistory.AddUserMessage(BuildContextMessage(input));
        chatHistory.AddUserMessage(BuildRequestMessage(input));

        var kernel = kernelBuilder.Create();
        var callerContext = new CallerContext(GetType(), context.AdventureId, context.NewSceneId);

        var queryPlugin = new QueryCharacterPlugin(characterAgent, logger);
        await queryPlugin.SetupAsync(context, callerContext, input);
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(queryPlugin));

        var builtKernel = kernel.Build();
        var promptExecutionSettings = kernelBuilder.GetDefaultFunctionPromptExecutionSettings();

        logger.Information(
            "Starting cohort simulation for characters: {Characters}",
            string.Join(", ", input.CohortMembers.Select(m => m.Name)));

        var output = ResponseParser.CreateJsonParser<CohortSimulationOutput>(
            "simulation",
            true);
        var response = await agentKernel.SendRequestAsync(
            chatHistory,
            output,
            promptExecutionSettings,
            nameof(SimulationModeratorAgent),
            builtKernel,
            cancellationToken);

        context.CohortSimulationState = new CohortSimulationState
        {
            ModeratorResult = response,
            Context = context,
            Sessions = queryPlugin.GetAllSessions(),
            PendingCharacters = input.CohortMembers.ToList()
        };

        return await CollectReflectionsAsync(context, kernelBuilder, cancellationToken);
    }

    /// <summary>
    ///     Collect reflections from all pending characters in parallel.
    ///     Saves successful reflections to state before throwing on failure.
    /// </summary>
    private async Task<CohortSimulationResult> CollectReflectionsAsync(
        GenerationContext context,
        Infrastructure.Llm.IKernelBuilder kernelBuilder,
        CancellationToken cancellationToken)
    {
        var state = context.CohortSimulationState!;
        if (state.PendingCharacters.Count == 0)
        {
            logger.Information("All reflections already collected from previous run.");
            context.CohortSimulationState = null;
            return new CohortSimulationResult
            {
                CharacterReflections = new Dictionary<string, StandaloneSimulationOutput>(
                    state.CollectedReflections,
                    StringComparer.OrdinalIgnoreCase),
                Result = state.ModeratorResult
            };
        }

        logger.Information(
            "Collecting reflections from {Count} characters in parallel.",
            state.PendingCharacters.Count);

        var reflectionTasks = state.PendingCharacters
            .Select(async member =>
            {
                if (!state.Sessions.TryGetValue(member.Name, out var session))
                {
                    throw new InvalidOperationException(
                        $"No session found for character '{member.Name}' - character was not queried during simulation");
                }

                var result = await QueryCharacterReflectionAsync(member, session, kernelBuilder, cancellationToken);
                state.CollectedReflections[member.Name] = result;
                logger.Information(
                    "Character {Name} reflection collected: {SceneCount} scenes",
                    member.Name,
                    result.Scenes.Count);
                state.PendingCharacters.Remove(member);
            })
            .ToList();

        await Task.WhenAll(reflectionTasks);

        var reflections = new Dictionary<string, StandaloneSimulationOutput>(
            state.CollectedReflections,
            StringComparer.OrdinalIgnoreCase);

        return new CohortSimulationResult
        {
            CharacterReflections = reflections,
            Result = state.ModeratorResult
        };
    }

    /// <summary>
    ///     Query a character directly for their reflection using their accumulated ChatHistory.
    /// </summary>
    private async Task<StandaloneSimulationOutput> QueryCharacterReflectionAsync(
        CharacterContext character,
        CharacterSimulationSession session,
        Infrastructure.Llm.IKernelBuilder kernelBuilder,
        CancellationToken cancellationToken)
    {
        var chatHistory = session.ChatHistory;

        if (!session.ReflectionPromptAdded)
        {
            chatHistory.AddUserMessage("""
                                       The simulation period has concluded.

                                       Process your reflection following the Output Format in your instructions.
                                       Provide your complete simulation output as JSON.
                                       """);
            session.ReflectionPromptAdded = true;
        }

        var kernel = kernelBuilder.Create().Build();
        var settings = kernelBuilder.GetDefaultFunctionPromptExecutionSettings();

        var parser = ResponseParser.CreateJsonParser<StandaloneSimulationOutput>("reflection", true);

        var reflection = await agentKernel.SendRequestAsync(
            chatHistory,
            parser,
            settings,
            $"{nameof(CharacterSimulationAgent)}_Reflection_{character.Name}",
            kernel,
            cancellationToken);

        return reflection;
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
            sb.AppendLine($"- Description: {member.Description}");

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
        sb.AppendLine($"Simulate to: {input.SimulationPeriod}");
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
}