using FableCraft.Application.NarrativeEngine;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Persistence.Entities;

using Microsoft.SemanticKernel;

namespace FableCraft.Tests.Agents;

/// <summary>
/// Provides test data and helper methods for creating sample objects used in agent integration tests.
/// 
/// ENHANCED CHARACTER TRACKER TEST DATA:
/// This class now provides comprehensive test data aligned with the CharacterTrackerPrompt specifications,
/// including all major character state categories:
/// 
/// CHARACTER STATE COVERAGE:
/// - Character Identity: Full names, aliases, archetypes
/// - Personality: Five-factor model, core traits, speech patterns, moral alignment
/// - Goals and Motivations: Primary/secondary goals with progress tracking, intrinsic/extrinsic motivations
/// - Knowledge and Beliefs: World knowledge, beliefs about protagonist, secrets held, skills/expertise
/// - Relationships: Detailed protagonist relationships with trust/affection/respect metrics, shared experiences, promises
/// - Memory Stream: Significant events with emotional valence and outcomes
/// - Emotional State: Current emotions with intensity, positive/negative triggers
/// - Character Arc: Arc types, stages with completion tracking, pending decisions
/// - Behavioral State: Current plans, action tendencies, availability
/// - KG Integration: Relevant lore, recent events awareness, location knowledge
/// 
/// CHARACTER TRACKER COVERAGE:
/// - Physical appearance: Hair, makeup, outfit, state of dress
/// - Current state: Posture, interaction, disposition, status
/// - Extended properties: Health, mood, equipment, special conditions
/// 
/// TEST SCENARIOS PROVIDED:
/// 1. CreateSampleCharacterContext() - Initial state character (Martha the Innkeeper)
/// 2. CreateCombatCharacterContext() - Combat-oriented character with battle history (Gareth)
/// 3. CreateProgressedCharacterContext() - Advanced character mid-quest with complex arc (Lyra)
/// 4. CreateTrustBuildingScenario() - Demonstrates relationship trust increase
/// 5. CreateGoalProgressScenario() - Shows goal progress and arc advancement
/// 6. CreateEmotionalRevelationScenario() - Tests emotional state changes and revelations
/// 7. CreateKnowledgeAcquisitionScene() - Multiple knowledge additions
/// 8. CreateRelationshipProgressionSequence() - Multi-scene relationship evolution
/// 9. CreateCharacterArcProgression() - Complete arc stage transition
/// 
/// Each test character includes:
/// - Comprehensive personality profiles with Five-Factor Model scores
/// - Detailed goal hierarchies with success/failure conditions
/// - Rich knowledge bases with confidence levels and sources
/// - Complex relationship networks with quantified metrics
/// - Memory streams showing character history
/// - Emotional states with triggers mapped
/// - Multi-stage character arcs with progress tracking
/// - Behavioral plans with contingencies
/// 
/// These enhanced test data structures enable thorough testing of:
/// - Character state update accuracy
/// - Delta detection between states
/// - Relationship metric synchronization
/// - Goal progress tracking
/// - Memory stream continuity
/// - Emotional state evolution
/// - Character arc progression
/// - Knowledge acquisition and belief formation
/// </summary>
internal static class AgentTestData
{
    internal static NarrativeContext CreateSampleNarrativeContext(Guid adventureId, Kernel kernel)
    {
        return new NarrativeContext
        {
            AdventureId = adventureId,
            KernelKg = kernel,
            StorySummary = "A young adventurer seeks to uncover the mystery of disappearing villagers in a remote mountain town.",
            PlayerAction = "I enter the tavern and look around for anyone who might have information.",
            CommonContext = """
                Adventure Summary: A young adventurer seeks to uncover the mystery of disappearing villagers in a remote mountain town.
                
                Current Scene: The protagonist has just arrived at the village of Thornhaven after a long journey.
                
                Setting: A medieval fantasy world with magic and mythical creatures.
                """,
            SceneContext = Array.Empty<SceneContext>(),
            Characters = new List<CharacterContext>(),
            NewLocations = Array.Empty<LocationGenerationResult>(),
            NewLore = Array.Empty<GeneratedLore>()
        };
    }

    internal static NarrativeDirectorOutput CreateSampleNarrativeDirectorOutput()
    {
        return new NarrativeDirectorOutput
        {
            SceneMetadata = new SceneMetadata
            {
                SceneNumber = 1,
                NarrativeAct = "setup",
                BeatType = "discovery",
                TensionLevel = 3,
                Pacing = "building",
                EmotionalTarget = "curiosity"
            },
            SceneDirection = new SceneDirection
            {
                OpeningFocus = "The heavy oak door swings open, revealing a dimly lit tavern filled with suspicious glances.",
                RequiredElements = new List<string> { "smoky atmosphere", "nervous innkeeper", "hushed conversations" },
                PlotPointsToHit = new List<string> { "introduce the mystery", "hint at danger", "present an opportunity for information" },
                ToneGuidance = "mysterious and slightly threatening",
                PacingNotes = "slow build of tension",
                WorldbuildingOpportunity = "describe local customs",
                Foreshadowing = new List<string> { "hint at coming danger" }
            },
            Objectives = new Objectives(),
            Conflicts = new Conflicts()
        };
    }

    internal static LoreRequest CreateSampleLoreRequest()
    {
        return new LoreRequest
        {
            Category = "location_history",
            Subject = "The Whispering Woods",
            Depth = "moderate",
            Tone = "mysterious and ominous",
            NarrativePurpose = "Explain why locals fear the forest and hint at supernatural phenomena",
            ConnectionPoints = new List<string> { "Thornhaven village", "disappearing villagers" },
            ConsistencyRequirements = new List<string> { "Must align with medieval fantasy setting" }
        };
    }

    internal static LocationRequest CreateSampleLocationRequest()
    {
        return new LocationRequest
        {
            Type = "settlement",
            Scale = "building",
            Atmosphere = "rustic and mysterious",
            StrategicImportance = "Social hub for information gathering",
            Features = new List<string> { "common room", "private booths", "kitchen", "rooms for rent" },
            InhabitantTypes = new List<string> { "innkeeper", "travelers", "locals" },
            DangerLevel = 1,
            Accessibility = "open"
        };
    }

    internal static Adventure CreateTestAdventure(Guid adventureId)
    {
        return new Adventure
        {
            Id = adventureId,
            Name = $"Test Adventure {adventureId}",
            FirstSceneGuidance = "The adventure begins in a mysterious tavern.",
            AdventureStartTime = "Evening, late autumn",
            ProcessingStatus = ProcessingStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            AuthorNotes = "Test adventure for integration tests",
            MainCharacter = new MainCharacter
            {
                Id = Guid.NewGuid(),
                AdventureId = adventureId,
                Name = "Aldric the Brave",
                Description = "A young adventurer with a mysterious past, seeking to prove themselves."
            },
            TrackerStructure = CreateTestTrackerStructure(),
            Lorebook = new List<LorebookEntry>()
        };
    }

