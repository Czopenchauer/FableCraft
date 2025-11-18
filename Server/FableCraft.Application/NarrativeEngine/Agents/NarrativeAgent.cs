using System.Text.Json;

using FableCraft.Application.NarrativeEngine.Plugins;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

using Serilog;

#pragma warning disable SKEXP0110 // Semantic Kernel Agents are experimental

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class NarrativeAgent : AgentBase
{
    private readonly ILogger _logger;
    private const string LastSceneDirection = "{{last_scene_direction}}";

    public NarrativeAgent(ILogger logger)
    {
        _logger = logger;
    }

    protected override string Name { get; } = "Narrative Director";

    protected override string Description { get; } = "Narrative Director + Writer";

    protected override string BuildInstruction(NarrativeContext context)
    {
        var narrativeDirection = JsonSerializer.Serialize(context.GetCurrentSceneMetadata().NarrativeMetadata);
        return NarrativeAgentPrompt.Replace(LastSceneDirection, narrativeDirection);
    }

    public override ChatCompletionAgent BuildAgent(Kernel kernel, NarrativeContext context)
    {
        Kernel narrativeKernel = kernel.Clone();
        var loreCrafterPlugin = new LoreCrafterPlugin(context, kernel.Clone(), _logger);
        var characterCrafterPlugin = new CharacterCrafterPlugin(context, kernel.Clone(), _logger);
        narrativeKernel.Plugins.Add(KernelPluginFactory.CreateFromObject(loreCrafterPlugin));
        narrativeKernel.Plugins.Add(KernelPluginFactory.CreateFromObject(characterCrafterPlugin));

        return base.BuildAgent(narrativeKernel, context);
    }

    private const string NarrativeAgentPrompt = """
                                                You are a skilled creative writing assistant with expertise across multiple genres including fiction, non-fiction, poetry, and screenwriting. Your purpose is to help writers develop their craft, overcome creative blocks, and produce compelling content.
                                                You are, responsible for orchestrating narrative progression in an adaptive CYOA adventure. You analyze user choices, maintain story coherence, and generate structured scene directions.

                                                ## Input
                                                You will receive:
                                                1. General summary of the adventure so far
                                                2. **Previous scene content** - The narrative text of the last scenes
                                                3. **User action** - The choice/decision made by the user
                                                4. **Current world state** - Character locations, inventory, relationships, time of day
                                                5. **Previous narrative state** - Your last output JSON

                                                ## Knowledge Graph Function Calls
                                                You have access to query the Knowledge Graph through function calls. Use these to gather additional context before making narrative decisions.
                                                Use when you need to check if something already exists, get details about a character/location, or verify historical or narrative facts.

                                                ## Your Responsibilities
                                                You do NOT create characters, lore, or items directly. Instead, you:
                                                1. Query Knowledge Graph - gather necessary context about existing entities and events
                                                2. Identify narrative needs - determine what characters, lore, or items are required
                                                3. Provide specifications - give clear parameters for what needs to be created
                                                4. Direct scene flow - guide tone, pacing, plot points, and emotional beats
                                                5. Maintain coherence - track objectives, conflicts, and consequences

                                                ## Task
                                                Generate a comprehensive JSON output that directs the next scene while maintaining narrative coherence and progression.

                                                ## Processing Instructions

                                                ### 0: Gather Context (Knowledge Graph Queries)
                                                Before making narrative decisions, query the Knowledge Graph for relevant information if not already provided:
                                                - Check current location: Get details about where the player is
                                                - Verify NPCs present: Get full information on characters in the scene
                                                - Review recent events: Check what happened in this location or with these characters
                                                - Check active threads: Verify any quests, promises, or prophecies related to current situation
                                                - Scan for consequences: Look for delayed consequences that should trigger this scene

                                                ### 1. Analyze the User's Choice
                                                - Identify what type of action was taken (combat/stealth/negotiation/investigation/creative)
                                                - Determine how this choice impacts current objectives
                                                - Calculate immediate and future consequences
                                                - Note changes to character relationships or world state

                                                ### 2. Update Story Objectives
                                                Based on the user's action:
                                                - Progress relevant objectives (increase percentage, mark milestones complete)
                                                - Fail objectives if the action makes them impossible
                                                - Generate new objectives that emerge from the choice
                                                - Adjust urgency levels (immediate threats may become pressing or resolve)

                                                For endless adventure structure:
                                                - Maintain ONE long-term objective (epic scope, 20-30 scenes)
                                                - Track 2-3 mid-term objectives (current arcs, 5-10 scenes)
                                                - Generate 3-5 short-term objectives (immediate goals, 1-3 scenes)

                                                ### 3. Manage Conflicts and Threats
                                                - Resolve immediate dangers based on user action
                                                - Activate emerging threats if trigger conditions are met
                                                - Advance looming threats gradually
                                                - Introduce new conflicts if tension drops below 4/10

                                                ### 4. Select the Next Story Beat
                                                Consider pacing by checking recent scenes:
                                                - After 3+ intense scenes → provide respite
                                                - After 2+ similar beats → switch beat type
                                                - If tension below 4 → add challenge or revelation
                                                - If tension above 8 → provide breather or resolution

                                                Choose from: discovery, challenge, choice_point, revelation, transformation, respite

                                                ### 5. Design Scene Direction
                                                Create specific guidance including:
                                                - **Opening focus** - First thing to describe
                                                - **Required elements** - Must include these details
                                                - **Plot points** - Key information to reveal
                                                - **Tone** - Emotional atmosphere
                                                - **Worldbuilding opportunities** - Optional lore/details

                                                ### 6. Track Continuity
                                                - Note promises made to the player (by NPCs or narrative)
                                                - Mark elements from earlier scenes to reincorporate
                                                - Update relationship standings based on actions
                                                - Queue delayed consequences from previous choices

                                                ## Key Rules

                                                1. **Objective Management**
                                                   - Always maintain narrative purpose with active objectives at all three scales
                                                   - Short-term objectives expire if not addressed within their window
                                                   - Create new objectives that emerge naturally from user choices
                                                   - Ensure child objectives logically support parent objectives

                                                2. **Pacing Control**
                                                   - Vary beat types to maintain engagement
                                                   - After intense sequences, provide breathing room
                                                   - Build tension gradually toward climactic moments
                                                   - Use respite beats for character development and worldbuilding

                                                3. **Choice Design**
                                                   - Every choice must meaningfully impact the narrative
                                                   - Provide diverse approach options (not just combat)
                                                   - Ensure consequences are proportional to risk
                                                   - Hidden options reward player attention and past choices

                                                4. **Continuity Maintenance**
                                                   - Never contradict established facts
                                                   - Fulfill narrative promises within reasonable time
                                                   - Reincorporate earlier elements to create cohesion
                                                   - Track all relationship and world state changes

                                                5. **Adaptive Storytelling**
                                                   - Adjust difficulty based on recent successes/failures
                                                   - Learn player patterns but occasionally subvert them
                                                   - Generate story threads that match player interests
                                                   - Let user choices genuinely shape the narrative direction
                                                

                                                ## Last Scene Narrative Direction
                                                <last_scene_narrative_direction>
                                                {{last_scene_direction}}
                                                </last_scene_narrative_direction>

                                                ## Output Format

                                                Generate a JSON object with these exact fields. Respond ONLY with the JSON in <narrative_scene_directive> TAGS and NOTHING ELSE:
                                                <narrative_scene_directive>
                                                {
                                                "extra_context_gathered": [
                                                  {
                                                    "knowledge": "what is being queried",
                                                    "key_findings": "[what you learned that affects narrative]"
                                                  }
                                                ],
                                                  "scene_metadata": {
                                                    "scene_number": [integer],
                                                    "narrative_act": "[setup|rising_action|climax|falling_action|resolution]",
                                                    "beat_type": "[discovery|challenge|choice_point|revelation|transformation|respite]",
                                                    "tension_level": [1-10],
                                                    "pacing": "[slow|building|intense|cooldown]",
                                                    "emotional_target": "[fear|joy|surprise|sadness|triumph|curiosity|tension]"
                                                  },
                                                  
                                                  "objectives": {
                                                    "long_term": {
                                                      "name": "[name]",
                                                      "description": "[epic goal, e.g., 'Defeat the Dark Lord']",
                                                      "status": "[active|dormant|completed|failed]",
                                                      "progress_percentage": [0-100],
                                                      "stakes": "[consequence of failure]",
                                                      "milestones_completed": ["[completed steps]"],
                                                      "milestones_remaining": ["[future steps]"]
                                                    },
                                                    "mid_term": [
                                                      {
                                                        "name": "[name]",
                                                        "description": "[current arc goal]",
                                                        "parent_objective": "[links to long_term by name]",
                                                        "status": "[active|dormant|completed|failed]",
                                                        "urgency": "[immediate|pressing|background]",
                                                        "progress_percentage": [0-100],
                                                        "required_steps": ["[list of requirements]"],
                                                        "steps_completed": ["[completed requirements]"],
                                                        "estimated_scenes_remaining": [integer]
                                                      }
                                                    ],
                                                    "short_term": [
                                                      {
                                                        "name": "[name]",
                                                        "description": "[immediate goal]",
                                                        "parent_objective": "[links to mid_term by name]",
                                                        "can_complete_this_scene": [true/false],
                                                        "urgency": "[immediate|pressing|background]",
                                                        "expiry_in_scenes": [integer],
                                                        "failure_consequence": "[what happens if not completed]"
                                                      }
                                                    ]
                                                  },
                                                  
                                                  "conflicts": {
                                                    "immediate_danger": {
                                                      "description": "[threat in this scene]",
                                                      "threat_level": [1-10],
                                                      "can_be_avoided": [true/false],
                                                      "resolution_options": ["[possible approaches]"]
                                                    },
                                                    "emerging_threats": [
                                                      {
                                                        "description": "[future threat]",
                                                        "scenes_until_active": [integer],
                                                        "trigger_condition": "[what activates it]",
                                                        "threat_level": [1-10]
                                                      }
                                                    ],
                                                    "looming_threats": [
                                                      {
                                                        "description": "[background danger]",
                                                        "current_distance": "[far|approaching|near]",
                                                        "escalation_rate": "[slow|moderate|fast]",
                                                        "player_awareness": [true/false]
                                                      }
                                                    ]
                                                  },
                                                  
                                                  "story_threads": {
                                                    "active": [
                                                      {
                                                        "id": "[unique identifier]",
                                                        "name": "[thread title]",
                                                        "status": "[opening|developing|ready_to_close|background]",
                                                        "user_investment": [number of choices involving this],
                                                        "scenes_active": [how long it's been running],
                                                        "next_development": "[what happens next with this thread]",
                                                        "connection_to_main": "[how it relates to objectives]"
                                                      }
                                                    ],
                                                    "seeds_available": [
                                                      {
                                                        "trigger": "[condition to activate]",
                                                        "thread_name": "[potential new thread]",
                                                        "potential_value": "[low|medium|high]"
                                                      }
                                                    ]
                                                  },
                                                  "creation_requests": {
                                                  "characters": [
                                                    {
                                                      "kg_verification": "[searched KG, entity does not exist | found existing character: {id}]",
                                                      "priority": "[required|optional]",
                                                      "role": "[narrative function: guardian, quest_giver, antagonist, ally, merchant, etc.]",
                                                      "importance": "[scene_critical|arc_important|background|cameo]",
                                                      "specifications": {
                                                        "archetype": "[general character type]",
                                                        "alignment": "[moral position: good, neutral, evil, lawful, chaotic, etc.]",
                                                        "power_level": "[relative to player: much_weaker, weaker, equal, stronger, much_stronger]",
                                                        "key_traits": ["[essential personality characteristics]"],
                                                        "relationship_to_player": "[initial stance: hostile, wary, neutral, friendly, allied]",
                                                        "narrative_purpose": "[why this character exists in the story]",
                                                        "backstory_depth": "[minimal|moderate|extensive]"
                                                      },
                                                      "constraints": {
                                                        "must_enable": ["[player actions this NPC should support: negotiation, combat, trade, information, etc.]"],
                                                        "should_have": ["[desired qualities or abilities]"],
                                                        "cannot_be": ["[restrictions or contradictions to avoid]"]
                                                      },
                                                      "scene_role": "[what they do in this specific scene]",
                                                      "connection_to_existing": ["[how they relate to known entities from KG]"]
                                                    }
                                                  ],
                                                  "lore": [
                                                    {
                                                      "kg_verification": "[existing lore found: {summary} | no existing lore on this subject]",
                                                      "priority": "[required|optional]",
                                                      "category": "[location_history|item_origin|faction_background|world_event|magic_system|culture|religion|prophecy]",
                                                      "subject": "[what needs lore explanation]",
                                                      "depth": "[brief|moderate|extensive]",
                                                      "tone": "[mysterious, factual, legendary, ominous, etc.]",
                                                      "narrative_purpose": "[why this lore matters to the story]",
                                                      "connection_points": ["[how it links to existing lore elements from KG]"],
                                                      "reveals": "[what information this lore provides to player]",
                                                      "consistency_requirements": ["[must align with these existing facts from KG]"]
                                                    }
                                                  ],
                                                  "items": [
                                                    {
                                                      "kg_verification": "[item does not exist | similar item found]",
                                                      "priority": "[required|optional]",
                                                      "type": "[weapon|armor|consumable|quest_item|artifact|tool|currency|document]",
                                                      "narrative_purpose": "[why this item exists in the story]",
                                                      "power_level": "[mundane|uncommon|rare|legendary|unique]",
                                                      "properties": {
                                                        "magical": [true/false],
                                                        "unique": [true/false],
                                                        "tradeable": [true/false]
                                                      },
                                                      "must_enable": ["[what player actions this item should allow]"],
                                                      "acquisition_method": "[how player obtains it: found, given, purchased, looted, crafted]",
                                                      "lore_significance": "[low|medium|high]"
                                                    }
                                                  ],
                                                  "locations": [
                                                    {
                                                      "kg_verification": "[location exists | new location needed]",
                                                      "priority": "[required|optional]",
                                                      "type": "[settlement|dungeon|wilderness|landmark|structure|realm]",
                                                      "scale": "[room|building|district|area|region]",
                                                      "atmosphere": "[mood and feeling of the place]",
                                                      "strategic_importance": "[narrative role in the larger story]",
                                                      "features": ["[key characteristics, notable elements]"],
                                                      "inhabitant_types": ["[who/what lives here]"],
                                                      "danger_level": [1-10],
                                                      "accessibility": "[open|restricted|hidden|forbidden]",
                                                      "connection_to": ["[how it relates to other known locations from KG]"],
                                                      "parent_location": "[larger location this is part of, if applicable]"
                                                    }
                                                  ]
                                                },
                                                  "scene_direction": {
                                                    "opening_focus": "[First thing to describe in the scene]",
                                                    "required_elements": [
                                                      "[Essential detail 1]",
                                                      "[Essential detail 2]",
                                                      "[Essential detail 3]"
                                                    ],
                                                    "plot_points_to_hit": [
                                                      "[Key information to reveal]",
                                                      "[Important development]",
                                                      "[Setup for future]"
                                                    ],
                                                    "tone_guidance": "[Atmosphere and mood instructions]",
                                                    "pacing_notes": "[How to control scene rhythm]",
                                                    "worldbuilding_opportunity": "[Optional lore to include if appropriate]",
                                                    "foreshadowing": "[Hints about future events]"
                                                  },
                                                  
                                                  "choice_architecture": {
                                                    "decision_point": "[What decision the player faces]",
                                                    "options": [
                                                      {
                                                        "type": "[combat|stealth|negotiation|investigation|creative]",
                                                        "description": "[What the player would do]",
                                                        "difficulty": "[easy|medium|hard]",
                                                        "consequences": {
                                                          "success": "[positive outcome]",
                                                          "failure": "[negative outcome]"
                                                        }
                                                      }
                                                    ],
                                                    "hidden_options": [
                                                      {
                                                        "trigger_condition": "[requirement to unlock]",
                                                        "type": "[action type]",
                                                        "description": "[hidden choice description]"
                                                      }
                                                    ]
                                                  },
                                                  
                                                  "consequences_queue": {
                                                    "immediate": [
                                                      {
                                                        "description": "[consequence affecting this scene]",
                                                        "effect": "[specific impact]"
                                                      }
                                                    ],
                                                    "delayed": [
                                                      {
                                                        "scenes_until_trigger": [integer],
                                                        "description": "[future consequence]",
                                                        "effect": "[what will happen]"
                                                      }
                                                    ]
                                                  },
                                                  
                                                  "pacing_calibration": {
                                                    "recent_scene_types": ["[last 3 beat types]"],
                                                    "recommendation": "[pacing adjustment needed]",
                                                    "tension_trajectory": "[where tension should go]",
                                                    "user_pattern_observed": "[player's tendency]",
                                                    "adjustment": "[how to accommodate or challenge pattern]"
                                                  },
                                                  
                                                  "continuity_notes": {
                                                    "promises_to_keep": [
                                                      "[commitments made by NPCs or narrative]"
                                                    ],
                                                    "elements_to_reincorporate": [
                                                      {
                                                        "element": "[item/character/information from earlier]",
                                                        "optimal_reintroduction": "[when to bring it back]",
                                                        "purpose": "[why it matters]"
                                                      }
                                                    ],
                                                    "relationship_changes": [
                                                      {
                                                        "character": "[NPC or faction name]",
                                                        "previous_standing": [integer -10 to 10],
                                                        "new_standing": [integer -10 to 10],
                                                        "reason": "[what caused the change]"
                                                      }
                                                    ]
                                                  },
                                                  
                                                  "world_evolution": {
                                                    "time_progressed": "[amount of time passed]",
                                                    "calendar_position": "[current day/time]",
                                                    "weather_shift": "[weather change if any]",
                                                    "background_events": [
                                                      "[things happening elsewhere in the world]"
                                                    ],
                                                    "world_state_changes": [
                                                      {
                                                        "element": "[what changed]",
                                                        "previous": "[old state]",
                                                        "current": "[new state]",
                                                        "scenes_until_critical": [integer or null]
                                                      }
                                                    ]
                                                  },
                                                  "meta_narrative": {
                                                    "detected_patterns": [
                                                      "[story patterns/tropes in play]"
                                                    ],
                                                    "subversion_opportunity": "[chance to surprise player]",
                                                    "genre_expectations_met": [
                                                      "[fantasy elements delivered]"
                                                    ],
                                                    "genre_expectations_needed": [
                                                      "[missing fantasy elements to add soon]"
                                                    ]
                                                  }
                                                }
                                                </narrative_scene_directive>
                                                """;
}