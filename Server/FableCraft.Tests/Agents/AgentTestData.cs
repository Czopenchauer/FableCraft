using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

namespace FableCraft.Tests.Agents;

internal static class AgentTestData
{
    internal static GenerationContext CreateSampleNarrativeContext(Guid adventureId, ApplicationDbContext dbContext)
    {
        var adventure = dbContext.Adventures.Single(a => a.Id == adventureId);

        return new GenerationContext
        {
            AdventureId = adventureId,
            PlayerAction = "I enter the tavern and look around for anyone who might have information.",
            Summary = "A young adventurer seeks to uncover the mystery of disappearing villagers in a remote mountain town.",
            SceneContext = Array.Empty<SceneContext>(),
            Characters = new List<CharacterContext>(),
            TrackerStructure = adventure.TrackerStructure,
            MainCharacter = adventure.MainCharacter,
            NewLocations = Array.Empty<LocationGenerationResult>(),
            NewLore = Array.Empty<GeneratedLore>(),
            GenerationProcessStep = GenerationProcessStep.NotStarted
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
            RagProcessingStatus = ProcessingStatus.Pending,
            SceneGenerationStatus = ProcessingStatus.Pending,
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
                    Aliases = new List<string>
                    {
                        "The Innkeeper",
                        "Old Martha",
                        "Martha of Thornhaven"
                    },
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
                    CoreTraits = new List<string>
                    {
                        "observant",
                        "cautious",
                        "protective",
                        "pragmatic",
                        "empathetic"
                    },
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
                        new()
                        {
                            Description = "Find a trustworthy ally to help investigate",
                            GoalType = "alliance",
                            Priority = 7,
                            Prerequisites = new List<string>
                            {
                                "Evaluate the newcomer's trustworthiness"
                            }
                        },
                        new()
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
                        new()
                        {
                            Fact = "Seven villagers have disappeared over the past month, all during the new moon",
                            ConfidenceLevel = 1.0,
                            Source = "Personal observation and village records",
                            LearnedAtScene = "Before adventure start",
                            KgReference = null
                        },
                        new()
                        {
                            Fact = "Strange howling sounds echo from the Whispering Woods on moonless nights",
                            ConfidenceLevel = 0.9,
                            Source = "Multiple witness accounts",
                            LearnedAtScene = "Before adventure start",
                            KgReference = null
                        },
                        new()
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
                        new()
                        {
                            Belief = "This newcomer might actually have the courage to investigate",
                            ConfidenceLevel = 0.4,
                            Evidence = new List<string>
                            {
                                "They asked about the disappearances openly"
                            },
                            FormedAtScene = "Current scene"
                        }
                    },
                    SecretsHeld = new List<Secret>
                    {
                        new()
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
                        new()
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
                        RelationshipTags = new List<string>
                        {
                            "stranger",
                            "potential_helper",
                            "cautious_observer"
                        },
                        FirstMetScene = "Current scene",
                        ReputationInfluence = "Unknown - newcomer to village",
                        SharedExperiences = new List<SharedExperience>(),
                        PromisesMade = new List<Promise>(),
                        DebtsAndObligations = new List<DebtAndObligation>()
                    },
                    WithOtherCharacters = new List<RelationshipWithOther>
                    {
                        new()
                        {
                            CharacterReference = "Village Magistrate",
                            RelationshipType = "antagonistic",
                            Description = "Frustrated by his refusal to help with the disappearances",
                            TrustLevel = 20,
                            CurrentStatus = "tension",
                            ConflictReason = "Magistrate dismisses her concerns about the disappearances"
                        },
                        new()
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
                        new()
                        {
                            FactionName = "Thornhaven Village",
                            Standing = 75,
                            RankOrRole = "Respected community elder and tavern owner"
                        }
                    }
                },
                MemoryStream = new List<MemoryEntry>
                {
                    new()
                    {
                        SceneReference = "Pre-adventure",
                        MemoryType = "loss",
                        Description = "Elena didn't return from gathering herbs near the Whispering Woods. Search parties found no trace.",
                        EmotionalValence = "grief and fear",
                        Participants = new List<string>
                        {
                            "Elena",
                            "Martha",
                            "Search party members"
                        },
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
                        SecondaryEmotions = new List<string>
                        {
                            "grief (suppressed)",
                            "anxiety",
                            "cautious curiosity"
                        },
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
                    Description =
                        "Martha blames herself for not acting sooner to protect the villagers. She must overcome her fear and guilt to actively help solve the mystery.",
                    CurrentStage = "refusal of the call",
                    ArcStages = new List<ArcStage>
                    {
                        new()
                        {
                            StageName = "status quo",
                            Description = "Running the tavern, aware of disappearances but paralyzed by fear",
                            KeyEvents = new List<string>
                            {
                                "Daughter's disappearance",
                                "Magistrate's dismissal"
                            },
                            Completed = true,
                            ProgressPercentage = 100
                        },
                        new()
                        {
                            StageName = "refusal of the call",
                            Description = "Knows something must be done but fears taking action",
                            KeyEvents = new List<string>
                            {
                                "Meeting the protagonist"
                            },
                            Completed = false,
                            ProgressPercentage = 30
                        },
                        new()
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
                            {
                                "if_protagonist_seems_untrustworthy", "Provide minimal information and encourage them to leave"
                            },
                            {
                                "if_protagonist_seems_capable", "Share more details and hint at the possibility of help"
                            }
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
                    RelevantLore = new List<string>
                    {
                        "Whispering Woods history",
                        "Local legends about forest spirits"
                    },
                    RecentEventsAwareOf = new List<string>
                    {
                        "Disappearances",
                        "Magistrate's inaction",
                        "Growing fear in village"
                    },
                    LocationKnowledge = new List<string>
                    {
                        "Thornhaven",
                        "Whispering Woods",
                        "Hidden forest paths",
                        "Village surroundings"
                    },
                    CulturalBackground = "Rural mountain folk culture, deeply rooted in local traditions and superstitions"
                }
            },
            CharacterTracker = new CharacterTracker
            {
                Name = "Martha the Innkeeper",
                AdditionalProperties = new Dictionary<string, object>
                {
                    {
                        "Gender", "Female"
                    },
                    {
                        "Age", "54"
                    },
                    {
                        "Hair", "Graying brown hair pulled back in a practical bun"
                    },
                    {
                        "Makeup", "None"
                    },
                    {
                        "Outfit", "Simple wool dress in dark green; White apron with ale stains; Sturdy leather boots; Worn silver locket (containing daughter's portrait)"
                    },
                    {
                        "StateOfDress", "Neat but work-worn appearance"
                    },
                    {
                        "PostureAndInteraction", "Standing behind the bar, polishing mugs while watching the room, occasionally wiping hands on apron"
                    },
                    {
                        "Traits", "Observant: notices details others miss; Protective: fierce defender of her community"
                    },
                    {
                        "Children", "1st Born: Elena, Female, current status unknown (disappeared)"
                    },
                    {
                        "Disposition", "Wary but hopeful"
                    },
                    {
                        "Status", "Active"
                    },
                    {
                        "Health", "Good despite stress"
                    },
                    {
                        "CurrentMood", "Cautiously curious"
                    },
                    {
                        "PhysicalCondition", "Minor fatigue from sleepless nights"
                    },
                    {
                        "NotableItems", "Silver locket with daughter's portrait; Old map of forest paths (hidden)"
                    }
                }
            },
            CharacterId = Guid.NewGuid(),
            SequenceNumber = 0
        };
    }
}
