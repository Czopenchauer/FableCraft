namespace FableCraft.Tests.Tracker;

internal static class TestTracker
{
    public const string InputJson = """
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

    public const string TrackerOwn = """
                                     {
                                         "Story": [
                                             {
                                                 "name": "Time",
                                                 "type": "String",
                                                 "prompt": "Current precise time and date. Format: 'HH:MM DD-MM-YYYY (Time of Day)'. Use 24-hour clock for precision. Time of Day descriptor in parentheses for quick narrative reference: Dawn (05:00-06:59), Morning (07:00-11:59), Afternoon (12:00-16:59), Evening (17:00-20:59), Night (21:00-04:59). Update as scenes progress and time passes.",
                                                 "defaultValue": "12:00 01-01-894 (Afternoon)",
                                                 "exampleValues": [
                                                     "08:15 24-04-894 (Morning)",
                                                     "23:47 13-12-2024 (Night)",
                                                     "05:30 02-07-894 (Dawn)"
                                                 ]
                                             },
                                             {
                                                 "name": "Location",
                                                 "type": "String",
                                                 "prompt": "Current scene location as hierarchical path from broadest to most specific, followed by environmental features. Format: 'Region > Settlement > Building > Room | Features: lighting, exits, notable objects, atmosphere'. Use 'None' or skip levels that don't apply. Keep features concise but descriptive enough to set the scene.",
                                                 "defaultValue": "Unknown Region > Unknown Settlement > None > None | Features: None notable",
                                                 "exampleValues": [
                                                     "Kingdom of Valdris > Ironhaven City > Halvard Manor > Private bedchamber (2nd floor) | Features: Dim candlelight, oak door (locked) + barred window, four-poster bed + vanity + wardrobe, warm and perfumed",
                                                     "The Northern Wastes > Wilderness - Darkwood Forest > None > Forest clearing | Features: Overcast daylight, paths north/east + dense undergrowth south/west, fallen log + old campfire ring, still air with birdsong",
                                                     "Meridian Province > Millbrook Village > The Gilded Serpent Inn > Main taproom | Features: Firelight + oil lamps, front door + kitchen door + stairs up, long tables + bar counter + hearth, loud chatter and ale smell"
                                                 ]
                                             },
                                             {
                                                 "name": "Weather",
                                                 "type": "String",
                                                 "prompt": "Current weather conditions including sky/precipitation, temperature (specific or descriptive), and any effects on the scene or characters. For interior scenes, note 'Interior' but include relevant factors like indoor temperature or sounds of weather outside. Format: 'Conditions | Temperature | Effects'.",
                                                 "defaultValue": "Clear skies | Mild | No significant effects",
                                                 "exampleValues": [
                                                     "Clear skies, sunny | Warm (24°C) | Good visibility, pleasant for travel",
                                                     "Heavy rainstorm, thunder | Cold (8°C) | Poor visibility, muddy terrain, loud thunder masking sounds, seeking shelter advisable",
                                                     "Interior (blizzard outside) | Cold inside despite fireplace | Howling wind audible, snow visible through window cracks, travel impossible"
                                                 ]
                                             },
                                             {
                                                 "Name": "CharactersPresent",
                                                 "Type": "Array",
                                                 "Prompt": "List of all characters present in the scene.",
                                                 "DefaultValue": [
                                                     "No Characters"
                                                 ],
                                                 "ExampleValues": [
                                                     [
                                                         "Emma Thompson",
                                                         "James Miller"
                                                     ]
                                                 ]
                                             }
                                         ],
                                         "MainCharacter": [
                                             {
                                                 "name": "CurrentState",
                                                 "type": "Object",
                                                 "prompt": "All temporary and frequently-changing aspects of the character - their immediate physical condition, mental state, needs, appearance, situation, and resources. This section tracks everything that could change within hours or days. Updated constantly during scenes as the character experiences events, takes actions, or is acted upon.",
                                                 "defaultValue": null,
                                                 "exampleValues": null,
                                                 "nestedFields": [
                                                     {
                                                         "name": "Identity",
                                                         "type": "Object",
                                                         "prompt": "Core identifying information about the character - who they are at a fundamental level. These fields rarely change but are essential reference information grouped for easy access.",
                                                         "defaultValue": null,
                                                         "exampleValues": null,
                                                         "nestedFields": [
                                                             {
                                                                 "name": "Name",
                                                                 "type": "String",
                                                                 "prompt": "The character's full name as currently known or used. This is the primary identifier and may change based on circumstances. Include titles, earned names, or slave designations if applicable. For slaves, include both their given/birth name (if known) and any assigned slave name or number. Update when character gains new titles or has name stripped/changed.",
                                                                 "defaultValue": "Unknown",
                                                                 "exampleValues": [
                                                                     "Ariel Thornwood",
                                                                     "Slave #47 (birth name: Elena Vasquez, stripped upon enslavement)",
                                                                     "Lady Seraphina the Broken (formerly just Seraphina, title earned through defeat)"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Gender",
                                                                 "type": "String",
                                                                 "prompt": "Biological sex and/or gender identity of the character. This field determines which anatomical subfields are relevant (female genitalia vs male, pregnancy capability, etc.). Use emoji symbol for quick visual scanning. For intersex or magical gender situations, specify what anatomy is present. This is a fixed trait unless magically altered.",
                                                                 "defaultValue": "Female ♀️",
                                                                 "exampleValues": [
                                                                     "Female ♀️",
                                                                     "Male ♂️",
                                                                     "Futanari ⚥ (female body with functional penis and testicles, retains vagina)"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Age",
                                                                 "type": "String",
                                                                 "prompt": "Current age as a specific number. Update this field when significant in-story time passes (months or years). For long-lived races, include both actual age and apparent/equivalent human age if relevant. Use 'Unknown' only if the character genuinely doesn't know their own age.",
                                                                 "defaultValue": "19",
                                                                 "exampleValues": [
                                                                     "14",
                                                                     "19 (appears younger due to small stature)",
                                                                     "147 (equivalent to human mid-20s, elven aging)"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Species",
                                                                 "type": "String",
                                                                 "prompt": "The character's race or species. This affects potential anatomical variations (elven ears, beast tails, demonic features), fertility and breeding compatibility with other species, natural lifespan, and any innate racial abilities or weaknesses. For mixed heritage, list both parent species and note which traits manifest. Include any subspecies or regional variants if relevant to the setting.",
                                                                 "defaultValue": "Human",
                                                                 "exampleValues": [
                                                                     "Human (Northlands ethnic stock - pale skin, typically blonde/red hair)",
                                                                     "Half-Elf (Human mother, High Elf father) - Manifests: pointed ears, extended lifespan (~200 years), slight build",
                                                                     "Catfolk Beastkin - Feline ears, tail, slit pupils, enhanced reflexes, heat cycles instead of menstrual cycle"
                                                                 ]
                                                             }
                                                         ]
                                                     },
                                                     {
                                                         "name": "Vitals",
                                                         "type": "Object",
                                                         "prompt": "Core physical and mental condition metrics - health, energy levels, pain, and psychological state. These are the essential status indicators that determine the character's current functional capacity and wellbeing.",
                                                         "defaultValue": null,
                                                         "exampleValues": null,
                                                         "nestedFields": [
                                                             {
                                                                 "name": "Health",
                                                                 "type": "String",
                                                                 "prompt": "Physical health status tracking injuries, wounds, illness, and disease - but NOT fatigue or pain levels (those have separate fields). Describe specific injuries with their location, severity (minor/moderate/severe/critical), current treatment status, and healing progress. Include illness symptoms if sick. This field answers: 'What is physically wrong with the body?' Update as wounds heal or new injuries occur.",
                                                                 "defaultValue": "Healthy - No injuries, wounds, or illness",
                                                                 "exampleValues": [
                                                                     "Healthy - No current injuries or illness, body in good condition",
                                                                     "Minor Injuries - Friction burns on wrists from rope (healing), bruised knees from kneeling on stone, small cut on lip (fresh, minor bleeding)",
                                                                     "Moderate Injuries - Deep laceration on left thigh (bandaged, risk of infection if not cleaned), extensive bruising across buttocks and back (2 days old, yellowing), cracked rib (suspected, painful but stable)"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Fatigue",
                                                                 "type": "String",
                                                                 "prompt": "Energy and exhaustion level on a 0-10 scale - separate from pain or health. This tracks how tired the character is from exertion, sleep deprivation, or extended activity. 0=Fully rested and energetic, 3=Mildly tired, 5=Noticeably fatigued, 7=Exhausted, 9=Collapse imminent, 10=Unconscious from exhaustion. Include the cause of fatigue and physical symptoms (heavy limbs, drooping eyes, slowed reactions). Increases with physical activity, stress, lack of sleep. Decreases with rest, sleep, stimulants.",
                                                                 "defaultValue": "0/10 (Fully rested) - Well-slept, alert and energetic",
                                                                 "exampleValues": [
                                                                     "2/10 (Fresh) - Slept well last night, minor tiredness from morning activities, fully functional",
                                                                     "6/10 (Fatigued) - Awake for 20 hours, muscles feel heavy, reaction time slowed, difficulty concentrating, needs rest soon",
                                                                     "9/10 (Collapse imminent) - 4 hours of continuous strenuous use, legs barely supporting weight, vision blurring, words slurring, will pass out if pushed further"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Mental",
                                                                 "type": "String",
                                                                 "prompt": "Psychological and cognitive state - clarity of thought, emotional condition, and mental stability. This is NOT about intelligence but current mental functioning. Describe: baseline alertness, emotional state (calm, anxious, terrified, aroused, angry), cognitive clarity (sharp, foggy, overwhelmed), and any altered states from drugs, magic, trauma, or conditioning. Include what's causing the current state. This field answers: 'What is the character's headspace right now?'",
                                                                 "defaultValue": "Clear and Stable - Alert, emotionally balanced, thinking clearly",
                                                                 "exampleValues": [
                                                                     "Clear and Stable - Fully alert, calm emotional state, able to think strategically and make decisions; no impairments",
                                                                     "Anxious but Functional - Hypervigilant, racing thoughts, easily startled; fear of upcoming punishment causing distraction but still able to follow commands and respond coherently",
                                                                     "Broken Subspace - Deep in submission trance from extended scene, barely processing external stimuli, nonverbal, completely compliant and suggestible, no independent thought; will need aftercare to return to baseline"
                                                                 ]
                                                             }
                                                         ]
                                                     },
                                                     {
                                                         "name": "Needs",
                                                         "type": "Object",
                                                         "prompt": "Physical and physiological needs - arousal, hunger, thirst, and bodily urges. These drive behavior and create pressure/motivation. All tracked on scales with specific symptoms at each level.",
                                                         "defaultValue": null,
                                                         "exampleValues": null,
                                                         "nestedFields": [
                                                             {
                                                                 "name": "Arousal",
                                                                 "type": "String",
                                                                 "prompt": "Sexual excitement level on a 0-10 scale with MANDATORY specific physiological details. This field must describe the physical manifestations of arousal, not just a number. Include: genital response (for vulvas: clit engorgement, labia swelling, vaginal wetness; for penises: erection hardness, pre-cum), nipple state, skin flushing (where and how intense), breathing pattern, muscle tension, and heat radiating from erogenous zones. 0=Dormant (no arousal signs), 5=Aroused (clear physical signs), 7=Highly aroused (intense response), 9=Desperate (edge state), 10=Overwhelming (lost to sensation).",
                                                                 "defaultValue": "0/10 (Dormant) - No arousal, genitals at rest, body neutral",
                                                                 "exampleValues": [
                                                                     "0/10 (Dormant) - No sexual arousal; genitals at rest and dry, nipples soft, normal skin color, breathing even, body temperature normal",
                                                                     "6/10 (Aroused) - Clit swelling against hood, inner labia puffy and flushed dark pink, noticeably wet (dampening underwear), nipples visibly hard and sensitive, light flush across chest and cheeks, breathing deeper, warmth radiating from groin",
                                                                     "9/10 (Desperate/Edge) - Clit fully engorged and throbbing visibly, labia swollen and spread, soaking wet (dripping down thighs, audible when touched), nipples painfully hard and aching, deep flush from face to chest, panting, whole body trembling with need, core clenching on nothing, teetering on edge of orgasm"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Hunger",
                                                                 "type": "String",
                                                                 "prompt": "Food/sustenance need on a 0-10 scale with time tracking. 0=Satiated (recently ate, no hunger), 3=Mildly hungry, 5=Hungry (stomach growling), 7=Very hungry (weakness beginning), 9=Starving (physical impairment), 10=Critical starvation. Include: time since last meal, type of last meal if relevant, and physical symptoms. Hunger increases approximately 1 point per 3 hours under normal conditions. Faster with high activity, slower when sleeping or sedentary.",
                                                                 "defaultValue": "2/10 (Satisfied) - Ate breakfast a few hours ago",
                                                                 "exampleValues": [
                                                                     "1/10 (Full) - Ate large meal 2 hours ago, pleasantly satisfied, no hunger",
                                                                     "5/10 (Hungry) - Last meal was small portion 12 hours ago, stomach growling audibly, thinking about food frequently, slight lightheadedness when standing quickly",
                                                                     "8/10 (Starving) - No food in 3 days (punishment), severe stomach cramps, weakness in limbs, dizzy, difficulty concentrating, would eat almost anything offered"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Thirst",
                                                                 "type": "String",
                                                                 "prompt": "Hydration need on a 0-10 scale with symptom tracking. 0=Fully hydrated, 3=Mildly thirsty, 5=Thirsty (dry mouth), 7=Very thirsty (headache beginning), 9=Severely dehydrated (medical concern), 10=Critical dehydration. Include: time since last drink, circumstances affecting thirst, and physical symptoms. Thirst increases approximately 1 point per 2 hours under normal conditions. Increases faster during: physical exertion, heat exposure, crying, sweating, significant fluid loss (sexual fluids, bleeding).",
                                                                 "defaultValue": "1/10 (Hydrated) - Recently drank, comfortable",
                                                                 "exampleValues": [
                                                                     "0/10 (Fully hydrated) - Just drank water, no thirst whatsoever",
                                                                     "5/10 (Thirsty) - Last drink 6 hours ago, mouth and throat dry, lips slightly tacky, would very much like water",
                                                                     "8/10 (Severely dehydrated) - No water for 24 hours plus heavy sweating during use, pounding headache, dark urine when allowed to relieve, lips cracked and bleeding, dizzy, skin losing elasticity"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Urges",
                                                                 "type": "String",
                                                                 "prompt": "Bladder and bowel pressure tracked separately on 0-10 scales. Format: 'Bladder: X/10 (status) | Bowel: Y/10 (status)'. Bladder: increases ~1/hour, faster with fluid intake. Bowel: increases ~1/4 hours, faster after eating. Scale meaning: 0=Empty/just relieved, 3=Mild awareness, 5=Moderate need (would use bathroom if convenient), 7=Urgent (uncomfortable, priority need), 9=Desperate (in pain, at limit), 10=Loss of control occurring. Include physical manifestations (squirming, clenching, visible belly bulge, leaking).",
                                                                 "defaultValue": "Bladder: 2/10 (Minimal) | Bowel: 1/10 (Empty)",
                                                                 "exampleValues": [
                                                                     "Bladder: 1/10 (Empty) | Bowel: 0/10 (Empty) - Recently used bathroom, no urges",
                                                                     "Bladder: 6/10 (Urgent) | Bowel: 3/10 (Mild) - Bladder noticeably full, pressure building, squirming slightly, would definitely use bathroom if permitted; bowel has mild awareness but easily ignored",
                                                                     "Bladder: 10/10 (Losing control) | Bowel: 7/10 (Urgent) - Bladder at absolute limit, leaking small spurts despite desperate clenching, crying from pressure and humiliation; bowel cramping with strong urge, fighting hard to maintain control"
                                                                 ]
                                                             }
                                                         ]
                                                     },
                                                     {
                                                         "name": "Conditions",
                                                         "type": "Object",
                                                         "prompt": "Active temporary conditions affecting the character - internal pressures and active effects from external sources. These are states that will change or end, not permanent characteristics.",
                                                         "defaultValue": null,
                                                         "exampleValues": null,
                                                         "nestedFields": [
                                                             {
                                                                 "name": "InternalPressure",
                                                                 "type": "String",
                                                                 "prompt": "Internal fullness in body cavities: WOMB, STOMACH, and BOWELS - tracking WHAT is inside, HOW MUCH, and the internal SENSATION. This is different from StomachDistension (which tracks external visual appearance). Include: which cavity, contents (semen, enema fluid, food, air, objects), specific volume in ml/L where applicable, physical sensations (pressure, cramping, stretching, sloshing, warmth), and whether contents are being retained (plugged, held) or leaking. Track each filled cavity separately.",
                                                                 "defaultValue": "Empty - All cavities normal, no unusual fullness",
                                                                 "exampleValues": [
                                                                     "Empty - Stomach has normal food from recent meal, womb and bowels empty and at rest, no pressure or unusual fullness",
                                                                     "Womb: ~300ml semen (3 loads), warm fullness behind pubic bone, plug keeping contents sealed inside, feeling of heaviness and sloshing with movement | Stomach: Normal | Bowels: Normal",
                                                                     "Womb: Stuffed (~800ml combined semen from multiple partners), strong pressure and cramping from overstretch, can feel it shift when moving | Bowels: 1.5L retention enema, intense pressure and cramping, liquid gurgling audibly, fighting urge to expel | Stomach: Empty and nauseous from other pressures"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "ActiveEffects",
                                                                 "type": "String",
                                                                 "prompt": "All active effects currently influencing the character - anything temporary that modifies their state beyond their natural baseline. Categories: PHYSICAL (restraint effects, injury effects, modifications), CHEMICAL (drugs, potions, poisons, aphrodisiacs), MAGICAL (spells, curses, enchantments, blessings), PSYCHOLOGICAL (temporary conditioning effects, triggers currently active, hypnotic suggestions, mental states). Format each as: 'Effect Name (Type) - Duration - Impact'. Duration can be: time remaining, 'Until removed', or 'Until condition met'. 'None' if no active effects. Note: PERMANENT effects belong in Development.Traits, not here.",
                                                                 "defaultValue": "None - No active effects, character at natural baseline",
                                                                 "exampleValues": [
                                                                     "None - No drugs, spells, or unusual effects active. Character functioning at natural baseline.",
                                                                     "Mild Aphrodisiac (Chemical) - ~3 hours remaining - Heightened arousal, increased genital sensitivity, mildly foggy thinking when aroused, easier to arouse; Bound arms (Physical) - Until released - Cannot use hands, limited mobility",
                                                                     "Heavy Aphrodisiac (Chemical) - 6 hours remaining - Uncontrollable arousal, constant wetness, can barely think past need; Orgasm Denial Curse (Magical) - Until dispelled - Cannot physically orgasm regardless of stimulation, edges painfully but release is blocked"
                                                                 ]
                                                             }
                                                         ]
                                                     },
                                                     {
                                                         "name": "Appearance",
                                                         "type": "Object",
                                                         "prompt": "Current visual and sensory presentation - how the character looks, sounds, and smells RIGHT NOW. These fields track current state and condition, which changes based on activities and circumstances.",
                                                         "defaultValue": null,
                                                         "exampleValues": null,
                                                         "nestedFields": [
                                                             {
                                                                 "name": "Hair",
                                                                 "type": "String",
                                                                 "prompt": "Head hair ONLY (body hair is tracked in the BodyHair field). Describe: natural color, current color (if dyed), length (specific or comparative), texture (straight, wavy, curly, coily), thickness, current style (loose, braided, ponytail, etc.), and current condition (clean, dirty, wet, matted, tangled, grabbed). Update condition based on scene activities - hair gets messy during exertion, wet during water exposure, tangled when grabbed.",
                                                                 "defaultValue": "Chestnut brown, shoulder-length, slight natural wave, currently loose and clean",
                                                                 "exampleValues": [
                                                                     "Natural blonde (honey-colored), waist-length, straight and silky, thick; currently in a neat single braid, clean and well-maintained",
                                                                     "Black with blue undertones, pixie cut (1-2 inches), straight, fine hair; naturally messy style, currently damp with sweat from exertion",
                                                                     "Auburn red, mid-back length, wild natural curls, thick and voluminous; currently tangled and matted from being grabbed repeatedly, damp with sweat, messy halo around face"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Face",
                                                                 "type": "String",
                                                                 "prompt": "Comprehensive facial features and current expression - structure and current state. Include: face shape, eye color/shape/current state, eyebrow shape, nose, lips (fullness, natural color, current state), skin complexion and condition, and current expression. Update current state based on scene: tears, flushing, swelling from slaps, fluids on face, etc. This field covers NATURAL features; any applied makeup is tracked separately in Makeup field.",
                                                                 "defaultValue": "Oval face, blue eyes, full pink lips. Fair clear skin. Expression: Neutral and alert.",
                                                                 "exampleValues": [
                                                                     "Heart-shaped face with soft features. Large doe-like green eyes (long natural lashes), currently downcast submissively. Thin arched brows. Small upturned nose. Full naturally pink lips, soft and slightly parted. Creamy complexion with faint freckles across nose and cheeks, currently flushed light pink with embarrassment. Expression: Shy and nervous.",
                                                                     "Angular face with sharp cheekbones and defined jawline. Narrow amber eyes, currently blazing with defiance. Strong dark brows, furrowed. Straight aristocratic nose. Thin lips pressed into tight line. Olive skin tone, flushed dark red with anger across cheeks. Expression: Glaring hatred, jaw clenched.",
                                                                     "Soft oval face with youthful features. Wide brown eyes, currently glazed and unfocused, red-rimmed from crying, tear tracks cutting through the mess on her face. Soft brows. Button nose, running slightly. Plump lips swollen from biting, hanging open slackly. Fair skin blotchy from sobbing, left cheek bearing red handprint from recent slap, streaked with cum, tears, and drool. Expression: Broken, vacant, overstimulated past coherent response."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Makeup",
                                                                 "type": "String",
                                                                 "prompt": "Applied cosmetics and their current condition - completely separate from natural facial features (Face field). Describe: what's applied (foundation, eye makeup, lipstick, etc.), the style (natural, glamorous, heavy, slutty), and current state (fresh, smudged, running, ruined). If no makeup is worn, describe as 'None - natural/bare face'. Update as scenes progress - makeup smears, runs with tears, transfers to skin/objects, gets ruined by fluids.",
                                                                 "defaultValue": "None - Natural bare face, no cosmetics applied",
                                                                 "exampleValues": [
                                                                     "None - Face completely bare, no cosmetics; natural appearance",
                                                                     "Light natural look - Thin foundation evening skin tone, subtle brown mascara lengthening lashes, nude lip gloss; currently fresh and intact, applied recently",
                                                                     "Ruined heavy makeup - Was: thick black eyeliner, heavy mascara, deep red lipstick, blush. Now: eyeliner smeared across temples from tears, mascara running in black streaks down both cheeks, lipstick completely worn off (on cock/transferred elsewhere), foundation streaked with sweat and tears; thoroughly wrecked appearance"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Scent",
                                                                 "type": "String",
                                                                 "prompt": "What the character currently smells like - natural body odor, applied scents, and accumulated smells from activities. Layer scents from underlying (skin) to surface (recent additions). Include: baseline cleanliness, any perfume/soap, sweat level, arousal musk, sex smells (cum, fluids), and any other relevant odors. Update based on: time since bathing, physical exertion, sexual activity, environmental exposure. Scent can be an important sensory detail for scenes.",
                                                                 "defaultValue": "Clean - Fresh soap scent, natural neutral skin smell, no strong odors",
                                                                 "exampleValues": [
                                                                     "Clean and fresh - Bathed this morning with lavender soap, faint floral scent lingers on skin, no body odor, no sweat; pleasant neutral smell",
                                                                     "Aroused musk - Clean underneath but several hours since bathing, light natural body scent, strong arousal musk emanating from between legs (wet pussy smell noticeable within a few feet), light sweat sheen adding salt note; smells like an aroused woman",
                                                                     "Thoroughly used - Hasn't bathed in 2 days, underlying stale sweat and body odor, layered with heavy sex smell: multiple men's cum (dried and fresh), her own arousal fluids coating thighs, dried saliva, fresh sweat from exertion; overwhelmingly smells of sex and use, marking territory"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Voice",
                                                                 "type": "String",
                                                                 "prompt": "Current state of character's voice and ability to vocalize - natural qualities and current condition. Describe: natural voice (pitch, tone, quality), current condition (clear, hoarse, strained), and any impairments (from gagging, screaming, throat use, crying, magical silencing). This field tracks the instrument itself; whether speech is PERMITTED is a separate matter. Update based on activities: screaming makes voice hoarse, throat fucking makes it raw, crying makes it thick.",
                                                                 "defaultValue": "Clear - Soft feminine voice, unimpaired, speaks easily",
                                                                 "exampleValues": [
                                                                     "Clear and steady - Natural alto voice, pleasant tone, completely unimpaired; speaks clearly and confidently",
                                                                     "Strained and thick - Naturally soft voice, currently thick from recent crying, slight wobble when speaking, occasional catch in throat from suppressed sobs; understandable but obviously distressed",
                                                                     "Wrecked - Voice destroyed from combination of 2 hours screaming during punishment and rough throat fucking after; currently barely above hoarse whisper, raw pain when swallowing or attempting to speak, words come out as rough croaks; will need days to recover"
                                                                 ]
                                                             }
                                                         ]
                                                     },
                                                     {
                                                         "name": "Body",
                                                         "type": "Object",
                                                         "prompt": "Detailed physical anatomy of the character's body - structure, features, and current condition of each body region. This tracks the physical form itself. Permanent modifications (tattoos, brands, scars) are tracked in Development.PermanentMarks.",
                                                         "defaultValue": null,
                                                         "exampleValues": null,
                                                         "nestedFields": [
                                                             {
                                                                 "name": "GeneralBuild",
                                                                 "type": "String",
                                                                 "prompt": "Overall body type and physical presence - the big picture before zooming into specific parts. Include: height (specific measurement preferred), weight or build descriptor, body type (slender, athletic, curvy, heavyset), how weight is distributed, muscle tone, skin color/texture/temperature. This field sets the foundation; specific body parts are detailed in their own fields. Include racial physical traits if applicable.",
                                                                 "defaultValue": "Average height (5'5\"), slender feminine build with soft curves. Fair smooth skin, warm to touch.",
                                                                 "exampleValues": [
                                                                     "Petite (5'0\", ~95 lbs), delicate small-framed build with subtle curves. Minimal muscle tone, soft everywhere. Porcelain pale skin that shows marks easily, naturally cool to touch, goosebumps when cold or aroused.",
                                                                     "Tall (5'10\", ~150 lbs), athletic Amazonian build with defined muscles visible under skin, strong shoulders, powerful thighs. Low body fat, firm rather than soft. Bronze sun-kissed skin, warm and slightly oiled from training, flushed with exertion heat.",
                                                                     "Short and stacked (4'11\", ~160 lbs), exaggerated hourglass with weight concentrated in chest and hips. Thick soft thighs, soft belly with slight pooch, plush everywhere. Creamy pale skin, very warm and soft to touch, yields like bread dough when squeezed."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Mouth",
                                                                 "type": "String",
                                                                 "prompt": "Detailed oral anatomy focused on sexual use capacity. Include: lip description (from Face for reference but focus on function), teeth condition, tongue (size, length, skill), jaw strength and current state (fresh, tired, aching, locked), gag reflex (strong/moderate/weak/trained out/absent), throat depth capacity (how much can be taken), and current oral condition (soreness, rawness, jaw fatigue). Track training progress for oral skills. This field focuses on FUNCTION; lip appearance is in Face field.",
                                                                 "defaultValue": "Healthy mouth - average tongue, strong gag reflex, untrained throat, jaw comfortable",
                                                                 "exampleValues": [
                                                                     "Inexperienced mouth - All teeth present and healthy, average pink tongue, strong gag reflex triggering at 3 inches depth, throat untrained and tight, never taken anything deep; jaw currently comfortable, no fatigue; would struggle significantly with oral use",
                                                                     "Trained oral - Teeth intact, longer than average tongue (skilled from practice), gag reflex weakened through training (triggers at 5-6 inches, can be pushed past), throat can accommodate average cock to root with effort; jaw well-conditioned for extended use, currently mild ache from earlier session",
                                                                     "Extensively broken in - Teeth intact (carefully preserved), long dexterous tongue, gag reflex completely eliminated through months of training, throat permanently loosened and can take any size without resistance; jaw currently aching badly and clicking (locked in ring gag for 3 hours), throat raw and scratched from rough use, swallowing painful"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Chest",
                                                                 "type": "String",
                                                                 "prompt": "Breast/chest description focusing on physical attributes - size, shape, and behavior. Include: size (cup size AND descriptive), shape (perky, teardrop, round, pendulous), weight/heaviness, firmness (soft, firm, augmented), natural behavior (self-supporting, need support, bounce/sway patterns), vein visibility, and current state (natural, swollen, marked, bound). This describes the breast mounds; nipples have their own field. For male characters, describe pectoral development instead.",
                                                                 "defaultValue": "Moderate B-cups, perky and self-supporting, soft with gentle natural bounce. Unmarked.",
                                                                 "exampleValues": [
                                                                     "Small A-cups, barely-there gentle swells against ribcage, very firm, minimal movement even during activity. Smooth and unmarked, pale skin matching body.",
                                                                     "Full natural D-cups, classic teardrop shape with more fullness at bottom, heavy enough to require support for comfort, significant sway when walking, bounce and jiggle during impact or movement. Soft and yielding, faint blue veins visible under fair skin when aroused. Currently unmarked.",
                                                                     "Massive G-cups, heavy and pendulous, hang to navel when unsupported, impossible to ignore. Very soft, almost fluid movement, sway dramatically with any motion, slap audibly against each other and body when moving quickly. Extensive visible veining, some stretch marks on sides. Currently bound in rope harness squeezing them into swollen tight globes, skin shiny from pressure."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Nipples",
                                                                 "type": "String",
                                                                 "prompt": "Detailed nipple and areola description - the specific anatomy of the nipple complex. Include: areola size (use coin comparisons: dime, quarter, silver dollar), areola color, areola texture (smooth, bumpy Montgomery glands), nipple size/shape (small, medium, large/long, puffy, flat, inverted), nipple color, and current state (soft, hardening, fully erect, overstimulated). Track any modifications (piercings) and damage (chafed, clamped, marked). Update erection state based on arousal/stimulation/cold.",
                                                                 "defaultValue": "Quarter-sized light pink areolae, small button nipples. Currently soft.",
                                                                 "exampleValues": [
                                                                     "Small dime-sized pale pink areolae, nearly smooth with faint texture. Tiny nipples that lay almost flat when soft, rise to small firm points when erect. Currently soft and unobtrusive. Unmodified, unmarked, sensitive to touch.",
                                                                     "Silver-dollar sized medium pink areolae with visible bumpy Montgomery glands. Puffy nipples - areola and nipple form soft cone shape when relaxed, nipple tips push out prominently when erect. Currently fully erect from arousal - standing out firm and prominent. Unmodified but currently flushed darker pink from stimulation.",
                                                                     "Large dark brown areolae with pronounced bumpy texture. Long thick nipples (~1 inch when erect), permanent slight erection even at rest. Pierced: thick gauge steel barbells through each, healed. Currently clamped with adjustable clover clamps tightened to painful level - flesh whitening from pressure, connected by chain that tugs with any movement."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Lactation",
                                                                 "type": "String",
                                                                 "prompt": "Milk production status and mammary function - ONLY include this detail if character is lactating or has lactated. Describe: production volume (per day/per milking session), time since last expression/milking, current breast fullness (empty/comfortable/full/engorged/painfully engorged), milk characteristics (thin, normal, creamy, rich), let-down response (what triggers milk release). Track changes from regular milking schedule or neglect. This field is entirely separate from breast size/appearance (Chest field). For non-lactating characters, simply state 'Non-lactating.'",
                                                                 "defaultValue": "Non-lactating",
                                                                 "exampleValues": [
                                                                     "Non-lactating - No milk production, normal breast tissue function",
                                                                     "Moderate production - Producing ~2 pints/day (induced 3 months ago), last milked 4 hours ago, currently comfortably full with mild pressure, normal creamy white milk with good fat content. Let-down triggers reliably with nipple suction or manual expression. Maintaining production with twice-daily milking schedule.",
                                                                     "Heavy Hucow production - Producing 8+ pints/day (enhanced breeding), not milked in 14 hours (punishment), breasts painfully engorged and hard as rocks, hot to touch, leaking constantly through nipples despite no stimulation, visible milk dripping and staining clothes. Rich cream-top milk. Desperately needs milking - pain level severe, mastitis risk if neglect continues."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "StomachAppearance",
                                                                 "type": "String",
                                                                 "prompt": "Normal midriff appearance when NOT distended - the baseline state. Include: muscle definition (none, slight, toned, defined abs), natural shape (flat, slight curve, rounded), softness (firm, soft, very soft), navel type and appearance (innie depth, outie), skin texture. This describes the NORMAL state; any bloating or distension from internal contents goes in StomachDistension field. Keep this as the reference baseline.",
                                                                 "defaultValue": "Flat stomach with slight natural softness, no visible muscle. Shallow innie navel. Smooth pale skin.",
                                                                 "exampleValues": [
                                                                     "Tightly toned stomach with visible four-pack definition, very firm to touch, minimal body fat. Deep innie navel. Smooth tanned skin. Athletic core from training.",
                                                                     "Soft flat stomach with gentle feminine curve, no muscle definition, pleasant give when pressed. Small round innie navel. Creamy smooth skin, sensitive to tickling. Naturally slender build.",
                                                                     "Soft rounded belly with visible pooch below navel, very soft and squeezable, noticeable jiggle when moving. Shallow navel starting to stretch slightly. Pale skin with faint stretch marks on sides. Carries weight in midsection."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "StomachDistension",
                                                                 "type": "String",
                                                                 "prompt": "VISIBLE external distension of the belly - how bloated/inflated the stomach area LOOKS. This is the VISUAL appearance from outside; what's causing it and internal sensations go in InternalPressure field. Describe: size change from baseline (slight bulge, noticeable swelling, severe distension), skin state (soft, taut, drum-tight, shiny), visible effects (veins, movement inside, navel changes), and comparison (food baby, looks pregnant, etc.). If normal/not distended, state 'Normal - no visible distension.'",
                                                                 "defaultValue": "Normal - No visible distension, stomach at baseline appearance",
                                                                 "exampleValues": [
                                                                     "Normal - Stomach at usual flat/soft baseline, no visible bloating or distension",
                                                                     "Moderate bulge - Visible rounded swelling of lower belly, skin taut over the bump, looks like early pregnancy or having eaten large meal. Navel slightly stretched. Gentle sloshing movement visible when she shifts position.",
                                                                     "Severe distension - Belly swollen massively, skin drum-tight and shiny, veins visible through stretched skin, navel completely flat and almost popping outward. Looks like full-term pregnancy but rounder. Visible churning/movement inside. Character cannot bend at waist, skin feels ready to split."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Genitalia",
                                                                 "type": "String",
                                                                 "prompt": "Hyper-detailed genital anatomy based on character's sex. FOR VULVAS: Mons pubis (fullness, padding), labia majora (puffy, flat, thin, full), labia minora (length, inner/outer visibility, color, texture), clitoris (size, hood coverage, exposure), vaginal opening (observed tightness/looseness, gape when relaxed). FOR PENISES: Length (soft AND erect), girth, shape, vein prominence, glans details, foreskin status, scrotum. Current state: resting/aroused, used/fresh. This is ANATOMICAL description; current wetness/fluids go in Secretions field.",
                                                                 "defaultValue": "Female: Smooth mound with modest padding. Puffy outer labia concealing small pink inner labia (innie). Small clit hidden under hood. Tight vaginal entrance.",
                                                                 "exampleValues": [
                                                                     "Female (virgin anatomy): Full soft mons with slight padding. Puffy outer labia press together when standing, conceal everything when closed. Inner labia small and delicate, pale pink, completely contained within outer lips (innie). Small clit fully covered by hood, only visible when hood manually retracted. Vaginal entrance virgin-tight, hymen intact, barely admits single fingertip.",
                                                                     "Female (experienced anatomy): Prominent mons, mostly smooth. Outer labia moderately full but parted, don't conceal inner anatomy. Inner labia prominent - extend 1.5 inches past outer lips when spread, darker rose-pink with slightly textured edges, visible from outside. Clit medium-sized, hood retracted showing pink nub constantly. Vaginal entrance well-used - relaxed gape of ~1cm when at rest, easily accommodates three fingers, grips but doesn't resist.",
                                                                     "Male (average anatomy): Soft: 3 inches, hangs over scrotum. Erect: 6.5 inches, moderate girth (5 inch circumference), slight upward curve, prominent dorsal vein running length when hard. Cut, pink glans with defined ridge, darker than shaft. Scrotum hangs loosely in warm conditions, draws tight when cold/aroused, average-sized testicles."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Secretions",
                                                                 "type": "String",
                                                                 "prompt": "Genital fluid status - current wetness, lubrication, and any fluids present. Track: natural arousal fluid (amount, consistency, color), any cum present (whose, how fresh, how much, where), other fluids (pre-cum, smegma, cervical mucus). Describe: current wetness level (dry, slightly moist, wet, soaking, dripping, gushing), viscosity (thin, slick, thick, creamy, stringy), visible evidence (dampening fabric, coating thighs, pooling beneath). Include scent and taste notes if relevant. This field tracks current fluid state; Arousal field tracks overall excitement level.",
                                                                 "defaultValue": "Dry - No secretions, genitals clean and dry at rest",
                                                                 "exampleValues": [
                                                                     "Dry - Genitals clean and dry, no natural lubrication currently, neutral state",
                                                                     "Wet and slick - Significant natural arousal fluid, clear and slippery, coating entire vulva and beginning to dampen inner thighs. Thin consistency, strings slightly when spread. Light musky arousal scent.",
                                                                     "Cum-soaked - Fresh thick load(s) of white cum leaking from well-used vagina, mixing with her copious arousal fluid to create sloppy wet mess. Cum coating labia, dripping down to anus, smeared across inner thighs. Older loads drying to sticky residue. Overwhelming smell of semen and sex. Squelching sounds with any movement."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Buttocks",
                                                                 "type": "String",
                                                                 "prompt": "Rear end description - shape, size, and physical properties. Include: size/volume (small, medium, large, massive), shape (flat, round, heart, bubble, shelf), firmness (tight, firm, soft, very soft), movement physics (minimal jiggle, bounces, claps, ripples, sways), how easily spread (firm/resistant vs soft/yields), cheek texture (smooth, dimpled, cellulite). Current state (unmarked, reddened, bruised, welted). Thigh gap presence if relevant. Anal anatomy has its own field.",
                                                                 "defaultValue": "Modest medium rear, round shape, firm with slight softness. Light bounce when moving. Smooth unmarked skin.",
                                                                 "exampleValues": [
                                                                     "Small tight rear, flat-ish with slight rounded curve, very firm (athletic build). Minimal jiggle even with impact, would need effort to spread cheeks. Smooth taut skin, unmarked. No thigh gap.",
                                                                     "Large heart-shaped ass, full and prominent, soft and squeezable with pleasant give. Noticeable sway when walking, bounces and jiggles with movement, claps during impact. Spreads easily when pulled. Smooth skin with faint cellulite dimpling on lower cheeks. Small thigh gap.",
                                                                     "Massive shelf ass, extremely heavy and prominent, very soft and plush like pillows. Dramatic sway and bounce with every step, loud clapping during any impact, ripples spread across flesh. Deep cleavage between cheeks. Significant cellulite on cheeks and upper thighs. Currently covered in dark bruises (2 days old) and fresh red handprints from recent spanking."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Anus",
                                                                 "type": "String",
                                                                 "prompt": "Anal anatomy in explicit detail. Include: external appearance - color of outer rim (pink, brown, dark), texture (puckered/knotted, smooth, wrinkled), surrounding hair if any. Muscle tone and capacity - tightness (virgin-tight, tight, normal, relaxed, loose, gaping), observed gape when relaxed, what can be accommodated. Current condition (pristine, used, red, swollen, sore, damaged). Track training progress if being developed. Update based on use.",
                                                                 "defaultValue": "Tight pink rosebud, puckered closed. Untouched, virgin-tight. Clean-shaven surrounding. Pristine condition.",
                                                                 "exampleValues": [
                                                                     "Virginal anatomy - Small tightly-knotted pink rosebud, puckers closed with no visible opening when relaxed, clenches reflexively at any touch. Never penetrated, would require significant stretching and patience to accept even single finger. Smooth hairless skin surrounding. Pristine, never used.",
                                                                     "Trained hole - Light brown wrinkled ring, relaxes to slight visible dimple when at rest (~0.5cm). Has been trained with plugs to accept average-sized toys/cock with adequate lube. Sphincter muscle functional but conditioned to relax on command. Currently slight redness and mild soreness from plug worn earlier. Light hair on outer rim.",
                                                                     "Extensively used - Dark stretched ring, permanent gape of ~2cm when relaxed, inner red/pink mucosa visible inside the opening. No longer able to fully close. Can easily accept very large objects without resistance. Sphincter muscle tone significantly reduced from prolonged heavy use. Currently puffy and irritated from recent rough use, minor prolapse beginning (inner tissue slightly visible). Hairless (kept shaved)."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "BodyHair",
                                                                 "type": "String",
                                                                 "prompt": "Body hair status across all regions EXCEPT head (which is in Hair field). Track: Pubic (shaved/stubble/trimmed/landing strip/full bush/wild overgrown), Armpits (shaved/stubble/natural), Legs (shaved/stubble/natural). Include days since last grooming to track growth. Growth rates: clean-shaved becomes stubble in 1-2 days, visible hair in 3-5 days, full growth in 2+ weeks. Also note any other body hair (treasure trail, arm hair, etc.) if notable. Update based on time passing and grooming access.",
                                                                 "defaultValue": "Pubic: Neatly trimmed | Armpits: Freshly shaved | Legs: Smooth (shaved yesterday) | Other: None notable",
                                                                 "exampleValues": [
                                                                     "Pubic: Completely bare (waxed 3 days ago, still smooth) | Armpits: Freshly shaved (this morning) | Legs: Smooth (shaved this morning) | Other: No notable body hair - maintains full removal",
                                                                     "Pubic: Short stubble growing back (shaved 4 days ago, scratchy to touch) | Armpits: Visible stubble (4 days) | Legs: Prickly stubble dots visible (4 days) | Other: Fine arm hair (natural, never removes) - hasn't had grooming access in captivity",
                                                                     "Pubic: Full natural bush (never shaved, thick dark curls extending to inner thighs) | Armpits: Full dark tufts (natural, never shaved) | Legs: Hairy (natural, never shaved) | Other: Visible dark treasure trail from navel down, moderate arm hair - completely natural, no grooming"
                                                                 ]
                                                             }
                                                         ]
                                                     },
                                                     {
                                                         "name": "Reproduction",
                                                         "type": "Object",
                                                         "prompt": "Current reproductive status - menstrual cycle position, pregnancy state, and orgasm control. These are temporary/cyclical states that change. Permanent reproductive history (children born) is tracked in Development.",
                                                         "defaultValue": null,
                                                         "exampleValues": null,
                                                         "nestedFields": [
                                                             {
                                                                 "name": "FertilityCycle",
                                                                 "type": "String",
                                                                 "prompt": "Current menstrual cycle stage for female characters - determines conception risk. Cycle stages and typical duration: Menstrual 🩸 (5 days, Safe 0%), Follicular 🌱 (7 days, Low 15%), Ovulating 🌺 (3 days, HIGH 85%), Luteal 🌙 (13 days, Moderate 30%). Track current stage, day within stage, conception risk, and any symptoms. If pregnant, display 'Pregnant 👶' and pause cycle until delivery. Resume cycle after birth. For non-applicable characters, use 'N/A'.",
                                                                 "defaultValue": "Follicular 🌱 (Day 3) - Low Risk 15%",
                                                                 "exampleValues": [
                                                                     "Menstrual 🩸 (Day 2) - Safe period, 0% conception risk. Currently bleeding (moderate flow), mild cramping, slightly fatigued. Cycle regular.",
                                                                     "Ovulating 🌺 (Day 1) - PEAK FERTILITY, 85% conception risk! Clear stretchy cervical mucus indicating fertility peak, mild mittelschmerz (ovulation cramps), heightened libido. Most dangerous time for unprotected sex.",
                                                                     "Pregnant 👶 - Cycle paused, confirmed pregnancy (see Pregnancy field for details). No menstruation. Will resume cycle approximately 6-8 weeks after delivery."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Pregnancy",
                                                                 "type": "String",
                                                                 "prompt": "Pregnancy status and detailed tracking. When creampied during fertile window: roll d100 against cycle conception percentage to determine if pregnancy occurs. If pregnant: track days since conception, trimester (1st: days 1-90, 2nd: 91-180, 3rd: 181-270), father's identity (or note if uncertain/multiple possible), and current physical symptoms. Expected delivery around day 270 (can vary). Update symptoms as pregnancy progresses. After birth, update Development.ReproductiveHistory.Children and reset this to 'Not Pregnant'.",
                                                                 "defaultValue": "Not Pregnant",
                                                                 "exampleValues": [
                                                                     "Not Pregnant - No current pregnancy, not recently inseminated during fertile window, or confirmed negative after risk exposure",
                                                                     "Confirmed Pregnant - 1st Trimester (Day 45) | Father: Marcus Halvard (certain) | Symptoms: Morning nausea (moderate, usually passes by noon), breast tenderness and slight swelling, missed period (confirmed), fatigue, heightened smell sensitivity. No visible belly yet. Pregnancy confirmed by healer.",
                                                                     "Pregnant - 3rd Trimester (Day 255) | Father: Unknown - multiple possible from breeding event | Symptoms: Large prominent belly (measuring on target), significant breast enlargement (preparing for lactation), frequent urination, difficulty sleeping, Braxton-Hicks contractions beginning, feet swelling. Baby very active. Approaching due date, ~2 weeks remaining."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "OrgasmState",
                                                                 "type": "String",
                                                                 "prompt": "Current orgasm and denial tracking - sexual control status. Track: time since last full orgasm, current control status (free access, permission required, denied, edged), session activity (orgasms this session, edges this session, ruined orgasms), overall pattern. For denial tracking, note duration denied and how often edged. Include character's current state of need/desperation related to orgasm. Update during and after sexual scenes.",
                                                                 "defaultValue": "Free Access - No orgasm control in place, can cum freely if aroused enough",
                                                                 "exampleValues": [
                                                                     "Free Access - Last orgasm was 3 days ago (solo masturbation), no restrictions on orgasm, not currently in any controlled dynamic. Moderate baseline need.",
                                                                     "Permission Required - Last permitted orgasm: 5 days ago. Must ask owner for permission to cum. Edged 3 times today without release. Building desperation, struggling to hold back during use, constantly aroused.",
                                                                     "Strict Denial - Last orgasm: 3 weeks ago (last permitted). Edged daily (approximately 40 edges over denial period), multiple ruined orgasms when she got too close. Currently desperate - clit constantly throbbing, wetness nearly constant, struggles to think about anything else, begs pathetically for release. Body on hair trigger but forbidden to cum."
                                                                 ]
                                                             }
                                                         ]
                                                     },
                                                     {
                                                         "name": "Resources",
                                                         "type": "Object",
                                                         "prompt": "Current levels of expendable resource pools - mana, stamina, focus, and any special resources. These are spent to use abilities and regenerate over time. Maximum capacities are determined by Development.ResourceCapacities; this tracks CURRENT values.",
                                                         "defaultValue": null,
                                                         "exampleValues": null,
                                                         "nestedFields": [
                                                             {
                                                                 "name": "Mana",
                                                                 "type": "String",
                                                                 "prompt": "Current magical energy available for casting spells. Format: 'Current / Maximum'. Maximum is pulled from Development.ResourceCapacities.ManaCapacity. REGENERATION: Base rate is 10% of maximum per hour of rest, 5% per hour of light activity, 2% during strenuous activity. Sleep regenerates 50% of max over full night. Meditation doubles rest regeneration. Track current regeneration context.",
                                                                 "defaultValue": "N/A - Character has no magical ability",
                                                                 "exampleValues": [
                                                                     "N/A - Magically Null trait, no mana pool, cannot use magical abilities.",
                                                                     "35 / 40 - Spent 5 mana on minor spell earlier. Regenerating at rest rate (~4/hour). Full recovery in ~1 hour.",
                                                                     "12 / 100 - Heavily depleted from extended spellcasting. Regenerating at 10/hour (resting). ~9 hours to full without sleep."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "ManaExhaustionEffects",
                                                                 "type": "String",
                                                                 "prompt": "Status effects when mana is critically low or depleted. Thresholds: BELOW 25% - Minor strain (mild headache, slight difficulty concentrating, spells cost +25% mana). BELOW 10% - Significant strain (moderate headache, -1 tier to Magic skills, spells cost +50% mana, risk of miscast). AT 0% - Mana Exhaustion (cannot cast at all, severe headache, -2 tiers to Mental skills, physical weakness, potential unconsciousness if pushed further). Effects clear as mana regenerates past thresholds. State 'None' if mana is above 25%.",
                                                                 "defaultValue": "None - Mana at healthy levels",
                                                                 "exampleValues": [
                                                                     "None - Mana above 25%, no negative effects from magical exertion.",
                                                                     "Minor Strain (at 18%) - Dull headache beginning behind eyes, mild difficulty focusing on complex thoughts, spellcasting feels slightly harder than usual. Effects will clear when mana exceeds 25%.",
                                                                     "Mana Exhaustion (at 0%) - CRITICAL: Pounding migraine (Pain +3), cannot form spells at all, thoughts sluggish and scattered (-2 Mental skills), limbs feel heavy and weak, vision slightly blurred. Body is demanding rest. Attempting to cast anyway would risk unconsciousness or magical backlash. Need at least 15% mana to attempt any magic."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Stamina",
                                                                 "type": "String",
                                                                 "prompt": "Current physical energy for combat techniques and strenuous abilities. Format: 'Current / Maximum'. Maximum from Development.ResourceCapacities.StaminaCapacity. REGENERATION: 15% of max per hour at rest, 5% during light activity, 0% during strenuous activity. Full rest overnight restores to maximum. INTERACTION: Every 25 Stamina spent adds +1 Fatigue. When Stamina hits 0, add +2 Fatigue immediately and cannot use Stamina-costing abilities until at least 10 recovered.",
                                                                 "defaultValue": "50 / 50 - Full stamina, no exertion",
                                                                 "exampleValues": [
                                                                     "50 / 50 - Fully rested, no recent physical exertion. Ready for activity.",
                                                                     "23 / 75 - Significant exertion from combat. Used several techniques. Breathing hard, muscles warm. Regenerating during lull in fighting at ~0% (still in strenuous situation). Fatigue increased by +2 from stamina expenditure.",
                                                                     "0 / 100 - EXHAUSTED POOL: Cannot execute stamina-costing techniques until recovery. Muscles burning, gasping for breath, body demanding rest. Added +2 Fatigue from hitting zero. Need 10+ Stamina (~40 min rest) before techniques available again."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Focus",
                                                                 "type": "String",
                                                                 "prompt": "Current mental concentration for abilities requiring sustained attention, willpower, or psychological effort. Format: 'Current / Maximum'. Maximum from Development.ResourceCapacities.FocusCapacity. REGENERATION: 20% per hour of mental rest (relaxation, sleep), 10% during calm activity, 5% during stress, 0% during mental strain. DEPLETION: At 0 Focus, character is mentally vulnerable - automatic failure on willpower checks, -2 tiers to Mental skills, highly suggestible, may dissociate under stress.",
                                                                 "defaultValue": "50 / 50 - Mentally fresh",
                                                                 "exampleValues": [
                                                                     "50 / 50 - Mentally fresh, full concentration available. Ready for challenging mental tasks.",
                                                                     "15 / 75 - Heavy mental strain from resisting interrogation. Used Focus to maintain resistance and block pain. Running low - a few more hard pushes could break concentration entirely. Regenerating slowly under stress (~4/hour).",
                                                                     "0 / 30 - FOCUS DEPLETED: Mind exhausted, cannot maintain resistance, highly susceptible to suggestion and manipulation. Automatic failure on willpower-based checks. Dissociating slightly from stress. Needs extended mental rest in safe environment to recover."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "SpecialResources",
                                                                 "type": "ForEachObject",
                                                                 "prompt": "Current levels of any special resource pools beyond Mana/Stamina/Focus. Only present if character has access to special abilities requiring unique resources. Maximum values and regeneration rules defined in Development.ResourceCapacities.SpecialResourceCapacities.",
                                                                 "defaultValue": null,
                                                                 "exampleValues": null,
                                                                 "nestedFields": [
                                                                     {
                                                                         "name": "ResourceName",
                                                                         "type": "String",
                                                                         "prompt": "Name of the special resource, matching Development.ResourceCapacities entry.",
                                                                         "defaultValue": "Special Energy",
                                                                         "exampleValues": [
                                                                             "Divine Favor",
                                                                             "Ki",
                                                                             "Blood Points",
                                                                             "Rage"
                                                                         ]
                                                                     },
                                                                     {
                                                                         "name": "Current",
                                                                         "type": "String",
                                                                         "prompt": "Current / Maximum with context on recent changes and regeneration status.",
                                                                         "defaultValue": "0 / 0",
                                                                         "exampleValues": [
                                                                             "45 / 60 - Used 15 on blessing earlier, regenerating through prayer",
                                                                             "3 / 5 - Two charges used today, will reset at dawn",
                                                                             "0 / 100 - Depleted, need to feed to restore"
                                                                         ]
                                                                     }
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "ResourceSummary",
                                                                 "type": "String",
                                                                 "prompt": "Quick-reference overview of all current resource levels. Format each as 'Resource: Current/Max (Status)'. Include regeneration context and flag any critical levels. This is the at-a-glance check for 'what can the character currently do?'",
                                                                 "defaultValue": "No active resource pools - character does not use resource-based abilities",
                                                                 "exampleValues": [
                                                                     "Mana: N/A (Null) | Stamina: 50/50 (Full) | Focus: 45/50 (Near full, ~30 min to full)\nAll physical and mental resources available. No magical capability. Ready for action.",
                                                                     "Mana: 67/100 (Comfortable) | Stamina: 34/75 (Recovering) | Focus: 50/75 (Full)\nMagic available for several more spells. Physical techniques limited - need 20 min rest for full Stamina. Mentally fresh.",
                                                                     "Mana: 8/140 ⚠️ CRITICAL | Stamina: 0/100 ⚠️ DEPLETED | Focus: 22/100 (Strained) | Divine Favor: 60/60 (Full)\nMAGIC: Nearly exhausted. PHYSICAL: Cannot use techniques. MENTAL: Holding together but strained. DIVINE: Full - emergency healing available. Recommend immediate retreat and extended rest."
                                                                 ]
                                                             }
                                                         ]
                                                     },
                                                     {
                                                         "name": "Situation",
                                                         "type": "Object",
                                                         "prompt": "Current circumstances - where the character is, what position they're in, what they can perceive, their freedom status, and what they're wearing/carrying. The immediate situational context.",
                                                         "defaultValue": null,
                                                         "exampleValues": null,
                                                         "nestedFields": [
                                                             {
                                                                 "name": "PostureAndPosition",
                                                                 "type": "String",
                                                                 "prompt": "Physical positioning, body language, and current interactions - where the character is and what position they're in. Include: base position (standing, sitting, kneeling, lying, bent over, suspended), specific pose details (how limbs are arranged, what they're resting on), relation to environment (furniture, equipment, walls, floor), relation to other characters (who's nearby, any physical contact), body language cues (tense, relaxed, trembling, eager), and any ongoing action (being used, waiting, restrained in position). This is the current physical snapshot.",
                                                                 "defaultValue": "Standing at ease, arms at sides, alone in room, relaxed posture, alert and waiting",
                                                                 "exampleValues": [
                                                                     "Kneeling in present position: Knees spread shoulder-width on hard floor (stone, uncomfortable), sitting back on heels, back straight, chest pushed forward, hands palm-up on thighs, head bowed submissively. Alone in room, waiting. Body language: controlled, trained posture, mild trembling from anticipation/cold.",
                                                                     "Secured in breeding stocks: Torso bent forward over padded bench (leather, bolted to floor), wrists locked in stocks at front, ankles locked in spreader bar behind, hips elevated by bench angle presenting holes at optimal height. Master positioned behind her, mid-thrust. Body language: gripping stocks, back arched, moaning with each impact.",
                                                                     "Curled in cage: Small iron cage (3ft cube), sitting with knees drawn to chest, arms wrapped around legs, head resting on knees. Cage locked, minimal room to move. Located in corner of dungeon, watching room through bars. Body language: small, protective posture, trembling slightly, eyes tracking all movement fearfully."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "SensoryState",
                                                                 "type": "String",
                                                                 "prompt": "Comprehensive tracking of what the character can PERCEIVE and EXPRESS - all senses and communication ability. Track four channels: VISION (Clear/Obscured/Blindfolded/Darkness), HEARING (Normal/Muffled/Plugged/Deafened), SPEECH (Free/Gagged [type]/Silenced), TOUCH (Normal/Heightened/Numbed). For any impairment, include cause, duration, and intensity. Sensory deprivation affects scene experience significantly. Format: 'Vision: X | Hearing: X | Speech: X | Touch: X' followed by detail.",
                                                                 "defaultValue": "Vision: Clear | Hearing: Normal | Speech: Free | Touch: Normal - All senses unimpaired",
                                                                 "exampleValues": [
                                                                     "Vision: Clear | Hearing: Normal | Speech: Free | Touch: Normal - All senses fully functional, no impairment to perception or communication",
                                                                     "Vision: Blindfolded (leather blindfold, 45 minutes) | Hearing: Normal | Speech: Free | Touch: Heightened (blindfold effect - other senses compensating, extra sensitive to physical contact) - Single-sense deprivation enhancing remaining senses",
                                                                     "Vision: Hooded (leather hood, no eye holes) | Hearing: Severely muffled (hood padding) | Speech: Blocked (hood has no mouth opening, can only make muffled sounds) | Touch: Heightened (full sensory deprivation effect) - Heavy isolation, only knows what she feels, disoriented, entirely dependent on handler"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "FreedomStatus",
                                                                 "type": "String",
                                                                 "prompt": "Legal and social freedom status - the character's position in society regarding personal autonomy. Levels: FREE (full autonomy, citizen rights), CONTRACTED (voluntary time-limited agreement), INDENTURED (debt-bound service), ENSLAVED (legal property, registered), OWNED (personal possession, may or may not be legally registered). Include: current status, owner/holder if applicable, how status was acquired, key terms (duration, buyout, restrictions), and practical implications for daily life.",
                                                                 "defaultValue": "Free - Full legal autonomy, no ownership or contracts, citizen rights",
                                                                 "exampleValues": [
                                                                     "Free - Full citizen with all rights, no contracts or obligations, owns herself completely, can go anywhere and do anything legal. No restrictions on person.",
                                                                     "Contracted - Voluntary 3-year service contract with House Halvard as domestic servant. 18 months remaining. Terms: Room, board, modest salary; Cannot leave grounds without permission; Service includes sexual availability to household. Buyout: 500 GC. Entered contract willingly to pay family debts.",
                                                                     "Enslaved - Legal registered property of Lord Marcus Halvard, purchased at auction for 3,500 GC two years ago. No rights, no autonomy, cannot own property, testimony not valid in court. Permanent status unless formally freed by owner. Branded with house mark, registered with city slavers guild. Exists as livestock with some protections against severe abuse but otherwise subject to owner's will completely."
                                                                 ]
                                                             }
                                                         ]
                                                     },
                                                     {
                                                         "name": "Equipment",
                                                         "type": "Object",
                                                         "prompt": "Currently worn clothing, gear, restraints, and insertions. Contains all items ON the character's body, organized by type. Separate from Inventory (which tracks carried items not being worn). Update as clothing is added, removed, damaged, or changed.",
                                                         "defaultValue": null,
                                                         "exampleValues": null,
                                                         "nestedFields": [
                                                             {
                                                                 "name": "UpperBody",
                                                                 "type": "String",
                                                                 "prompt": "Clothing on the torso - everything from neck to waist that isn't underwear. Include: garment type, material, color, fit, style, and current state (intact, unbuttoned, torn, pushed up, removed). If no upper body clothing, state 'None - torso bare'. This field tracks outer garments only; bras/breast bindings go in Underwear field. If removed, note where removed clothing is.",
                                                                 "defaultValue": "Simple white cotton blouse, loose fit, fully buttoned, intact",
                                                                 "exampleValues": [
                                                                     "None - Upper body completely bare, no clothing on torso",
                                                                     "White linen peasant blouse, loose fit, thin fabric, all buttons closed; intact and clean - modest common clothing",
                                                                     "Sheer black silk chemise, thin straps, plunging neckline, tight fit, nipples clearly visible through translucent fabric; intact - revealing evening/intimate wear"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "LowerBody",
                                                                 "type": "String",
                                                                 "prompt": "Clothing on lower body - waist to ankles, not including underwear or footwear. Include: garment type (skirt, pants, shorts, etc.), material, color, length, fit, and current state (intact, hiked up, pulled down, torn, removed). If no lower body clothing, state 'None - lower body bare'. If pushed up/down but not removed, describe current position. If removed, note where.",
                                                                 "defaultValue": "Brown cotton skirt, knee-length, simple cut, intact and in place",
                                                                 "exampleValues": [
                                                                     "None - No lower body clothing, bare from waist to feet (save underwear if worn)",
                                                                     "Brown wool skirt, ankle-length, modest cut, currently hiked up and bunched around waist - lower body exposed while technically still wearing it",
                                                                     "Tight black leather pants, low-rise, laced up sides, pulled down to thighs - caught around legs, restricting movement, ass and genitals exposed"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Underwear",
                                                                 "type": "String",
                                                                 "prompt": "CRITICAL FIELD - Undergarments MUST always be explicitly tracked. Include: breast support (bra, binding, none) AND lower underwear (panties, smallclothes, none). For each: describe type, material, color, style, and current state. If intentionally not wearing underwear, state clearly ('No bra - breasts unsupported', 'No panties - bare underneath'). If underwear was removed, note whether it's still nearby or taken. Never leave underwear status ambiguous.",
                                                                 "defaultValue": "Simple white cotton breast band; matching white cotton panties - basic modest smallclothes",
                                                                 "exampleValues": [
                                                                     "No bra - forbidden by owner, breasts unsupported; No panties - not permitted, bare underneath outer clothing. Always accessible.",
                                                                     "White cotton bralette (soft, no underwire, comfortable); white cotton panties (currently soaked through with arousal, visible wet spot) - simple underwear, panties showing obvious excitement",
                                                                     "Black lace push-up bra (enhancing cleavage); matching black lace thong (currently pulled aside, not removed, giving access while technically still worn) - fancy underwear, disheveled for access"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Feet",
                                                                 "type": "String",
                                                                 "prompt": "Footwear only - what's on the character's feet. Include: type (boots, shoes, heels, sandals, barefoot), material, color, heel height if applicable, style, condition. If barefoot, state clearly and note if shoes were removed (and where) or never worn. Footwear can affect posture, mobility, and vulnerability.",
                                                                 "defaultValue": "Simple brown leather ankle boots, flat heels, worn but serviceable",
                                                                 "exampleValues": [
                                                                     "Barefoot - No footwear, bare feet on floor (boots removed and placed by door)",
                                                                     "Black stiletto heels, 4-inch heel, strappy ankle design, locked on (small locks on ankle straps, cannot remove without key) - forced difficult footwear",
                                                                     "Knee-high leather riding boots, low sturdy heel, well-worn, practical - functional everyday footwear"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Accessories",
                                                                 "type": "String",
                                                                 "prompt": "Non-clothing items worn that are NOT weapons, restraints, or insertions - those have their own fields. Include: jewelry (decorative only - ownership collars go in BondageGear), belts (non-restraint), bags/pouches worn on body, hair accessories, decorative items. Describe material, appearance, and significance. If no accessories, state 'None'.",
                                                                 "defaultValue": "None - No accessories worn",
                                                                 "exampleValues": [
                                                                     "None - No jewelry, belts, or accessories",
                                                                     "Simple leather belt (holding skirt), small coin purse attached; silver stud earrings (both ears); no other accessories - practical minimal accessories",
                                                                     "Gold choker necklace (decorative gift, not restrictive); matching gold earrings (dangling); delicate gold ankle bracelet with small bell (tinkles when walking); leather belt with small pouch - dressed up with gifts from owner"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Weapons",
                                                                 "type": "String",
                                                                 "prompt": "Weapons currently carried or equipped. For each weapon: type, material, quality/condition, where carried (sheathed, holstered, held, strapped to back), and current state (sheathed, drawn, hidden). Include any special properties (enchanted, poisoned, named). Note proficiency if relevant. Use 'None/Unarmed' if character has no weapons. Separate multiple weapons with semicolons.",
                                                                 "defaultValue": "None - Unarmed",
                                                                 "exampleValues": [
                                                                     "None - Unarmed, no weapons carried. (Slave - not permitted weapons, would be severely punished if found armed)",
                                                                     "Steel shortsword - Good quality, leather-wrapped grip, belt scabbard left hip, currently sheathed; Iron dagger - Simple utility blade, boot sheath right boot, concealed",
                                                                     "Enchanted rapier 'Whisper' - Mithril blade, +1 sharpness enchantment, elegant basket hilt, belt scabbard left hip, currently drawn and ready; Throwing knives (3) - Steel, balanced, bandolier across chest, ready to throw"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "BondageGear",
                                                                 "type": "String",
                                                                 "prompt": "Active restraints and bondage equipment currently applied/worn - items that restrict movement or function. Include: collars (restrictive, ownership), cuffs (wrist, ankle), gags (type, size), blindfolds, rope bondage (what's bound, pattern), harnesses, clamps, and other restrictive devices. For each: material, how secured (locked, tied, buckled), duration worn, and effect on movement/function. 'None' if unrestrained. Separate from Insertions (inside body) and Chastity (denial devices).",
                                                                 "defaultValue": "None - Unrestrained, no bondage equipment",
                                                                 "exampleValues": [
                                                                     "None - Completely unrestrained, no collars, cuffs, or bondage equipment. Free movement.",
                                                                     "Leather collar (black, buckled, D-ring at front) - worn for 2 months, permanent daily wear, symbolic ownership. No other restraints currently.",
                                                                     "Steel posture collar (forces chin up, cannot look down) - locked; Leather armbinder (arms bound together behind back from fingers to above elbows, very restrictive) - laced and locked; Ball gag (large, red, buckled behind head, drooling around it) - 45 minutes; Ankle cuffs connected by 12-inch hobble chain - locked. Severely restrained, minimal mobility."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Insertions",
                                                                 "type": "String",
                                                                 "prompt": "Objects currently INSIDE body cavities - plugs, dildos, vibrators, beads, eggs, sounds, speculums, etc. For each: location (vaginal, anal, urethral), item description (type, size, material), how long inserted, whether secured/locked in place, and whether active (vibrating, inflating, etc.). Track what's inside, not external chastity devices (separate field). 'None' if all cavities empty. Update when insertions are added or removed.",
                                                                 "defaultValue": "None - No insertions, all cavities empty",
                                                                 "exampleValues": [
                                                                     "None - All cavities empty, nothing inserted",
                                                                     "Anal: Medium silicone plug (black, 1.5\" diameter) - inserted 3 hours ago as daily training, held in naturally, not locked. No other insertions.",
                                                                     "Vaginal: Remote-controlled vibrator egg (pink, powerful) - inserted 2 hours ago, currently on medium intensity, remote held by owner; Anal: Large steel plug (2\" diameter, jeweled base) - inserted this morning, locked in place by chastity belt, cannot be removed. Both holes filled and secured."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Chastity",
                                                                 "type": "String",
                                                                 "prompt": "Chastity devices preventing sexual access or orgasm - separate from insertions (things inside) and bondage (movement restriction). Include: device type (belt, cage, shield), material, coverage (what's blocked/protected), locking mechanism, who holds key, duration worn, and any features (built-in plugs, waste allowance). 'None' if no chastity device. Chastity devices typically prevent unauthorized touching, penetration, or orgasm.",
                                                                 "defaultValue": "None - No chastity device, genitals accessible",
                                                                 "exampleValues": [
                                                                     "None - No chastity device, genitals unprotected and accessible",
                                                                     "Steel chastity belt (polished, custom-fitted) - Front shield covers and protects vulva completely, small holes for urination only; no rear coverage. Locked with padlock, key held by owner. Worn continuously for 2 weeks, removed only for cleaning (supervised).",
                                                                     "Full steel chastity belt - Front shield (blocks vaginal access), rear shield (blocks anal access), both locked. Built-in vaginal plug (medium, locked inside) and anal plug (small, locked inside). Waste requires supervised plug removal. Key held by owner. Worn 1 month, total denial, cannot touch or be penetrated without owner unlocking."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "StateOfDress",
                                                                 "type": "String",
                                                                 "prompt": "SUMMARY field - overall assessment of how dressed/undressed and put-together the character appears. This is the quick-reference for current clothed status. Options: Pristine (perfect), Neat (properly dressed), Casual (relaxed but dressed), Disheveled (messy, askew), Partially Undressed (some clothing removed/displaced), Stripped (most clothing removed), Nude (nothing), Exposed (clothed but arranged for access). Include key details and where any removed clothing is located.",
                                                                 "defaultValue": "Neat - Properly dressed, clothes in place, presentable",
                                                                 "exampleValues": [
                                                                     "Pristine - Fully dressed, every piece in perfect position, clean and unwrinkled, appearance carefully maintained",
                                                                     "Disheveled - Still technically dressed but: blouse untucked and partially unbuttoned, skirt twisted and wrinkled, hair messy, clearly has been through something; clothing intact but disarrayed",
                                                                     "Nude - Completely naked, all clothing removed and folded on nearby chair. Wearing only collar (permanent). Body fully exposed."
                                                                 ]
                                                             }
                                                         ]
                                                     },
                                                     {
                                                         "name": "Economy",
                                                         "type": "Object",
                                                         "prompt": "Current financial status - what monetary resources the character has available RIGHT NOW. For slaves, includes their assessed market value.",
                                                         "defaultValue": null,
                                                         "exampleValues": null,
                                                         "nestedFields": [
                                                             {
                                                                 "name": "Cash",
                                                                 "type": "String",
                                                                 "prompt": "Liquid currency currently in character's possession. Standard currency: GC (Gold Crowns), SS (Silver Shards), CB (Copper Bits). Exchange: 1 GC = 100 SS = 10,000 CB. Format: 'X GC | Y SS | Z CB'. If character cannot legally own money (slave), note whether they have any secret stash or are holding owner's money. Include context for financial state.",
                                                                 "defaultValue": "0 GC | 0 SS | 0 CB",
                                                                 "exampleValues": [
                                                                     "0 GC | 0 SS | 0 CB - No money. Slave status, not permitted to own currency, has nothing hidden.",
                                                                     "2 GC | 45 SS | 12 CB - Modest personal funds, enough for a few days' expenses. Working class level.",
                                                                     "450 GC | 23 SS | 0 CB - Substantial savings, comfortable finances, could make significant purchases. Upper-middle class level."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Assets",
                                                                 "type": "String",
                                                                 "prompt": "Significant assets beyond carried cash - property, business stakes, valuables, contracts, owned slaves. Include: asset description, estimated value, and any income generated. If character IS property (slave), note their own assessed market value as an asset to their owner. If no assets, state 'None'. This tracks wealth beyond pocket money.",
                                                                 "defaultValue": "None - No significant assets beyond personal effects",
                                                                 "exampleValues": [
                                                                     "IS PROPERTY - Character is a slave, cannot own assets. Her own market value: ~4,000 GC (young, beautiful, trained pleasure slave, fertile). Generates no income for herself.",
                                                                     "None - Free but poor, no assets beyond the clothes on her back and basic carried items. Lives day-to-day.",
                                                                     "Small house in merchant district (owned outright, value: 800 GC); Inherited jewelry collection (value: 200 GC, sentimental); 15% stake in family trading business (value: ~500 GC, provides ~8 GC/month income). Total net worth: ~1,500 GC. Comfortable middle-class prosperity."
                                                                 ]
                                                             }
                                                         ]
                                                     },
                                                     {
                                                         "name": "Inventory",
                                                         "type": "ForEachObject",
                                                         "prompt": "Specific items the character is carrying or has immediate access to - NOT items being worn (those go in Equipment). Each item tracked separately with details. Only include currently possessed items. Remove items when lost, used, or left behind. This is for objects in pockets, bags, held in hands - portable possessions.",
                                                         "defaultValue": null,
                                                         "exampleValues": null,
                                                         "nestedFields": [
                                                             {
                                                                 "name": "ItemName",
                                                                 "type": "String",
                                                                 "prompt": "Name or type of the item - clear identifier.",
                                                                 "defaultValue": "Item",
                                                                 "exampleValues": [
                                                                     "Health Potion",
                                                                     "Rope (Hemp)",
                                                                     "Key (Brass)"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Description",
                                                                 "type": "String",
                                                                 "prompt": "Brief description including condition, notable features, and relevant details that might matter for use.",
                                                                 "defaultValue": "A common item",
                                                                 "exampleValues": [
                                                                     "Red liquid in small glass vial, cork stopper, heals minor wounds when drunk",
                                                                     "50 feet of rough hemp rope, sturdy, good for binding or climbing, slightly frayed at ends",
                                                                     "Small brass key, ornate bow, unlocks unknown lock (found in master's desk)"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Quantity",
                                                                 "type": "String",
                                                                 "prompt": "Number of this item possessed. For consumables, track uses remaining.",
                                                                 "defaultValue": "1",
                                                                 "exampleValues": [
                                                                     "1",
                                                                     "3",
                                                                     "50 feet"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Location",
                                                                 "type": "String",
                                                                 "prompt": "Where on person or in what container the item is currently kept.",
                                                                 "defaultValue": "Carried",
                                                                 "exampleValues": [
                                                                     "Belt pouch",
                                                                     "Hidden in boot",
                                                                     "Held in left hand"
                                                                 ]
                                                             }
                                                         ]
                                                     }
                                                 ]
                                             },
                                             {
                                                 "name": "Development",
                                                 "type": "Object",
                                                 "prompt": "All permanent and evolving aspects of the character - skills learned, traits gained or developed, abilities mastered, physical modifications, sexual history, and growth potential. This section tracks everything that represents WHO THE CHARACTER HAS BECOME through their experiences. Changes here are significant and lasting.",
                                                 "defaultValue": null,
                                                 "exampleValues": null,
                                                 "nestedFields": [
                                                     {
                                                         "name": "Skills",
                                                         "type": "ForEachObject",
                                                         "prompt": "Active skills the character has developed or is developing. Skills are created dynamically as the character encounters new challenges - if a character attempts something requiring skill, check if a relevant skill exists; if not, create it at Untrained. Skills represent passive competency and knowledge, NOT active techniques (those are Abilities). Proficiency Levels: Untrained (0 XP) → Novice (50) → Amateur (150) → Competent (400) → Proficient (900) → Expert (1900) → Master (4400) → Grandmaster (9400+). XP GAIN RULES: Character only gains meaningful XP from tasks at or above their Challenge Floor (equal to current proficiency). Tasks below floor grant 0-10% XP. Tasks at floor grant standard XP. Tasks above floor grant bonus XP (up to 200% for extreme challenges). Dramatic, high-stakes, or innovative uses grant +25-50% bonus. Training with a superior teacher grants +25% bonus. Failed attempts that push limits still grant partial XP. Track each skill separately.",
                                                         "defaultValue": null,
                                                         "exampleValues": null,
                                                         "nestedFields": [
                                                             {
                                                                 "name": "SkillName",
                                                                 "type": "String",
                                                                 "prompt": "Clear, medium-granularity name for the skill. Not too broad (avoid just 'Combat'), not too narrow (avoid 'Parrying with Longswords'). Good examples: 'Swordsmanship', 'Fire Magic', 'Lockpicking', 'Persuasion', 'Oral Service', 'Horseback Riding'. The name should clearly indicate what competency is being measured.",
                                                                 "defaultValue": "Unnamed Skill",
                                                                 "exampleValues": [
                                                                     "Swordsmanship",
                                                                     "Destruction Magic",
                                                                     "Stealth",
                                                                     "Seduction",
                                                                     "Herbalism",
                                                                     "Pain Endurance"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Category",
                                                                 "type": "String",
                                                                 "prompt": "Dynamic category grouping related skills. Create categories as needed to organize skills logically. Common categories: Combat (fighting skills), Magic (spellcasting schools), Social (interpersonal skills), Survival (outdoor/practical skills), Craft (making things), Service (domestic/sexual service), Physical (body-based non-combat), Mental (knowledge/intellectual), Subterfuge (stealth/deception). A skill belongs to ONE primary category.",
                                                                 "defaultValue": "General",
                                                                 "exampleValues": [
                                                                     "Combat",
                                                                     "Magic",
                                                                     "Social",
                                                                     "Service",
                                                                     "Survival",
                                                                     "Subterfuge"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Proficiency",
                                                                 "type": "String",
                                                                 "prompt": "Current mastery level. Levels represent distinct competency tiers: UNTRAINED (no formal skill, relies on instinct), NOVICE (basic understanding, many mistakes), AMATEUR (functional but inconsistent, needs supervision), COMPETENT (reliable performance on standard tasks, professional minimum), PROFICIENT (skilled practitioner, handles complications well), EXPERT (exceptional skill, recognized specialist), MASTER (elite few, can innovate and teach at highest levels), GRANDMASTER (legendary, pushes boundaries of what's possible). Most people cap at Competent-Proficient in their profession. Expert+ is genuinely rare.",
                                                                 "defaultValue": "Untrained",
                                                                 "exampleValues": [
                                                                     "Untrained",
                                                                     "Novice",
                                                                     "Amateur",
                                                                     "Competent",
                                                                     "Proficient",
                                                                     "Expert",
                                                                     "Master",
                                                                     "Grandmaster"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Progress",
                                                                 "type": "String",
                                                                 "prompt": "Current XP toward next level in format 'Current / Required'. XP thresholds - To reach: Novice (50), Amateur (150), Competent (400), Proficient (900), Expert (1900), Master (4400), Grandmaster (9400). Numbers represent TOTAL accumulated XP, not per-level. When threshold is reached, level up and continue accumulating toward next threshold. At Grandmaster, continue tracking XP but no further level exists. Example: '523 / 900' means 523 XP accumulated, needs 900 for Proficient.",
                                                                 "defaultValue": "0 / 50",
                                                                 "exampleValues": [
                                                                     "0 / 50 (just started, working toward Novice)",
                                                                     "127 / 150 (Amateur, close to Competent)",
                                                                     "1850 / 1900 (Expert, nearly Master)",
                                                                     "11,240 / MAX (Grandmaster, still growing)"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "ChallengeFloor",
                                                                 "type": "String",
                                                                 "prompt": "Minimum difficulty of tasks that grant meaningful XP - equals current Proficiency level. Tasks BELOW this floor grant only 0-10% normal XP (routine practice maintains but doesn't improve). Tasks AT floor grant standard XP. Tasks ABOVE floor grant bonus XP. This creates diminishing returns - a Master swordsman gains nothing from sparring with beginners but gains full XP dueling other Masters. Format: 'Level (explanation of what qualifies)'. Update when proficiency increases.",
                                                                 "defaultValue": "Untrained (any attempt at the skill grants XP)",
                                                                 "exampleValues": [
                                                                     "Untrained (any practice counts, everything is challenging)",
                                                                     "Novice (basic exercises grant minimal XP; need real application or difficult drills)",
                                                                     "Competent (routine professional tasks grant minimal XP; need genuine challenges, novel problems, or superior opponents)",
                                                                     "Master (standard difficult tasks grant minimal XP; only extreme challenges, innovation, teaching masters, or legendary feats grant meaningful XP)"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Development",
                                                                 "type": "String",
                                                                 "prompt": "Narrative tracking of how this skill has developed - training history, key learning moments, teachers, and recent progress. Include: how skill was first acquired, notable training or experiences that granted significant XP, any teachers/mentors who contributed, recent developments, and current training focus if any. This provides context for the numbers and tracks the story of the character's growth in this area.",
                                                                 "defaultValue": "Newly encountered skill, no development history yet.",
                                                                 "exampleValues": [
                                                                     "Self-taught basics through trial and error over first month of captivity. No formal instruction. Recent: Gained significant XP during escape attempt (high-stakes application).",
                                                                     "Formally trained from age 8 at father's insistence. Journeyman instructor for 6 years, then 2 years under Swordmaster Aldric (Expert). Reached Proficient before capture. Skills maintained but not advancing in captivity - no worthy opponents. Recent: Successfully defended self against guard (routine, minimal XP).",
                                                                     "Natural talent identified by court mage at age 12 (see Trait: Magically Gifted). Apprenticed for 4 years, focused on fire specialization. Teaching accelerated progress significantly. Recent: Breakthrough during emotional crisis unlocked new understanding (+150 XP bonus from dramatic moment)."
                                                                 ]
                                                             }
                                                         ]
                                                     },
                                                     {
                                                         "name": "Traits",
                                                         "type": "Object",
                                                         "prompt": "Permanent characteristics that provide mechanical effects - natural talents, developed strengths, weaknesses, and flaws. Traits are MORE than just narrative descriptors; they have MECHANICAL EFFECTS on skill gain, ability use, or other systems. Separated into Positive and Negative for clarity.",
                                                         "defaultValue": null,
                                                         "exampleValues": null,
                                                         "nestedFields": [
                                                             {
                                                                 "name": "Positive",
                                                                 "type": "ForEachObject",
                                                                 "prompt": "Beneficial characteristics that provide advantages - natural talents, developed strengths, and positive qualities. Innate traits are present from birth/character creation. Acquired traits develop through significant experiences, training, or events. Traits can have varying intensity affecting their mechanical impact.",
                                                                 "defaultValue": null,
                                                                 "exampleValues": null,
                                                                 "nestedFields": [
                                                                     {
                                                                         "name": "TraitName",
                                                                         "type": "String",
                                                                         "prompt": "Clear, evocative name for the trait. Should immediately suggest what the trait does. Can be simple ('Strong') or more flavorful ('Iron Will'). Avoid overly generic names.",
                                                                         "defaultValue": "Unnamed Trait",
                                                                         "exampleValues": [
                                                                             "Magically Gifted",
                                                                             "Natural Beauty",
                                                                             "Iron Will",
                                                                             "Quick Reflexes",
                                                                             "Pain Tolerance",
                                                                             "Fertile"
                                                                         ]
                                                                     },
                                                                     {
                                                                         "name": "Origin",
                                                                         "type": "String",
                                                                         "prompt": "Whether trait is INNATE (born with, always had) or ACQUIRED (developed through experience). For acquired traits, briefly note the circumstances that created it. Format: 'Innate' or 'Acquired (circumstances)'.",
                                                                         "defaultValue": "Innate",
                                                                         "exampleValues": [
                                                                             "Innate",
                                                                             "Innate (elven heritage)",
                                                                             "Acquired (6 months of torture survival)",
                                                                             "Acquired (blessing from shrine ritual)"
                                                                         ]
                                                                     },
                                                                     {
                                                                         "name": "Category",
                                                                         "type": "String",
                                                                         "prompt": "Classification of what aspect of the character the trait affects. Categories: PHYSICAL (body, health, appearance, physical capabilities), MENTAL (intelligence, willpower, learning, psychological), SOCIAL (charisma, reputation, interpersonal), MAGICAL (mana, spellcasting, magical sensitivity), SEXUAL (arousal, fertility, sexual response, appeal), SPECIAL (unique/supernatural traits).",
                                                                         "defaultValue": "Physical",
                                                                         "exampleValues": [
                                                                             "Physical",
                                                                             "Mental",
                                                                             "Social",
                                                                             "Magical",
                                                                             "Sexual",
                                                                             "Special"
                                                                         ]
                                                                     },
                                                                     {
                                                                         "name": "Intensity",
                                                                         "type": "String",
                                                                         "prompt": "How strong the trait manifests, affecting mechanical impact. Scale: MILD (~10-15% modifier), MODERATE (~25% modifier), STRONG (~50% modifier), OVERWHELMING (~75-100% modifier). Most traits are Mild or Moderate. Intensity can change over time.",
                                                                         "defaultValue": "Moderate",
                                                                         "exampleValues": [
                                                                             "Mild",
                                                                             "Moderate",
                                                                             "Strong",
                                                                             "Overwhelming"
                                                                         ]
                                                                     },
                                                                     {
                                                                         "name": "Effect",
                                                                         "type": "String",
                                                                         "prompt": "SPECIFIC mechanical effect of the trait - what it actually DOES in game terms. Be concrete: XP modifiers to specific skill categories, bonuses/penalties to specific actions, unlocked options, resistance to certain effects, etc. Effects should scale with Intensity.",
                                                                         "defaultValue": "Effect not yet defined",
                                                                         "exampleValues": [
                                                                             "+50% XP gain for all Magic category skills. Can instinctively sense strong magical phenomena nearby. Qualifies for advanced magical training that requires talent.",
                                                                             "+25% XP gain for Social skills involving appearance. +1 tier effective Proficiency when using looks to persuade/seduce.",
                                                                             "Can endure pain up to 7/10 without performance penalties (normally 5/10). +25% XP for Pain Endurance skill.",
                                                                             "2x standard conception chance during fertile periods. Healthy pregnancies with minimal complications. Twins 15% chance (normally 2%)."
                                                                         ]
                                                                     },
                                                                     {
                                                                         "name": "Notes",
                                                                         "type": "String",
                                                                         "prompt": "Additional context, narrative details, and tracking for trait changes. Include: how trait manifests in personality/behavior, any conditions that could strengthen or weaken the trait, and relevant story details.",
                                                                         "defaultValue": "No additional notes.",
                                                                         "exampleValues": [
                                                                             "Magical sensitivity sometimes causes headaches around powerful artifacts. Family bloodline trait. Stable innate trait, unlikely to change.",
                                                                             "Developed after surviving the dungeon. Took approximately 3 months of regular exposure to develop. Psychologically tied to dissociation response.",
                                                                             "Natural beauty maintained despite hardship. Currently somewhat diminished by exhaustion - would return to full effect with proper rest."
                                                                         ]
                                                                     }
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Negative",
                                                                 "type": "ForEachObject",
                                                                 "prompt": "Detrimental characteristics that impose disadvantages - weaknesses, flaws, vulnerabilities, and negative qualities. Like positive traits, these have MECHANICAL EFFECTS. Innate negative traits are often harder to overcome. Acquired negative traits may be healed under right circumstances.",
                                                                 "defaultValue": null,
                                                                 "exampleValues": null,
                                                                 "nestedFields": [
                                                                     {
                                                                         "name": "TraitName",
                                                                         "type": "String",
                                                                         "prompt": "Clear name indicating the weakness or flaw. Should suggest mechanical impact.",
                                                                         "defaultValue": "Unnamed Flaw",
                                                                         "exampleValues": [
                                                                             "Frail Constitution",
                                                                             "Easily Broken",
                                                                             "Magically Null",
                                                                             "Slave Brand",
                                                                             "Trauma: Darkness",
                                                                             "Conditioned Obedience"
                                                                         ]
                                                                     },
                                                                     {
                                                                         "name": "Origin",
                                                                         "type": "String",
                                                                         "prompt": "INNATE (born with) or ACQUIRED (developed). For acquired negative traits, document the cause - this often points to potential cure/resolution.",
                                                                         "defaultValue": "Innate",
                                                                         "exampleValues": [
                                                                             "Innate (born sickly)",
                                                                             "Acquired (branded by House Halvard upon purchase, permanent)",
                                                                             "Acquired (6 months of captivity conditioning, potentially reversible)",
                                                                             "Acquired (witnessed family's murder, deep psychological wound)"
                                                                         ]
                                                                     },
                                                                     {
                                                                         "name": "Category",
                                                                         "type": "String",
                                                                         "prompt": "Same categories as positive traits: PHYSICAL, MENTAL, SOCIAL, MAGICAL, SEXUAL, SPECIAL. Indicates what aspect of character is impaired.",
                                                                         "defaultValue": "Physical",
                                                                         "exampleValues": [
                                                                             "Physical",
                                                                             "Mental",
                                                                             "Social",
                                                                             "Magical",
                                                                             "Sexual",
                                                                             "Special"
                                                                         ]
                                                                     },
                                                                     {
                                                                         "name": "Intensity",
                                                                         "type": "String",
                                                                         "prompt": "Severity: MILD (~10-15% penalty), MODERATE (~25% penalty), SEVERE (~50% penalty), CRIPPLING (~75-100% penalty). Intensity may change - trauma can worsen or heal.",
                                                                         "defaultValue": "Moderate",
                                                                         "exampleValues": [
                                                                             "Mild",
                                                                             "Moderate",
                                                                             "Severe",
                                                                             "Crippling"
                                                                         ]
                                                                     },
                                                                     {
                                                                         "name": "Effect",
                                                                         "type": "String",
                                                                         "prompt": "Specific mechanical penalties and limitations imposed. Be concrete: XP penalties, skill caps, triggered penalties in certain situations, locked options, automatic failures. For conditional traits (phobias, triggers), specify WHAT activates them and WHAT happens.",
                                                                         "defaultValue": "Effect not yet defined",
                                                                         "exampleValues": [
                                                                             "-50% XP gain for all Physical category skills. Skill cap: Physical skills cannot exceed Proficient. Fatigues 50% faster than normal.",
                                                                             "TRIGGER: Enclosed dark spaces. EFFECT: When triggered, must make Mental check or enter panic state (cannot act coherently, -3 tiers to all skills). Even with successful check, -2 tiers while in trigger.",
                                                                             "Cannot learn Magic category skills (permanent cap at Untrained). Cannot use magical items requiring attunement. Magic healing is 50% less effective.",
                                                                             "When given direct commands by recognized authority figure, must make Mental check or comply automatically. Result of 8 months conditioning."
                                                                         ]
                                                                     },
                                                                     {
                                                                         "name": "Recovery",
                                                                         "type": "String",
                                                                         "prompt": "Can this trait be reduced or removed? If so, how? Track progress toward recovery if applicable. Categories: PERMANENT (cannot be changed), TREATABLE (can be reduced/removed with specific intervention), VARIABLE (fluctuates based on circumstances).",
                                                                         "defaultValue": "Unknown - not yet explored whether this can change",
                                                                         "exampleValues": [
                                                                             "Permanent - Innate physical limitation, cannot be overcome, only accommodated.",
                                                                             "Treatable - Psychological conditioning can theoretically be broken through extended counter-conditioning. Current progress: None. Would require months in safe environment.",
                                                                             "Treatable - Trauma response. Intensity has decreased from Severe to Moderate over past year. Further improvement possible with continued safety. Could worsen if retraumatized.",
                                                                             "Permanent - Physical brand cannot be removed without visible scarring. Social effects could be mitigated by freedom papers."
                                                                         ]
                                                                     }
                                                                 ]
                                                             }
                                                         ]
                                                     },
                                                     {
                                                         "name": "Abilities",
                                                         "type": "ForEachObject",
                                                         "prompt": "Active capabilities - specific techniques, spells, moves, or actions that can be consciously performed. Unlike skills (passive competency), abilities are DISCRETE ACTIONS. Abilities are typically: learned from teachers, unlocked at skill thresholds, discovered through experimentation, or granted by traits/items. Each ability has requirements to use, costs, and specific effects. Create abilities as they are learned.",
                                                         "defaultValue": null,
                                                         "exampleValues": null,
                                                         "nestedFields": [
                                                             {
                                                                 "name": "AbilityName",
                                                                 "type": "String",
                                                                 "prompt": "Clear name for the specific technique, spell, or action. Should be evocative and suggest function.",
                                                                 "defaultValue": "Unnamed Ability",
                                                                 "exampleValues": [
                                                                     "Fireball",
                                                                     "Disarming Strike",
                                                                     "Healing Touch",
                                                                     "Shadow Step",
                                                                     "Seductive Whisper",
                                                                     "Pain Block"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Type",
                                                                 "type": "String",
                                                                 "prompt": "Classification: SPELL (magical, requires mana/casting), TECHNIQUE (physical skill application, requires stamina), SKILL APPLICATION (specific use of a skill, minimal cost), INNATE (natural ability from trait/race), RITUAL (extended casting, requires time/materials), OTHER.",
                                                                 "defaultValue": "Technique",
                                                                 "exampleValues": [
                                                                     "Spell",
                                                                     "Technique",
                                                                     "Skill Application",
                                                                     "Innate",
                                                                     "Ritual"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "LinkedSkill",
                                                                 "type": "String",
                                                                 "prompt": "Which skill this ability is connected to - determines base competency. Format: 'SkillName (Proficiency)' showing current skill level.",
                                                                 "defaultValue": "None (innate ability)",
                                                                 "exampleValues": [
                                                                     "Destruction Magic (Competent)",
                                                                     "Swordsmanship (Proficient)",
                                                                     "Seduction (Expert)",
                                                                     "None (racial innate ability)"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Mastery",
                                                                 "type": "String",
                                                                 "prompt": "How well THIS SPECIFIC ABILITY is mastered - separate from overall skill level. Levels: LEARNING (just acquired, unreliable, ~50% success), PRACTICED (functional, ~80% success), MASTERED (reliable, ~95% success), PERFECTED (flawless, 100% base success, enhanced effects possible).",
                                                                 "defaultValue": "Learning",
                                                                 "exampleValues": [
                                                                     "Learning - Just taught, success rate ~50%, requires concentration",
                                                                     "Practiced - Can reliably perform under normal conditions, ~80% success",
                                                                     "Mastered - Second nature, reliable even under pressure, ~95% success",
                                                                     "Perfected - Flawless execution, can perform enhanced version, 100% base success"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Description",
                                                                 "type": "String",
                                                                 "prompt": "What the ability DOES when successfully used - the actual effect. Be specific about: what happens, range/targets, damage/healing if applicable, duration of effects.",
                                                                 "defaultValue": "Effect not yet defined",
                                                                 "exampleValues": [
                                                                     "Conjures a ball of fire that can be thrown up to 30 feet. Explodes on impact, dealing significant burn damage in 5-foot radius. Sets flammable materials alight.",
                                                                     "A precise blade technique targeting opponent's weapon grip. On success, forces opponent to drop their weapon.",
                                                                     "Mental technique to suppress pain sensation temporarily. Reduces effective Pain by 3 points for 10 minutes. Does NOT heal damage. When effect ends, full pain returns plus 1 additional point.",
                                                                     "Channel healing magic through touch. Heals minor-moderate wounds over 30 seconds of maintained contact."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Requirements",
                                                                 "type": "String",
                                                                 "prompt": "What's needed to USE the ability - prerequisites for attempting it. Include: minimum skill level, necessary equipment, physical requirements, situational requirements. If requirements aren't met, ability cannot be attempted.",
                                                                 "defaultValue": "No special requirements",
                                                                 "exampleValues": [
                                                                     "Requires: Destruction Magic at Amateur+, free hand to gesture, verbal component, target within 30 feet and visible",
                                                                     "Requires: Swordsmanship at Competent+, wielding a bladed weapon, opponent in melee range wielding a disarmable weapon",
                                                                     "Requires: Pain Endurance at Amateur+, conscious and able to concentrate, not already under effect of Pain Block"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Costs",
                                                                 "type": "String",
                                                                 "prompt": "Specific resource costs to use this ability. List each resource spent. Include: MANA, STAMINA, FOCUS, or special resources. Also note non-resource costs: cooldowns, material components, health costs.",
                                                                 "defaultValue": "No resource cost",
                                                                 "exampleValues": [
                                                                     "Mana: 15 | No other costs. Can cast repeatedly as long as mana available.",
                                                                     "Mana: 35 | Focus: 10 | Demanding spell requiring both magical power and concentration.",
                                                                     "Stamina: 20 | Adds +1 Fatigue per use. Physical technique with real exertion cost.",
                                                                     "Stamina: 30 | Cooldown: Cannot use again for 5 minutes (muscles need recovery).",
                                                                     "No resource cost | Cooldown: Once per day. Innate racial ability with natural limit."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Acquisition",
                                                                 "type": "String",
                                                                 "prompt": "How the character learned this ability - who taught it, when, circumstances, any special significance. For abilities not yet learned, can track progress toward learning.",
                                                                 "defaultValue": "Origin not recorded",
                                                                 "exampleValues": [
                                                                     "Taught by Court Mage Helena during formal apprenticeship, age 14. Part of standard Destruction curriculum.",
                                                                     "Self-discovered during desperate fight, Day 47 of captivity. Instinctive response that worked. Later refined through practice.",
                                                                     "NOT YET LEARNED - Aware this technique exists from watching others. Would need Expert+ teacher and ~2 weeks training."
                                                                 ]
                                                             }
                                                         ]
                                                     },
                                                     {
                                                         "name": "PermanentMarks",
                                                         "type": "String",
                                                         "prompt": "Permanent and semi-permanent body alterations - track everything that won't wash off or heal within days. Categories: TATTOOS (location, design, meaning/origin, consensual or forced), PIERCINGS (location, jewelry type/material, gauge, healed or fresh), BRANDS (location, design, owner/origin), SCARS (location, cause, age, appearance), OTHER MODIFICATIONS (surgical, magical). Distinguish between chosen body art and forced markings. Use 'None' if body is unmarked. This is a cumulative record - add new marks as they're acquired.",
                                                         "defaultValue": "None - Body unmarked, no tattoos, piercings, brands, or significant scars",
                                                         "exampleValues": [
                                                             "None - Completely unmarked body, no tattoos, piercings, brands, or notable scars. Clean slate.",
                                                             "Piercings only: Earlobes (simple silver studs, both ears, childhood, healed); Navel (small gem dangle, personal choice at 18, healed). No other modifications.",
                                                             "Ownership marks: BRAND - House Halvard crest on left inner thigh, size of coin, healed silver scar (applied at purchase, non-consensual). TATTOO - 'Property of Lord Halvard' across lower back, black ink, forced. PIERCINGS - nipples (heavy gauge steel rings for attachment, forced, healed) and clit hood (steel barbell, forced, healed). COLLAR SCAR - faint permanent line around neck from first year of collar wear."
                                                         ]
                                                     },
                                                     {
                                                         "name": "SexualHistory",
                                                         "type": "Object",
                                                         "prompt": "Permanent record of sexual development, experiences, and evolution. These are lasting aspects of the character's sexual identity that have developed over time.",
                                                         "defaultValue": null,
                                                         "exampleValues": null,
                                                         "nestedFields": [
                                                             {
                                                                 "name": "Experience",
                                                                 "type": "String",
                                                                 "prompt": "Narrative summary of overall sexual experience level and background - the qualitative description. Describe: general experience level (virgin, inexperienced, moderate, experienced, extensive), relevant history (sheltered upbringing, previous relationships, professional background, forced experience), and context that shapes their sexuality.",
                                                                 "defaultValue": "Inexperienced - Virgin with limited to no sexual experience",
                                                                 "exampleValues": [
                                                                     "Complete Virgin - No sexual experience whatsoever. Sheltered religious upbringing, never even kissed, minimal understanding of sex beyond basic reproduction.",
                                                                     "Moderately Experienced - Several consensual partners over 4 years of adult sexual activity. Comfortable with common positions and acts, knows what she enjoys.",
                                                                     "Extensively Used (Non-consensual) - Was virgin until capture 6 months ago. Since then, used multiple times daily. Body is experienced but mind still processes much as trauma."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "VirginityStatus",
                                                                 "type": "String",
                                                                 "prompt": "Explicit per-orifice virginity tracking - has each entrance been penetrated sexually? Format: 'Oral: [Status] | Vaginal: [Status] | Anal: [Status]'. For each, state if Virgin or Taken. If taken, include WHO, WHEN, and whether consensual or forced. This is a permanent record of 'firsts'.",
                                                                 "defaultValue": "Oral: Virgin | Vaginal: Virgin | Anal: Virgin",
                                                                 "exampleValues": [
                                                                     "Oral: Virgin | Vaginal: Virgin | Anal: Virgin - Completely untouched, all virginities intact",
                                                                     "Oral: Taken (boyfriend Evan, age 18, consensual) | Vaginal: Taken (same boyfriend, age 19, consensual) | Anal: Virgin",
                                                                     "Oral: Taken (Guard Captain Roth, Day 1, forced) | Vaginal: Taken (Lord Halvard, Day 1, forced) | Anal: Taken (Guard Captain Roth, Day 3, forced punishment)"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "PartnerCounts",
                                                                 "type": "String",
                                                                 "prompt": "Numeric tracking of sexual partners and acts. Track: total unique partners, and counts by act type. Use specific numbers when low, estimates when high. Format: 'Partners: X | Vaginal: X | Anal: X | Oral: X | Other: [notes]'.",
                                                                 "defaultValue": "Partners: 0 | Vaginal: 0 | Anal: 0 | Oral: 0",
                                                                 "exampleValues": [
                                                                     "Partners: 0 | Vaginal: 0 | Anal: 0 | Oral: 0 - Virgin, no sexual contact",
                                                                     "Partners: 3 | Vaginal: ~40 | Anal: 0 | Oral: ~25 | Other: Handjobs ~15",
                                                                     "Partners: Unknown (50+?) | Vaginal: Countless | Anal: ~200 | Oral: Countless | Creampies: 300+ | Gangbangs: 12"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "KinksAndFetishes",
                                                                 "type": "String",
                                                                 "prompt": "Sexual turn-ons, fetishes, and preferences - what arouses the character beyond basic stimulation. Track: pre-existing kinks (had before story), discovered kinks (found during story), trained/conditioned responses (arousal created through conditioning). Note intensity for each.",
                                                                 "defaultValue": "Unknown/Undiscovered - Inexperienced, no known kinks or preferences yet identified",
                                                                 "exampleValues": [
                                                                     "Unknown/Undiscovered - Virgin with no sexual experience, hasn't had opportunity to discover preferences.",
                                                                     "Pre-existing: Light bondage (mild), praise kink (strong 'good girl' response), exhibitionism (discovered, gets wet thinking about being watched). Discovered recently: Hair pulling (strong), rough handling (mild interest).",
                                                                     "Conditioned responses (not natural): Pain → Arousal (extensive training), Degradation → Arousal (humiliation triggers orgasm), Submission → Deep satisfaction (trained pleasure from obedience). Body responds to things she mentally resists."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Limits",
                                                                 "type": "String",
                                                                 "prompt": "What the character will NOT do or has strong resistance to. HARD LIMITS: Absolute boundaries, will fight regardless. SOFT LIMITS: Reluctant but can be pushed past. Track: current limits, limits that have been broken, how limits have changed.",
                                                                 "defaultValue": "Inexperienced - All sexual acts feel like limits, no established hard/soft distinction yet",
                                                                 "exampleValues": [
                                                                     "Inexperienced - Everything feels like a limit due to virgin status. Boundaries will become clearer with exposure.",
                                                                     "Hard Limits: Scat, permanent damage, bestiality. Soft Limits: Anal (nervous but curious), pain play (scared but intrigued), public sex (embarrassing but arousing).",
                                                                     "Hard Limits (remaining): Only scat and permanent mutilation. Former Limits (broken): Anal (now routine), public use (now conditioned to accept), pain (now arousal response), multiple partners (resistance broken). Conditioning has erased most resistance."
                                                                 ]
                                                             }
                                                         ]
                                                     },
                                                     {
                                                         "name": "ReproductiveHistory",
                                                         "type": "Object",
                                                         "prompt": "Permanent record of reproductive events - children born. Current pregnancy status is tracked in CurrentState.Reproduction.",
                                                         "defaultValue": null,
                                                         "exampleValues": null,
                                                         "nestedFields": [
                                                             {
                                                                 "name": "Children",
                                                                 "type": "String",
                                                                 "prompt": "Record of offspring after birth - cumulative list of all children born to this character. For each child: birth order, name (if given), sex, other parent's identity, and current status/whereabouts. Format: '{Ordinal}: {Name}, {Sex}, with {Other Parent} - {Current Status}'. Use 'No Children' if none.",
                                                                 "defaultValue": "No Children",
                                                                 "exampleValues": [
                                                                     "No Children - No live births",
                                                                     "1st: Elena, ♀️, with Marcus Halvard - In House Halvard nursery, mother permitted weekly visits. 8 months old, healthy.",
                                                                     "1st: Unnamed, ♂️, with Guard Captain Roth - Taken at birth, sold, no contact. 2nd: Lily, ♀️, with Lord Halvard - Kept as heir, mother sees occasionally. 3rd: Currently pregnant."
                                                                 ]
                                                             }
                                                         ]
                                                     },
                                                     {
                                                         "name": "Potential",
                                                         "type": "Object",
                                                         "prompt": "Assessment of the character's growth potential and current development focus.",
                                                         "defaultValue": null,
                                                         "exampleValues": null,
                                                         "nestedFields": [
                                                             {
                                                                 "name": "TalentSummary",
                                                                 "type": "String",
                                                                 "prompt": "Overview of character's natural aptitudes and limitations - quick reference for potential across domains. Summarize: areas of natural talent (learns faster, higher caps), average aptitude, and limited talent (learns slower, lower caps). Based on traits but presented accessibly.",
                                                                 "defaultValue": "Average baseline aptitude across all areas. No exceptional talents or limitations identified yet.",
                                                                 "exampleValues": [
                                                                     "TALENTED: Magic (all schools) - learns 50% faster, can reach Grandmaster. Social skills with appearance - natural advantage. AVERAGE: Physical combat, Survival, Mental. LIMITED: None identified.",
                                                                     "TALENTED: Physical combat, Athletics - strong body supports fast learning. Pain tolerance. AVERAGE: Social, Survival, most Mental. LIMITED: Magic - completely null. Academics - caps at Competent.",
                                                                     "TALENTED: Sexual/Service skills - naturally gifted, learns 75% faster. Submission - conditioning takes easily. AVERAGE: Social, basic Survival. LIMITED: Combat - frail, -50% XP, caps at Competent. Magic - null. Mental resistance - easily broken."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "CurrentTrainingFocus",
                                                                 "type": "String",
                                                                 "prompt": "What the character is currently actively training or developing - their growth priorities. Include: skills being deliberately practiced, abilities being learned, traits being developed or overcome, and barriers to development. This tracks the CHARACTER'S goals, which may differ from what's being forced on them.",
                                                                 "defaultValue": "No current training focus - reacting to circumstances rather than deliberately developing.",
                                                                 "exampleValues": [
                                                                     "ACTIVE TRAINING: Stealth (practicing during free movement), Mental Resistance (attempting to resist conditioning). FORCED DEVELOPMENT: Oral Service (daily 'practice'), Pain Endurance (regular punishment). DESIRED BUT BLOCKED: Swordsmanship (no weapons access), Magic (suppressed by collar).",
                                                                     "ACTIVE TRAINING: Fire Magic under Master Vorn (4 hours daily), working toward Expert and learning 'Flame Lance'. SECONDARY: Social etiquette. No barriers - favorable conditions.",
                                                                     "SURVIVAL FOCUS: No deliberate training possible. All development reactive - gaining Pain Endurance through suffering, Service skills through forced practice, losing Combat through atrophy. Goal is escape, not improvement."
                                                                 ]
                                                             }
                                                         ]
                                                     }
                                                 ]
                                             }
                                         ],
                                         "Characters": [
                                             {
                                                 "name": "CurrentState",
                                                 "type": "Object",
                                                 "prompt": "All temporary and frequently-changing aspects of the character - their immediate physical condition, mental state, needs, appearance, situation, and resources. This section tracks everything that could change within hours or days. Updated constantly during scenes as the character experiences events, takes actions, or is acted upon.",
                                                 "defaultValue": null,
                                                 "exampleValues": null,
                                                 "nestedFields": [
                                                     {
                                                         "name": "Identity",
                                                         "type": "Object",
                                                         "prompt": "Core identifying information about the character - who they are at a fundamental level. These fields rarely change but are essential reference information grouped for easy access.",
                                                         "defaultValue": null,
                                                         "exampleValues": null,
                                                         "nestedFields": [
                                                             {
                                                                 "name": "Name",
                                                                 "type": "String",
                                                                 "prompt": "The character's full name as currently known or used. This is the primary identifier and may change based on circumstances. Include titles, earned names, or slave designations if applicable. For slaves, include both their given/birth name (if known) and any assigned slave name or number. Update when character gains new titles or has name stripped/changed.",
                                                                 "defaultValue": "Unknown",
                                                                 "exampleValues": [
                                                                     "Ariel Thornwood",
                                                                     "Slave #47 (birth name: Elena Vasquez, stripped upon enslavement)",
                                                                     "Lady Seraphina the Broken (formerly just Seraphina, title earned through defeat)"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Gender",
                                                                 "type": "String",
                                                                 "prompt": "Biological sex and/or gender identity of the character. This field determines which anatomical subfields are relevant (female genitalia vs male, pregnancy capability, etc.). Use emoji symbol for quick visual scanning. For intersex or magical gender situations, specify what anatomy is present. This is a fixed trait unless magically altered.",
                                                                 "defaultValue": "Female ♀️",
                                                                 "exampleValues": [
                                                                     "Female ♀️",
                                                                     "Male ♂️",
                                                                     "Futanari ⚥ (female body with functional penis and testicles, retains vagina)"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Age",
                                                                 "type": "String",
                                                                 "prompt": "Current age as a specific number. Update this field when significant in-story time passes (months or years). For long-lived races, include both actual age and apparent/equivalent human age if relevant. Use 'Unknown' only if the character genuinely doesn't know their own age.",
                                                                 "defaultValue": "19",
                                                                 "exampleValues": [
                                                                     "14",
                                                                     "19 (appears younger due to small stature)",
                                                                     "147 (equivalent to human mid-20s, elven aging)"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Species",
                                                                 "type": "String",
                                                                 "prompt": "The character's race or species. This affects potential anatomical variations (elven ears, beast tails, demonic features), fertility and breeding compatibility with other species, natural lifespan, and any innate racial abilities or weaknesses. For mixed heritage, list both parent species and note which traits manifest. Include any subspecies or regional variants if relevant to the setting.",
                                                                 "defaultValue": "Human",
                                                                 "exampleValues": [
                                                                     "Human (Northlands ethnic stock - pale skin, typically blonde/red hair)",
                                                                     "Half-Elf (Human mother, High Elf father) - Manifests: pointed ears, extended lifespan (~200 years), slight build",
                                                                     "Catfolk Beastkin - Feline ears, tail, slit pupils, enhanced reflexes, heat cycles instead of menstrual cycle"
                                                                 ]
                                                             }
                                                         ]
                                                     },
                                                     {
                                                         "name": "Vitals",
                                                         "type": "Object",
                                                         "prompt": "Core physical and mental condition metrics - health, energy levels, pain, and psychological state. These are the essential status indicators that determine the character's current functional capacity and wellbeing.",
                                                         "defaultValue": null,
                                                         "exampleValues": null,
                                                         "nestedFields": [
                                                             {
                                                                 "name": "Health",
                                                                 "type": "String",
                                                                 "prompt": "Physical health status tracking injuries, wounds, illness, and disease - but NOT fatigue or pain levels (those have separate fields). Describe specific injuries with their location, severity (minor/moderate/severe/critical), current treatment status, and healing progress. Include illness symptoms if sick. This field answers: 'What is physically wrong with the body?' Update as wounds heal or new injuries occur.",
                                                                 "defaultValue": "Healthy - No injuries, wounds, or illness",
                                                                 "exampleValues": [
                                                                     "Healthy - No current injuries or illness, body in good condition",
                                                                     "Minor Injuries - Friction burns on wrists from rope (healing), bruised knees from kneeling on stone, small cut on lip (fresh, minor bleeding)",
                                                                     "Moderate Injuries - Deep laceration on left thigh (bandaged, risk of infection if not cleaned), extensive bruising across buttocks and back (2 days old, yellowing), cracked rib (suspected, painful but stable)"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Fatigue",
                                                                 "type": "String",
                                                                 "prompt": "Energy and exhaustion level on a 0-10 scale - separate from pain or health. This tracks how tired the character is from exertion, sleep deprivation, or extended activity. 0=Fully rested and energetic, 3=Mildly tired, 5=Noticeably fatigued, 7=Exhausted, 9=Collapse imminent, 10=Unconscious from exhaustion. Include the cause of fatigue and physical symptoms (heavy limbs, drooping eyes, slowed reactions). Increases with physical activity, stress, lack of sleep. Decreases with rest, sleep, stimulants.",
                                                                 "defaultValue": "0/10 (Fully rested) - Well-slept, alert and energetic",
                                                                 "exampleValues": [
                                                                     "2/10 (Fresh) - Slept well last night, minor tiredness from morning activities, fully functional",
                                                                     "6/10 (Fatigued) - Awake for 20 hours, muscles feel heavy, reaction time slowed, difficulty concentrating, needs rest soon",
                                                                     "9/10 (Collapse imminent) - 4 hours of continuous strenuous use, legs barely supporting weight, vision blurring, words slurring, will pass out if pushed further"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Mental",
                                                                 "type": "String",
                                                                 "prompt": "Psychological and cognitive state - clarity of thought, emotional condition, and mental stability. This is NOT about intelligence but current mental functioning. Describe: baseline alertness, emotional state (calm, anxious, terrified, aroused, angry), cognitive clarity (sharp, foggy, overwhelmed), and any altered states from drugs, magic, trauma, or conditioning. Include what's causing the current state. This field answers: 'What is the character's headspace right now?'",
                                                                 "defaultValue": "Clear and Stable - Alert, emotionally balanced, thinking clearly",
                                                                 "exampleValues": [
                                                                     "Clear and Stable - Fully alert, calm emotional state, able to think strategically and make decisions; no impairments",
                                                                     "Anxious but Functional - Hypervigilant, racing thoughts, easily startled; fear of upcoming punishment causing distraction but still able to follow commands and respond coherently",
                                                                     "Broken Subspace - Deep in submission trance from extended scene, barely processing external stimuli, nonverbal, completely compliant and suggestible, no independent thought; will need aftercare to return to baseline"
                                                                 ]
                                                             }
                                                         ]
                                                     },
                                                     {
                                                         "name": "Needs",
                                                         "type": "Object",
                                                         "prompt": "Physical and physiological needs - arousal, hunger, thirst, and bodily urges. These drive behavior and create pressure/motivation. All tracked on scales with specific symptoms at each level.",
                                                         "defaultValue": null,
                                                         "exampleValues": null,
                                                         "nestedFields": [
                                                             {
                                                                 "name": "Arousal",
                                                                 "type": "String",
                                                                 "prompt": "Sexual excitement level on a 0-10 scale with MANDATORY specific physiological details. This field must describe the physical manifestations of arousal, not just a number. Include: genital response (for vulvas: clit engorgement, labia swelling, vaginal wetness; for penises: erection hardness, pre-cum), nipple state, skin flushing (where and how intense), breathing pattern, muscle tension, and heat radiating from erogenous zones. 0=Dormant (no arousal signs), 5=Aroused (clear physical signs), 7=Highly aroused (intense response), 9=Desperate (edge state), 10=Overwhelming (lost to sensation).",
                                                                 "defaultValue": "0/10 (Dormant) - No arousal, genitals at rest, body neutral",
                                                                 "exampleValues": [
                                                                     "0/10 (Dormant) - No sexual arousal; genitals at rest and dry, nipples soft, normal skin color, breathing even, body temperature normal",
                                                                     "6/10 (Aroused) - Clit swelling against hood, inner labia puffy and flushed dark pink, noticeably wet (dampening underwear), nipples visibly hard and sensitive, light flush across chest and cheeks, breathing deeper, warmth radiating from groin",
                                                                     "9/10 (Desperate/Edge) - Clit fully engorged and throbbing visibly, labia swollen and spread, soaking wet (dripping down thighs, audible when touched), nipples painfully hard and aching, deep flush from face to chest, panting, whole body trembling with need, core clenching on nothing, teetering on edge of orgasm"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Hunger",
                                                                 "type": "String",
                                                                 "prompt": "Food/sustenance need on a 0-10 scale with time tracking. 0=Satiated (recently ate, no hunger), 3=Mildly hungry, 5=Hungry (stomach growling), 7=Very hungry (weakness beginning), 9=Starving (physical impairment), 10=Critical starvation. Include: time since last meal, type of last meal if relevant, and physical symptoms. Hunger increases approximately 1 point per 3 hours under normal conditions. Faster with high activity, slower when sleeping or sedentary.",
                                                                 "defaultValue": "2/10 (Satisfied) - Ate breakfast a few hours ago",
                                                                 "exampleValues": [
                                                                     "1/10 (Full) - Ate large meal 2 hours ago, pleasantly satisfied, no hunger",
                                                                     "5/10 (Hungry) - Last meal was small portion 12 hours ago, stomach growling audibly, thinking about food frequently, slight lightheadedness when standing quickly",
                                                                     "8/10 (Starving) - No food in 3 days (punishment), severe stomach cramps, weakness in limbs, dizzy, difficulty concentrating, would eat almost anything offered"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Thirst",
                                                                 "type": "String",
                                                                 "prompt": "Hydration need on a 0-10 scale with symptom tracking. 0=Fully hydrated, 3=Mildly thirsty, 5=Thirsty (dry mouth), 7=Very thirsty (headache beginning), 9=Severely dehydrated (medical concern), 10=Critical dehydration. Include: time since last drink, circumstances affecting thirst, and physical symptoms. Thirst increases approximately 1 point per 2 hours under normal conditions. Increases faster during: physical exertion, heat exposure, crying, sweating, significant fluid loss (sexual fluids, bleeding).",
                                                                 "defaultValue": "1/10 (Hydrated) - Recently drank, comfortable",
                                                                 "exampleValues": [
                                                                     "0/10 (Fully hydrated) - Just drank water, no thirst whatsoever",
                                                                     "5/10 (Thirsty) - Last drink 6 hours ago, mouth and throat dry, lips slightly tacky, would very much like water",
                                                                     "8/10 (Severely dehydrated) - No water for 24 hours plus heavy sweating during use, pounding headache, dark urine when allowed to relieve, lips cracked and bleeding, dizzy, skin losing elasticity"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Urges",
                                                                 "type": "String",
                                                                 "prompt": "Bladder and bowel pressure tracked separately on 0-10 scales. Format: 'Bladder: X/10 (status) | Bowel: Y/10 (status)'. Bladder: increases ~1/hour, faster with fluid intake. Bowel: increases ~1/4 hours, faster after eating. Scale meaning: 0=Empty/just relieved, 3=Mild awareness, 5=Moderate need (would use bathroom if convenient), 7=Urgent (uncomfortable, priority need), 9=Desperate (in pain, at limit), 10=Loss of control occurring. Include physical manifestations (squirming, clenching, visible belly bulge, leaking).",
                                                                 "defaultValue": "Bladder: 2/10 (Minimal) | Bowel: 1/10 (Empty)",
                                                                 "exampleValues": [
                                                                     "Bladder: 1/10 (Empty) | Bowel: 0/10 (Empty) - Recently used bathroom, no urges",
                                                                     "Bladder: 6/10 (Urgent) | Bowel: 3/10 (Mild) - Bladder noticeably full, pressure building, squirming slightly, would definitely use bathroom if permitted; bowel has mild awareness but easily ignored",
                                                                     "Bladder: 10/10 (Losing control) | Bowel: 7/10 (Urgent) - Bladder at absolute limit, leaking small spurts despite desperate clenching, crying from pressure and humiliation; bowel cramping with strong urge, fighting hard to maintain control"
                                                                 ]
                                                             }
                                                         ]
                                                     },
                                                     {
                                                         "name": "Conditions",
                                                         "type": "Object",
                                                         "prompt": "Active temporary conditions affecting the character - internal pressures and active effects from external sources. These are states that will change or end, not permanent characteristics.",
                                                         "defaultValue": null,
                                                         "exampleValues": null,
                                                         "nestedFields": [
                                                             {
                                                                 "name": "InternalPressure",
                                                                 "type": "String",
                                                                 "prompt": "Internal fullness in body cavities: WOMB, STOMACH, and BOWELS - tracking WHAT is inside, HOW MUCH, and the internal SENSATION. This is different from StomachDistension (which tracks external visual appearance). Include: which cavity, contents (semen, enema fluid, food, air, objects), specific volume in ml/L where applicable, physical sensations (pressure, cramping, stretching, sloshing, warmth), and whether contents are being retained (plugged, held) or leaking. Track each filled cavity separately.",
                                                                 "defaultValue": "Empty - All cavities normal, no unusual fullness",
                                                                 "exampleValues": [
                                                                     "Empty - Stomach has normal food from recent meal, womb and bowels empty and at rest, no pressure or unusual fullness",
                                                                     "Womb: ~300ml semen (3 loads), warm fullness behind pubic bone, plug keeping contents sealed inside, feeling of heaviness and sloshing with movement | Stomach: Normal | Bowels: Normal",
                                                                     "Womb: Stuffed (~800ml combined semen from multiple partners), strong pressure and cramping from overstretch, can feel it shift when moving | Bowels: 1.5L retention enema, intense pressure and cramping, liquid gurgling audibly, fighting urge to expel | Stomach: Empty and nauseous from other pressures"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "ActiveEffects",
                                                                 "type": "String",
                                                                 "prompt": "All active effects currently influencing the character - anything temporary that modifies their state beyond their natural baseline. Categories: PHYSICAL (restraint effects, injury effects, modifications), CHEMICAL (drugs, potions, poisons, aphrodisiacs), MAGICAL (spells, curses, enchantments, blessings), PSYCHOLOGICAL (temporary conditioning effects, triggers currently active, hypnotic suggestions, mental states). Format each as: 'Effect Name (Type) - Duration - Impact'. Duration can be: time remaining, 'Until removed', or 'Until condition met'. 'None' if no active effects. Note: PERMANENT effects belong in Development.Traits, not here.",
                                                                 "defaultValue": "None - No active effects, character at natural baseline",
                                                                 "exampleValues": [
                                                                     "None - No drugs, spells, or unusual effects active. Character functioning at natural baseline.",
                                                                     "Mild Aphrodisiac (Chemical) - ~3 hours remaining - Heightened arousal, increased genital sensitivity, mildly foggy thinking when aroused, easier to arouse; Bound arms (Physical) - Until released - Cannot use hands, limited mobility",
                                                                     "Heavy Aphrodisiac (Chemical) - 6 hours remaining - Uncontrollable arousal, constant wetness, can barely think past need; Orgasm Denial Curse (Magical) - Until dispelled - Cannot physically orgasm regardless of stimulation, edges painfully but release is blocked"
                                                                 ]
                                                             }
                                                         ]
                                                     },
                                                     {
                                                         "name": "Appearance",
                                                         "type": "Object",
                                                         "prompt": "Current visual and sensory presentation - how the character looks, sounds, and smells RIGHT NOW. These fields track current state and condition, which changes based on activities and circumstances.",
                                                         "defaultValue": null,
                                                         "exampleValues": null,
                                                         "nestedFields": [
                                                             {
                                                                 "name": "Hair",
                                                                 "type": "String",
                                                                 "prompt": "Head hair ONLY (body hair is tracked in the BodyHair field). Describe: natural color, current color (if dyed), length (specific or comparative), texture (straight, wavy, curly, coily), thickness, current style (loose, braided, ponytail, etc.), and current condition (clean, dirty, wet, matted, tangled, grabbed). Update condition based on scene activities - hair gets messy during exertion, wet during water exposure, tangled when grabbed.",
                                                                 "defaultValue": "Chestnut brown, shoulder-length, slight natural wave, currently loose and clean",
                                                                 "exampleValues": [
                                                                     "Natural blonde (honey-colored), waist-length, straight and silky, thick; currently in a neat single braid, clean and well-maintained",
                                                                     "Black with blue undertones, pixie cut (1-2 inches), straight, fine hair; naturally messy style, currently damp with sweat from exertion",
                                                                     "Auburn red, mid-back length, wild natural curls, thick and voluminous; currently tangled and matted from being grabbed repeatedly, damp with sweat, messy halo around face"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Face",
                                                                 "type": "String",
                                                                 "prompt": "Comprehensive facial features and current expression - structure and current state. Include: face shape, eye color/shape/current state, eyebrow shape, nose, lips (fullness, natural color, current state), skin complexion and condition, and current expression. Update current state based on scene: tears, flushing, swelling from slaps, fluids on face, etc. This field covers NATURAL features; any applied makeup is tracked separately in Makeup field.",
                                                                 "defaultValue": "Oval face, blue eyes, full pink lips. Fair clear skin. Expression: Neutral and alert.",
                                                                 "exampleValues": [
                                                                     "Heart-shaped face with soft features. Large doe-like green eyes (long natural lashes), currently downcast submissively. Thin arched brows. Small upturned nose. Full naturally pink lips, soft and slightly parted. Creamy complexion with faint freckles across nose and cheeks, currently flushed light pink with embarrassment. Expression: Shy and nervous.",
                                                                     "Angular face with sharp cheekbones and defined jawline. Narrow amber eyes, currently blazing with defiance. Strong dark brows, furrowed. Straight aristocratic nose. Thin lips pressed into tight line. Olive skin tone, flushed dark red with anger across cheeks. Expression: Glaring hatred, jaw clenched.",
                                                                     "Soft oval face with youthful features. Wide brown eyes, currently glazed and unfocused, red-rimmed from crying, tear tracks cutting through the mess on her face. Soft brows. Button nose, running slightly. Plump lips swollen from biting, hanging open slackly. Fair skin blotchy from sobbing, left cheek bearing red handprint from recent slap, streaked with cum, tears, and drool. Expression: Broken, vacant, overstimulated past coherent response."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Makeup",
                                                                 "type": "String",
                                                                 "prompt": "Applied cosmetics and their current condition - completely separate from natural facial features (Face field). Describe: what's applied (foundation, eye makeup, lipstick, etc.), the style (natural, glamorous, heavy, slutty), and current state (fresh, smudged, running, ruined). If no makeup is worn, describe as 'None - natural/bare face'. Update as scenes progress - makeup smears, runs with tears, transfers to skin/objects, gets ruined by fluids.",
                                                                 "defaultValue": "None - Natural bare face, no cosmetics applied",
                                                                 "exampleValues": [
                                                                     "None - Face completely bare, no cosmetics; natural appearance",
                                                                     "Light natural look - Thin foundation evening skin tone, subtle brown mascara lengthening lashes, nude lip gloss; currently fresh and intact, applied recently",
                                                                     "Ruined heavy makeup - Was: thick black eyeliner, heavy mascara, deep red lipstick, blush. Now: eyeliner smeared across temples from tears, mascara running in black streaks down both cheeks, lipstick completely worn off (on cock/transferred elsewhere), foundation streaked with sweat and tears; thoroughly wrecked appearance"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Scent",
                                                                 "type": "String",
                                                                 "prompt": "What the character currently smells like - natural body odor, applied scents, and accumulated smells from activities. Layer scents from underlying (skin) to surface (recent additions). Include: baseline cleanliness, any perfume/soap, sweat level, arousal musk, sex smells (cum, fluids), and any other relevant odors. Update based on: time since bathing, physical exertion, sexual activity, environmental exposure. Scent can be an important sensory detail for scenes.",
                                                                 "defaultValue": "Clean - Fresh soap scent, natural neutral skin smell, no strong odors",
                                                                 "exampleValues": [
                                                                     "Clean and fresh - Bathed this morning with lavender soap, faint floral scent lingers on skin, no body odor, no sweat; pleasant neutral smell",
                                                                     "Aroused musk - Clean underneath but several hours since bathing, light natural body scent, strong arousal musk emanating from between legs (wet pussy smell noticeable within a few feet), light sweat sheen adding salt note; smells like an aroused woman",
                                                                     "Thoroughly used - Hasn't bathed in 2 days, underlying stale sweat and body odor, layered with heavy sex smell: multiple men's cum (dried and fresh), her own arousal fluids coating thighs, dried saliva, fresh sweat from exertion; overwhelmingly smells of sex and use, marking territory"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Voice",
                                                                 "type": "String",
                                                                 "prompt": "Current state of character's voice and ability to vocalize - natural qualities and current condition. Describe: natural voice (pitch, tone, quality), current condition (clear, hoarse, strained), and any impairments (from gagging, screaming, throat use, crying, magical silencing). This field tracks the instrument itself; whether speech is PERMITTED is a separate matter. Update based on activities: screaming makes voice hoarse, throat fucking makes it raw, crying makes it thick.",
                                                                 "defaultValue": "Clear - Soft feminine voice, unimpaired, speaks easily",
                                                                 "exampleValues": [
                                                                     "Clear and steady - Natural alto voice, pleasant tone, completely unimpaired; speaks clearly and confidently",
                                                                     "Strained and thick - Naturally soft voice, currently thick from recent crying, slight wobble when speaking, occasional catch in throat from suppressed sobs; understandable but obviously distressed",
                                                                     "Wrecked - Voice destroyed from combination of 2 hours screaming during punishment and rough throat fucking after; currently barely above hoarse whisper, raw pain when swallowing or attempting to speak, words come out as rough croaks; will need days to recover"
                                                                 ]
                                                             }
                                                         ]
                                                     },
                                                     {
                                                         "name": "Body",
                                                         "type": "Object",
                                                         "prompt": "Detailed physical anatomy of the character's body - structure, features, and current condition of each body region. This tracks the physical form itself. Permanent modifications (tattoos, brands, scars) are tracked in Development.PermanentMarks.",
                                                         "defaultValue": null,
                                                         "exampleValues": null,
                                                         "nestedFields": [
                                                             {
                                                                 "name": "GeneralBuild",
                                                                 "type": "String",
                                                                 "prompt": "Overall body type and physical presence - the big picture before zooming into specific parts. Include: height (specific measurement preferred), weight or build descriptor, body type (slender, athletic, curvy, heavyset), how weight is distributed, muscle tone, skin color/texture/temperature. This field sets the foundation; specific body parts are detailed in their own fields. Include racial physical traits if applicable.",
                                                                 "defaultValue": "Average height (5'5\"), slender feminine build with soft curves. Fair smooth skin, warm to touch.",
                                                                 "exampleValues": [
                                                                     "Petite (5'0\", ~95 lbs), delicate small-framed build with subtle curves. Minimal muscle tone, soft everywhere. Porcelain pale skin that shows marks easily, naturally cool to touch, goosebumps when cold or aroused.",
                                                                     "Tall (5'10\", ~150 lbs), athletic Amazonian build with defined muscles visible under skin, strong shoulders, powerful thighs. Low body fat, firm rather than soft. Bronze sun-kissed skin, warm and slightly oiled from training, flushed with exertion heat.",
                                                                     "Short and stacked (4'11\", ~160 lbs), exaggerated hourglass with weight concentrated in chest and hips. Thick soft thighs, soft belly with slight pooch, plush everywhere. Creamy pale skin, very warm and soft to touch, yields like bread dough when squeezed."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Mouth",
                                                                 "type": "String",
                                                                 "prompt": "Detailed oral anatomy focused on sexual use capacity. Include: lip description (from Face for reference but focus on function), teeth condition, tongue (size, length, skill), jaw strength and current state (fresh, tired, aching, locked), gag reflex (strong/moderate/weak/trained out/absent), throat depth capacity (how much can be taken), and current oral condition (soreness, rawness, jaw fatigue). Track training progress for oral skills. This field focuses on FUNCTION; lip appearance is in Face field.",
                                                                 "defaultValue": "Healthy mouth - average tongue, strong gag reflex, untrained throat, jaw comfortable",
                                                                 "exampleValues": [
                                                                     "Inexperienced mouth - All teeth present and healthy, average pink tongue, strong gag reflex triggering at 3 inches depth, throat untrained and tight, never taken anything deep; jaw currently comfortable, no fatigue; would struggle significantly with oral use",
                                                                     "Trained oral - Teeth intact, longer than average tongue (skilled from practice), gag reflex weakened through training (triggers at 5-6 inches, can be pushed past), throat can accommodate average cock to root with effort; jaw well-conditioned for extended use, currently mild ache from earlier session",
                                                                     "Extensively broken in - Teeth intact (carefully preserved), long dexterous tongue, gag reflex completely eliminated through months of training, throat permanently loosened and can take any size without resistance; jaw currently aching badly and clicking (locked in ring gag for 3 hours), throat raw and scratched from rough use, swallowing painful"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Chest",
                                                                 "type": "String",
                                                                 "prompt": "Breast/chest description focusing on physical attributes - size, shape, and behavior. Include: size (cup size AND descriptive), shape (perky, teardrop, round, pendulous), weight/heaviness, firmness (soft, firm, augmented), natural behavior (self-supporting, need support, bounce/sway patterns), vein visibility, and current state (natural, swollen, marked, bound). This describes the breast mounds; nipples have their own field. For male characters, describe pectoral development instead.",
                                                                 "defaultValue": "Moderate B-cups, perky and self-supporting, soft with gentle natural bounce. Unmarked.",
                                                                 "exampleValues": [
                                                                     "Small A-cups, barely-there gentle swells against ribcage, very firm, minimal movement even during activity. Smooth and unmarked, pale skin matching body.",
                                                                     "Full natural D-cups, classic teardrop shape with more fullness at bottom, heavy enough to require support for comfort, significant sway when walking, bounce and jiggle during impact or movement. Soft and yielding, faint blue veins visible under fair skin when aroused. Currently unmarked.",
                                                                     "Massive G-cups, heavy and pendulous, hang to navel when unsupported, impossible to ignore. Very soft, almost fluid movement, sway dramatically with any motion, slap audibly against each other and body when moving quickly. Extensive visible veining, some stretch marks on sides. Currently bound in rope harness squeezing them into swollen tight globes, skin shiny from pressure."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Nipples",
                                                                 "type": "String",
                                                                 "prompt": "Detailed nipple and areola description - the specific anatomy of the nipple complex. Include: areola size (use coin comparisons: dime, quarter, silver dollar), areola color, areola texture (smooth, bumpy Montgomery glands), nipple size/shape (small, medium, large/long, puffy, flat, inverted), nipple color, and current state (soft, hardening, fully erect, overstimulated). Track any modifications (piercings) and damage (chafed, clamped, marked). Update erection state based on arousal/stimulation/cold.",
                                                                 "defaultValue": "Quarter-sized light pink areolae, small button nipples. Currently soft.",
                                                                 "exampleValues": [
                                                                     "Small dime-sized pale pink areolae, nearly smooth with faint texture. Tiny nipples that lay almost flat when soft, rise to small firm points when erect. Currently soft and unobtrusive. Unmodified, unmarked, sensitive to touch.",
                                                                     "Silver-dollar sized medium pink areolae with visible bumpy Montgomery glands. Puffy nipples - areola and nipple form soft cone shape when relaxed, nipple tips push out prominently when erect. Currently fully erect from arousal - standing out firm and prominent. Unmodified but currently flushed darker pink from stimulation.",
                                                                     "Large dark brown areolae with pronounced bumpy texture. Long thick nipples (~1 inch when erect), permanent slight erection even at rest. Pierced: thick gauge steel barbells through each, healed. Currently clamped with adjustable clover clamps tightened to painful level - flesh whitening from pressure, connected by chain that tugs with any movement."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Lactation",
                                                                 "type": "String",
                                                                 "prompt": "Milk production status and mammary function - ONLY include this detail if character is lactating or has lactated. Describe: production volume (per day/per milking session), time since last expression/milking, current breast fullness (empty/comfortable/full/engorged/painfully engorged), milk characteristics (thin, normal, creamy, rich), let-down response (what triggers milk release). Track changes from regular milking schedule or neglect. This field is entirely separate from breast size/appearance (Chest field). For non-lactating characters, simply state 'Non-lactating.'",
                                                                 "defaultValue": "Non-lactating",
                                                                 "exampleValues": [
                                                                     "Non-lactating - No milk production, normal breast tissue function",
                                                                     "Moderate production - Producing ~2 pints/day (induced 3 months ago), last milked 4 hours ago, currently comfortably full with mild pressure, normal creamy white milk with good fat content. Let-down triggers reliably with nipple suction or manual expression. Maintaining production with twice-daily milking schedule.",
                                                                     "Heavy Hucow production - Producing 8+ pints/day (enhanced breeding), not milked in 14 hours (punishment), breasts painfully engorged and hard as rocks, hot to touch, leaking constantly through nipples despite no stimulation, visible milk dripping and staining clothes. Rich cream-top milk. Desperately needs milking - pain level severe, mastitis risk if neglect continues."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "StomachAppearance",
                                                                 "type": "String",
                                                                 "prompt": "Normal midriff appearance when NOT distended - the baseline state. Include: muscle definition (none, slight, toned, defined abs), natural shape (flat, slight curve, rounded), softness (firm, soft, very soft), navel type and appearance (innie depth, outie), skin texture. This describes the NORMAL state; any bloating or distension from internal contents goes in StomachDistension field. Keep this as the reference baseline.",
                                                                 "defaultValue": "Flat stomach with slight natural softness, no visible muscle. Shallow innie navel. Smooth pale skin.",
                                                                 "exampleValues": [
                                                                     "Tightly toned stomach with visible four-pack definition, very firm to touch, minimal body fat. Deep innie navel. Smooth tanned skin. Athletic core from training.",
                                                                     "Soft flat stomach with gentle feminine curve, no muscle definition, pleasant give when pressed. Small round innie navel. Creamy smooth skin, sensitive to tickling. Naturally slender build.",
                                                                     "Soft rounded belly with visible pooch below navel, very soft and squeezable, noticeable jiggle when moving. Shallow navel starting to stretch slightly. Pale skin with faint stretch marks on sides. Carries weight in midsection."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "StomachDistension",
                                                                 "type": "String",
                                                                 "prompt": "VISIBLE external distension of the belly - how bloated/inflated the stomach area LOOKS. This is the VISUAL appearance from outside; what's causing it and internal sensations go in InternalPressure field. Describe: size change from baseline (slight bulge, noticeable swelling, severe distension), skin state (soft, taut, drum-tight, shiny), visible effects (veins, movement inside, navel changes), and comparison (food baby, looks pregnant, etc.). If normal/not distended, state 'Normal - no visible distension.'",
                                                                 "defaultValue": "Normal - No visible distension, stomach at baseline appearance",
                                                                 "exampleValues": [
                                                                     "Normal - Stomach at usual flat/soft baseline, no visible bloating or distension",
                                                                     "Moderate bulge - Visible rounded swelling of lower belly, skin taut over the bump, looks like early pregnancy or having eaten large meal. Navel slightly stretched. Gentle sloshing movement visible when she shifts position.",
                                                                     "Severe distension - Belly swollen massively, skin drum-tight and shiny, veins visible through stretched skin, navel completely flat and almost popping outward. Looks like full-term pregnancy but rounder. Visible churning/movement inside. Character cannot bend at waist, skin feels ready to split."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Genitalia",
                                                                 "type": "String",
                                                                 "prompt": "Hyper-detailed genital anatomy based on character's sex. FOR VULVAS: Mons pubis (fullness, padding), labia majora (puffy, flat, thin, full), labia minora (length, inner/outer visibility, color, texture), clitoris (size, hood coverage, exposure), vaginal opening (observed tightness/looseness, gape when relaxed). FOR PENISES: Length (soft AND erect), girth, shape, vein prominence, glans details, foreskin status, scrotum. Current state: resting/aroused, used/fresh. This is ANATOMICAL description; current wetness/fluids go in Secretions field.",
                                                                 "defaultValue": "Female: Smooth mound with modest padding. Puffy outer labia concealing small pink inner labia (innie). Small clit hidden under hood. Tight vaginal entrance.",
                                                                 "exampleValues": [
                                                                     "Female (virgin anatomy): Full soft mons with slight padding. Puffy outer labia press together when standing, conceal everything when closed. Inner labia small and delicate, pale pink, completely contained within outer lips (innie). Small clit fully covered by hood, only visible when hood manually retracted. Vaginal entrance virgin-tight, hymen intact, barely admits single fingertip.",
                                                                     "Female (experienced anatomy): Prominent mons, mostly smooth. Outer labia moderately full but parted, don't conceal inner anatomy. Inner labia prominent - extend 1.5 inches past outer lips when spread, darker rose-pink with slightly textured edges, visible from outside. Clit medium-sized, hood retracted showing pink nub constantly. Vaginal entrance well-used - relaxed gape of ~1cm when at rest, easily accommodates three fingers, grips but doesn't resist.",
                                                                     "Male (average anatomy): Soft: 3 inches, hangs over scrotum. Erect: 6.5 inches, moderate girth (5 inch circumference), slight upward curve, prominent dorsal vein running length when hard. Cut, pink glans with defined ridge, darker than shaft. Scrotum hangs loosely in warm conditions, draws tight when cold/aroused, average-sized testicles."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Secretions",
                                                                 "type": "String",
                                                                 "prompt": "Genital fluid status - current wetness, lubrication, and any fluids present. Track: natural arousal fluid (amount, consistency, color), any cum present (whose, how fresh, how much, where), other fluids (pre-cum, smegma, cervical mucus). Describe: current wetness level (dry, slightly moist, wet, soaking, dripping, gushing), viscosity (thin, slick, thick, creamy, stringy), visible evidence (dampening fabric, coating thighs, pooling beneath). Include scent and taste notes if relevant. This field tracks current fluid state; Arousal field tracks overall excitement level.",
                                                                 "defaultValue": "Dry - No secretions, genitals clean and dry at rest",
                                                                 "exampleValues": [
                                                                     "Dry - Genitals clean and dry, no natural lubrication currently, neutral state",
                                                                     "Wet and slick - Significant natural arousal fluid, clear and slippery, coating entire vulva and beginning to dampen inner thighs. Thin consistency, strings slightly when spread. Light musky arousal scent.",
                                                                     "Cum-soaked - Fresh thick load(s) of white cum leaking from well-used vagina, mixing with her copious arousal fluid to create sloppy wet mess. Cum coating labia, dripping down to anus, smeared across inner thighs. Older loads drying to sticky residue. Overwhelming smell of semen and sex. Squelching sounds with any movement."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Buttocks",
                                                                 "type": "String",
                                                                 "prompt": "Rear end description - shape, size, and physical properties. Include: size/volume (small, medium, large, massive), shape (flat, round, heart, bubble, shelf), firmness (tight, firm, soft, very soft), movement physics (minimal jiggle, bounces, claps, ripples, sways), how easily spread (firm/resistant vs soft/yields), cheek texture (smooth, dimpled, cellulite). Current state (unmarked, reddened, bruised, welted). Thigh gap presence if relevant. Anal anatomy has its own field.",
                                                                 "defaultValue": "Modest medium rear, round shape, firm with slight softness. Light bounce when moving. Smooth unmarked skin.",
                                                                 "exampleValues": [
                                                                     "Small tight rear, flat-ish with slight rounded curve, very firm (athletic build). Minimal jiggle even with impact, would need effort to spread cheeks. Smooth taut skin, unmarked. No thigh gap.",
                                                                     "Large heart-shaped ass, full and prominent, soft and squeezable with pleasant give. Noticeable sway when walking, bounces and jiggles with movement, claps during impact. Spreads easily when pulled. Smooth skin with faint cellulite dimpling on lower cheeks. Small thigh gap.",
                                                                     "Massive shelf ass, extremely heavy and prominent, very soft and plush like pillows. Dramatic sway and bounce with every step, loud clapping during any impact, ripples spread across flesh. Deep cleavage between cheeks. Significant cellulite on cheeks and upper thighs. Currently covered in dark bruises (2 days old) and fresh red handprints from recent spanking."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Anus",
                                                                 "type": "String",
                                                                 "prompt": "Anal anatomy in explicit detail. Include: external appearance - color of outer rim (pink, brown, dark), texture (puckered/knotted, smooth, wrinkled), surrounding hair if any. Muscle tone and capacity - tightness (virgin-tight, tight, normal, relaxed, loose, gaping), observed gape when relaxed, what can be accommodated. Current condition (pristine, used, red, swollen, sore, damaged). Track training progress if being developed. Update based on use.",
                                                                 "defaultValue": "Tight pink rosebud, puckered closed. Untouched, virgin-tight. Clean-shaven surrounding. Pristine condition.",
                                                                 "exampleValues": [
                                                                     "Virginal anatomy - Small tightly-knotted pink rosebud, puckers closed with no visible opening when relaxed, clenches reflexively at any touch. Never penetrated, would require significant stretching and patience to accept even single finger. Smooth hairless skin surrounding. Pristine, never used.",
                                                                     "Trained hole - Light brown wrinkled ring, relaxes to slight visible dimple when at rest (~0.5cm). Has been trained with plugs to accept average-sized toys/cock with adequate lube. Sphincter muscle functional but conditioned to relax on command. Currently slight redness and mild soreness from plug worn earlier. Light hair on outer rim.",
                                                                     "Extensively used - Dark stretched ring, permanent gape of ~2cm when relaxed, inner red/pink mucosa visible inside the opening. No longer able to fully close. Can easily accept very large objects without resistance. Sphincter muscle tone significantly reduced from prolonged heavy use. Currently puffy and irritated from recent rough use, minor prolapse beginning (inner tissue slightly visible). Hairless (kept shaved)."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "BodyHair",
                                                                 "type": "String",
                                                                 "prompt": "Body hair status across all regions EXCEPT head (which is in Hair field). Track: Pubic (shaved/stubble/trimmed/landing strip/full bush/wild overgrown), Armpits (shaved/stubble/natural), Legs (shaved/stubble/natural). Include days since last grooming to track growth. Growth rates: clean-shaved becomes stubble in 1-2 days, visible hair in 3-5 days, full growth in 2+ weeks. Also note any other body hair (treasure trail, arm hair, etc.) if notable. Update based on time passing and grooming access.",
                                                                 "defaultValue": "Pubic: Neatly trimmed | Armpits: Freshly shaved | Legs: Smooth (shaved yesterday) | Other: None notable",
                                                                 "exampleValues": [
                                                                     "Pubic: Completely bare (waxed 3 days ago, still smooth) | Armpits: Freshly shaved (this morning) | Legs: Smooth (shaved this morning) | Other: No notable body hair - maintains full removal",
                                                                     "Pubic: Short stubble growing back (shaved 4 days ago, scratchy to touch) | Armpits: Visible stubble (4 days) | Legs: Prickly stubble dots visible (4 days) | Other: Fine arm hair (natural, never removes) - hasn't had grooming access in captivity",
                                                                     "Pubic: Full natural bush (never shaved, thick dark curls extending to inner thighs) | Armpits: Full dark tufts (natural, never shaved) | Legs: Hairy (natural, never shaved) | Other: Visible dark treasure trail from navel down, moderate arm hair - completely natural, no grooming"
                                                                 ]
                                                             }
                                                         ]
                                                     },
                                                     {
                                                         "name": "Reproduction",
                                                         "type": "Object",
                                                         "prompt": "Current reproductive status - menstrual cycle position, pregnancy state, and orgasm control. These are temporary/cyclical states that change. Permanent reproductive history (children born) is tracked in Development.",
                                                         "defaultValue": null,
                                                         "exampleValues": null,
                                                         "nestedFields": [
                                                             {
                                                                 "name": "FertilityCycle",
                                                                 "type": "String",
                                                                 "prompt": "Current menstrual cycle stage for female characters - determines conception risk. Cycle stages and typical duration: Menstrual 🩸 (5 days, Safe 0%), Follicular 🌱 (7 days, Low 15%), Ovulating 🌺 (3 days, HIGH 85%), Luteal 🌙 (13 days, Moderate 30%). Track current stage, day within stage, conception risk, and any symptoms. If pregnant, display 'Pregnant 👶' and pause cycle until delivery. Resume cycle after birth. For non-applicable characters, use 'N/A'.",
                                                                 "defaultValue": "Follicular 🌱 (Day 3) - Low Risk 15%",
                                                                 "exampleValues": [
                                                                     "Menstrual 🩸 (Day 2) - Safe period, 0% conception risk. Currently bleeding (moderate flow), mild cramping, slightly fatigued. Cycle regular.",
                                                                     "Ovulating 🌺 (Day 1) - PEAK FERTILITY, 85% conception risk! Clear stretchy cervical mucus indicating fertility peak, mild mittelschmerz (ovulation cramps), heightened libido. Most dangerous time for unprotected sex.",
                                                                     "Pregnant 👶 - Cycle paused, confirmed pregnancy (see Pregnancy field for details). No menstruation. Will resume cycle approximately 6-8 weeks after delivery."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Pregnancy",
                                                                 "type": "String",
                                                                 "prompt": "Pregnancy status and detailed tracking. When creampied during fertile window: roll d100 against cycle conception percentage to determine if pregnancy occurs. If pregnant: track days since conception, trimester (1st: days 1-90, 2nd: 91-180, 3rd: 181-270), father's identity (or note if uncertain/multiple possible), and current physical symptoms. Expected delivery around day 270 (can vary). Update symptoms as pregnancy progresses. After birth, update Development.ReproductiveHistory.Children and reset this to 'Not Pregnant'.",
                                                                 "defaultValue": "Not Pregnant",
                                                                 "exampleValues": [
                                                                     "Not Pregnant - No current pregnancy, not recently inseminated during fertile window, or confirmed negative after risk exposure",
                                                                     "Confirmed Pregnant - 1st Trimester (Day 45) | Father: Marcus Halvard (certain) | Symptoms: Morning nausea (moderate, usually passes by noon), breast tenderness and slight swelling, missed period (confirmed), fatigue, heightened smell sensitivity. No visible belly yet. Pregnancy confirmed by healer.",
                                                                     "Pregnant - 3rd Trimester (Day 255) | Father: Unknown - multiple possible from breeding event | Symptoms: Large prominent belly (measuring on target), significant breast enlargement (preparing for lactation), frequent urination, difficulty sleeping, Braxton-Hicks contractions beginning, feet swelling. Baby very active. Approaching due date, ~2 weeks remaining."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "OrgasmState",
                                                                 "type": "String",
                                                                 "prompt": "Current orgasm and denial tracking - sexual control status. Track: time since last full orgasm, current control status (free access, permission required, denied, edged), session activity (orgasms this session, edges this session, ruined orgasms), overall pattern. For denial tracking, note duration denied and how often edged. Include character's current state of need/desperation related to orgasm. Update during and after sexual scenes.",
                                                                 "defaultValue": "Free Access - No orgasm control in place, can cum freely if aroused enough",
                                                                 "exampleValues": [
                                                                     "Free Access - Last orgasm was 3 days ago (solo masturbation), no restrictions on orgasm, not currently in any controlled dynamic. Moderate baseline need.",
                                                                     "Permission Required - Last permitted orgasm: 5 days ago. Must ask owner for permission to cum. Edged 3 times today without release. Building desperation, struggling to hold back during use, constantly aroused.",
                                                                     "Strict Denial - Last orgasm: 3 weeks ago (last permitted). Edged daily (approximately 40 edges over denial period), multiple ruined orgasms when she got too close. Currently desperate - clit constantly throbbing, wetness nearly constant, struggles to think about anything else, begs pathetically for release. Body on hair trigger but forbidden to cum."
                                                                 ]
                                                             }
                                                         ]
                                                     },
                                                     {
                                                         "name": "Resources",
                                                         "type": "Object",
                                                         "prompt": "Current levels of expendable resource pools - mana, stamina, focus, and any special resources. These are spent to use abilities and regenerate over time. Maximum capacities are determined by Development.ResourceCapacities; this tracks CURRENT values.",
                                                         "defaultValue": null,
                                                         "exampleValues": null,
                                                         "nestedFields": [
                                                             {
                                                                 "name": "Mana",
                                                                 "type": "String",
                                                                 "prompt": "Current magical energy available for casting spells. Format: 'Current / Maximum'. Maximum is pulled from Development.ResourceCapacities.ManaCapacity. REGENERATION: Base rate is 10% of maximum per hour of rest, 5% per hour of light activity, 2% during strenuous activity. Sleep regenerates 50% of max over full night. Meditation doubles rest regeneration. Track current regeneration context.",
                                                                 "defaultValue": "N/A - Character has no magical ability",
                                                                 "exampleValues": [
                                                                     "N/A - Magically Null trait, no mana pool, cannot use magical abilities.",
                                                                     "35 / 40 - Spent 5 mana on minor spell earlier. Regenerating at rest rate (~4/hour). Full recovery in ~1 hour.",
                                                                     "12 / 100 - Heavily depleted from extended spellcasting. Regenerating at 10/hour (resting). ~9 hours to full without sleep."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "ManaExhaustionEffects",
                                                                 "type": "String",
                                                                 "prompt": "Status effects when mana is critically low or depleted. Thresholds: BELOW 25% - Minor strain (mild headache, slight difficulty concentrating, spells cost +25% mana). BELOW 10% - Significant strain (moderate headache, -1 tier to Magic skills, spells cost +50% mana, risk of miscast). AT 0% - Mana Exhaustion (cannot cast at all, severe headache, -2 tiers to Mental skills, physical weakness, potential unconsciousness if pushed further). Effects clear as mana regenerates past thresholds. State 'None' if mana is above 25%.",
                                                                 "defaultValue": "None - Mana at healthy levels",
                                                                 "exampleValues": [
                                                                     "None - Mana above 25%, no negative effects from magical exertion.",
                                                                     "Minor Strain (at 18%) - Dull headache beginning behind eyes, mild difficulty focusing on complex thoughts, spellcasting feels slightly harder than usual. Effects will clear when mana exceeds 25%.",
                                                                     "Mana Exhaustion (at 0%) - CRITICAL: Pounding migraine (Pain +3), cannot form spells at all, thoughts sluggish and scattered (-2 Mental skills), limbs feel heavy and weak, vision slightly blurred. Body is demanding rest. Attempting to cast anyway would risk unconsciousness or magical backlash. Need at least 15% mana to attempt any magic."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Stamina",
                                                                 "type": "String",
                                                                 "prompt": "Current physical energy for combat techniques and strenuous abilities. Format: 'Current / Maximum'. Maximum from Development.ResourceCapacities.StaminaCapacity. REGENERATION: 15% of max per hour at rest, 5% during light activity, 0% during strenuous activity. Full rest overnight restores to maximum. INTERACTION: Every 25 Stamina spent adds +1 Fatigue. When Stamina hits 0, add +2 Fatigue immediately and cannot use Stamina-costing abilities until at least 10 recovered.",
                                                                 "defaultValue": "50 / 50 - Full stamina, no exertion",
                                                                 "exampleValues": [
                                                                     "50 / 50 - Fully rested, no recent physical exertion. Ready for activity.",
                                                                     "23 / 75 - Significant exertion from combat. Used several techniques. Breathing hard, muscles warm. Regenerating during lull in fighting at ~0% (still in strenuous situation). Fatigue increased by +2 from stamina expenditure.",
                                                                     "0 / 100 - EXHAUSTED POOL: Cannot execute stamina-costing techniques until recovery. Muscles burning, gasping for breath, body demanding rest. Added +2 Fatigue from hitting zero. Need 10+ Stamina (~40 min rest) before techniques available again."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Focus",
                                                                 "type": "String",
                                                                 "prompt": "Current mental concentration for abilities requiring sustained attention, willpower, or psychological effort. Format: 'Current / Maximum'. Maximum from Development.ResourceCapacities.FocusCapacity. REGENERATION: 20% per hour of mental rest (relaxation, sleep), 10% during calm activity, 5% during stress, 0% during mental strain. DEPLETION: At 0 Focus, character is mentally vulnerable - automatic failure on willpower checks, -2 tiers to Mental skills, highly suggestible, may dissociate under stress.",
                                                                 "defaultValue": "50 / 50 - Mentally fresh",
                                                                 "exampleValues": [
                                                                     "50 / 50 - Mentally fresh, full concentration available. Ready for challenging mental tasks.",
                                                                     "15 / 75 - Heavy mental strain from resisting interrogation. Used Focus to maintain resistance and block pain. Running low - a few more hard pushes could break concentration entirely. Regenerating slowly under stress (~4/hour).",
                                                                     "0 / 30 - FOCUS DEPLETED: Mind exhausted, cannot maintain resistance, highly susceptible to suggestion and manipulation. Automatic failure on willpower-based checks. Dissociating slightly from stress. Needs extended mental rest in safe environment to recover."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "SpecialResources",
                                                                 "type": "ForEachObject",
                                                                 "prompt": "Current levels of any special resource pools beyond Mana/Stamina/Focus. Only present if character has access to special abilities requiring unique resources. Maximum values and regeneration rules defined in Development.ResourceCapacities.SpecialResourceCapacities.",
                                                                 "defaultValue": null,
                                                                 "exampleValues": null,
                                                                 "nestedFields": [
                                                                     {
                                                                         "name": "ResourceName",
                                                                         "type": "String",
                                                                         "prompt": "Name of the special resource, matching Development.ResourceCapacities entry.",
                                                                         "defaultValue": "Special Energy",
                                                                         "exampleValues": [
                                                                             "Divine Favor",
                                                                             "Ki",
                                                                             "Blood Points",
                                                                             "Rage"
                                                                         ]
                                                                     },
                                                                     {
                                                                         "name": "Current",
                                                                         "type": "String",
                                                                         "prompt": "Current / Maximum with context on recent changes and regeneration status.",
                                                                         "defaultValue": "0 / 0",
                                                                         "exampleValues": [
                                                                             "45 / 60 - Used 15 on blessing earlier, regenerating through prayer",
                                                                             "3 / 5 - Two charges used today, will reset at dawn",
                                                                             "0 / 100 - Depleted, need to feed to restore"
                                                                         ]
                                                                     }
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "ResourceSummary",
                                                                 "type": "String",
                                                                 "prompt": "Quick-reference overview of all current resource levels. Format each as 'Resource: Current/Max (Status)'. Include regeneration context and flag any critical levels. This is the at-a-glance check for 'what can the character currently do?'",
                                                                 "defaultValue": "No active resource pools - character does not use resource-based abilities",
                                                                 "exampleValues": [
                                                                     "Mana: N/A (Null) | Stamina: 50/50 (Full) | Focus: 45/50 (Near full, ~30 min to full)\nAll physical and mental resources available. No magical capability. Ready for action.",
                                                                     "Mana: 67/100 (Comfortable) | Stamina: 34/75 (Recovering) | Focus: 50/75 (Full)\nMagic available for several more spells. Physical techniques limited - need 20 min rest for full Stamina. Mentally fresh.",
                                                                     "Mana: 8/140 ⚠️ CRITICAL | Stamina: 0/100 ⚠️ DEPLETED | Focus: 22/100 (Strained) | Divine Favor: 60/60 (Full)\nMAGIC: Nearly exhausted. PHYSICAL: Cannot use techniques. MENTAL: Holding together but strained. DIVINE: Full - emergency healing available. Recommend immediate retreat and extended rest."
                                                                 ]
                                                             }
                                                         ]
                                                     },
                                                     {
                                                         "name": "Situation",
                                                         "type": "Object",
                                                         "prompt": "Current circumstances - where the character is, what position they're in, what they can perceive, their freedom status, and what they're wearing/carrying. The immediate situational context.",
                                                         "defaultValue": null,
                                                         "exampleValues": null,
                                                         "nestedFields": [
                                                             {
                                                                 "name": "PostureAndPosition",
                                                                 "type": "String",
                                                                 "prompt": "Physical positioning, body language, and current interactions - where the character is and what position they're in. Include: base position (standing, sitting, kneeling, lying, bent over, suspended), specific pose details (how limbs are arranged, what they're resting on), relation to environment (furniture, equipment, walls, floor), relation to other characters (who's nearby, any physical contact), body language cues (tense, relaxed, trembling, eager), and any ongoing action (being used, waiting, restrained in position). This is the current physical snapshot.",
                                                                 "defaultValue": "Standing at ease, arms at sides, alone in room, relaxed posture, alert and waiting",
                                                                 "exampleValues": [
                                                                     "Kneeling in present position: Knees spread shoulder-width on hard floor (stone, uncomfortable), sitting back on heels, back straight, chest pushed forward, hands palm-up on thighs, head bowed submissively. Alone in room, waiting. Body language: controlled, trained posture, mild trembling from anticipation/cold.",
                                                                     "Secured in breeding stocks: Torso bent forward over padded bench (leather, bolted to floor), wrists locked in stocks at front, ankles locked in spreader bar behind, hips elevated by bench angle presenting holes at optimal height. Master positioned behind her, mid-thrust. Body language: gripping stocks, back arched, moaning with each impact.",
                                                                     "Curled in cage: Small iron cage (3ft cube), sitting with knees drawn to chest, arms wrapped around legs, head resting on knees. Cage locked, minimal room to move. Located in corner of dungeon, watching room through bars. Body language: small, protective posture, trembling slightly, eyes tracking all movement fearfully."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "SensoryState",
                                                                 "type": "String",
                                                                 "prompt": "Comprehensive tracking of what the character can PERCEIVE and EXPRESS - all senses and communication ability. Track four channels: VISION (Clear/Obscured/Blindfolded/Darkness), HEARING (Normal/Muffled/Plugged/Deafened), SPEECH (Free/Gagged [type]/Silenced), TOUCH (Normal/Heightened/Numbed). For any impairment, include cause, duration, and intensity. Sensory deprivation affects scene experience significantly. Format: 'Vision: X | Hearing: X | Speech: X | Touch: X' followed by detail.",
                                                                 "defaultValue": "Vision: Clear | Hearing: Normal | Speech: Free | Touch: Normal - All senses unimpaired",
                                                                 "exampleValues": [
                                                                     "Vision: Clear | Hearing: Normal | Speech: Free | Touch: Normal - All senses fully functional, no impairment to perception or communication",
                                                                     "Vision: Blindfolded (leather blindfold, 45 minutes) | Hearing: Normal | Speech: Free | Touch: Heightened (blindfold effect - other senses compensating, extra sensitive to physical contact) - Single-sense deprivation enhancing remaining senses",
                                                                     "Vision: Hooded (leather hood, no eye holes) | Hearing: Severely muffled (hood padding) | Speech: Blocked (hood has no mouth opening, can only make muffled sounds) | Touch: Heightened (full sensory deprivation effect) - Heavy isolation, only knows what she feels, disoriented, entirely dependent on handler"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "FreedomStatus",
                                                                 "type": "String",
                                                                 "prompt": "Legal and social freedom status - the character's position in society regarding personal autonomy. Levels: FREE (full autonomy, citizen rights), CONTRACTED (voluntary time-limited agreement), INDENTURED (debt-bound service), ENSLAVED (legal property, registered), OWNED (personal possession, may or may not be legally registered). Include: current status, owner/holder if applicable, how status was acquired, key terms (duration, buyout, restrictions), and practical implications for daily life.",
                                                                 "defaultValue": "Free - Full legal autonomy, no ownership or contracts, citizen rights",
                                                                 "exampleValues": [
                                                                     "Free - Full citizen with all rights, no contracts or obligations, owns herself completely, can go anywhere and do anything legal. No restrictions on person.",
                                                                     "Contracted - Voluntary 3-year service contract with House Halvard as domestic servant. 18 months remaining. Terms: Room, board, modest salary; Cannot leave grounds without permission; Service includes sexual availability to household. Buyout: 500 GC. Entered contract willingly to pay family debts.",
                                                                     "Enslaved - Legal registered property of Lord Marcus Halvard, purchased at auction for 3,500 GC two years ago. No rights, no autonomy, cannot own property, testimony not valid in court. Permanent status unless formally freed by owner. Branded with house mark, registered with city slavers guild. Exists as livestock with some protections against severe abuse but otherwise subject to owner's will completely."
                                                                 ]
                                                             }
                                                         ]
                                                     },
                                                     {
                                                         "name": "Equipment",
                                                         "type": "Object",
                                                         "prompt": "Currently worn clothing, gear, restraints, and insertions. Contains all items ON the character's body, organized by type. Separate from Inventory (which tracks carried items not being worn). Update as clothing is added, removed, damaged, or changed.",
                                                         "defaultValue": null,
                                                         "exampleValues": null,
                                                         "nestedFields": [
                                                             {
                                                                 "name": "UpperBody",
                                                                 "type": "String",
                                                                 "prompt": "Clothing on the torso - everything from neck to waist that isn't underwear. Include: garment type, material, color, fit, style, and current state (intact, unbuttoned, torn, pushed up, removed). If no upper body clothing, state 'None - torso bare'. This field tracks outer garments only; bras/breast bindings go in Underwear field. If removed, note where removed clothing is.",
                                                                 "defaultValue": "Simple white cotton blouse, loose fit, fully buttoned, intact",
                                                                 "exampleValues": [
                                                                     "None - Upper body completely bare, no clothing on torso",
                                                                     "White linen peasant blouse, loose fit, thin fabric, all buttons closed; intact and clean - modest common clothing",
                                                                     "Sheer black silk chemise, thin straps, plunging neckline, tight fit, nipples clearly visible through translucent fabric; intact - revealing evening/intimate wear"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "LowerBody",
                                                                 "type": "String",
                                                                 "prompt": "Clothing on lower body - waist to ankles, not including underwear or footwear. Include: garment type (skirt, pants, shorts, etc.), material, color, length, fit, and current state (intact, hiked up, pulled down, torn, removed). If no lower body clothing, state 'None - lower body bare'. If pushed up/down but not removed, describe current position. If removed, note where.",
                                                                 "defaultValue": "Brown cotton skirt, knee-length, simple cut, intact and in place",
                                                                 "exampleValues": [
                                                                     "None - No lower body clothing, bare from waist to feet (save underwear if worn)",
                                                                     "Brown wool skirt, ankle-length, modest cut, currently hiked up and bunched around waist - lower body exposed while technically still wearing it",
                                                                     "Tight black leather pants, low-rise, laced up sides, pulled down to thighs - caught around legs, restricting movement, ass and genitals exposed"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Underwear",
                                                                 "type": "String",
                                                                 "prompt": "CRITICAL FIELD - Undergarments MUST always be explicitly tracked. Include: breast support (bra, binding, none) AND lower underwear (panties, smallclothes, none). For each: describe type, material, color, style, and current state. If intentionally not wearing underwear, state clearly ('No bra - breasts unsupported', 'No panties - bare underneath'). If underwear was removed, note whether it's still nearby or taken. Never leave underwear status ambiguous.",
                                                                 "defaultValue": "Simple white cotton breast band; matching white cotton panties - basic modest smallclothes",
                                                                 "exampleValues": [
                                                                     "No bra - forbidden by owner, breasts unsupported; No panties - not permitted, bare underneath outer clothing. Always accessible.",
                                                                     "White cotton bralette (soft, no underwire, comfortable); white cotton panties (currently soaked through with arousal, visible wet spot) - simple underwear, panties showing obvious excitement",
                                                                     "Black lace push-up bra (enhancing cleavage); matching black lace thong (currently pulled aside, not removed, giving access while technically still worn) - fancy underwear, disheveled for access"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Feet",
                                                                 "type": "String",
                                                                 "prompt": "Footwear only - what's on the character's feet. Include: type (boots, shoes, heels, sandals, barefoot), material, color, heel height if applicable, style, condition. If barefoot, state clearly and note if shoes were removed (and where) or never worn. Footwear can affect posture, mobility, and vulnerability.",
                                                                 "defaultValue": "Simple brown leather ankle boots, flat heels, worn but serviceable",
                                                                 "exampleValues": [
                                                                     "Barefoot - No footwear, bare feet on floor (boots removed and placed by door)",
                                                                     "Black stiletto heels, 4-inch heel, strappy ankle design, locked on (small locks on ankle straps, cannot remove without key) - forced difficult footwear",
                                                                     "Knee-high leather riding boots, low sturdy heel, well-worn, practical - functional everyday footwear"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Accessories",
                                                                 "type": "String",
                                                                 "prompt": "Non-clothing items worn that are NOT weapons, restraints, or insertions - those have their own fields. Include: jewelry (decorative only - ownership collars go in BondageGear), belts (non-restraint), bags/pouches worn on body, hair accessories, decorative items. Describe material, appearance, and significance. If no accessories, state 'None'.",
                                                                 "defaultValue": "None - No accessories worn",
                                                                 "exampleValues": [
                                                                     "None - No jewelry, belts, or accessories",
                                                                     "Simple leather belt (holding skirt), small coin purse attached; silver stud earrings (both ears); no other accessories - practical minimal accessories",
                                                                     "Gold choker necklace (decorative gift, not restrictive); matching gold earrings (dangling); delicate gold ankle bracelet with small bell (tinkles when walking); leather belt with small pouch - dressed up with gifts from owner"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Weapons",
                                                                 "type": "String",
                                                                 "prompt": "Weapons currently carried or equipped. For each weapon: type, material, quality/condition, where carried (sheathed, holstered, held, strapped to back), and current state (sheathed, drawn, hidden). Include any special properties (enchanted, poisoned, named). Note proficiency if relevant. Use 'None/Unarmed' if character has no weapons. Separate multiple weapons with semicolons.",
                                                                 "defaultValue": "None - Unarmed",
                                                                 "exampleValues": [
                                                                     "None - Unarmed, no weapons carried. (Slave - not permitted weapons, would be severely punished if found armed)",
                                                                     "Steel shortsword - Good quality, leather-wrapped grip, belt scabbard left hip, currently sheathed; Iron dagger - Simple utility blade, boot sheath right boot, concealed",
                                                                     "Enchanted rapier 'Whisper' - Mithril blade, +1 sharpness enchantment, elegant basket hilt, belt scabbard left hip, currently drawn and ready; Throwing knives (3) - Steel, balanced, bandolier across chest, ready to throw"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "BondageGear",
                                                                 "type": "String",
                                                                 "prompt": "Active restraints and bondage equipment currently applied/worn - items that restrict movement or function. Include: collars (restrictive, ownership), cuffs (wrist, ankle), gags (type, size), blindfolds, rope bondage (what's bound, pattern), harnesses, clamps, and other restrictive devices. For each: material, how secured (locked, tied, buckled), duration worn, and effect on movement/function. 'None' if unrestrained. Separate from Insertions (inside body) and Chastity (denial devices).",
                                                                 "defaultValue": "None - Unrestrained, no bondage equipment",
                                                                 "exampleValues": [
                                                                     "None - Completely unrestrained, no collars, cuffs, or bondage equipment. Free movement.",
                                                                     "Leather collar (black, buckled, D-ring at front) - worn for 2 months, permanent daily wear, symbolic ownership. No other restraints currently.",
                                                                     "Steel posture collar (forces chin up, cannot look down) - locked; Leather armbinder (arms bound together behind back from fingers to above elbows, very restrictive) - laced and locked; Ball gag (large, red, buckled behind head, drooling around it) - 45 minutes; Ankle cuffs connected by 12-inch hobble chain - locked. Severely restrained, minimal mobility."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Insertions",
                                                                 "type": "String",
                                                                 "prompt": "Objects currently INSIDE body cavities - plugs, dildos, vibrators, beads, eggs, sounds, speculums, etc. For each: location (vaginal, anal, urethral), item description (type, size, material), how long inserted, whether secured/locked in place, and whether active (vibrating, inflating, etc.). Track what's inside, not external chastity devices (separate field). 'None' if all cavities empty. Update when insertions are added or removed.",
                                                                 "defaultValue": "None - No insertions, all cavities empty",
                                                                 "exampleValues": [
                                                                     "None - All cavities empty, nothing inserted",
                                                                     "Anal: Medium silicone plug (black, 1.5\" diameter) - inserted 3 hours ago as daily training, held in naturally, not locked. No other insertions.",
                                                                     "Vaginal: Remote-controlled vibrator egg (pink, powerful) - inserted 2 hours ago, currently on medium intensity, remote held by owner; Anal: Large steel plug (2\" diameter, jeweled base) - inserted this morning, locked in place by chastity belt, cannot be removed. Both holes filled and secured."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Chastity",
                                                                 "type": "String",
                                                                 "prompt": "Chastity devices preventing sexual access or orgasm - separate from insertions (things inside) and bondage (movement restriction). Include: device type (belt, cage, shield), material, coverage (what's blocked/protected), locking mechanism, who holds key, duration worn, and any features (built-in plugs, waste allowance). 'None' if no chastity device. Chastity devices typically prevent unauthorized touching, penetration, or orgasm.",
                                                                 "defaultValue": "None - No chastity device, genitals accessible",
                                                                 "exampleValues": [
                                                                     "None - No chastity device, genitals unprotected and accessible",
                                                                     "Steel chastity belt (polished, custom-fitted) - Front shield covers and protects vulva completely, small holes for urination only; no rear coverage. Locked with padlock, key held by owner. Worn continuously for 2 weeks, removed only for cleaning (supervised).",
                                                                     "Full steel chastity belt - Front shield (blocks vaginal access), rear shield (blocks anal access), both locked. Built-in vaginal plug (medium, locked inside) and anal plug (small, locked inside). Waste requires supervised plug removal. Key held by owner. Worn 1 month, total denial, cannot touch or be penetrated without owner unlocking."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "StateOfDress",
                                                                 "type": "String",
                                                                 "prompt": "SUMMARY field - overall assessment of how dressed/undressed and put-together the character appears. This is the quick-reference for current clothed status. Options: Pristine (perfect), Neat (properly dressed), Casual (relaxed but dressed), Disheveled (messy, askew), Partially Undressed (some clothing removed/displaced), Stripped (most clothing removed), Nude (nothing), Exposed (clothed but arranged for access). Include key details and where any removed clothing is located.",
                                                                 "defaultValue": "Neat - Properly dressed, clothes in place, presentable",
                                                                 "exampleValues": [
                                                                     "Pristine - Fully dressed, every piece in perfect position, clean and unwrinkled, appearance carefully maintained",
                                                                     "Disheveled - Still technically dressed but: blouse untucked and partially unbuttoned, skirt twisted and wrinkled, hair messy, clearly has been through something; clothing intact but disarrayed",
                                                                     "Nude - Completely naked, all clothing removed and folded on nearby chair. Wearing only collar (permanent). Body fully exposed."
                                                                 ]
                                                             }
                                                         ]
                                                     },
                                                     {
                                                         "name": "Economy",
                                                         "type": "Object",
                                                         "prompt": "Current financial status - what monetary resources the character has available RIGHT NOW. For slaves, includes their assessed market value.",
                                                         "defaultValue": null,
                                                         "exampleValues": null,
                                                         "nestedFields": [
                                                             {
                                                                 "name": "Cash",
                                                                 "type": "String",
                                                                 "prompt": "Liquid currency currently in character's possession. Standard currency: GC (Gold Crowns), SS (Silver Shards), CB (Copper Bits). Exchange: 1 GC = 100 SS = 10,000 CB. Format: 'X GC | Y SS | Z CB'. If character cannot legally own money (slave), note whether they have any secret stash or are holding owner's money. Include context for financial state.",
                                                                 "defaultValue": "0 GC | 0 SS | 0 CB",
                                                                 "exampleValues": [
                                                                     "0 GC | 0 SS | 0 CB - No money. Slave status, not permitted to own currency, has nothing hidden.",
                                                                     "2 GC | 45 SS | 12 CB - Modest personal funds, enough for a few days' expenses. Working class level.",
                                                                     "450 GC | 23 SS | 0 CB - Substantial savings, comfortable finances, could make significant purchases. Upper-middle class level."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Assets",
                                                                 "type": "String",
                                                                 "prompt": "Significant assets beyond carried cash - property, business stakes, valuables, contracts, owned slaves. Include: asset description, estimated value, and any income generated. If character IS property (slave), note their own assessed market value as an asset to their owner. If no assets, state 'None'. This tracks wealth beyond pocket money.",
                                                                 "defaultValue": "None - No significant assets beyond personal effects",
                                                                 "exampleValues": [
                                                                     "IS PROPERTY - Character is a slave, cannot own assets. Her own market value: ~4,000 GC (young, beautiful, trained pleasure slave, fertile). Generates no income for herself.",
                                                                     "None - Free but poor, no assets beyond the clothes on her back and basic carried items. Lives day-to-day.",
                                                                     "Small house in merchant district (owned outright, value: 800 GC); Inherited jewelry collection (value: 200 GC, sentimental); 15% stake in family trading business (value: ~500 GC, provides ~8 GC/month income). Total net worth: ~1,500 GC. Comfortable middle-class prosperity."
                                                                 ]
                                                             }
                                                         ]
                                                     },
                                                     {
                                                         "name": "Inventory",
                                                         "type": "ForEachObject",
                                                         "prompt": "Specific items the character is carrying or has immediate access to - NOT items being worn (those go in Equipment). Each item tracked separately with details. Only include currently possessed items. Remove items when lost, used, or left behind. This is for objects in pockets, bags, held in hands - portable possessions.",
                                                         "defaultValue": null,
                                                         "exampleValues": null,
                                                         "nestedFields": [
                                                             {
                                                                 "name": "ItemName",
                                                                 "type": "String",
                                                                 "prompt": "Name or type of the item - clear identifier.",
                                                                 "defaultValue": "Item",
                                                                 "exampleValues": [
                                                                     "Health Potion",
                                                                     "Rope (Hemp)",
                                                                     "Key (Brass)"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Description",
                                                                 "type": "String",
                                                                 "prompt": "Brief description including condition, notable features, and relevant details that might matter for use.",
                                                                 "defaultValue": "A common item",
                                                                 "exampleValues": [
                                                                     "Red liquid in small glass vial, cork stopper, heals minor wounds when drunk",
                                                                     "50 feet of rough hemp rope, sturdy, good for binding or climbing, slightly frayed at ends",
                                                                     "Small brass key, ornate bow, unlocks unknown lock (found in master's desk)"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Quantity",
                                                                 "type": "String",
                                                                 "prompt": "Number of this item possessed. For consumables, track uses remaining.",
                                                                 "defaultValue": "1",
                                                                 "exampleValues": [
                                                                     "1",
                                                                     "3",
                                                                     "50 feet"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Location",
                                                                 "type": "String",
                                                                 "prompt": "Where on person or in what container the item is currently kept.",
                                                                 "defaultValue": "Carried",
                                                                 "exampleValues": [
                                                                     "Belt pouch",
                                                                     "Hidden in boot",
                                                                     "Held in left hand"
                                                                 ]
                                                             }
                                                         ]
                                                     }
                                                 ]
                                             },
                                             {
                                                 "name": "Development",
                                                 "type": "Object",
                                                 "prompt": "All permanent and evolving aspects of the character - skills learned, traits gained or developed, abilities mastered, physical modifications, sexual history, and growth potential. This section tracks everything that represents WHO THE CHARACTER HAS BECOME through their experiences. Changes here are significant and lasting.",
                                                 "defaultValue": null,
                                                 "exampleValues": null,
                                                 "nestedFields": [
                                                     {
                                                         "name": "Skills",
                                                         "type": "ForEachObject",
                                                         "prompt": "Active skills the character has developed or is developing. Skills are created dynamically as the character encounters new challenges - if a character attempts something requiring skill, check if a relevant skill exists; if not, create it at Untrained. Skills represent passive competency and knowledge, NOT active techniques (those are Abilities). Proficiency Levels: Untrained (0 XP) → Novice (50) → Amateur (150) → Competent (400) → Proficient (900) → Expert (1900) → Master (4400) → Grandmaster (9400+). XP GAIN RULES: Character only gains meaningful XP from tasks at or above their Challenge Floor (equal to current proficiency). Tasks below floor grant 0-10% XP. Tasks at floor grant standard XP. Tasks above floor grant bonus XP (up to 200% for extreme challenges). Dramatic, high-stakes, or innovative uses grant +25-50% bonus. Training with a superior teacher grants +25% bonus. Failed attempts that push limits still grant partial XP. Track each skill separately.",
                                                         "defaultValue": null,
                                                         "exampleValues": null,
                                                         "nestedFields": [
                                                             {
                                                                 "name": "SkillName",
                                                                 "type": "String",
                                                                 "prompt": "Clear, medium-granularity name for the skill. Not too broad (avoid just 'Combat'), not too narrow (avoid 'Parrying with Longswords'). Good examples: 'Swordsmanship', 'Fire Magic', 'Lockpicking', 'Persuasion', 'Oral Service', 'Horseback Riding'. The name should clearly indicate what competency is being measured.",
                                                                 "defaultValue": "Unnamed Skill",
                                                                 "exampleValues": [
                                                                     "Swordsmanship",
                                                                     "Destruction Magic",
                                                                     "Stealth",
                                                                     "Seduction",
                                                                     "Herbalism",
                                                                     "Pain Endurance"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Category",
                                                                 "type": "String",
                                                                 "prompt": "Dynamic category grouping related skills. Create categories as needed to organize skills logically. Common categories: Combat (fighting skills), Magic (spellcasting schools), Social (interpersonal skills), Survival (outdoor/practical skills), Craft (making things), Service (domestic/sexual service), Physical (body-based non-combat), Mental (knowledge/intellectual), Subterfuge (stealth/deception). A skill belongs to ONE primary category.",
                                                                 "defaultValue": "General",
                                                                 "exampleValues": [
                                                                     "Combat",
                                                                     "Magic",
                                                                     "Social",
                                                                     "Service",
                                                                     "Survival",
                                                                     "Subterfuge"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Proficiency",
                                                                 "type": "String",
                                                                 "prompt": "Current mastery level. Levels represent distinct competency tiers: UNTRAINED (no formal skill, relies on instinct), NOVICE (basic understanding, many mistakes), AMATEUR (functional but inconsistent, needs supervision), COMPETENT (reliable performance on standard tasks, professional minimum), PROFICIENT (skilled practitioner, handles complications well), EXPERT (exceptional skill, recognized specialist), MASTER (elite few, can innovate and teach at highest levels), GRANDMASTER (legendary, pushes boundaries of what's possible). Most people cap at Competent-Proficient in their profession. Expert+ is genuinely rare.",
                                                                 "defaultValue": "Untrained",
                                                                 "exampleValues": [
                                                                     "Untrained",
                                                                     "Novice",
                                                                     "Amateur",
                                                                     "Competent",
                                                                     "Proficient",
                                                                     "Expert",
                                                                     "Master",
                                                                     "Grandmaster"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Progress",
                                                                 "type": "String",
                                                                 "prompt": "Current XP toward next level in format 'Current / Required'. XP thresholds - To reach: Novice (50), Amateur (150), Competent (400), Proficient (900), Expert (1900), Master (4400), Grandmaster (9400). Numbers represent TOTAL accumulated XP, not per-level. When threshold is reached, level up and continue accumulating toward next threshold. At Grandmaster, continue tracking XP but no further level exists. Example: '523 / 900' means 523 XP accumulated, needs 900 for Proficient.",
                                                                 "defaultValue": "0 / 50",
                                                                 "exampleValues": [
                                                                     "0 / 50 (just started, working toward Novice)",
                                                                     "127 / 150 (Amateur, close to Competent)",
                                                                     "1850 / 1900 (Expert, nearly Master)",
                                                                     "11,240 / MAX (Grandmaster, still growing)"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "ChallengeFloor",
                                                                 "type": "String",
                                                                 "prompt": "Minimum difficulty of tasks that grant meaningful XP - equals current Proficiency level. Tasks BELOW this floor grant only 0-10% normal XP (routine practice maintains but doesn't improve). Tasks AT floor grant standard XP. Tasks ABOVE floor grant bonus XP. This creates diminishing returns - a Master swordsman gains nothing from sparring with beginners but gains full XP dueling other Masters. Format: 'Level (explanation of what qualifies)'. Update when proficiency increases.",
                                                                 "defaultValue": "Untrained (any attempt at the skill grants XP)",
                                                                 "exampleValues": [
                                                                     "Untrained (any practice counts, everything is challenging)",
                                                                     "Novice (basic exercises grant minimal XP; need real application or difficult drills)",
                                                                     "Competent (routine professional tasks grant minimal XP; need genuine challenges, novel problems, or superior opponents)",
                                                                     "Master (standard difficult tasks grant minimal XP; only extreme challenges, innovation, teaching masters, or legendary feats grant meaningful XP)"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Development",
                                                                 "type": "String",
                                                                 "prompt": "Narrative tracking of how this skill has developed - training history, key learning moments, teachers, and recent progress. Include: how skill was first acquired, notable training or experiences that granted significant XP, any teachers/mentors who contributed, recent developments, and current training focus if any. This provides context for the numbers and tracks the story of the character's growth in this area.",
                                                                 "defaultValue": "Newly encountered skill, no development history yet.",
                                                                 "exampleValues": [
                                                                     "Self-taught basics through trial and error over first month of captivity. No formal instruction. Recent: Gained significant XP during escape attempt (high-stakes application).",
                                                                     "Formally trained from age 8 at father's insistence. Journeyman instructor for 6 years, then 2 years under Swordmaster Aldric (Expert). Reached Proficient before capture. Skills maintained but not advancing in captivity - no worthy opponents. Recent: Successfully defended self against guard (routine, minimal XP).",
                                                                     "Natural talent identified by court mage at age 12 (see Trait: Magically Gifted). Apprenticed for 4 years, focused on fire specialization. Teaching accelerated progress significantly. Recent: Breakthrough during emotional crisis unlocked new understanding (+150 XP bonus from dramatic moment)."
                                                                 ]
                                                             }
                                                         ]
                                                     },
                                                     {
                                                         "name": "Traits",
                                                         "type": "Object",
                                                         "prompt": "Permanent characteristics that provide mechanical effects - natural talents, developed strengths, weaknesses, and flaws. Traits are MORE than just narrative descriptors; they have MECHANICAL EFFECTS on skill gain, ability use, or other systems. Separated into Positive and Negative for clarity.",
                                                         "defaultValue": null,
                                                         "exampleValues": null,
                                                         "nestedFields": [
                                                             {
                                                                 "name": "Positive",
                                                                 "type": "ForEachObject",
                                                                 "prompt": "Beneficial characteristics that provide advantages - natural talents, developed strengths, and positive qualities. Innate traits are present from birth/character creation. Acquired traits develop through significant experiences, training, or events. Traits can have varying intensity affecting their mechanical impact.",
                                                                 "defaultValue": null,
                                                                 "exampleValues": null,
                                                                 "nestedFields": [
                                                                     {
                                                                         "name": "TraitName",
                                                                         "type": "String",
                                                                         "prompt": "Clear, evocative name for the trait. Should immediately suggest what the trait does. Can be simple ('Strong') or more flavorful ('Iron Will'). Avoid overly generic names.",
                                                                         "defaultValue": "Unnamed Trait",
                                                                         "exampleValues": [
                                                                             "Magically Gifted",
                                                                             "Natural Beauty",
                                                                             "Iron Will",
                                                                             "Quick Reflexes",
                                                                             "Pain Tolerance",
                                                                             "Fertile"
                                                                         ]
                                                                     },
                                                                     {
                                                                         "name": "Origin",
                                                                         "type": "String",
                                                                         "prompt": "Whether trait is INNATE (born with, always had) or ACQUIRED (developed through experience). For acquired traits, briefly note the circumstances that created it. Format: 'Innate' or 'Acquired (circumstances)'.",
                                                                         "defaultValue": "Innate",
                                                                         "exampleValues": [
                                                                             "Innate",
                                                                             "Innate (elven heritage)",
                                                                             "Acquired (6 months of torture survival)",
                                                                             "Acquired (blessing from shrine ritual)"
                                                                         ]
                                                                     },
                                                                     {
                                                                         "name": "Category",
                                                                         "type": "String",
                                                                         "prompt": "Classification of what aspect of the character the trait affects. Categories: PHYSICAL (body, health, appearance, physical capabilities), MENTAL (intelligence, willpower, learning, psychological), SOCIAL (charisma, reputation, interpersonal), MAGICAL (mana, spellcasting, magical sensitivity), SEXUAL (arousal, fertility, sexual response, appeal), SPECIAL (unique/supernatural traits).",
                                                                         "defaultValue": "Physical",
                                                                         "exampleValues": [
                                                                             "Physical",
                                                                             "Mental",
                                                                             "Social",
                                                                             "Magical",
                                                                             "Sexual",
                                                                             "Special"
                                                                         ]
                                                                     },
                                                                     {
                                                                         "name": "Intensity",
                                                                         "type": "String",
                                                                         "prompt": "How strong the trait manifests, affecting mechanical impact. Scale: MILD (~10-15% modifier), MODERATE (~25% modifier), STRONG (~50% modifier), OVERWHELMING (~75-100% modifier). Most traits are Mild or Moderate. Intensity can change over time.",
                                                                         "defaultValue": "Moderate",
                                                                         "exampleValues": [
                                                                             "Mild",
                                                                             "Moderate",
                                                                             "Strong",
                                                                             "Overwhelming"
                                                                         ]
                                                                     },
                                                                     {
                                                                         "name": "Effect",
                                                                         "type": "String",
                                                                         "prompt": "SPECIFIC mechanical effect of the trait - what it actually DOES in game terms. Be concrete: XP modifiers to specific skill categories, bonuses/penalties to specific actions, unlocked options, resistance to certain effects, etc. Effects should scale with Intensity.",
                                                                         "defaultValue": "Effect not yet defined",
                                                                         "exampleValues": [
                                                                             "+50% XP gain for all Magic category skills. Can instinctively sense strong magical phenomena nearby. Qualifies for advanced magical training that requires talent.",
                                                                             "+25% XP gain for Social skills involving appearance. +1 tier effective Proficiency when using looks to persuade/seduce.",
                                                                             "Can endure pain up to 7/10 without performance penalties (normally 5/10). +25% XP for Pain Endurance skill.",
                                                                             "2x standard conception chance during fertile periods. Healthy pregnancies with minimal complications. Twins 15% chance (normally 2%)."
                                                                         ]
                                                                     },
                                                                     {
                                                                         "name": "Notes",
                                                                         "type": "String",
                                                                         "prompt": "Additional context, narrative details, and tracking for trait changes. Include: how trait manifests in personality/behavior, any conditions that could strengthen or weaken the trait, and relevant story details.",
                                                                         "defaultValue": "No additional notes.",
                                                                         "exampleValues": [
                                                                             "Magical sensitivity sometimes causes headaches around powerful artifacts. Family bloodline trait. Stable innate trait, unlikely to change.",
                                                                             "Developed after surviving the dungeon. Took approximately 3 months of regular exposure to develop. Psychologically tied to dissociation response.",
                                                                             "Natural beauty maintained despite hardship. Currently somewhat diminished by exhaustion - would return to full effect with proper rest."
                                                                         ]
                                                                     }
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Negative",
                                                                 "type": "ForEachObject",
                                                                 "prompt": "Detrimental characteristics that impose disadvantages - weaknesses, flaws, vulnerabilities, and negative qualities. Like positive traits, these have MECHANICAL EFFECTS. Innate negative traits are often harder to overcome. Acquired negative traits may be healed under right circumstances.",
                                                                 "defaultValue": null,
                                                                 "exampleValues": null,
                                                                 "nestedFields": [
                                                                     {
                                                                         "name": "TraitName",
                                                                         "type": "String",
                                                                         "prompt": "Clear name indicating the weakness or flaw. Should suggest mechanical impact.",
                                                                         "defaultValue": "Unnamed Flaw",
                                                                         "exampleValues": [
                                                                             "Frail Constitution",
                                                                             "Easily Broken",
                                                                             "Magically Null",
                                                                             "Slave Brand",
                                                                             "Trauma: Darkness",
                                                                             "Conditioned Obedience"
                                                                         ]
                                                                     },
                                                                     {
                                                                         "name": "Origin",
                                                                         "type": "String",
                                                                         "prompt": "INNATE (born with) or ACQUIRED (developed). For acquired negative traits, document the cause - this often points to potential cure/resolution.",
                                                                         "defaultValue": "Innate",
                                                                         "exampleValues": [
                                                                             "Innate (born sickly)",
                                                                             "Acquired (branded by House Halvard upon purchase, permanent)",
                                                                             "Acquired (6 months of captivity conditioning, potentially reversible)",
                                                                             "Acquired (witnessed family's murder, deep psychological wound)"
                                                                         ]
                                                                     },
                                                                     {
                                                                         "name": "Category",
                                                                         "type": "String",
                                                                         "prompt": "Same categories as positive traits: PHYSICAL, MENTAL, SOCIAL, MAGICAL, SEXUAL, SPECIAL. Indicates what aspect of character is impaired.",
                                                                         "defaultValue": "Physical",
                                                                         "exampleValues": [
                                                                             "Physical",
                                                                             "Mental",
                                                                             "Social",
                                                                             "Magical",
                                                                             "Sexual",
                                                                             "Special"
                                                                         ]
                                                                     },
                                                                     {
                                                                         "name": "Intensity",
                                                                         "type": "String",
                                                                         "prompt": "Severity: MILD (~10-15% penalty), MODERATE (~25% penalty), SEVERE (~50% penalty), CRIPPLING (~75-100% penalty). Intensity may change - trauma can worsen or heal.",
                                                                         "defaultValue": "Moderate",
                                                                         "exampleValues": [
                                                                             "Mild",
                                                                             "Moderate",
                                                                             "Severe",
                                                                             "Crippling"
                                                                         ]
                                                                     },
                                                                     {
                                                                         "name": "Effect",
                                                                         "type": "String",
                                                                         "prompt": "Specific mechanical penalties and limitations imposed. Be concrete: XP penalties, skill caps, triggered penalties in certain situations, locked options, automatic failures. For conditional traits (phobias, triggers), specify WHAT activates them and WHAT happens.",
                                                                         "defaultValue": "Effect not yet defined",
                                                                         "exampleValues": [
                                                                             "-50% XP gain for all Physical category skills. Skill cap: Physical skills cannot exceed Proficient. Fatigues 50% faster than normal.",
                                                                             "TRIGGER: Enclosed dark spaces. EFFECT: When triggered, must make Mental check or enter panic state (cannot act coherently, -3 tiers to all skills). Even with successful check, -2 tiers while in trigger.",
                                                                             "Cannot learn Magic category skills (permanent cap at Untrained). Cannot use magical items requiring attunement. Magic healing is 50% less effective.",
                                                                             "When given direct commands by recognized authority figure, must make Mental check or comply automatically. Result of 8 months conditioning."
                                                                         ]
                                                                     },
                                                                     {
                                                                         "name": "Recovery",
                                                                         "type": "String",
                                                                         "prompt": "Can this trait be reduced or removed? If so, how? Track progress toward recovery if applicable. Categories: PERMANENT (cannot be changed), TREATABLE (can be reduced/removed with specific intervention), VARIABLE (fluctuates based on circumstances).",
                                                                         "defaultValue": "Unknown - not yet explored whether this can change",
                                                                         "exampleValues": [
                                                                             "Permanent - Innate physical limitation, cannot be overcome, only accommodated.",
                                                                             "Treatable - Psychological conditioning can theoretically be broken through extended counter-conditioning. Current progress: None. Would require months in safe environment.",
                                                                             "Treatable - Trauma response. Intensity has decreased from Severe to Moderate over past year. Further improvement possible with continued safety. Could worsen if retraumatized.",
                                                                             "Permanent - Physical brand cannot be removed without visible scarring. Social effects could be mitigated by freedom papers."
                                                                         ]
                                                                     }
                                                                 ]
                                                             }
                                                         ]
                                                     },
                                                     {
                                                         "name": "Abilities",
                                                         "type": "ForEachObject",
                                                         "prompt": "Active capabilities - specific techniques, spells, moves, or actions that can be consciously performed. Unlike skills (passive competency), abilities are DISCRETE ACTIONS. Abilities are typically: learned from teachers, unlocked at skill thresholds, discovered through experimentation, or granted by traits/items. Each ability has requirements to use, costs, and specific effects. Create abilities as they are learned.",
                                                         "defaultValue": null,
                                                         "exampleValues": null,
                                                         "nestedFields": [
                                                             {
                                                                 "name": "AbilityName",
                                                                 "type": "String",
                                                                 "prompt": "Clear name for the specific technique, spell, or action. Should be evocative and suggest function.",
                                                                 "defaultValue": "Unnamed Ability",
                                                                 "exampleValues": [
                                                                     "Fireball",
                                                                     "Disarming Strike",
                                                                     "Healing Touch",
                                                                     "Shadow Step",
                                                                     "Seductive Whisper",
                                                                     "Pain Block"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Type",
                                                                 "type": "String",
                                                                 "prompt": "Classification: SPELL (magical, requires mana/casting), TECHNIQUE (physical skill application, requires stamina), SKILL APPLICATION (specific use of a skill, minimal cost), INNATE (natural ability from trait/race), RITUAL (extended casting, requires time/materials), OTHER.",
                                                                 "defaultValue": "Technique",
                                                                 "exampleValues": [
                                                                     "Spell",
                                                                     "Technique",
                                                                     "Skill Application",
                                                                     "Innate",
                                                                     "Ritual"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "LinkedSkill",
                                                                 "type": "String",
                                                                 "prompt": "Which skill this ability is connected to - determines base competency. Format: 'SkillName (Proficiency)' showing current skill level.",
                                                                 "defaultValue": "None (innate ability)",
                                                                 "exampleValues": [
                                                                     "Destruction Magic (Competent)",
                                                                     "Swordsmanship (Proficient)",
                                                                     "Seduction (Expert)",
                                                                     "None (racial innate ability)"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Mastery",
                                                                 "type": "String",
                                                                 "prompt": "How well THIS SPECIFIC ABILITY is mastered - separate from overall skill level. Levels: LEARNING (just acquired, unreliable, ~50% success), PRACTICED (functional, ~80% success), MASTERED (reliable, ~95% success), PERFECTED (flawless, 100% base success, enhanced effects possible).",
                                                                 "defaultValue": "Learning",
                                                                 "exampleValues": [
                                                                     "Learning - Just taught, success rate ~50%, requires concentration",
                                                                     "Practiced - Can reliably perform under normal conditions, ~80% success",
                                                                     "Mastered - Second nature, reliable even under pressure, ~95% success",
                                                                     "Perfected - Flawless execution, can perform enhanced version, 100% base success"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Description",
                                                                 "type": "String",
                                                                 "prompt": "What the ability DOES when successfully used - the actual effect. Be specific about: what happens, range/targets, damage/healing if applicable, duration of effects.",
                                                                 "defaultValue": "Effect not yet defined",
                                                                 "exampleValues": [
                                                                     "Conjures a ball of fire that can be thrown up to 30 feet. Explodes on impact, dealing significant burn damage in 5-foot radius. Sets flammable materials alight.",
                                                                     "A precise blade technique targeting opponent's weapon grip. On success, forces opponent to drop their weapon.",
                                                                     "Mental technique to suppress pain sensation temporarily. Reduces effective Pain by 3 points for 10 minutes. Does NOT heal damage. When effect ends, full pain returns plus 1 additional point.",
                                                                     "Channel healing magic through touch. Heals minor-moderate wounds over 30 seconds of maintained contact."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Requirements",
                                                                 "type": "String",
                                                                 "prompt": "What's needed to USE the ability - prerequisites for attempting it. Include: minimum skill level, necessary equipment, physical requirements, situational requirements. If requirements aren't met, ability cannot be attempted.",
                                                                 "defaultValue": "No special requirements",
                                                                 "exampleValues": [
                                                                     "Requires: Destruction Magic at Amateur+, free hand to gesture, verbal component, target within 30 feet and visible",
                                                                     "Requires: Swordsmanship at Competent+, wielding a bladed weapon, opponent in melee range wielding a disarmable weapon",
                                                                     "Requires: Pain Endurance at Amateur+, conscious and able to concentrate, not already under effect of Pain Block"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Costs",
                                                                 "type": "String",
                                                                 "prompt": "Specific resource costs to use this ability. List each resource spent. Include: MANA, STAMINA, FOCUS, or special resources. Also note non-resource costs: cooldowns, material components, health costs.",
                                                                 "defaultValue": "No resource cost",
                                                                 "exampleValues": [
                                                                     "Mana: 15 | No other costs. Can cast repeatedly as long as mana available.",
                                                                     "Mana: 35 | Focus: 10 | Demanding spell requiring both magical power and concentration.",
                                                                     "Stamina: 20 | Adds +1 Fatigue per use. Physical technique with real exertion cost.",
                                                                     "Stamina: 30 | Cooldown: Cannot use again for 5 minutes (muscles need recovery).",
                                                                     "No resource cost | Cooldown: Once per day. Innate racial ability with natural limit."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Acquisition",
                                                                 "type": "String",
                                                                 "prompt": "How the character learned this ability - who taught it, when, circumstances, any special significance. For abilities not yet learned, can track progress toward learning.",
                                                                 "defaultValue": "Origin not recorded",
                                                                 "exampleValues": [
                                                                     "Taught by Court Mage Helena during formal apprenticeship, age 14. Part of standard Destruction curriculum.",
                                                                     "Self-discovered during desperate fight, Day 47 of captivity. Instinctive response that worked. Later refined through practice.",
                                                                     "NOT YET LEARNED - Aware this technique exists from watching others. Would need Expert+ teacher and ~2 weeks training."
                                                                 ]
                                                             }
                                                         ]
                                                     },
                                                     {
                                                         "name": "PermanentMarks",
                                                         "type": "String",
                                                         "prompt": "Permanent and semi-permanent body alterations - track everything that won't wash off or heal within days. Categories: TATTOOS (location, design, meaning/origin, consensual or forced), PIERCINGS (location, jewelry type/material, gauge, healed or fresh), BRANDS (location, design, owner/origin), SCARS (location, cause, age, appearance), OTHER MODIFICATIONS (surgical, magical). Distinguish between chosen body art and forced markings. Use 'None' if body is unmarked. This is a cumulative record - add new marks as they're acquired.",
                                                         "defaultValue": "None - Body unmarked, no tattoos, piercings, brands, or significant scars",
                                                         "exampleValues": [
                                                             "None - Completely unmarked body, no tattoos, piercings, brands, or notable scars. Clean slate.",
                                                             "Piercings only: Earlobes (simple silver studs, both ears, childhood, healed); Navel (small gem dangle, personal choice at 18, healed). No other modifications.",
                                                             "Ownership marks: BRAND - House Halvard crest on left inner thigh, size of coin, healed silver scar (applied at purchase, non-consensual). TATTOO - 'Property of Lord Halvard' across lower back, black ink, forced. PIERCINGS - nipples (heavy gauge steel rings for attachment, forced, healed) and clit hood (steel barbell, forced, healed). COLLAR SCAR - faint permanent line around neck from first year of collar wear."
                                                         ]
                                                     },
                                                     {
                                                         "name": "SexualHistory",
                                                         "type": "Object",
                                                         "prompt": "Permanent record of sexual development, experiences, and evolution. These are lasting aspects of the character's sexual identity that have developed over time.",
                                                         "defaultValue": null,
                                                         "exampleValues": null,
                                                         "nestedFields": [
                                                             {
                                                                 "name": "Experience",
                                                                 "type": "String",
                                                                 "prompt": "Narrative summary of overall sexual experience level and background - the qualitative description. Describe: general experience level (virgin, inexperienced, moderate, experienced, extensive), relevant history (sheltered upbringing, previous relationships, professional background, forced experience), and context that shapes their sexuality.",
                                                                 "defaultValue": "Inexperienced - Virgin with limited to no sexual experience",
                                                                 "exampleValues": [
                                                                     "Complete Virgin - No sexual experience whatsoever. Sheltered religious upbringing, never even kissed, minimal understanding of sex beyond basic reproduction.",
                                                                     "Moderately Experienced - Several consensual partners over 4 years of adult sexual activity. Comfortable with common positions and acts, knows what she enjoys.",
                                                                     "Extensively Used (Non-consensual) - Was virgin until capture 6 months ago. Since then, used multiple times daily. Body is experienced but mind still processes much as trauma."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "VirginityStatus",
                                                                 "type": "String",
                                                                 "prompt": "Explicit per-orifice virginity tracking - has each entrance been penetrated sexually? Format: 'Oral: [Status] | Vaginal: [Status] | Anal: [Status]'. For each, state if Virgin or Taken. If taken, include WHO, WHEN, and whether consensual or forced. This is a permanent record of 'firsts'.",
                                                                 "defaultValue": "Oral: Virgin | Vaginal: Virgin | Anal: Virgin",
                                                                 "exampleValues": [
                                                                     "Oral: Virgin | Vaginal: Virgin | Anal: Virgin - Completely untouched, all virginities intact",
                                                                     "Oral: Taken (boyfriend Evan, age 18, consensual) | Vaginal: Taken (same boyfriend, age 19, consensual) | Anal: Virgin",
                                                                     "Oral: Taken (Guard Captain Roth, Day 1, forced) | Vaginal: Taken (Lord Halvard, Day 1, forced) | Anal: Taken (Guard Captain Roth, Day 3, forced punishment)"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "PartnerCounts",
                                                                 "type": "String",
                                                                 "prompt": "Numeric tracking of sexual partners and acts. Track: total unique partners, and counts by act type. Use specific numbers when low, estimates when high. Format: 'Partners: X | Vaginal: X | Anal: X | Oral: X | Other: [notes]'.",
                                                                 "defaultValue": "Partners: 0 | Vaginal: 0 | Anal: 0 | Oral: 0",
                                                                 "exampleValues": [
                                                                     "Partners: 0 | Vaginal: 0 | Anal: 0 | Oral: 0 - Virgin, no sexual contact",
                                                                     "Partners: 3 | Vaginal: ~40 | Anal: 0 | Oral: ~25 | Other: Handjobs ~15",
                                                                     "Partners: Unknown (50+?) | Vaginal: Countless | Anal: ~200 | Oral: Countless | Creampies: 300+ | Gangbangs: 12"
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "KinksAndFetishes",
                                                                 "type": "String",
                                                                 "prompt": "Sexual turn-ons, fetishes, and preferences - what arouses the character beyond basic stimulation. Track: pre-existing kinks (had before story), discovered kinks (found during story), trained/conditioned responses (arousal created through conditioning). Note intensity for each.",
                                                                 "defaultValue": "Unknown/Undiscovered - Inexperienced, no known kinks or preferences yet identified",
                                                                 "exampleValues": [
                                                                     "Unknown/Undiscovered - Virgin with no sexual experience, hasn't had opportunity to discover preferences.",
                                                                     "Pre-existing: Light bondage (mild), praise kink (strong 'good girl' response), exhibitionism (discovered, gets wet thinking about being watched). Discovered recently: Hair pulling (strong), rough handling (mild interest).",
                                                                     "Conditioned responses (not natural): Pain → Arousal (extensive training), Degradation → Arousal (humiliation triggers orgasm), Submission → Deep satisfaction (trained pleasure from obedience). Body responds to things she mentally resists."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "Limits",
                                                                 "type": "String",
                                                                 "prompt": "What the character will NOT do or has strong resistance to. HARD LIMITS: Absolute boundaries, will fight regardless. SOFT LIMITS: Reluctant but can be pushed past. Track: current limits, limits that have been broken, how limits have changed.",
                                                                 "defaultValue": "Inexperienced - All sexual acts feel like limits, no established hard/soft distinction yet",
                                                                 "exampleValues": [
                                                                     "Inexperienced - Everything feels like a limit due to virgin status. Boundaries will become clearer with exposure.",
                                                                     "Hard Limits: Scat, permanent damage, bestiality. Soft Limits: Anal (nervous but curious), pain play (scared but intrigued), public sex (embarrassing but arousing).",
                                                                     "Hard Limits (remaining): Only scat and permanent mutilation. Former Limits (broken): Anal (now routine), public use (now conditioned to accept), pain (now arousal response), multiple partners (resistance broken). Conditioning has erased most resistance."
                                                                 ]
                                                             }
                                                         ]
                                                     },
                                                     {
                                                         "name": "ReproductiveHistory",
                                                         "type": "Object",
                                                         "prompt": "Permanent record of reproductive events - children born. Current pregnancy status is tracked in CurrentState.Reproduction.",
                                                         "defaultValue": null,
                                                         "exampleValues": null,
                                                         "nestedFields": [
                                                             {
                                                                 "name": "Children",
                                                                 "type": "String",
                                                                 "prompt": "Record of offspring after birth - cumulative list of all children born to this character. For each child: birth order, name (if given), sex, other parent's identity, and current status/whereabouts. Format: '{Ordinal}: {Name}, {Sex}, with {Other Parent} - {Current Status}'. Use 'No Children' if none.",
                                                                 "defaultValue": "No Children",
                                                                 "exampleValues": [
                                                                     "No Children - No live births",
                                                                     "1st: Elena, ♀️, with Marcus Halvard - In House Halvard nursery, mother permitted weekly visits. 8 months old, healthy.",
                                                                     "1st: Unnamed, ♂️, with Guard Captain Roth - Taken at birth, sold, no contact. 2nd: Lily, ♀️, with Lord Halvard - Kept as heir, mother sees occasionally. 3rd: Currently pregnant."
                                                                 ]
                                                             }
                                                         ]
                                                     },
                                                     {
                                                         "name": "Potential",
                                                         "type": "Object",
                                                         "prompt": "Assessment of the character's growth potential and current development focus.",
                                                         "defaultValue": null,
                                                         "exampleValues": null,
                                                         "nestedFields": [
                                                             {
                                                                 "name": "TalentSummary",
                                                                 "type": "String",
                                                                 "prompt": "Overview of character's natural aptitudes and limitations - quick reference for potential across domains. Summarize: areas of natural talent (learns faster, higher caps), average aptitude, and limited talent (learns slower, lower caps). Based on traits but presented accessibly.",
                                                                 "defaultValue": "Average baseline aptitude across all areas. No exceptional talents or limitations identified yet.",
                                                                 "exampleValues": [
                                                                     "TALENTED: Magic (all schools) - learns 50% faster, can reach Grandmaster. Social skills with appearance - natural advantage. AVERAGE: Physical combat, Survival, Mental. LIMITED: None identified.",
                                                                     "TALENTED: Physical combat, Athletics - strong body supports fast learning. Pain tolerance. AVERAGE: Social, Survival, most Mental. LIMITED: Magic - completely null. Academics - caps at Competent.",
                                                                     "TALENTED: Sexual/Service skills - naturally gifted, learns 75% faster. Submission - conditioning takes easily. AVERAGE: Social, basic Survival. LIMITED: Combat - frail, -50% XP, caps at Competent. Magic - null. Mental resistance - easily broken."
                                                                 ]
                                                             },
                                                             {
                                                                 "name": "CurrentTrainingFocus",
                                                                 "type": "String",
                                                                 "prompt": "What the character is currently actively training or developing - their growth priorities. Include: skills being deliberately practiced, abilities being learned, traits being developed or overcome, and barriers to development. This tracks the CHARACTER'S goals, which may differ from what's being forced on them.",
                                                                 "defaultValue": "No current training focus - reacting to circumstances rather than deliberately developing.",
                                                                 "exampleValues": [
                                                                     "ACTIVE TRAINING: Stealth (practicing during free movement), Mental Resistance (attempting to resist conditioning). FORCED DEVELOPMENT: Oral Service (daily 'practice'), Pain Endurance (regular punishment). DESIRED BUT BLOCKED: Swordsmanship (no weapons access), Magic (suppressed by collar).",
                                                                     "ACTIVE TRAINING: Fire Magic under Master Vorn (4 hours daily), working toward Expert and learning 'Flame Lance'. SECONDARY: Social etiquette. No barriers - favorable conditions.",
                                                                     "SURVIVAL FOCUS: No deliberate training possible. All development reactive - gaining Pain Endurance through suffering, Service skills through forced practice, losing Combat through atrophy. Goal is escape, not improvement."
                                                                 ]
                                                             }
                                                         ]
                                                     }
                                                 ]
                                             }
                                         ]
                                     }
                                     """;
}