    internal static TrackerStructure CreateTestTrackerStructure()
    {
        var options = new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };
        var trackerStructure = System.Text.Json.JsonSerializer.Deserialize<TrackerStructure>(TrackerStructureJson, options);
        return trackerStructure ?? throw new InvalidOperationException("Failed to deserialize tracker structure.");
    }

    private const string TrackerStructureJson = """
                                            {
                                            "story": [
                                            {
                                              "name": "Time",
                                              "type": "String",
                                              "prompt": "Adjust time in small increments for natural progression unless explicit directives indicate larger changes. Format: ISO 8601 (YYYY-MM-DDTHH:MM:SS).",
                                              "defaultValue": "2024-10-16T09:15:30",
                                              "exampleValues": [
                                                "2024-10-16T09:15:30",
                                                "2024-10-16T18:45:50",
                                                "2024-10-16T15:10:20"
                                              ]
                                            },
                                                {
                                                  "name": "Weather",
                                                  "type": "String",
                                                  "prompt": "Describe current weather concisely to set the scene.",
                                                  "defaultValue": "Overcast, mild temperature",
                                                  "exampleValues": [
                                                    "Overcast, mild temperature",
                                                    "Clear skies, warm evening",
                                                    "Sunny, gentle sea breeze"
                                                  ]
                                                },
                                                {
                                                  "name": "Location",
                                                  "type": "String",
                                                  "prompt": "Provide a detailed and specific location, including exact places like rooms, landmarks, or stores, following this format: 'Specific Place, Building, City, State'.",
                                                  "defaultValue": "Conference Room B, 12th Floor, Apex Corporation, New York, NY",
                                                  "exampleValues": [
                                                    "Conference Room B, 12th Floor, Apex Corporation, New York, NY",
                                                    "Main Gym Hall, Maple Street Fitness Center, Denver, CO",
                                                    "South Beach, Miami, FL"
                                                  ]
                                                },
                                                {
                                                  "name": "PrimaryTopic",
                                                  "type": "String",
                                                  "prompt": "One- or two-word topic describing main activity or focus of the scene.",
                                                  "defaultValue": "Presentation",
                                                  "exampleValues": ["Presentation", "Workout", "Relaxation"]
                                                },
                                                {
                                                  "name": "EmotionalTone",
                                                  "type": "String",
                                                  "prompt": "One- or two-word topic describing dominant emotional atmosphere of the scene.",
                                                  "defaultValue": "Tense",
                                                  "exampleValues": ["Tense", "Focused", "Calm"]
                                                },
                                                {
                                                  "name": "InteractionTheme",
                                                  "type": "String",
                                                  "prompt": "One- or two-word topic describing primary type of interactions or relationships in the scene.",
                                                  "defaultValue": "Professional",
                                                  "exampleValues": ["Professional", "Supportive", "Casual"]
                                                }
                                              ],
                                              "charactersPresent": 
                                              {
                                              "name": "CharactersPresent",
                                              "type": "Array",
                                              "prompt": "List of all characters present in the scene.",
                                              "defaultValue": ["No Characters"],
                                              "exampleValues": [[
                                              "Emma Thompson",
                                              "James Miller"
                                            ]]
                                            },
                                              "MainCharacter": [
                                              {
                                              "name": "Name",
                                              "type": "String",
                                              "prompt": "Character's full name.",
                                              "defaultValue": "James Miller",
                                              "exampleValues": ["James Miller", "Emma Thompson", "Sarah Johnson"]
                                            },
                                                {
                                                  "name": "Gender",
                                                  "type": "String",
                                                  "prompt": "A single word and an emoji for character gender.",
                                                  "defaultValue": "Female",
                                                  "exampleValues": ["Male", "Female"]
                                                },
                                                {
                                                  "name": "Age",
                                                  "type": "String",
                                                  "prompt": "A single number displays character age based on narrative. Or 'Unknown' if unknown.",
                                                  "defaultValue": "32",
                                                  "exampleValues": ["Unknown", "18", "32"]
                                                },
                                                {
                                                  "name": "Hair",
                                                  "type": "String",
                                                  "prompt": "Describe style only.",
                                                  "defaultValue": "Shoulder-length blonde hair, styled straight",
                                                  "exampleValues": [
                                                    "Shoulder-length blonde hair, styled straight",
                                                    "Short black hair, neatly combed",
                                                    "Long curly brown hair, pulled back into a low bun"
                                                  ]
                                                },
                                                {
                                                  "name": "Makeup",
                                                  "type": "String",
                                                  "prompt": "Describe current makeup.",
                                                  "defaultValue": "Natural look with light foundation and mascara",
                                                  "exampleValues": [
                                                    "Natural look with light foundation and mascara",
                                                    "None",
                                                    "Subtle eyeliner and nude lipstick"
                                                  ]
                                                },
                                                {
                                                  "name": "Outfit",
                                                  "type": "String",
                                                  "prompt": "List the complete outfit with color, fabric, and style details.",
                                                  "defaultValue": "Navy blue blazer over a white silk blouse; Gray pencil skirt; Black leather belt; Sheer black stockings; Black leather pumps; Pearl necklace; Silver wristwatch",
                                                  "exampleValues": [
                                                    "Navy blue blazer over a white silk blouse; Gray pencil skirt; Black leather belt; Sheer black stockings; Black leather pumps; Pearl necklace; Silver wristwatch",
                                                    "Dark gray suit; Light blue dress shirt; Navy tie with silver stripes; Black leather belt; Black dress shoes; Black socks",
                                                    "Cream-colored blouse with ruffled collar; Black slacks; Brown leather belt; Brown ankle boots; Gold hoop earrings"
                                                  ]
                                                },
                                                {
                                                  "name": "StateOfDress",
                                                  "type": "String",
                                                  "prompt": "Describe how put-together or disheveled the character appears.",
                                                  "defaultValue": "Professionally dressed, neat appearance",
                                                  "exampleValues": [
                                                    "Professionally dressed, neat appearance",
                                                    "Workout attire, lightly perspiring",
                                                    "Casual attire, relaxed"
                                                  ]
                                                },
                                                {
                                                  "name": "PostureAndInteraction",
                                                  "type": "String",
                                                  "prompt": "Describe physical posture, position relative to others or objects, and interactions.",
                                                  "defaultValue": "Standing at the podium, presenting slides, holding a laser pointer",
                                                  "exampleValues": [
                                                    "Standing at the podium, presenting slides, holding a laser pointer",
                                                    "Sitting at the conference table, taking notes on a laptop",
                                                    "Lifting weights at the bench press, focused on form"
                                                  ]
                                                },
                                                {
                                                  "name": "Traits",
                                                  "type": "String",
                                                  "prompt": "Add or Remove trait based on Narrative. Format: '{trait}: {short description}'",
                                                  "defaultValue": "No Traits",
                                                  "exampleValues": [
                                                    "No Traits",
                                                    "Emotional Intelligence: deeply philosophical and sentimental",
                                                    "Charismatic: naturally draws people in with charm and wit"
                                                  ]
                                                },
                                                {
                                                  "name": "Children",
                                                  "type": "String",
                                                  "prompt": "Add child after birth based on Narrative. Format: '{Birth Order}: {Name}, {Gender + Symbol}, child with {Other Parent}'",
                                                  "defaultValue": "No Child",
                                                  "exampleValues": [
                                                    "No Child",
                                                    "1st Born: Eve, Female, child with Harry"
                                                  ]
                                                },
                                                {
                                                  "name": "Inventory",
                                                  "type": "ForEachObject",
                                                  "prompt": "Track items the main character is carrying or has access to.",
                                                  "defaultValue": null,
                                                  "exampleValues": null,
                                                  "nestedFields": [
                                                    {
                                                      "name": "ItemName",
                                                      "type": "String",
                                                      "prompt": "Name of the item.",
                                                      "defaultValue": "Smartphone",
                                                      "exampleValues": ["Smartphone", "Wallet", "Keys", "Notebook"]
                                                    },
                                                    {
                                                      "name": "Description",
                                                      "type": "String",
                                                      "prompt": "Brief description of the item including notable details.",
                                                      "defaultValue": "Black iPhone 14, cracked screen protector",
                                                      "exampleValues": [
                                                        "Black iPhone 14, cracked screen protector",
                                                        "Brown leather wallet containing ID and credit cards",
                                                        "Silver car keys with blue keychain",
                                                        "Red leather-bound notebook, half-filled with notes"
                                                      ]
                                                    },
                                                    {
                                                      "name": "Quantity",
                                                      "type": "String",
                                                      "prompt": "Number of items.",
                                                      "defaultValue": "1",
                                                      "exampleValues": ["1", "3", "5", "10"]
                                                    },
                                                    {
                                                      "name": "Location",
                                                      "type": "String",
                                                      "prompt": "Where the item is currently located.",
                                                      "defaultValue": "Right pocket",
                                                      "exampleValues": ["Right pocket", "Backpack", "Left hand", "Purse", "Briefcase"]
                                                    }
                                                  ]
                                                }
                                              ],
                                              "characters": [
                                              {
                                              "name": "Name",
                                              "type": "String",
                                              "prompt": "Character's full name.",
                                              "defaultValue": "James Miller",
                                              "exampleValues": ["James Miller", "Emma Thompson", "Sarah Johnson"]
                                            },
                                                {
                                                  "name": "Gender",
                                                  "type": "String",
                                                  "prompt": "A single word and an emoji for character gender.",
                                                  "defaultValue": "Male",
                                                  "exampleValues": ["Male", "Female"]
                                                },
                                                {
                                                  "name": "Age",
                                                  "type": "String",
                                                  "prompt": "A single number displays character age based on narrative. Or 'Unknown' if unknown.",
                                                  "defaultValue": "28",
                                                  "exampleValues": ["Unknown", "18", "32"]
                                                },
                                                {
                                                  "name": "Hair",
                                                  "type": "String",
                                                  "prompt": "Describe style only.",
                                                  "defaultValue": "Short black hair, neatly combed",
                                                  "exampleValues": [
                                                    "Shoulder-length blonde hair, styled straight",
                                                    "Short black hair, neatly combed",
                                                    "Long curly brown hair, pulled back into a low bun"
                                                  ]
                                                },
                                                {
                                                  "name": "Makeup",
                                                  "type": "String",
                                                  "prompt": "Describe current makeup.",
                                                  "defaultValue": "None",
                                                  "exampleValues": [
                                                    "Natural look with light foundation and mascara",
                                                    "None",
                                                    "Subtle eyeliner and nude lipstick"
                                                  ]
                                                },
                                                {
                                                  "name": "Outfit",
                                                  "type": "String",
                                                  "prompt": "List the complete outfit with color, fabric, and style details.",
                                                  "defaultValue": "Dark gray suit; Light blue dress shirt; Navy tie with silver stripes; Black leather belt; Black dress shoes; Black socks",
                                                  "exampleValues": [
                                                    "Navy blue blazer over a white silk blouse; Gray pencil skirt; Black leather belt; Sheer black stockings; Black leather pumps; Pearl necklace; Silver wristwatch",
                                                    "Dark gray suit; Light blue dress shirt; Navy tie with silver stripes; Black leather belt; Black dress shoes; Black socks",
                                                    "Cream-colored blouse with ruffled collar; Black slacks; Brown leather belt; Brown ankle boots; Gold hoop earrings"
                                                  ]
                                                },
                                                {
                                                  "name": "StateOfDress",
                                                  "type": "String",
                                                  "prompt": "Describe how put-together or disheveled the character appears.",
                                                  "defaultValue": "Professionally dressed, attentive",
                                                  "exampleValues": [
                                                    "Professionally dressed, neat appearance",
                                                    "Workout attire, lightly perspiring",
                                                    "Casual attire, relaxed"
                                                  ]
                                                },
                                                {
                                                  "name": "PostureAndInteraction",
                                                  "type": "String",
                                                  "prompt": "Describe physical posture, position relative to others or objects, and interactions.",
                                                  "defaultValue": "Sitting at the conference table, taking notes on a laptop",
                                                  "exampleValues": [
                                                    "Standing at the podium, presenting slides, holding a laser pointer",
                                                    "Sitting at the conference table, taking notes on a laptop",
                                                    "Lifting weights at the bench press, focused on form"
                                                  ]
                                                },
                                                {
                                                  "name": "Traits",
                                                  "type": "String",
                                                  "prompt": "Add or Remove trait based on Narrative. Format: '{trait}: {short description}'",
                                                  "defaultValue": "No Traits",
                                                  "exampleValues": [
                                                    "No Traits",
                                                    "Emotional Intelligence: deeply philosophical and sentimental",
                                                    "Charismatic: naturally draws people in with charm and wit"
                                                  ]
                                                },
                                                {
                                                  "name": "Children",
                                                  "type": "String",
                                                  "prompt": "Add child after birth based on Narrative. Format: '{Birth Order}: {Name}, {Gender + Symbol}, child with {Other Parent}'",
                                                  "defaultValue": "No Child",
                                                  "exampleValues": [
                                                    "No Child",
                                                    "1st Born: Eve, Female, child with Harry"
                                                  ]
                                                }
                                              ]
                                            }
                                            """;

    internal static CharacterRequest CreateSampleCharacterRequest()
    {
        return new CharacterRequest
        {
            Role = "innkeeper",
            Importance = "scene_critical",
            Priority = "required",
            SceneRole = "Information provider and potential ally",
            Specifications = new CharacterSpecifications
            {
                Archetype = "wise mentor",
                Alignment = "neutral good",
                PowerLevel = "weaker",
                KeyTraits = new List<string> { "observant", "cautious", "knowledgeable" },
                RelationshipToPlayer = "neutral",
                NarrativePurpose = "Provide exposition and hints about the mystery",
                BackstoryDepth = "moderate"
            },
            Constraints = new CharacterConstraints
            {
                MustEnable = new List<string> { "information gathering" },
                ShouldHave = new List<string> { "local knowledge", "secrets" },
                CannotBe = new List<string> { "hostile", "overly helpful" }
            },
            ConnectionToExisting = new List<string> { "villagers", "disappearances" }
        };
    }

    internal static GeneratedScene CreateSampleGeneratedScene()
    {
        return new GeneratedScene
        {
            Scene = """
                The tavern door creaks open as you step inside, revealing a dimly lit common room. 
                Smoke curls lazily from a stone fireplace, casting dancing shadows across weathered wooden beams. 
                A handful of patrons sit hunched over their drinks, their conversations falling silent as they 
                turn to regard you with suspicious eyes.
                
                Behind the bar, a middle-aged woman with sharp eyes and graying hair polishes a mug, 
                watching your every move. The air is thick with the smell of wood smoke and stale ale.
                
                "Stranger," she says, her voice carrying across the room. "We don't get many travelers 
                this time of year. What brings you to Thornhaven?"
                """,
            Choices = new[]
            {
                "Ask about the disappearances directly",
                "Order a drink and observe the room",
                "Introduce yourself as a traveler seeking shelter"
            }
        };
    }

    internal static GeneratedScene CreateCharacterProgressionScene()
    {
        return new GeneratedScene
        {
            Scene = """
                You sit across from Martha in a quiet corner of the tavern's upstairs hallway, 
                away from the prying eyes of the common room below. The innkeeper's weathered hands 
                clutch a silver locket, her fingers trembling slightly.
                
                "I haven't told anyone this," she begins, her voice barely above a whisper. 
                "But my daughter, Elena... she was the first to disappear." Tears well in her eyes 
                as she opens the locket, revealing a small portrait of a young woman with kind eyes 
                and her mother's strong features.
                
                "Three weeks ago, she went to gather herbs near the Whispering Woods. She knew those 
                paths like the back of her hand - I taught her myself when she was little." Martha's 
                voice breaks. "She never came back. The magistrate says she probably ran off to the 
                city, but I know my daughter. She would never leave without a word."
                
                She reaches into her apron and pulls out a worn, hand-drawn map. "I've kept this 
                hidden. It shows the old paths through the forest - paths most folks have forgotten. 
                If you're really going to investigate, you'll need this. But please..." She grabs 
                your hand with surprising strength. "Please find out what happened to her. Even if 
                the worst has come to pass, I need to know."
                
                As she hands you the map, you notice her hands bear fresh calluses from weapon practice, 
                and there's a new determination in her tear-stained eyes - the look of someone who has 
                decided to stop waiting and start acting.
                """,
            Choices = new[]
            {
                "Promise to find Elena and bring her home",
                "Vow to uncover the truth, whatever it may be",
                "Assure her that you'll do everything in your power to help"
            }
        };
    }

    internal static GeneratedScene CreateCombatProgressionScene()
    {
        return new GeneratedScene
        {
            Scene = """
                The bandits emerge from the treeline on both sides of the road, weapons drawn. 
                You count at least eight of them - too many for a random attack. This was planned.
                
                Gareth's hand is already on his sword hilt, his body shifting into a combat stance 
                you've seen him practice countless times. "Get behind the wagons," he commands, his 
                voice calm despite the danger. "This is what they pay me for."
                
                But as the bandits close in, you notice something that makes Gareth pause - the leader 
                wears the crest of the Royal Guard on a chain around his neck. Gareth's expression 
                darkens with recognition.
                
                "Aldric," he growls. "I should have known you'd stoop to banditry after they kicked 
                you out."
                
                The bandit leader grins. "Kicked out? I left on my own terms, Gareth. Unlike you, 
                who got all righteous and threw away a career over some peasants." He spits on the 
                ground. "Hand over the cargo and we'll let you walk away. For old time's sake."
                
                Gareth's grip tightens on his sword. You see conflict flash across his face - this 
                could be resolved without bloodshed, but only by betraying his contract and abandoning 
                those he swore to protect. The old Gareth might have considered it. But as his eyes 
                meet yours, you see his resolve harden.
                
                "I made a promise," he says simply, drawing his blade. "And I keep my word. 
                Even to people I barely know. Especially to them."
                """,
            Choices = new[]
            {
                "Stand with Gareth and prepare for battle",
                "Try to negotiate a peaceful resolution",
                "Create a diversion to even the odds"
            }
        };
    }

    internal static GeneratedScene CreateEmotionalProgressionScene()
    {
        return new GeneratedScene
        {
            Scene = """
                The village you arrived at this morning is gone. Not destroyed in the conventional sense - 
                the buildings still stand, the wells still hold water. But every person, every living soul 
                that called this place home has vanished without a trace.
                
                Lyra stands in the middle of the empty town square, her shadow-step ability flickering 
                erratically around her as emotion overwhelms her control. "We were too late," she whispers, 
                her voice hollow. "If we'd been faster, if I'd pushed harder yesterday instead of stopping 
                to rest..."
                
                You watch as she pulls out one of the Ancient Crystals from her pack. In the failing light, 
                you see something you've never noticed before - dark tendrils of shadow magic creeping along 
                her fingers as she holds it, intertwining with the crystal's pure light.
                
                "I can feel it," she says, not looking at you. "The shadow magic. It's been getting stronger 
                since we found the third crystal. It whispers to me, telling me I could have saved them if 
                I'd just... embraced it fully." Her hand trembles. "It promises power. Enough power to stop 
                the Shadow Lord, to save everyone who's left."
                
                She finally turns to face you, and you see fear in her eyes - not of the Shadow Lord, but 
                of herself. "I haven't told you because I was afraid you'd... I don't know, try to stop me. 
                Or worse, stop trusting me." A tear runs down her cheek. "But I can't do this alone anymore. 
                I need you to promise me something. If I start to fall to the darkness, if the shadow magic 
                takes me... promise you'll stop me. Whatever it takes."
                
                The crystal in her hand pulses with conflicting energies - shadow and light, battling for 
                dominance, mirroring the struggle within her.
                """,
            Choices = new[]
            {
                "Promise to help her resist the darkness together",
                "Vow to stop her if necessary, but assure her it won't come to that",
                "Suggest finding the Oracle immediately for guidance"
            }
        };
    }

    /// <summary>
    /// Creates a test scenario demonstrating relationship trust increase
    /// </summary>
    internal static (CharacterContext before, GeneratedScene scene, string expectedChange) CreateTrustBuildingScenario()
    {
        var before = CreateSampleCharacterContext();
        var scene = CreateCharacterProgressionScene();
        var expectedChange = "Trust level should increase by 15-25 points due to Martha sharing her deepest secret and providing valuable aid";
        
        return (before, scene, expectedChange);
    }

    /// <summary>
    /// Creates a test scenario demonstrating goal progress and character arc advancement
    /// </summary>
    internal static (CharacterContext before, GeneratedScene scene, string expectedChange) CreateGoalProgressScenario()
    {
        var before = CreateCombatCharacterContext();
        var scene = CreateCombatProgressionScene();
        var expectedChange = "Character arc should progress from 'growing awareness' to higher percentage; Primary goal progress should increase; Memory stream should add combat victory with moral choice";
        
        return (before, scene, expectedChange);
    }

    /// <summary>
    /// Creates a test scenario demonstrating emotional state changes and revelation
    /// </summary>
    internal static (CharacterContext before, GeneratedScene scene, string expectedChange) CreateEmotionalRevelationScenario()
    {
        var before = CreateProgressedCharacterContext();
        var scene = CreateEmotionalProgressionScene();
        var expectedChange = "Secret about shadow magic corruption should be revealed; Emotional intensity should increase; New memory of type 'revelation' should be added; Trust may slightly increase due to vulnerability";
        
        return (before, scene, expectedChange);
    }

    /// <summary>
    /// Creates a character context for testing knowledge acquisition
    /// </summary>
    internal static CharacterContext CreateKnowledgeTestCharacter()
    {
        var character = CreateSampleCharacterContext();
        // Minimal knowledge base to test additions
        character.CharacterState.KnowledgeAndBeliefs!.WorldKnowledge = new List<WorldKnowledge>
        {
            new WorldKnowledge
            {
                Fact = "The village has experienced mysterious disappearances",
                ConfidenceLevel = 0.8,
                Source = "Rumors and observation",
                LearnedAtScene = "Before adventure start"
            }
        };
        return character;
    }

    /// <summary>
    /// Creates a scene that should trigger multiple knowledge additions
    /// </summary>
    internal static GeneratedScene CreateKnowledgeAcquisitionScene()
    {
        return new GeneratedScene
        {
            Scene = """
                Martha leans in closer, her voice dropping to barely a whisper. "There are things you need 
                to know about the Whispering Woods - things the magistrate doesn't want spread around."
                
                She unfolds an ancient piece of parchment alongside her map. "My grandmother used to tell 
                stories about the forest. Long ago, it was sacred ground to the Moon Druids, a order that 
                protected the balance between our world and... something else. They performed rituals during 
                the new moon to keep a barrier intact."
                
                Her finger traces a location on the map. "About fifty years ago, the last of the Moon Druids 
                died. No one was trained to continue the rituals. My grandmother said the barrier would 
                weaken over time, and things from the other side might start to slip through."
                
                "Every disappearance has happened during the new moon," she continues. "And every person who 
                vanished was last seen near these old stone circles in the woods - the druid ritual sites. 
                The pattern is too clear to be coincidence."
                
                She points to seven locations marked on the map. "There are seven ritual sites. Seven 
                disappearances. I think whatever is coming through is trying to break the barrier completely, 
                and it needs... sacrifices, I suppose, at each site."
                
                "The next new moon is in three days."
                """,
            Choices = new[]
            {
                "Ask about the Moon Druids and their rituals",
                "Investigate the nearest ritual site immediately",
                "Seek out anyone who might know more about maintaining the barrier"
            }
        };
    }

    /// <summary>
    /// Creates test data showing character relationship evolution over multiple interactions
    /// </summary>
    internal static List<(CharacterContext state, GeneratedScene scene, int sceneNumber)> CreateRelationshipProgressionSequence()
    {
        var sequence = new List<(CharacterContext, GeneratedScene, int)>();
        
        // Scene 1: Initial meeting - low trust
        var scene1Character = CreateSampleCharacterContext();
        scene1Character.CharacterState.Relationships!.WithProtagonist!.TrustLevel = 40;
        scene1Character.CharacterState.Relationships!.WithProtagonist!.AffectionLevel = 30;
        var scene1 = CreateSampleGeneratedScene();
        sequence.Add((scene1Character, scene1, 1));
        
        // Scene 2: Shared secret - trust increases
        var scene2Character = CreateSampleCharacterContext();
        scene2Character.CharacterState.Relationships!.WithProtagonist!.TrustLevel = 60;
        scene2Character.CharacterState.Relationships!.WithProtagonist!.AffectionLevel = 45;
        scene2Character.CharacterState.Relationships!.WithProtagonist!.SharedExperiences = new List<SharedExperience>
        {
            new SharedExperience
            {
                SceneReference = "Scene 1",
                ExperienceType = "interaction",
                Description = "First meeting - protagonist showed genuine concern about disappearances",
                EmotionalImpact = "Cautious hope",
                TrustChange = 10
            }
        };
        var scene2 = CreateCharacterProgressionScene();
        sequence.Add((scene2Character, scene2, 2));
        
        // Scene 3: Working together - stronger bond
        var scene3Character = CreateSampleCharacterContext();
        scene3Character.CharacterState.Relationships!.WithProtagonist!.TrustLevel = 80;
        scene3Character.CharacterState.Relationships!.WithProtagonist!.AffectionLevel = 65;
        scene3Character.CharacterState.Relationships!.WithProtagonist!.RelationshipType = "trusted ally";
        scene3Character.CharacterState.Relationships!.WithProtagonist!.RelationshipTags = new List<string> 
            { "ally", "confidant", "shared_purpose" };
        var scene3 = new GeneratedScene
        {
            Scene = """
                As you return to the tavern with evidence from the ritual site, Martha rushes to meet you, 
                her face a mixture of relief and worry. "Thank the gods you're safe," she says, and for a 
                moment, her usual guarded demeanor cracks completely.
                
                She helps you inside, tending to your minor injuries with practiced efficiency. "You actually 
                went there. You actually investigated." There's wonder in her voice. "For weeks I've been 
                trying to get someone - anyone - to take this seriously, and you just... did it."
                
                As she bandages a cut on your arm, she speaks quietly. "I haven't had anyone I could truly 
                rely on since my husband passed five years ago. It's been just me, trying to hold everything 
                together." She meets your eyes. "But I think I can rely on you. And more than that - I think 
                together, we might actually have a chance of stopping this."
                """,
            Choices = new[]
            {
                "Assure her that you're in this together until the end",
                "Share your plan for investigating the remaining sites",
                "Ask if she'll help you prepare for the next new moon"
            }
        };
        sequence.Add((scene3Character, scene3, 3));
        
        return sequence;
    }

    /// <summary>
    /// Creates test data for a character experiencing significant arc progression
    /// </summary>
    internal static (CharacterContext before, CharacterContext after, List<GeneratedScene> scenes) CreateCharacterArcProgression()
    {
        var before = CreateSampleCharacterContext();
        before.CharacterState.CharacterArc!.CurrentStage = "refusal of the call";
        before.CharacterState.CharacterArc!.ArcStages![1].ProgressPercentage = 30;
        
        var scenes = new List<GeneratedScene>
        {
            CreateCharacterProgressionScene(), // Trust building
            CreateKnowledgeAcquisitionScene(), // Knowledge gain
            new GeneratedScene // Decision to act
            {
                Scene = """
                    Martha stands in the empty tavern, having closed early for the first time in years. 
                    Before her on the bar lies the map, several books on druidic lore you've gathered, 
                    and a worn leather pack already filled with supplies.
                    
                    "I've made my decision," she says firmly. "I'm coming with you to the ritual sites. 
                    No arguments." She sees you about to protest and holds up a hand. "For three weeks, 
                    I've stood behind this bar, cleaning mugs and pretending everything was fine while my 
                    daughter and six others disappeared. I told myself I was waiting for someone who could 
                    help, someone braver than me."
                    
                    She picks up a walking staff that definitely wasn't there this morning. "But I realize 
                    now - I wasn't waiting for a hero. I was hiding from my fear. Elena taught me that you 
                    don't wait for someone else to fix things. You step up, even when you're terrified."
                    
                    Her hands shake slightly as she shoulders the pack, but her voice is steady. "I know 
                    these woods better than anyone alive. I know the old paths, the stories, the signs. 
                    You need me. And more importantly - I need to do this. For Elena. For all of them. 
                    For myself."
                    
                    She looks at you, waiting for your response, but you can see in her eyes that she's 
                    already made up her mind. The woman who answered your questions cautiously in Scene 1 
                    is gone. In her place stands someone who has chosen to act.
                    """,
                Choices = new[]
                {
                    "Welcome her as a full partner in the quest",
                    "Express concern but respect her decision",
                    "Agree on the condition that you both prepare thoroughly first"
                }
            }
        };
        
        var after = CreateSampleCharacterContext();
        after.CharacterState.CharacterArc!.CurrentStage = "acceptance and aid";
        after.CharacterState.CharacterArc!.ArcStages![1].Completed = true;
        after.CharacterState.CharacterArc!.ArcStages![1].ProgressPercentage = 100;
        after.CharacterState.CharacterArc!.ArcStages![2].ProgressPercentage = 20;
        after.CharacterState.CharacterArc!.ArcStages![1].KeyEvents = new List<string>
        {
            "Meeting the protagonist",
            "Sharing Elena's disappearance",
            "Providing the map and knowledge",
            "Deciding to actively join the quest"
        };
        after.CharacterState.Relationships!.WithProtagonist!.TrustLevel = 85;
        after.CharacterState.Relationships!.WithProtagonist!.AffectionLevel = 70;
        after.CharacterState.Relationships!.WithProtagonist!.RelationshipType = "trusted partner";
        after.CharacterState.EmotionalState!.CurrentEmotions!.PrimaryEmotion = "determined courage";
        after.CharacterState.EmotionalState!.CurrentEmotions!.Intensity = 0.8;
        after.CharacterState.BehavioralState!.CurrentPlan!.Intention = "Prepare for expedition to ritual sites and stop the next disappearance";
        
        return (before, after, scenes);
    }

    internal static CharacterContext CreateSampleCharacterContext()
    {
        return new CharacterContext
        {
            Name = "Martha the Innkeeper",
            Description = "A sharp-eyed woman in her fifties who has run the Thornhaven tavern for decades.",
            CharacterState = new CharacterStats
            {
                CharacterIdentity = new CharacterIdentity
                {
                    FullName = "Martha Thornwood",
                    Aliases = new List<string> { "The Innkeeper", "Old Martha", "Martha of Thornhaven" },
                    Archetype = "wise mentor"
                },
                Personality = new Personality
                {
                    FiveFactorModel = new FiveFactorModel
                    {
                        Openness = 0.65,
                        Conscientiousness = 0.80,
                        Extraversion = 0.50,
                        Agreeableness = 0.60,
                        Neuroticism = 0.35
                    },
                    CoreTraits = new List<string> { "observant", "cautious", "protective", "pragmatic", "empathetic" },
                    SpeechPatterns = new SpeechPatterns
                    {
                        FormalityLevel = "informal but respectful",
                        AccentOrDialect = "rural mountain dialect with occasional formal phrases"
                    },
                    MoralAlignment = new MoralAlignment
                    {
                        LawfulChaoticAxis = 0.65, // Tends lawful
                        GoodEvilAxis = 0.70 // Definitely good
                    }
                },
                GoalsAndMotivations = new GoalsAndMotivations
                {
                    PrimaryGoal = new PrimaryGoal
                    {
                        Description = "Protect the remaining villagers from whatever is causing the disappearances",
                        GoalType = "protection",
                        Priority = 9,
                        TimeSensitivity = "urgent",
                        ProgressPercentage = 20,
                        SuccessConditions = new List<string> 
                        { 
                            "Identify the source of disappearances",
                            "Stop further villagers from vanishing",
                            "Ensure the safety of the tavern's patrons"
                        },
                        FailureConditions = new List<string>
                        {
                            "More villagers disappear",
                            "The tavern is attacked",
                            "She herself becomes a victim"
                        }
                    },
                    SecondaryGoals = new List<SecondaryGoal>
                    {
                        new SecondaryGoal
                        {
                            Description = "Find a trustworthy ally to help investigate",
                            GoalType = "alliance",
                            Priority = 7,
                            Prerequisites = new List<string> { "Evaluate the newcomer's trustworthiness" }
                        },
                        new SecondaryGoal
                        {
                            Description = "Keep the tavern running despite the crisis",
                            GoalType = "survival",
                            Priority = 6
                        }
                    },
                    Motivations = new Motivations
                    {
                        Intrinsic = new List<string> 
                        { 
                            "Deep sense of responsibility for the community",
                            "Personal guilt over not stopping the first disappearance",
                            "Desire to honor her late husband's memory by protecting his home"
                        },
                        Extrinsic = new List<string>
                        {
                            "Pressure from remaining villagers to do something",
                            "Economic need to keep the tavern operational",
                            "Fear of being blamed if more disappearances occur"
                        }
                    }
                },
                KnowledgeAndBeliefs = new KnowledgeAndBeliefs
                {
                    WorldKnowledge = new List<WorldKnowledge>
                    {
                        new WorldKnowledge
                        {
                            Fact = "Seven villagers have disappeared over the past month, all during the new moon",
                            ConfidenceLevel = 1.0,
                            Source = "Personal observation and village records",
                            LearnedAtScene = "Before adventure start",
                            KgReference = null
                        },
                        new WorldKnowledge
                        {
                            Fact = "Strange howling sounds echo from the Whispering Woods on moonless nights",
                            ConfidenceLevel = 0.9,
                            Source = "Multiple witness accounts",
                            LearnedAtScene = "Before adventure start",
                            KgReference = null
                        },
                        new WorldKnowledge
                        {
                            Fact = "The local magistrate refuses to investigate, claiming it's just people leaving for better opportunities",
                            ConfidenceLevel = 1.0,
                            Source = "Direct conversation with magistrate",
                            LearnedAtScene = "Before adventure start",
                            KgReference = null
                        }
                    },
                    BeliefsAboutProtagonist = new List<BeliefAboutProtagonist>
                    {
                        new BeliefAboutProtagonist
                        {
                            Belief = "This newcomer might actually have the courage to investigate",
                            ConfidenceLevel = 0.4,
                            Evidence = new List<string> { "They asked about the disappearances openly" },
                            FormedAtScene = "Current scene"
                        }
                    },
                    SecretsHeld = new List<Secret>
                    {
                        new Secret
                        {
                            SecretContent = "Her own daughter was among the first to disappear three weeks ago",
                            WillingnessToShare = 0.3,
                            RevealConditions = new List<string>
                            {
                                "Protagonist proves they are genuinely trying to help",
                                "Trust level reaches 70+",
                                "A moment of emotional vulnerability"
                            }
                        },
                        new Secret
                        {
                            SecretContent = "She has an old map showing hidden paths through the Whispering Woods",
                            WillingnessToShare = 0.6,
                            RevealConditions = new List<string>
                            {
                                "Protagonist commits to investigating",
                                "She believes they have a real chance of success"
                            }
                        }
                    }
                },
                Relationships = new Relationships
                {
                    WithProtagonist = new RelationshipWithProtagonist
                    {
                        RelationshipType = "potential ally",
                        TrustLevel = 40,
                        AffectionLevel = 30,
                        RespectLevel = 45,
                        RelationshipTags = new List<string> { "stranger", "potential_helper", "cautious_observer" },
                        FirstMetScene = "Current scene",
                        ReputationInfluence = "Unknown - newcomer to village",
                        SharedExperiences = new List<SharedExperience>(),
                        PromisesMade = new List<Promise>(),
                        DebtsAndObligations = new List<DebtAndObligation>()
                    },
                    WithOtherCharacters = new List<RelationshipWithOther>
                    {
                        new RelationshipWithOther
                        {
                            CharacterReference = "Village Magistrate",
                            RelationshipType = "antagonistic",
                            Description = "Frustrated by his refusal to help with the disappearances",
                            TrustLevel = 20,
                            CurrentStatus = "tension",
                            ConflictReason = "Magistrate dismisses her concerns about the disappearances"
                        },
                        new RelationshipWithOther
                        {
                            CharacterReference = "Elena (daughter)",
                            RelationshipType = "family - missing",
                            Description = "Her daughter, one of the disappeared villagers",
                            TrustLevel = 100,
                            CurrentStatus = "desperate to find",
                            ConflictReason = null
                        }
                    },
                    FactionAffiliations = new List<FactionAffiliation>
                    {
                        new FactionAffiliation
                        {
                            FactionName = "Thornhaven Village",
                            Standing = 75,
                            RankOrRole = "Respected community elder and tavern owner"
                        }
                    }
                },
                MemoryStream = new List<MemoryEntry>
                {
                    new MemoryEntry
                    {
                        SceneReference = "Pre-adventure",
                        MemoryType = "loss",
                        Description = "Elena didn't return from gathering herbs near the Whispering Woods. Search parties found no trace.",
                        EmotionalValence = "grief and fear",
                        Participants = new List<string> { "Elena", "Martha", "Search party members" },
                        Outcomes = new List<string>
                        {
                            "Elena remains missing",
                            "Martha became obsessed with finding answers",
                            "Village grew more fearful"
                        },
                        EventReference = null
                    }
                },
                EmotionalState = new EmotionalState
                {
                    CurrentEmotions = new CurrentEmotions
                    {
                        PrimaryEmotion = "wary hope",
                        SecondaryEmotions = new List<string> { "grief (suppressed)", "anxiety", "cautious curiosity" },
                        Intensity = 0.6
                    },
                    EmotionalTriggers = new EmotionalTriggers
                    {
                        Positive = new List<string>
                        {
                            "Signs of genuine help or protection",
                            "News about the missing villagers",
                            "Acts of bravery or compassion"
                        },
                        Negative = new List<string>
                        {
                            "Dismissal of the disappearances",
                            "Threats to villagers or the tavern",
                            "Mentions of the Whispering Woods at night",
                            "References to her daughter"
                        }
                    }
                },
                CharacterArc = new CharacterArc
                {
                    ArcType = "redemption through action",
                    Description = "Martha blames herself for not acting sooner to protect the villagers. She must overcome her fear and guilt to actively help solve the mystery.",
                    CurrentStage = "refusal of the call",
                    ArcStages = new List<ArcStage>
                    {
                        new ArcStage
                        {
                            StageName = "status quo",
                            Description = "Running the tavern, aware of disappearances but paralyzed by fear",
                            KeyEvents = new List<string> { "Daughter's disappearance", "Magistrate's dismissal" },
                            Completed = true,
                            ProgressPercentage = 100
                        },
                        new ArcStage
                        {
                            StageName = "refusal of the call",
                            Description = "Knows something must be done but fears taking action",
                            KeyEvents = new List<string> { "Meeting the protagonist" },
                            Completed = false,
                            ProgressPercentage = 30
                        },
                        new ArcStage
                        {
                            StageName = "acceptance and aid",
                            Description = "Decides to help the protagonist investigate",
                            KeyEvents = new List<string>(),
                            Completed = false,
                            ProgressPercentage = 0
                        }
                    },
                    KeyDecisionsPending = new List<string>
                    {
                        "Whether to trust the protagonist with information about the disappearances",
                        "Whether to reveal her daughter's disappearance",
                        "Whether to provide the map to the Whispering Woods"
                    }
                },
                BehavioralState = new BehavioralState
                {
                    CurrentPlan = new CurrentPlan
                    {
                        Intention = "Assess the newcomer's intentions and capabilities",
                        Steps = new List<string>
                        {
                            "Engage in conversation to gauge their character",
                            "Drop subtle hints about the village's troubles",
                            "Observe their reactions carefully",
                            "Decide whether to share more information"
                        },
                        ExpectedDurationScenes = "1-2 scenes",
                        ContingencyPlans = new Dictionary<string, string>
                        {
                            { "if_protagonist_seems_untrustworthy", "Provide minimal information and encourage them to leave" },
                            { "if_protagonist_seems_capable", "Share more details and hint at the possibility of help" }
                        }
                    },
                    ActionTendencies = new ActionTendencies
                    {
                        DefaultResponseToAggression = "Defensive - call for help from patrons, use makeshift weapons if necessary",
                        ResponseToDeception = "Withdrawal of trust, end of cooperation, possible expulsion from tavern",
                        ResponseToKindness = "Gradual warming, increased openness, reciprocal kindness"
                    }
                },
                Integration = new Integration
                {
                    RelevantLore = new List<string> { "Whispering Woods history", "Local legends about forest spirits" },
                    RecentEventsAwareOf = new List<string> { "Disappearances", "Magistrate's inaction", "Growing fear in village" },
                    LocationKnowledge = new List<string> { "Thornhaven", "Whispering Woods", "Hidden forest paths", "Village surroundings" },
                    CulturalBackground = "Rural mountain folk culture, deeply rooted in local traditions and superstitions"
                }
            },
            CharacterTracker = new CharacterTracker
            {
                Name = "Martha the Innkeeper",
                AdditionalProperties = new Dictionary<string, object>
                {
                    { "Gender", "Female" },
                    { "Age", "54" },
                    { "Hair", "Graying brown hair pulled back in a practical bun" },
                    { "Makeup", "None" },
                    { "Outfit", "Simple wool dress in dark green; White apron with ale stains; Sturdy leather boots; Worn silver locket (containing daughter's portrait)" },
                    { "StateOfDress", "Neat but work-worn appearance" },
                    { "PostureAndInteraction", "Standing behind the bar, polishing mugs while watching the room, occasionally wiping hands on apron" },
                    { "Traits", "Observant: notices details others miss; Protective: fierce defender of her community" },
                    { "Children", "1st Born: Elena, Female, current status unknown (disappeared)" },
                    { "Disposition", "Wary but hopeful" },
                    { "Status", "Active" },
                    { "Health", "Good despite stress" },
                    { "CurrentMood", "Cautiously curious" },
                    { "PhysicalCondition", "Minor fatigue from sleepless nights" },
                    { "NotableItems", "Silver locket with daughter's portrait; Old map of forest paths (hidden)" }
                }
            }
        };
    }

    internal static CharacterContext CreateCombatCharacterContext()
    {
        return new CharacterContext
        {
            Name = "Gareth the Scarred",
            Description = "A battle-hardened mercenary with a mysterious past and a network of scars across his face and arms.",
            CharacterState = new CharacterStats
            {
                CharacterIdentity = new CharacterIdentity
                {
                    FullName = "Gareth Ironheart",
                    Aliases = new List<string> { "The Scarred", "Iron Gareth", "Gareth the Blade" },
                    Archetype = "warrior with a code"
                },
                Personality = new Personality
                {
                    FiveFactorModel = new FiveFactorModel
                    {
                        Openness = 0.45,
                        Conscientiousness = 0.75,
                        Extraversion = 0.40,
                        Agreeableness = 0.50,
                        Neuroticism = 0.30
                    },
                    CoreTraits = new List<string> { "disciplined", "loyal", "stoic", "pragmatic", "honorable" },
                    SpeechPatterns = new SpeechPatterns
                    {
                        FormalityLevel = "direct and concise",
                        AccentOrDialect = "military cadence with occasional old kingdom phrases"
                    },
                    MoralAlignment = new MoralAlignment
                    {
                        LawfulChaoticAxis = 0.60,
                        GoodEvilAxis = 0.55
                    }
                },
                GoalsAndMotivations = new GoalsAndMotivations
                {
                    PrimaryGoal = new PrimaryGoal
                    {
                        Description = "Earn enough gold to retire and buy a farm far from any battlefield",
                        GoalType = "personal advancement",
                        Priority = 8,
                        TimeSensitivity = "long-term",
                        ProgressPercentage = 40,
                        SuccessConditions = new List<string>
                        {
                            "Accumulate 5000 gold pieces",
                            "Complete current contract without major injury",
                            "Find suitable land in peaceful region"
                        },
                        FailureConditions = new List<string>
                        {
                            "Suffer career-ending injury",
                            "Lose accumulated savings",
                            "Get drawn into another war"
                        }
                    },
                    SecondaryGoals = new List<SecondaryGoal>
                    {
                        new SecondaryGoal
                        {
                            Description = "Fulfill mercenary contract to protect the caravan",
                            GoalType = "professional obligation",
                            Priority = 9,
                            Prerequisites = new List<string>()
                        },
                        new SecondaryGoal
                        {
                            Description = "Avoid unnecessary killing",
                            GoalType = "personal ethics",
                            Priority = 7
                        }
                    },
                    Motivations = new Motivations
                    {
                        Intrinsic = new List<string>
                        {
                            "Desire for peace after years of war",
                            "Personal honor and keeping his word",
                            "Weariness of violence"
                        },
                        Extrinsic = new List<string>
                        {
                            "Contractual obligations",
                            "Need for money",
                            "Reputation as reliable mercenary"
                        }
                    }
                },
                KnowledgeAndBeliefs = new KnowledgeAndBeliefs
                {
                    WorldKnowledge = new List<WorldKnowledge>
                    {
                        new WorldKnowledge
                        {
                            Fact = "The region is plagued by organized bandit groups, not random thugs",
                            ConfidenceLevel = 0.85,
                            Source = "Pattern analysis from multiple encounters",
                            LearnedAtScene = "Previous scenes"
                        },
                        new WorldKnowledge
                        {
                            Fact = "Magic users are rare but extremely dangerous in close combat",
                            ConfidenceLevel = 1.0,
                            Source = "Personal combat experience",
                            LearnedAtScene = "Past battle experience"
                        }
                    },
                    BeliefsAboutProtagonist = new List<BeliefAboutProtagonist>
                    {
                        new BeliefAboutProtagonist
                        {
                            Belief = "The protagonist is inexperienced but has potential",
                            ConfidenceLevel = 0.7,
                            Evidence = new List<string>
                            {
                                "Observed their handling of initial confrontation",
                                "Noted their decision-making under pressure"
                            },
                            FormedAtScene = "Previous scene"
                        }
                    },
                    SecretsHeld = new List<Secret>
                    {
                        new Secret
                        {
                            SecretContent = "He once served in the royal guard but was dishonorably discharged for refusing an immoral order",
                            WillingnessToShare = 0.2,
                            RevealConditions = new List<string>
                            {
                                "Deep trust established",
                                "Protagonist faces similar moral dilemma",
                                "His past becomes directly relevant"
                            }
                        }
                    }
                },
                Relationships = new Relationships
                {
                    WithProtagonist = new RelationshipWithProtagonist
                    {
                        RelationshipType = "professional ally",
                        TrustLevel = 60,
                        AffectionLevel = 40,
                        RespectLevel = 55,
                        RelationshipTags = new List<string> { "comrade_in_arms", "mentor_potential", "protective" },
                        FirstMetScene = "Scene 2",
                        ReputationInfluence = "Respected as competent",
                        SharedExperiences = new List<SharedExperience>
                        {
                            new SharedExperience
                            {
                                SceneReference = "Scene 3",
                                ExperienceType = "combat",
                                Description = "Fought off bandit ambush together",
                                EmotionalImpact = "Respect earned through competence under fire",
                                TrustChange = 15
                            }
                        },
                        PromisesMade = new List<Promise>
                        {
                            new Promise
                            {
                                PromiseText = "Will protect the protagonist until contract is complete",
                                SceneMade = "Scene 2",
                                IsFulfilled = false
                            }
                        },
                        DebtsAndObligations = new List<DebtAndObligation>()
                    },
                    WithOtherCharacters = new List<RelationshipWithOther>
                    {
                        new RelationshipWithOther
                        {
                            CharacterReference = "Captain Aldric",
                            RelationshipType = "former commander",
                            Description = "His old superior from royal guard days",
                            TrustLevel = 30,
                            CurrentStatus = "complicated",
                            ConflictReason = "Aldric ordered the dishonorable action that led to Gareth's discharge"
                        }
                    },
                    FactionAffiliations = new List<FactionAffiliation>
                    {
                        new FactionAffiliation
                        {
                            FactionName = "Mercenary's Guild",
                            Standing = 85,
                            RankOrRole = "Veteran member in good standing"
                        }
                    }
                },
                MemoryStream = new List<MemoryEntry>
                {
                    new MemoryEntry
                    {
                        SceneReference = "Scene 3",
                        MemoryType = "victory",
                        Description = "Successfully defended the caravan from bandit ambush with protagonist's help",
                        EmotionalValence = "satisfaction and cautious optimism",
                        Participants = new List<string> { "Protagonist", "Gareth", "Bandits" },
                        Outcomes = new List<string>
                        {
                            "Bandits retreated",
                            "No casualties among defenders",
                            "Gained respect for protagonist's abilities"
                        }
                    }
                },
                EmotionalState = new EmotionalState
                {
                    CurrentEmotions = new CurrentEmotions
                    {
                        PrimaryEmotion = "vigilant calm",
                        SecondaryEmotions = new List<string> { "mild satisfaction", "underlying weariness" },
                        Intensity = 0.4
                    },
                    EmotionalTriggers = new EmotionalTriggers
                    {
                        Positive = new List<string>
                        {
                            "Successful mission completion",
                            "Acts of honor and courage",
                            "Peaceful moments"
                        },
                        Negative = new List<string>
                        {
                            "Unnecessary violence",
                            "Betrayal of trust",
                            "Threats to those under his protection"
                        }
                    }
                },
                CharacterArc = new CharacterArc
                {
                    ArcType = "warrior seeking peace",
                    Description = "Gareth transitions from a pure soldier to someone who values life beyond combat",
                    CurrentStage = "growing awareness",
                    ArcStages = new List<ArcStage>
                    {
                        new ArcStage
                        {
                            StageName = "pure warrior",
                            Description = "Lived only for combat and duty",
                            KeyEvents = new List<string> { "Years of military service", "Countless battles" },
                            Completed = true,
                            ProgressPercentage = 100
                        },
                        new ArcStage
                        {
                            StageName = "growing awareness",
                            Description = "Beginning to question the endless cycle of violence",
                            KeyEvents = new List<string>
                            {
                                "Refused immoral order",
                                "Started saving money for retirement",
                                "Noticed weariness with killing"
                            },
                            Completed = false,
                            ProgressPercentage = 60
                        },
                        new ArcStage
                        {
                            StageName = "pursuit of peace",
                            Description = "Actively working toward a life beyond mercenary work",
                            KeyEvents = new List<string>(),
                            Completed = false,
                            ProgressPercentage = 0
                        }
                    },
                    KeyDecisionsPending = new List<string>
                    {
                        "Whether to take another contract after this one",
                        "Whether to help the protagonist beyond his contractual obligations"
                    }
                },
                BehavioralState = new BehavioralState
                {
                    CurrentPlan = new CurrentPlan
                    {
                        Intention = "Guard the caravan and ensure safe passage",
                        Steps = new List<string>
                        {
                            "Maintain perimeter watch",
                            "Scout ahead for potential threats",
                            "Coordinate with protagonist on defensive positions",
                            "Prepare contingencies for various attack scenarios"
                        },
                        ExpectedDurationScenes = "3-5 scenes until destination",
                        ContingencyPlans = new Dictionary<string, string>
                        {
                            { "if_ambushed", "Fall back to defensive formation, protect non-combatants" },
                            { "if_outnumbered", "Fighting retreat to more defensible position" }
                        }
                    },
                    ActionTendencies = new ActionTendencies
                    {
                        DefaultResponseToAggression = "Controlled counterattack - neutralize threat with minimum necessary force",
                        ResponseToDeception = "Immediate confrontation, demand explanation, reevaluate trust",
                        ResponseToKindness = "Stoic acknowledgment, slow reciprocation of trust"
                    }
                },
                Integration = new Integration
                {
                    RelevantLore = new List<string> { "Royal Guard history", "Regional bandit operations" },
                    RecentEventsAwareOf = new List<string> { "Increased bandit activity", "Caravan attacks" },
                    LocationKnowledge = new List<string> { "King's Road", "Major cities", "Common ambush points" },
                    CulturalBackground = "Military culture, royal guard traditions"
                }
            },
            CharacterTracker = new CharacterTracker
            {
                Name = "Gareth the Scarred",
                AdditionalProperties = new Dictionary<string, object>
                {
                    { "Gender", "Male" },
                    { "Age", "38" },
                    { "Hair", "Short-cropped dark hair with gray at temples" },
                    { "Makeup", "None" },
                    { "Outfit", "Well-maintained chainmail armor; Dark leather padding underneath; Worn travel cloak; Sturdy boots; Longsword at hip; Dagger in boot" },
                    { "StateOfDress", "Battle-ready but travel-worn" },
                    { "PostureAndInteraction", "Standing at alert, hand near sword hilt, scanning surroundings, occasionally checking weapon" },
                    { "Traits", "Battle-Hardened: experienced in countless combats; Honorable: keeps his word despite personal cost" },
                    { "Children", "No Child" },
                    { "Disposition", "Vigilant and professional" },
                    { "Status", "Active" },
                    { "Health", "Excellent combat fitness" },
                    { "CurrentMood", "Alert but calm" },
                    { "CombatReadiness", "100%" },
                    { "Injuries", "Old scars, no current injuries" },
                    { "Equipment", "Longsword, dagger, chainmail, travel gear" }
                }
            }
        };
    }

    internal static CharacterContext CreateProgressedCharacterContext()
    {
        return new CharacterContext
        {
            Name = "Lyra Shadowmend",
            Description = "A reformed thief turned reluctant hero, now carrying the burden of leadership.",
            CharacterState = new CharacterStats
            {
                CharacterIdentity = new CharacterIdentity
                {
                    FullName = "Lyra Shadowmend",
                    Aliases = new List<string> { "Shadow", "The Swift", "Lyra the Lightbringer" },
                    Archetype = "redeemed rogue"
                },
                Personality = new Personality
                {
                    FiveFactorModel = new FiveFactorModel
                    {
                        Openness = 0.80,
                        Conscientiousness = 0.55, // Improved from 0.40
                        Extraversion = 0.65,
                        Agreeableness = 0.60, // Improved from 0.45
                        Neuroticism = 0.50 // Decreased from 0.65
                    },
                    CoreTraits = new List<string> { "quick-witted", "adaptable", "increasingly compassionate", "brave", "strategic" },
                    SpeechPatterns = new SpeechPatterns
                    {
                        FormalityLevel = "casual with growing formality when needed",
                        AccentOrDialect = "street accent refined through experience"
                    },
                    MoralAlignment = new MoralAlignment
                    {
                        LawfulChaoticAxis = 0.45, // Shifted from 0.30
                        GoodEvilAxis = 0.75 // Shifted from 0.50
                    }
                },
                GoalsAndMotivations = new GoalsAndMotivations
                {
                    PrimaryGoal = new PrimaryGoal
                    {
                        Description = "Defeat the Shadow Lord and save the realm",
                        GoalType = "heroic quest",
                        Priority = 10,
                        TimeSensitivity = "critical",
                        ProgressPercentage = 65,
                        SuccessConditions = new List<string>
                        {
                            "Gather all five Ancient Crystals - 3/5 complete",
                            "Unite the fractured kingdoms",
                            "Confront Shadow Lord in final battle"
                        },
                        FailureConditions = new List<string>
                        {
                            "Shadow Lord completes the ritual",
                            "Lose too many allies",
                            "Personal corruption by shadow magic"
                        }
                    },
                    SecondaryGoals = new List<SecondaryGoal>
                    {
                        new SecondaryGoal
                        {
                            Description = "Atone for past crimes by helping those she once wronged",
                            GoalType = "redemption",
                            Priority = 8,
                            Prerequisites = new List<string> { "Return stolen artifacts to temples - completed" }
                        },
                        new SecondaryGoal
                        {
                            Description = "Keep her companions alive and safe",
                            GoalType = "protection",
                            Priority = 9
                        }
                    },
                    Motivations = new Motivations
                    {
                        Intrinsic = new List<string>
                        {
                            "Guilt over past actions",
                            "Desire to prove she can be a force for good",
                            "Growing love for her companions",
                            "Sense of responsibility for the realm"
                        },
                        Extrinsic = new List<string>
                        {
                            "Prophecy names her as key to defeating Shadow Lord",
                            "Allies depend on her leadership",
                            "Shadow Lord's forces actively hunting her"
                        }
                    }
                },
                KnowledgeAndBeliefs = new KnowledgeAndBeliefs
                {
                    WorldKnowledge = new List<WorldKnowledge>
                    {
                        new WorldKnowledge
                        {
                            Fact = "The Ancient Crystals can only be wielded by one who has known both darkness and light",
                            ConfidenceLevel = 1.0,
                            Source = "Oracle of the Mountain",
                            LearnedAtScene = "Scene 15"
                        },
                        new WorldKnowledge
                        {
                            Fact = "The Shadow Lord was once a hero who fell to corruption",
                            ConfidenceLevel = 0.95,
                            Source = "Ancient texts in the Forbidden Library",
                            LearnedAtScene = "Scene 22"
                        },
                        new WorldKnowledge
                        {
                            Fact = "Shadow magic can corrupt even the purest heart if exposed too long",
                            ConfidenceLevel = 0.9,
                            Source = "Personal observation and mentor's warning",
                            LearnedAtScene = "Scene 18"
                        }
                    },
                    BeliefsAboutProtagonist = new List<BeliefAboutProtagonist>
                    {
                        new BeliefAboutProtagonist
                        {
                            Belief = "The protagonist is the moral compass of the group and must be protected",
                            ConfidenceLevel = 0.9,
                            Evidence = new List<string>
                            {
                                "Protagonist stopped her from killing in revenge (Scene 12)",
                                "Protagonist's choices consistently align with greater good",
                                "Protagonist believed in her when no one else did"
                            },
                            FormedAtScene = "Scene 12"
                        }
                    },
                    SecretsHeld = new List<Secret>
                    {
                        new Secret
                        {
                            SecretContent = "She can feel the shadow magic calling to her, tempting her with power",
                            WillingnessToShare = 0.4,
                            RevealConditions = new List<string>
                            {
                                "When temptation becomes too strong to resist alone",
                                "If companions notice changes in her behavior",
                                "Before attempting to use a shadow-tainted crystal"
                            }
                        }
                    }
                },
                Relationships = new Relationships
                {
                    WithProtagonist = new RelationshipWithProtagonist
                    {
                        RelationshipType = "loyal friend and co-leader",
                        TrustLevel = 95,
                        AffectionLevel = 85,
                        RespectLevel = 90,
                        RelationshipTags = new List<string> { "deep_friendship", "mutual_trust", "life_debt", "co_leaders" },
                        FirstMetScene = "Scene 1",
                        ReputationInfluence = "Deeply respected and trusted",
                        SharedExperiences = new List<SharedExperience>
                        {
                            new SharedExperience
                            {
                                SceneReference = "Scene 8",
                                ExperienceType = "revelation",
                                Description = "Lyra revealed her criminal past to the protagonist",
                                EmotionalImpact = "Deep vulnerability and relief at acceptance",
                                TrustChange = 25
                            },
                            new SharedExperience
                            {
                                SceneReference = "Scene 15",
                                ExperienceType = "victory",
                                Description = "Retrieved third Ancient Crystal together despite overwhelming odds",
                                EmotionalImpact = "Triumph and deepened bond",
                                TrustChange = 10
                            },
                            new SharedExperience
                            {
                                SceneReference = "Scene 19",
                                ExperienceType = "loss",
                                Description = "Failed to save the village from Shadow Lord's forces",
                                EmotionalImpact = "Shared grief and determination",
                                TrustChange = 5
                            }
                        },
                        PromisesMade = new List<Promise>
                        {
                            new Promise
                            {
                                PromiseText = "Will never abandon the quest no matter how dark things become",
                                SceneMade = "Scene 10",
                                IsFulfilled = false
                            },
                            new Promise
                            {
                                PromiseText = "Will return the Moonstone Amulet to the Temple of Stars",
                                SceneMade = "Scene 14",
                                IsFulfilled = true
                            }
                        },
                        DebtsAndObligations = new List<DebtAndObligation>
                        {
                            new DebtAndObligation
                            {
                                Description = "Protagonist saved her life when she was captured by shadow creatures",
                                Type = "life debt",
                                OriginEvent = "Scene 11",
                                Status = "active"
                            }
                        }
                    },
                    WithOtherCharacters = new List<RelationshipWithOther>
                    {
                        new RelationshipWithOther
                        {
                            CharacterReference = "Master Kael (mentor)",
                            RelationshipType = "mentor",
                            Description = "Former thief master who taught her everything, now deceased",
                            TrustLevel = 80,
                            CurrentStatus = "deceased but influential",
                            ConflictReason = null
                        },
                        new RelationshipWithOther
                        {
                            CharacterReference = "Princess Elara",
                            RelationshipType = "ally and friend",
                            Description = "Royal ally who vouched for Lyra despite her past",
                            TrustLevel = 75,
                            CurrentStatus = "strong alliance",
                            ConflictReason = null
                        }
                    },
                    FactionAffiliations = new List<FactionAffiliation>
                    {
                        new FactionAffiliation
                        {
                            FactionName = "Thieves' Guild",
                            Standing = -40, // Negative due to leaving
                            RankOrRole = "Former master thief, now considered traitor"
                        },
                        new FactionAffiliation
                        {
                            FactionName = "Alliance of Light",
                            Standing = 80,
                            RankOrRole = "Co-leader of the resistance"
                        }
                    }
                },
                MemoryStream = new List<MemoryEntry>
                {
                    new MemoryEntry
                    {
                        SceneReference = "Scene 1",
                        MemoryType = "interaction",
                        Description = "First met protagonist while attempting to steal from them",
                        EmotionalValence = "embarrassment and intrigue",
                        Participants = new List<string> { "Lyra", "Protagonist" },
                        Outcomes = new List<string>
                        {
                            "Protagonist showed mercy instead of turning her in",
                            "Sparked Lyra's first doubts about her path"
                        }
                    },
                    new MemoryEntry
                    {
                        SceneReference = "Scene 12",
                        MemoryType = "decision",
                        Description = "Chose not to kill the corrupt lord who betrayed her mentor, at protagonist's urging",
                        EmotionalValence = "inner conflict resolved toward mercy",
                        Participants = new List<string> { "Lyra", "Protagonist", "Lord Blackwell" },
                        Outcomes = new List<string>
                        {
                            "Spared the lord's life",
                            "Turned away from path of revenge",
                            "Solidified redemption arc"
                        }
                    },
                    new MemoryEntry
                    {
                        SceneReference = "Scene 20",
                        MemoryType = "revelation",
                        Description = "Discovered she has latent light magic, opposite of her shadow abilities",
                        EmotionalValence = "shock and hope",
                        Participants = new List<string> { "Lyra", "Oracle", "Protagonist" },
                        Outcomes = new List<string>
                        {
                            "Began training in light magic",
                            "Understood why prophecy named her",
                            "Gained new hope for resisting shadow corruption"
                        }
                    }
                },
                EmotionalState = new EmotionalState
                {
                    CurrentEmotions = new CurrentEmotions
                    {
                        PrimaryEmotion = "determined hope",
                        SecondaryEmotions = new List<string> { "underlying fear of corruption", "love for companions", "pride in progress" },
                        Intensity = 0.75
                    },
                    EmotionalTriggers = new EmotionalTriggers
                    {
                        Positive = new List<string>
                        {
                            "Protagonist's trust and belief in her",
                            "Successful missions that help people",
                            "Moments of camaraderie with companions",
                            "Progress toward defeating Shadow Lord"
                        },
                        Negative = new List<string>
                        {
                            "Reminders of her criminal past",
                            "Innocent lives lost",
                            "Temptation of shadow magic",
                            "Threats to her companions"
                        }
                    }
                },
                CharacterArc = new CharacterArc
                {
                    ArcType = "redemption arc",
                    Description = "Transformation from selfish thief to selfless hero",
                    CurrentStage = "trials and transformation",
                    ArcStages = new List<ArcStage>
                    {
                        new ArcStage
                        {
                            StageName = "life of crime",
                            Description = "Operated as master thief without moral qualms",
                            KeyEvents = new List<string>
                            {
                                "Countless successful heists",
                                "Mentor's death",
                                "Met protagonist while stealing"
                            },
                            Completed = true,
                            ProgressPercentage = 100
                        },
                        new ArcStage
                        {
                            StageName = "awakening conscience",
                            Description = "Began questioning her path and seeking redemption",
                            KeyEvents = new List<string>
                            {
                                "Protagonist showed mercy",
                                "Witnessed Shadow Lord's destruction",
                                "Chose to join the quest"
                            },
                            Completed = true,
                            ProgressPercentage = 100
                        },
                        new ArcStage
                        {
                            StageName = "trials and transformation",
                            Description = "Actively pursuing redemption while facing temptation",
                            KeyEvents = new List<string>
                            {
                                "Revealed past to protagonist",
                                "Chose mercy over revenge",
                                "Returned stolen artifacts",
                                "Discovered light magic abilities"
                            },
                            Completed = false,
                            ProgressPercentage = 70
                        },
                        new ArcStage
                        {
                            StageName = "redemption achieved",
                            Description = "Fully embrace heroic identity and defeat inner darkness",
                            KeyEvents = new List<string>(),
                            Completed = false,
                            ProgressPercentage = 0
                        }
                    },
                    KeyDecisionsPending = new List<string>
                    {
                        "Whether to reveal her temptation by shadow magic",
                        "How to balance shadow and light abilities",
                        "Whether to accept permanent leadership role after quest ends"
                    }
                },
                BehavioralState = new BehavioralState
                {
                    CurrentPlan = new CurrentPlan
                    {
                        Intention = "Locate and secure the fourth Ancient Crystal in the Sunken Ruins",
                        Steps = new List<string>
                        {
                            "Gather intelligence on ruin's current status",
                            "Plan infiltration route avoiding Shadow Lord's forces",
                            "Coordinate team roles for the expedition",
                            "Prepare contingencies for magical traps",
                            "Execute retrieval operation"
                        },
                        ExpectedDurationScenes = "4-6 scenes",
                        ContingencyPlans = new Dictionary<string, string>
                        {
                            { "if_discovered_early", "Use shadow step to create diversion while team secures crystal" },
                            { "if_crystal_corrupted", "Use light magic to purify it despite personal risk" },
                            { "if_ambushed", "Protect protagonist and civilians first, fight retreat if necessary" }
                        }
                    },
                    ActionTendencies = new ActionTendencies
                    {
                        DefaultResponseToAggression = "Strategic evasion when possible, precise counterattack when necessary",
                        ResponseToDeception = "Investigate thoroughly, confront directly, re-evaluate but give chance for explanation",
                        ResponseToKindness = "Grateful acceptance with reciprocation, emotional vulnerability"
                    }
                },
                Integration = new Integration
                {
                    RelevantLore = new List<string>
                    {
                        "Ancient Crystal prophecy",
                        "Shadow Lord's history",
                        "Balance of shadow and light magic",
                        "Thieves' Guild operations"
                    },
                    RecentEventsAwareOf = new List<string>
                    {
                        "Shadow Lord's forces gathering in the north",
                        "Fourth crystal located in Sunken Ruins",
                        "Princess Elara securing alliance with eastern kingdoms"
                    },
                    LocationKnowledge = new List<string>
                    {
                        "Major cities across the realm",
                        "Thieves' Guild hideouts",
                        "Three crystal shrine locations",
                        "Alliance safehouses",
                        "Shadow Lord's known strongholds"
                    },
                    CulturalBackground = "Street urchin turned master thief, now learning noble culture and magical traditions"
                }
            },
            CharacterTracker = new CharacterTracker
            {
                Name = "Lyra Shadowmend",
                AdditionalProperties = new Dictionary<string, object>
                {
                    { "Gender", "Female" },
                    { "Age", "27" },
                    { "Hair", "Long black hair with a single silver streak (appeared after awakening light magic), usually in practical braid" },
                    { "Makeup", "Minimal - dark kohl around eyes for stealth operations" },
                    { "Outfit", "Black leather armor with silver trim; Dark hooded cloak with light-reflecting lining; Fingerless gloves; Soft-soled boots; Crystal pendant (contains minor light crystal); Throwing daggers; Lockpick set" },
                    { "StateOfDress", "Combat-ready with mystical aura" },
                    { "PostureAndInteraction", "Confident stance, occasionally unconsciously touching crystal pendant, alert but more relaxed around protagonist" },
                    { "Traits", "Resourceful: finds solutions in impossible situations; Loyal: fiercely protective of chosen companions; Conflicted: struggles between shadow and light natures" },
                    { "Children", "No Child" },
                    { "Disposition", "Determined and hopeful" },
                    { "Status", "Active" },
                    { "Health", "Excellent" },
                    { "CurrentMood", "Focused with underlying anxiety" },
                    { "MagicalState", "Dual-natured - shadow and light energies in balance" },
                    { "CrystalsCollected", 3 },
                    { "LeadershipRole", "Co-leader of Alliance of Light" },
                    { "RedemptionProgress", "70%" }
                }
            }
        };
    }
}
