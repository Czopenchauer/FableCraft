using FableCraft.Application.NarrativeEngine.Agents.Builders;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.NarrativeEngine.Agents;

/// <summary>
/// The SimulationPlanner determines which characters need off-screen simulation after a scene ends,
/// grouping them into cohorts or standalone simulations, and identifying significant characters
/// that need OffscreenInference.
/// </summary>
internal sealed class SimulationPlannerAgent(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    KernelBuilderFactory kernelBuilderFactory) : BaseAgent(dbContextFactory, kernelBuilderFactory)
{
    protected override AgentName GetAgentName() => AgentName.SimulationPlannerAgent;

    public async Task<SimulationPlannerOutput> Invoke(
        GenerationContext context,
        SceneTracker sceneTracker,
        CancellationToken cancellationToken)
    {
        IKernelBuilder kernelBuilder = await GetKernelBuilder(context);

        var systemPrompt = await GetPromptAsync(context);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        var input = BuildInput(context, sceneTracker);
        var contextPrompt = BuildContextPrompt(input);
        chatHistory.AddUserMessage(contextPrompt);

        Kernel kernel = kernelBuilder.Create().Build();

        var outputParser = ResponseParser.CreateJsonParser<SimulationPlannerOutput>("simulation_plan", ignoreNull: true);
        PromptExecutionSettings promptExecutionSettings = kernelBuilder.GetDefaultFunctionPromptExecutionSettings();

        return await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            promptExecutionSettings,
            nameof(SimulationPlannerAgent),
            kernel,
            cancellationToken);
    }

    private SimulationPlannerInput BuildInput(GenerationContext context, SceneTracker sceneTracker)
    {
        var roster = BuildCharacterRoster(context);
        var pendingMcInteractions = ExtractPendingMcInteractions(context);

        return new SimulationPlannerInput
        {
            StoryTracker = sceneTracker,
            CharacterRoster = roster,
            WorldEvents = context.NewWorldEvents,
            PendingMcInteractions = pendingMcInteractions,
            NarrativeDirection = context.WriterGuidance
        };
    }

    private List<CharacterRosterEntry> BuildCharacterRoster(GenerationContext context)
    {
        return context.Characters
            .Where(c => c.Importance == CharacterImportance.ArcImportance || c.Importance == CharacterImportance.Significant)
            .Select(c => new CharacterRosterEntry
            {
                Name = c.Name,
                Importance = c.Importance.Value,
                Location = c.CharacterTracker?.Location ?? "unknown",
                LastSimulated = c.SimulationMetadata?.LastSimulated,
                GoalsSummary = c.CharacterState.Goals.ToJsonString(),
                KeyRelationships = c.Relationships?.Select(r => r.TargetCharacterName).ToArray(),
                RelationshipNotes = BuildRelationshipNotes(c),
                RoutineSummary = c.CharacterState.Routine.ToJsonString(),
                PotentialInteractions = c.SimulationMetadata?.PotentialInteractions
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
            .Select(r => r.Dynamic);

        return string.Join("; ", notes);
    }

    private List<PendingMcInteractionEntry>? ExtractPendingMcInteractions(GenerationContext context)
    {
        var entries = context.Characters
            .Where(c => c.SimulationMetadata?.PendingMcInteraction?.ExtensionData != null)
            .Select(c =>
            {
                var data = c.SimulationMetadata!.PendingMcInteraction!.ExtensionData!;
                return new PendingMcInteractionEntry
                {
                    Character = c.Name,
                    ExtensionData = data
                };
            })
            .ToList();

        return entries.Count > 0 ? entries : null;
    }

    private string BuildContextPrompt(SimulationPlannerInput input)
    {
        var sections = new List<string>
        {
            $"""
             ### Story Tracker
             <story_tracker>
             Time: {input.StoryTracker.Time}
             Location: {input.StoryTracker.Location}
             Weather: {input.StoryTracker.Weather}
             Characters Present: {string.Join(", ", input.StoryTracker.CharactersPresent ?? [])}
             </story_tracker>
             """,
            $"""
             ### Character Roster
             <character_roster>
             {FormatCharacterRoster(input.CharacterRoster)}
             </character_roster>
             """
        };

        if (input.WorldEvents is { Length: > 0 })
        {
            sections.Add($"""
                          ### World Events
                          <world_events>
                          {FormatWorldEvents(input.WorldEvents)}
                          </world_events>
                          """);
        }

        if (input.PendingMcInteractions is { Count: > 0 })
        {
            sections.Add($"""
                          ### Pending MC Interactions
                          <pending_mc_interactions>
                          {FormatPendingMcInteractions(input.PendingMcInteractions)}
                          </pending_mc_interactions>
                          """);
        }

        if (input.NarrativeDirection != null)
        {
            sections.Add($"""
                          ### Narrative Direction
                          <narrative_direction>
                          {input.NarrativeDirection.ToJsonString(PromptSections.GetJsonOptions(ignoreNull: true))}
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

                if (c.PotentialInteractions is { Count: > 0 })
                {
                    var interactions = string.Join("; ",
                        c.PotentialInteractions.Select(p =>
                        {
                            var intent = p.ExtensionData?.TryGetValue("intent", out var i) == true ? i?.ToString() : "interact";
                            return $"intends to {intent} with {p.TargetCharacter}";
                        }));
                    lines.Add($"Pending interactions: {interactions}");
                }

                return string.Join("\n", lines);
            }));
    }

    private static string FormatWorldEvents(WorldEvent[] events)
    {
        return string.Join("\n", events.Select(e => $"- {e.ToJsonString()}"));
    }

    private static string FormatPendingMcInteractions(List<PendingMcInteractionEntry> interactions)
    {
        return string.Join("\n\n",
            interactions.Select(p =>
                $"""
                     **{p.Character}**
                     {p.ExtensionData}
                     """.Trim()));
    }
}