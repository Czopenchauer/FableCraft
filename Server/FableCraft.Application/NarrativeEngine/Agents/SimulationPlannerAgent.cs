using System.Text;

using FableCraft.Application.NarrativeEngine.Agents.Builders;
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
///     The SimulationPlanner determines which characters need off-screen simulation after a scene ends,
///     grouping them into cohorts or standalone simulations, and identifying significant characters
///     that need OffscreenInference.
/// </summary>
internal sealed class SimulationPlannerAgent(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    KernelBuilderFactory kernelBuilderFactory,
    IPluginFactory pluginFactory,
    ILogger logger) : BaseAgent(dbContextFactory, kernelBuilderFactory)
{
    protected override AgentName GetAgentName() => AgentName.SimulationPlannerAgent;

    public async Task<SimulationPlannerOutput> Invoke(
        GenerationContext context,
        SceneTracker sceneTracker,
        CancellationToken cancellationToken)
    {
        if (context.SimulationPlan != null)
        {
            return context.SimulationPlan;
        }

        var kernelBuilder = await GetKernelBuilder(context);

        var systemPrompt = await GetPromptAsync(context);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        var input = BuildInput(context, sceneTracker);
        var contextPrompt = BuildContextPrompt(input);
        chatHistory.AddUserMessage(contextPrompt);

        var kernelBuilderSk = kernelBuilder.Create();
        var callerContext = new CallerContext(GetType(), context.AdventureId, context.NewSceneId);
        await pluginFactory.AddPluginAsync<IntentCheckPlugin>(kernelBuilderSk, context, callerContext);
        var kernel = kernelBuilderSk.Build();

        var outputParser = ResponseParser.CreateJsonParser<SimulationPlannerOutput>("simulation_plan", true);
        var promptExecutionSettings = kernelBuilder.GetDefaultFunctionPromptExecutionSettings();

        var plan = await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            promptExecutionSettings,
            nameof(SimulationPlannerAgent),
            kernel,
            cancellationToken);

        var validationError = ValidateCohortIndependence(plan);
        if (validationError != null)
        {
            logger.Warning("Simulation plan validation failed: {ValidationError}", validationError);
            chatHistory.AddUserMessage(validationError);
            plan = await agentKernel.SendRequestAsync(
                chatHistory,
                outputParser,
                promptExecutionSettings,
                nameof(SimulationPlannerAgent),
                kernel,
                cancellationToken);
            validationError = ValidateCohortIndependence(plan);
            if (validationError != null)
            {
                logger.Error("Simulation plan validation failed again: {ValidationError}", validationError);
                throw new InvalidOperationException("Simulation plan validation failed after retry: " + validationError);
            }
        }

        return plan;
    }

    private SimulationPlannerInput BuildInput(GenerationContext context, SceneTracker sceneTracker)
    {
        var roster = BuildCharacterRoster(context);
        var previousState = context.SceneContext?
            .OrderByDescending(x => x.SequenceNumber)
            .FirstOrDefault()?.Metadata.ChroniclerState;
        return new SimulationPlannerInput
        {
            SceneTracker = sceneTracker,
            CharacterRoster = roster,
            WorldEvents = previousState?.WorldMomentum,
            NarrativeDirection = context.WriterGuidance
        };
    }

    private List<CharacterRosterEntry> BuildCharacterRoster(GenerationContext context)
    {
        return context.Characters
            .Where(c => c.Importance == CharacterImportance.ArcImportance || c.Importance == CharacterImportance.Significant)
            .Where(c => !context.NewTracker!.Scene!.CharactersPresent.Contains(c.Name))
            .Select(c => new CharacterRosterEntry
            {
                Name = c.Name,
                Importance = c.Importance.Value,
                Location = c.CharacterTracker?.Location ?? "unknown",
                LastSimulated = c.SceneRewrites.MaxBy(z => z.SequenceNumber)!.SceneTracker!.Time,
                GoalsSummary = c.CharacterState.Motivations.ToJsonString(),
                KeyRelationships = c.Relationships?.Select(r => r.TargetCharacterName).ToArray(),
                RelationshipNotes = BuildRelationshipNotes(c),
                RoutineSummary = c.CharacterState.Routine.ToJsonString()
            })
            .ToList();
    }

    private static string? BuildRelationshipNotes(CharacterContext c)
    {
        if (c.Relationships.Count == 0)
        {
            return null;
        }

        var notes = c.Relationships
            .Select(r =>
                $"""
                 Target name: {r.TargetCharacterName}
                 {r.Dynamic}
                 """);

        return string.Join(";", notes);
    }

    private string BuildContextPrompt(SimulationPlannerInput input)
    {
        var sections = new List<string>
        {
            $"""
             ### Story Tracker
             <story_tracker>
             Time: {input.SceneTracker.Time}
             Location: {input.SceneTracker.Location}
             Weather: {input.SceneTracker.Weather}
             Characters Present: {string.Join(", ", input.SceneTracker.CharactersPresent ?? [])}
             </story_tracker>
             """,
            $"""
             ### Character Roster
             <character_roster>
             {FormatCharacterRoster(input.CharacterRoster)}
             </character_roster>
             """
        };

        if (input.WorldEvents is not null)
        {
            sections.Add($"""
                          ### World Events
                          <world_events>
                          {FormatWorldEvents(input.WorldEvents)}
                          </world_events>
                          """);
        }

        if (input.NarrativeDirection != null)
        {
            sections.Add($"""
                          ### Narrative Direction
                          <narrative_direction>
                          {input.NarrativeDirection.ToJsonString(PromptSections.GetJsonOptions(true))}
                          </narrative_direction>
                          """);
        }

        return string.Join("\n\n", sections);
    }

    private static string FormatCharacterRoster(List<CharacterRosterEntry> roster)
    {
        return string.Join("\n\n",
            roster.Select(c =>
            {
                var lines = new List<string>
                {
                    $"**{c.Name}** ({c.Importance})",
                    $"Location: {c.Location}"
                };

                if (!string.IsNullOrEmpty(c.LastSimulated))
                    lines.Add($"Last simulated: {c.LastSimulated}");

                if (!string.IsNullOrEmpty(c.GoalsSummary))
                    lines.Add($"Goals: {c.GoalsSummary}");

                if (!string.IsNullOrEmpty(c.RoutineSummary))
                    lines.Add($"Routine: {c.RoutineSummary}");

                if (c.KeyRelationships is { Length: > 0 })
                    lines.Add($"Key relationships: {string.Join(", ", c.KeyRelationships)}");

                if (!string.IsNullOrEmpty(c.RelationshipNotes))
                    lines.Add($"Relationship notes: {c.RelationshipNotes}");

                return string.Join("\n", lines);
            }));
    }

    private static string FormatWorldEvents(object events) => string.Join("\n", events.ToJsonString());

    /// <summary>
    ///     Validates that cohorts are independent groups with no character overlaps.
    ///     Characters should only appear in one cohort - if they overlap, parallel execution
    ///     could cause race conditions or inconsistent state.
    /// </summary>
    private string? ValidateCohortIndependence(SimulationPlannerOutput plan)
    {
        if (plan.Cohorts is not { Count: > 1 })
        {
            return null;
        }

        var characterCohortMap = new Dictionary<string, List<int>>();

        for (var i = 0; i < plan.Cohorts.Count; i++)
        {
            var cohort = plan.Cohorts[i];
            foreach (var character in cohort.Characters)
            {
                if (!characterCohortMap.TryGetValue(character, out var cohortIndices))
                {
                    cohortIndices = [];
                    characterCohortMap[character] = cohortIndices;
                }

                cohortIndices.Add(i);
            }
        }

        var overlappingCharacters = characterCohortMap
            .Where(kvp => kvp.Value.Count > 1)
            .ToList();

        if (overlappingCharacters.Count > 0)
        {
            var error = new StringBuilder();
            foreach (var (character, cohortIndices) in overlappingCharacters)
            {
                var cohortDescriptions = cohortIndices
                    .Select(i => $"Cohort {i}: [{string.Join(", ", plan.Cohorts[i].Characters)}]");

                var res = $"""
                           Character {character} appears in multiple cohorts!
                           """;
                error.AppendJoin(res, cohortDescriptions);
            }

            return error.ToString();
        }

        return null;
    }
}