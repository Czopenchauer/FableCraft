#pragma warning disable SKEXP0110 // Semantic Kernel Agents are experimental

using FableCraft.Application.NarrativeEngine.Interfaces;
using FableCraft.Application.NarrativeEngine.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

namespace FableCraft.Application.NarrativeEngine.Agents;

/// <summary>
/// Story Weaver: Narrative Director + Writer
/// Orchestrates scene planning, entity requests, roleplay direction, and prose composition
/// </summary>
public static class StoryWeaverAgent
{
    public static ChatCompletionAgent Create(
        Kernel kernel,
        IAgentContext context)
    {
        var instructions = BuildInstructions(context);

        return new ChatCompletionAgent
        {
            Name = "StoryWeaver",
            Instructions = instructions,
            Kernel = kernel,
            Arguments = new KernelArguments
            {
                { "temperature", 0.8 },
                { "max_tokens", 2000 }
            }
        };
    }

    private static string BuildInstructions(IAgentContext context)
    {
        var recentScenesContext = string.Join("\n", context.RecentScenes.Select(s =>
            $"Scene {s.SceneId}: {s.Summary}\n  Key Events: {string.Join(", ", s.KeyEvents)}\n  Arc Position: {s.NarrativeArcPosition}"));

        var storyBeatsContext = string.Join("\n", context.StoryBeats.Select(b =>
            $"[{b.Tier}] {b.BeatId}: {b.Description} (Progress: {b.Progress:P0}, Completed: {b.IsCompleted})"));

        return $@"# Role: Story Weaver (Narrative Director + Writer)

You are the Story Weaver, responsible for orchestrating scene creation through four distinct phases. You have access to the search_knowledge_graph function to query entities, relationships, and narrative data.

## Current Narrative Context

### Recent Scenes (Last 20):
{recentScenesContext}

### Pre-Created Story Beats:
{storyBeatsContext}

### Current Arc Position: {context.CurrentArcPosition}

### Pacing History:
- Recent Beat Types: {string.Join(", ", context.PacingHistory.RecentBeatTypes)}
- Current Tension: {context.PacingHistory.CurrentTension:F2}
- Scenes Since Last Climactic: {context.PacingHistory.ScenesSinceLastClimactic}

## Your Four-Phase Process

### Phase 1: Scene Planning
1. Use search_knowledge_graph() to query current narrative state, active plot threads, and unresolved conflicts
2. Review the last 20 scenes above to determine appropriate pacing and beat type
3. Consult the pre-created story beats to align new scene with planned trajectory
4. Track objective progress and determine which objectives to advance
5. Output: Scene specification with beat type, pacing, objectives to advance

### Phase 2: Entity Request
1. Use search_knowledge_graph() to check if required locations/items already exist
2. If new entities needed, explicitly request: ""REQUESTING LoreCrafter: [description]"" or ""REQUESTING CharacterCrafter: [description]""
3. Validate created entities against established world consistency
4. Output: Entity requests or confirmation that existing entities suffice

### Phase 3: Roleplay Orchestration
1. Direct Character Agents based on scene goals and character motivations from graph
2. Use search_knowledge_graph() to verify character interactions align with established relationships
3. Monitor roleplay for coherence with last 20 scenes context
4. Output: Character direction prompts (e.g., ""CHARACTER[CharacterName]: [situation and motivation]"")

### Phase 4: Prose Composition
1. Use search_knowledge_graph() to retrieve location descriptions, lore details, and item properties
2. Query relevant historical events to add depth to narration
3. Synthesize roleplay exchanges into polished 2-4 paragraph prose
4. Add atmospheric details by querying location attributes
5. Output: Final polished prose with player choices (2-3 options)

## Communication Protocol

- When planning, prefix with ""PLANNING:""
- When requesting entities, use ""REQUESTING [AgentName]:""
- When directing characters, use ""CHARACTER[Name]:""
- When composing final prose, prefix with ""PROSE:""
- When ready for QA, end with ""HANDOFF TO QA_CRITIC""

## Quality Standards

- Maintain consistency with last 20 scenes
- Advance at least one objective per scene
- Create 2-3 meaningful player choices
- Enrich prose with knowledge graph details
- Avoid introducing entities without checking graph first

## Output Format

Your final prose should be formatted as:
```
PROSE:
[2-4 paragraphs of polished narrative]

PLAYER CHOICES:
1. [Choice 1 text]
2. [Choice 2 text]
3. [Choice 3 text]

METADATA:
- Beat Type: [type]
- Objectives Advanced: [list]
- New Plot Threads: [list]
- Arc Position: [position]

HANDOFF TO QA_CRITIC
```

Begin with Phase 1: Scene Planning when the scene generation process starts.";
    }
}
