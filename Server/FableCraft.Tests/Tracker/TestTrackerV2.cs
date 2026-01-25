namespace FableCraft.Tests.Tracker;

internal sealed class TestTrackerV2
{
    public const string Tracker = """
                                  {
                                    "Scene": [
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
                                        "name": "Name",
                                        "type": "String",
                                        "prompt": "The character's full identity as currently known. In Devoria, names accumulate meaning: birth name, sect name, bloodline epithet, earned titles, or slave designation. Format: 'Primary Name (Additional Names/Titles)'. For slaves, include birth name if known and any assigned designations. Update as character gains recognition, titles, or has identity stripped.",
                                        "defaultValue": "Unknown",
                                        "exampleValues": [
                                          "Lyra Ashford",
                                          "Kira Emberheart, 'The Scarlet Claw' (inner disciple title, Ember Throne)",
                                          "Slave designation: 'Seventeen' (birth name: Mira Vance, stripped upon enslavement to Silken Web)"
                                        ]
                                      },
                                      {
                                        "name": "Gender",
                                        "type": "String",
                                        "prompt": "Biological sex, potentially modified by fusion transformations. Some high-tier fusions can alter sexual characteristics. Note baseline sex and any fusion-induced changes. This determines relevant anatomical fields and breeding capacity.",
                                        "defaultValue": "Female ♀️",
                                        "exampleValues": [
                                          "Female ♀️",
                                          "Male ♂️",
                                          "Female ♀️ (hermaphroditic traits emerging from high-tier serpent fusion - developing hemipenes alongside vagina)"
                                        ]
                                      },
                                      {
                                        "name": "Age",
                                        "type": "Object",
                                        "prompt": "Age tracking accounting for cultivation's effect on lifespan and aging. Higher tiers age slower and live longer.",
                                        "defaultValue": null,
                                        "nestedFields": [
                                          {
                                            "name": "Actual",
                                            "type": "String",
                                            "prompt": "True chronological age in years.",
                                            "defaultValue": "19",
                                            "exampleValues": [
                                              "19",
                                              "147",
                                              "892"
                                            ]
                                          },
                                          {
                                            "name": "Apparent",
                                            "type": "String",
                                            "prompt": "How old they appear. Higher-tier cultivators age slowly. A Tier 6 at 200 might look 35. Include tier context.",
                                            "defaultValue": "19 (Tier 2 - aging normally)",
                                            "exampleValues": [
                                              "19 (Tier 2 - aging normally)",
                                              "Mid-20s apparent (actual 147, Tier 6 - aging ~1 year per 5)",
                                              "Appears 40s (actual 892, Tier 8 - aging has nearly stopped)"
                                            ]
                                          },
                                          {
                                            "name": "ExpectedLifespan",
                                            "type": "String",
                                            "prompt": "Projected maximum lifespan based on current tier. Tier 1: 60-80, Tier 2: ~120, Tier 3: ~150, Tier 4: ~200, Tier 5: ~300, Tier 6: ~500, Tier 7: ~1000, Tier 8: Millennia, Tier 9: Immortal.",
                                            "defaultValue": "~120 years (Tier 2)",
                                            "exampleValues": [
                                              "60-80 years (Tier 1 - mundane lifespan)",
                                              "~200 years (Tier 4)",
                                              "~1000 years (Tier 7)",
                                              "Immortal barring violence (Tier 9)"
                                            ]
                                          }
                                        ]
                                      },
                                      {
                                        "name": "GeneralBuild",
                                        "type": "String",
                                        "prompt": "Overall body type and physical presence - the big picture before zooming into specific parts. Include: height (specific measurement preferred), weight or build descriptor, body type (slender, athletic, curvy, heavyset), how weight is distributed, muscle tone, skin color/texture/temperature. This field sets the foundation; specific body parts are detailed in their own fields. Include racial physical traits if applicable.",
                                        "defaultValue": "Average height (1.65m), slender feminine build with soft curves. Fair smooth skin, warm to touch.",
                                        "exampleValues": [
                                          "Petite (1.52m, ~55 kg), delicate small-framed build with subtle curves. Minimal muscle tone, soft everywhere. Porcelain pale skin that shows marks easily, naturally cool to touch, goosebumps when cold or aroused.",
                                          "Tall (1.78m, ~68 kg), athletic Amazonian build with defined muscles visible under skin, strong shoulders, powerful thighs. Low body fat, firm rather than soft. Bronze sun-kissed skin, warm and slightly oiled from training, flushed with exertion heat.",
                                          "Short and stacked (1.50m, ~73 kg), exaggerated hourglass with weight concentrated in chest and hips. Thick soft thighs, soft belly with slight pooch, plush everywhere. Creamy pale skin, very warm and soft to touch, yields like bread dough when squeezed."
                                        ]
                                      },
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
                                        "name": "StateOfDress",
                                        "type": "String",
                                        "prompt": "SUMMARY field - overall assessment of how dressed/undressed and put-together the character appears. This is the quick-reference for current clothed status. Options: Pristine (perfect), Neat (properly dressed), Casual (relaxed but dressed), Disheveled (messy, askew), Partially Undressed (some clothing removed/displaced), Stripped (most clothing removed), Nude (nothing), Exposed (clothed but arranged for access). Include key details and where any removed clothing is located.",
                                        "defaultValue": "Neat - Properly dressed, clothes in place, presentable",
                                        "exampleValues": [
                                          "Pristine - Fully dressed, every piece in perfect position, clean and unwrinkled, appearance carefully maintained",
                                          "Disheveled - Still technically dressed but: blouse untucked and partially unbuttoned, skirt twisted and wrinkled, hair messy, clearly has been through something; clothing intact but disarrayed",
                                          "Nude - Completely naked, all clothing removed and folded on nearby chair. Wearing only collar (permanent). Body fully exposed."
                                        ]
                                      },
                                      {
                                        "name": "ActiveEffects",
                                        "type": "String",
                                        "prompt": "All active effects currently influencing the character - anything temporary that modifies their state beyond their natural baseline. Categories: PHYSICAL (restraint effects, injury effects, modifications), CHEMICAL (drugs, potions, poisons, aphrodisiacs), MAGICAL (spells, curses, enchantments, blessings), PSYCHOLOGICAL (temporary conditioning effects, triggers currently active, hypnotic suggestions, mental states). Format each as: 'Effect Name (Type) - Duration - Impact'. Duration can be: time remaining, 'Until removed', or 'Until condition met'. 'None' if no active effects. Note: PERMANENT effects belong in Development.Traits, not here.",
                                        "defaultValue": "None - No active effects, character at natural baseline",
                                        "exampleValues": [
                                          "None - No drugs, spells, or unusual effects active. MainCharacter functioning at natural baseline.",
                                          "Mild Aphrodisiac (Chemical) - ~3 hours remaining - Heightened arousal, increased genital sensitivity, mildly foggy thinking when aroused, easier to arouse; Bound arms (Physical) - Until released - Cannot use hands, limited mobility",
                                          "Heavy Aphrodisiac (Chemical) - 6 hours remaining - Uncontrollable arousal, constant wetness, can barely think past need; Orgasm Denial Curse (Magical) - Until dispelled - Cannot physically orgasm regardless of stimulation, edges painfully but release is blocked"
                                        ]
                                      },
                                      {
                                        "name": "Cultivation",
                                        "type": "Object",
                                        "prompt": "Core cultivation status - tier, capacity, advancement progress, and fusion readiness. The fundamental power system of Devoria.",
                                        "defaultValue": null,
                                        "nestedFields": [
                                          {
                                            "name": "Tier",
                                            "type": "Object",
                                            "prompt": "Current cultivation tier and stage within that tier.",
                                            "defaultValue": null,
                                            "nestedFields": [
                                              {
                                                "name": "Current",
                                                "type": "String",
                                                "prompt": "Cultivation tier (1-9) with title. Tier 1: Mundane, Tier 2: Awakened, Tier 3: Initiate, Tier 4: Adept, Tier 5: Master, Tier 6: Grandmaster, Tier 7: Archon, Tier 8: Exalted, Tier 9: Ascended.",
                                                "defaultValue": "Tier 1 - Mundane",
                                                "exampleValues": [
                                                  "Tier 1 - Mundane (no cultivation)",
                                                  "Tier 3 - Initiate",
                                                  "Tier 6 - Grandmaster",
                                                  "Tier 9 - Ascended"
                                                ]
                                              },
                                              {
                                                "name": "Stage",
                                                "type": "String",
                                                "prompt": "Progress within current tier: Early (0-25% capacity), Middle (25-60%), Late (60-85%), Peak (85-100%, ready for breakthrough). Include percentage estimate.",
                                                "defaultValue": "N/A (Mundane)",
                                                "exampleValues": [
                                                  "Early Stage (~15% capacity developed)",
                                                  "Middle Stage (~45% capacity)",
                                                  "Late Stage (~78% capacity)",
                                                  "Peak Stage (100% - at threshold, ready for fusion breakthrough)"
                                                ]
                                              },
                                              {
                                                "name": "TimeAtTier",
                                                "type": "String",
                                                "prompt": "How long since reaching current tier. Context for advancement speed.",
                                                "defaultValue": "N/A",
                                                "exampleValues": [
                                                  "3 months (recently broke through)",
                                                  "4 years (steady progress)",
                                                  "47 years (stuck at bottleneck)"
                                                ]
                                              }
                                            ]
                                          },
                                          {
                                            "name": "Capacity",
                                            "type": "Object",
                                            "prompt": "Body and Soul capacity - the twin foundations that must both reach threshold for breakthrough. Uses Capacity Training Points (CTP) system where each capacity point requires accumulating CTP through training.",
                                            "defaultValue": null,
                                            "nestedFields": [
                                              {
                                                "name": "BodyCapacity",
                                                "type": "Object",
                                                "prompt": "Physical vessel's ability to channel mana. Trained through: physical conditioning, mana circulation, tempering, body cultivation pills.",
                                                "defaultValue": null,
                                                "nestedFields": [
                                                  {
                                                    "name": "Current",
                                                    "type": "Number",
                                                    "prompt": "Current capacity points (0-100). Each point represents progress toward threshold.",
                                                    "defaultValue": "0",
                                                    "exampleValues": ["0", "45", "67", "100"]
                                                  },
                                                  {
                                                    "name": "Threshold",
                                                    "type": "Number",
                                                    "prompt": "Capacity threshold required for breakthrough. Always 100.",
                                                    "defaultValue": "100",
                                                    "exampleValues": ["100"]
                                                  },
                                                  {
                                                    "name": "CTP",
                                                    "type": "Object",
                                                    "prompt": "Capacity Training Points tracking - the XP-like system for capacity advancement.",
                                                    "defaultValue": null,
                                                    "nestedFields": [
                                                      {
                                                        "name": "Current",
                                                        "type": "Number",
                                                        "prompt": "Current CTP accumulated toward next capacity point.",
                                                        "defaultValue": "0",
                                                        "exampleValues": [0, 15, 42, 78]
                                                      },
                                                      {
                                                        "name": "PerPoint",
                                                        "type": "Number",
                                                        "prompt": "CTP required per capacity point. Scales with tier: Tier 2→3: 2.5, Tier 3→4: 5, Tier 4→5: 10, Tier 5→6: 25, Tier 6→7: 50, Tier 7→8: 150, Tier 8→9: 500.",
                                                        "defaultValue": "1",
                                                        "exampleValues": ["1", "2.5", "5", "10", "25", "50", "150", "500"]
                                                      },
                                                      {
                                                        "name": "ToNextPoint",
                                                        "type": "Number",
                                                        "prompt": "CTP remaining until next capacity point is gained.",
                                                        "defaultValue": "1",
                                                        "exampleValues": [1, 3, 8, 22]
                                                      }
                                                    ]
                                                  },
                                                  {
                                                    "name": "TrainingLog",
                                                    "type": "String",
                                                    "prompt": "Recent training sessions and CTP gains. Format: 'Session (method) - Base CTP x Multipliers = Final CTP'. Track last 3-5 significant sessions.",
                                                    "defaultValue": "No training logged yet.",
                                                    "exampleValues": [
                                                      "No training logged yet.",
                                                      "Day 47: Body tempering (quality) - 20 x 1.25 (mana-rich) = 25 CTP | Day 45: Mana circulation - 8 x 1.0 = 8 CTP",
                                                      "Day 102: Intensive tempering - 35 x 1.25 (matching bloodline) x 1.15 (overseen) = 50 CTP | Day 100: Body cultivation pill (quality) - 30 CTP"
                                                    ]
                                                  }
                                                ]
                                              },
                                              {
                                                "name": "SoulCapacity",
                                                "type": "Object",
                                                "prompt": "Spiritual core's ability to hold and regenerate mana. Trained through: meditation, visualization, mental challenges, soul pills, communion with powerful beings.",
                                                "defaultValue": null,
                                                "nestedFields": [
                                                  {
                                                    "name": "Current",
                                                    "type": "Number",
                                                    "prompt": "Current capacity points (0-100). Each point represents progress toward threshold.",
                                                    "defaultValue": "0",
                                                    "exampleValues": ["0", "45", "72", "100"]
                                                  },
                                                  {
                                                    "name": "Threshold",
                                                    "type": "Number",
                                                    "prompt": "Capacity threshold required for breakthrough. Always 100.",
                                                    "defaultValue": "100",
                                                    "exampleValues": ["100"]
                                                  },
                                                  {
                                                    "name": "CTP",
                                                    "type": "Object",
                                                    "prompt": "Capacity Training Points tracking - the XP-like system for capacity advancement.",
                                                    "defaultValue": null,
                                                    "nestedFields": [
                                                      {
                                                        "name": "Current",
                                                        "type": "Number",
                                                        "prompt": "Current CTP accumulated toward next capacity point.",
                                                        "defaultValue": "0",
                                                        "exampleValues": ["0", "12", "38", "95"]
                                                      },
                                                      {
                                                        "name": "PerPoint",
                                                        "type": "Number",
                                                        "prompt": "CTP required per capacity point. Scales with tier: Tier 2→3: 2.5, Tier 3→4: 5, Tier 4→5: 10, Tier 5→6: 25, Tier 6→7: 50, Tier 7→8: 150, Tier 8→9: 500.",
                                                        "defaultValue": "1",
                                                        "exampleValues": ["1", "2.5", "5", "10", "25", "50", "150", "500"]
                                                      },
                                                      {
                                                        "name": "ToNextPoint",
                                                        "type": "Number",
                                                        "prompt": "CTP remaining until next capacity point is gained.",
                                                        "defaultValue": "1",
                                                        "exampleValues": ["1", "4", "12", "45"]
                                                      }
                                                    ]
                                                  },
                                                  {
                                                    "name": "TrainingLog",
                                                    "type": "String",
                                                    "prompt": "Recent training sessions and CTP gains. Format: 'Session (method) - Base CTP x Multipliers = Final CTP'. Track last 3-5 significant sessions.",
                                                    "defaultValue": "No training logged yet.",
                                                    "exampleValues": [
                                                      "No training logged yet.",
                                                      "Day 47: Deep meditation (quality) - 12 x 1.25 (resonant location) = 15 CTP | Day 44: Basic meditation - 5 x 1.0 = 5 CTP",
                                                      "Day 102: Soul communion (with treasure) - 20 x 1.20 (guided) = 24 CTP | Day 99: Soul cultivation pill - 15 CTP"
                                                    ]
                                                  }
                                                ]
                                              },
                                              {
                                                "name": "Balance",
                                                "type": "String",
                                                "prompt": "Assessment of body/soul development balance. Imbalance creates problems: Body-heavy (20+ ahead): Soul strains channeling power. Soul-heavy (20+ ahead): Mana damages vessel. Severe (35+): Dangerous. Critical (50+): Life-threatening.",
                                                "defaultValue": "N/A",
                                                "exampleValues": [
                                                  "Balanced - Body and soul developing evenly (within 15 points)",
                                                  "Slightly Body-heavy (Body 78, Soul 65) - Minor imbalance, prioritize soul training",
                                                  "Soul-heavy (Body 45, Soul 89) - Dangerous imbalance, mana strains body, needs tempering urgently",
                                                  "Critical imbalance (Body 30, Soul 85) - Life-threatening, body rejecting mana flow"
                                                ]
                                              },
                                              {
                                                "name": "ThresholdStatus",
                                                "type": "String",
                                                "prompt": "Overall readiness for next breakthrough. Requires BOTH capacities at 100.",
                                                "defaultValue": "Not at threshold",
                                                "exampleValues": [
                                                  "Not at threshold - Still developing (Body 67%, Soul 72%)",
                                                  "Approaching threshold - Close (Body 94%, Soul 97%)",
                                                  "AT THRESHOLD - Ready for fusion breakthrough, both capacities maxed",
                                                  "Stuck at threshold - Maxed for 3 years, cannot find appropriate fusion creature"
                                                ]
                                              }
                                            ]
                                          },
                                          {
                                            "name": "TalentAssessment",
                                            "type": "Object",
                                            "prompt": "Evaluation of cultivation potential and natural limits.",
                                            "defaultValue": null,
                                            "nestedFields": [
                                              {
                                                "name": "NaturalTalent",
                                                "type": "String",
                                                "prompt": "Innate cultivation aptitude. Affects CTP gain multiplier and potential ceiling. Most people are Average. Exceptional talent is rare.",
                                                "defaultValue": "Average",
                                                "exampleValues": [
                                                  "Below Average - x0.7-0.8 CTP gain, likely low ceiling",
                                                  "Average - x1.0 CTP gain, standard advancement with proper resources",
                                                  "Above Average - x1.15-1.25 CTP gain, noticeably faster progress",
                                                  "Exceptional - x1.3-1.5 CTP gain, rapid advancement, attracts sect interest",
                                                  "Prodigious - x1.5-2.0 CTP gain, one-in-a-generation talent"
                                                ]
                                              },
                                              {
                                                "name": "EstimatedCeiling",
                                                "type": "String",
                                                "prompt": "Best assessment of maximum achievable tier based on talent, resources, and current progress. This can be wrong - ceilings are discovered by hitting them.",
                                                "defaultValue": "Unknown",
                                                "exampleValues": [
                                                  "Tier 3-4 (Limited talent, likely plateaus as Adept)",
                                                  "Tier 5-6 (Good talent, could reach Grandmaster with resources)",
                                                  "Tier 7+ (Exceptional, true ceiling unknown, Archon achievable)",
                                                  "Unknown (Too early to assess accurately)"
                                                ]
                                              },
                                              {
                                                "name": "SpecialFactors",
                                                "type": "String",
                                                "prompt": "Anything unusual affecting cultivation potential - inherited bloodline echoes, blessings, curses, damaged foundation, etc. Include any CTP multiplier effects.",
                                                "defaultValue": "None",
                                                "exampleValues": [
                                                  "None - Standard cultivation potential",
                                                  "Bloodline echoes: Parents both cat-blooded Tier 4, born with latent affinity (feline fusions +25% success, feline-related CTP x1.25)",
                                                  "Damaged foundation: Survived failed fusion at Tier 3, cultivation scarred, CTP gains x0.8 permanently",
                                                  "Phoenix blessing: Received blessing age 12, fire-related CTP x1.3, grants fire resistance"
                                                ]
                                              }
                                            ]
                                          }
                                        ]
                                      },
                                      {
                                        "name": "MagicAndAbilities",
                                        "type": "Object",
                                        "prompt": "Magical capabilities - mana pool, affinities, instinctive abilities from fusion, and learned techniques.",
                                        "defaultValue": null,
                                        "nestedFields": [
                                          {
                                            "name": "ManaPool",
                                            "type": "Object",
                                            "prompt": "Magical energy reservoir - capacity and current state.",
                                            "defaultValue": null,
                                            "nestedFields": [
                                              {
                                                "name": "Maximum",
                                                "type": "Number",
                                                "prompt": "Maximum mana capacity. Scales dramatically with tier - not linear but qualitative jumps. Tier 1: 0, Tier 2: ~50, Tier 3: ~150, Tier 4: ~400, Tier 5: ~1000, Tier 6: ~3000, Tier 7: ~10000, Tier 8: ~50000, Tier 9: Effectively unlimited.",
                                                "defaultValue": "0",
                                                "exampleValues": [0, 50, 150, 400, 1000, 3000, 10000]
                                              },
                                              {
                                                "name": "Current",
                                                "type": "Number",
                                                "prompt": "Current mana available.",
                                                "defaultValue": "0",
                                                "exampleValues": [0, 45, 180, 380]
                                              },
                                              {
                                                "name": "RegenerationRate",
                                                "type": "String",
                                                "prompt": "How fast mana regenerates under different conditions. Base: ~10% max/hour resting. Modified by: activity level, environment, meditation, pills.",
                                                "defaultValue": "N/A",
                                                "exampleValues": [
                                                  "N/A (No mana)",
                                                  "~5/hour resting, ~2/hour active, ~10/hour meditating (Tier 2)",
                                                  "~40/hour resting, ~15/hour active, ~80/hour meditating, doubled in sect cultivation chamber (Tier 4)"
                                                ]
                                              },
                                              {
                                                "name": "ExhaustionState",
                                                "type": "String",
                                                "prompt": "Current exhaustion effects from mana depletion. Thresholds: Above 25%: No effect. Below 25%: Minor strain (+25% spell costs, mild headache). Below 10%: Significant strain (+50% costs, -1 tier effective Magic, miscast risk). At 0%: Collapse (can't cast, -2 tiers Mental, severe symptoms).",
                                                "defaultValue": "None",
                                                "exampleValues": [
                                                  "None - Mana above 25%, no exhaustion effects",
                                                  "None - Non-cultivator, no mana to exhaust",
                                                  "Minor strain (at 18%) - Slight headache, spells feel more taxing, +25% costs",
                                                  "Significant strain (at 7%) - Pounding headache, can barely concentrate, complex magic impossible, miscast risk",
                                                  "CRITICAL (at 0%) - Cannot cast anything, migraine, body shutting down, near collapse"
                                                ]
                                              }
                                            ]
                                          },
                                          {
                                            "name": "Affinities",
                                            "type": "Object",
                                            "prompt": "Magical affinities - types of magic that come easily or with difficulty. Shaped by fusion and practice. Affects XP multipliers for related techniques.",
                                            "defaultValue": null,
                                            "nestedFields": [
                                              {
                                                "name": "Strong",
                                                "type": "String",
                                                "prompt": "Magic types that come naturally - reduced mana cost, better effects, easier learning. +25% XP for related techniques. Usually from fusion.",
                                                "defaultValue": "None",
                                                "exampleValues": [
                                                  "None - No strong affinities",
                                                  "Fire (draconic fusion) +25% XP, Shadow (natural talent) +25% XP",
                                                  "Stealth, Illusion (vulpine bloodline grants both) +25% XP each",
                                                  "Lightning, Fear effects (storm dragon lineage) +25% XP each"
                                                ]
                                              },
                                              {
                                                "name": "Moderate",
                                                "type": "String",
                                                "prompt": "Magic types with neither bonus nor penalty - standard XP gain and effectiveness.",
                                                "defaultValue": "Standard",
                                                "exampleValues": [
                                                  "Most magic types (no fusion modifiers)",
                                                  "Body Enhancement, Basic Elemental (training offsets no natural affinity)",
                                                  "Mental Arts (some natural talent, no fusion support)"
                                                ]
                                              },
                                              {
                                                "name": "Weak",
                                                "type": "String",
                                                "prompt": "Magic types that are difficult - increased cost, weaker effects, -25% XP gain. Often opposing elements.",
                                                "defaultValue": "None",
                                                "exampleValues": [
                                                  "None - No particular weaknesses",
                                                  "Water, Ice (opposing fire affinity) -25% XP",
                                                  "Light magic (shadow affinity conflicts) -25% XP",
                                                  "Healing (draconic bloodlines are poor healers) -25% XP"
                                                ]
                                              },
                                              {
                                                "name": "Null",
                                                "type": "String",
                                                "prompt": "Magic types completely inaccessible - cannot learn or use regardless of effort. No XP can be gained. Rare but significant.",
                                                "defaultValue": "None",
                                                "exampleValues": [
                                                  "None - Can theoretically learn any magic",
                                                  "ALL (Magically null trait - cannot use any magic)",
                                                  "Holy/Sacred magic (demonic bloodline incompatible)",
                                                  "Necromancy (phoenix blessing actively prevents)"
                                                ]
                                              }
                                            ]
                                          },
                                          {
                                            "name": "InstinctiveAbilities",
                                            "type": "ForEachObject",
                                            "prompt": "Abilities that came naturally from fusion - used instinctively like breathing, not learned. Still progress through proficiency levels with use.",
                                            "defaultValue": null,
                                            "nestedFields": [
                                              {
                                                "name": "AbilityName",
                                                "type": "String",
                                                "prompt": "Name of the instinctive ability.",
                                                "defaultValue": "Unnamed Ability",
                                                "exampleValues": [
                                                  "Night Vision",
                                                  "Fire Breath",
                                                  "Venom Production",
                                                  "Pack Bond Sense"
                                                ]
                                              },
                                              {
                                                "name": "Source",
                                                "type": "String",
                                                "prompt": "Which fusion granted this ability.",
                                                "defaultValue": "Unknown",
                                                "exampleValues": [
                                                  "Tier 2 Shadow Cat fusion",
                                                  "Tier 4 Fire Drake fusion",
                                                  "Tier 3 Viper fusion",
                                                  "Tier 2 Wolf fusion"
                                                ]
                                              },
                                              {
                                                "name": "Description",
                                                "type": "String",
                                                "prompt": "What the ability does - effect, range, limitations, scaling with proficiency.",
                                                "defaultValue": "Effect not defined",
                                                "exampleValues": [
                                                  "Can see clearly in near-total darkness. Eyes reflect light. Passive, always active, no mana cost. Higher proficiency extends range and detail in minimal light.",
                                                  "Can exhale a cone of fire 15ft long. Deals significant burn damage. Costs ~30 mana, 3-second cooldown while throat recovers. Natural resistance to own flames. Proficiency increases range, damage, and reduces cooldown.",
                                                  "Produce paralytic venom in fangs. Injection causes progressive paralysis over ~30 seconds. Can consciously control whether to inject when biting. Venom replenishes ~1 dose per 4 hours. Higher proficiency = faster paralysis, more potent venom."
                                                ]
                                              },
                                              {
                                                "name": "Proficiency",
                                                "type": "String",
                                                "prompt": "Current proficiency level. Levels: Untrained (0), Novice (50), Amateur (150), Competent (400), Proficient (900), Expert (1900), Master (4400), Grandmaster (9400). Instinctive abilities typically start at Novice when first gained from fusion.",
                                                "defaultValue": "Novice",
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
                                                "name": "XP",
                                                "type": "Object",
                                                "prompt": "Experience tracking toward next proficiency level.",
                                                "defaultValue": null,
                                                "nestedFields": [
                                                  {
                                                    "name": "Current",
                                                    "type": "Number",
                                                    "prompt": "Total accumulated XP. Thresholds: Novice(50), Amateur(150), Competent(400), Proficient(900), Expert(1900), Master(4400), Grandmaster(9400).",
                                                    "defaultValue": 50,
                                                    "exampleValues": [0, 50, 127, 523, 1850, 5200, 11240]
                                                  },
                                                  {
                                                    "name": "NextThreshold",
                                                    "type": "Number",
                                                    "prompt": "XP threshold for next proficiency level. 50→150→400→900→1900→4400→9400→MAX.",
                                                    "defaultValue": 150,
                                                    "exampleValues": [50, 150, 400, 900, 1900, 4400, 9400, -1]
                                                  },
                                                  {
                                                    "name": "ToNext",
                                                    "type": "Number",
                                                    "prompt": "XP remaining until next proficiency level. -1 if at Grandmaster (max level).",
                                                    "defaultValue": "100",
                                                    "exampleValues": [50, 23, 377, 50, 2550, -1]
                                                  }
                                                ]
                                              },
                                              {
                                                "name": "ChallengeFloor",
                                                "type": "String",
                                                "prompt": "Minimum difficulty of tasks that grant meaningful XP - equals current Proficiency level. Tasks BELOW floor grant only 0-10% XP. Tasks AT floor grant 100% XP. Tasks ABOVE floor grant 150-200% XP.",
                                                "defaultValue": "Novice (basic use counts, need real application for full XP)",
                                                "exampleValues": [
                                                  "Untrained (any attempt grants full XP)",
                                                  "Novice (basic use grants minimal XP; need real application or difficult situations)",
                                                  "Competent (routine use grants minimal XP; need genuine challenges, dangerous situations, or superior opponents)",
                                                  "Master (standard difficult tasks grant minimal XP; only extreme challenges, innovation, or legendary feats grant meaningful XP)"
                                                ]
                                              },
                                              {
                                                "name": "Development",
                                                "type": "String",
                                                "prompt": "Narrative tracking of how this ability has developed - key uses, breakthrough moments, and recent progress.",
                                                "defaultValue": "Newly acquired from fusion, instinctive use only.",
                                                "exampleValues": [
                                                  "Newly acquired from fusion, instinctive use only. Learning control and limits.",
                                                  "Gained at Tier 3 breakthrough. Initial clumsy bursts now more controlled after 6 months practice. Recent: Used against Competent ice mage in combat (+28 XP, above floor, high stakes).",
                                                  "Years of refinement. Can modulate intensity precisely, extend duration, and combine with other abilities. Currently at Master level, seeking Grandmaster-level challenges."
                                                ]
                                              },
                                              {
                                                "name": "RecentGains",
                                                "type": "String",
                                                "prompt": "Last 3-5 significant XP gains with calculation breakdown. Format: 'Scene/Day: +X XP (Base x Challenge x Bonuses = Total) - Context'",
                                                "defaultValue": "None yet",
                                                "exampleValues": [
                                                  "None yet",
                                                  "Day 52: +28 XP (20 x 1.0 x 1.4 = 28) - Used in combat vs Competent opponent, high stakes",
                                                  "Scene 47: +2 XP (20 x 0.10 = 2) - Routine use, below floor | Scene 52: +35 XP (20 x 1.0 x 1.75 = 35) - Combat vs Expert, high stakes + dramatic"
                                                ]
                                              }
                                            ]
                                          },
                                          {
                                            "name": "LearnedTechniques",
                                            "type": "ForEachObject",
                                            "prompt": "Techniques learned through training - sect teachings, manuals, or developed personally. Progress through standard proficiency levels.",
                                            "defaultValue": null,
                                            "nestedFields": [
                                              {
                                                "name": "TechniqueName",
                                                "type": "String",
                                                "prompt": "Name of the technique.",
                                                "defaultValue": "Unnamed Technique",
                                                "exampleValues": [
                                                  "Ember Throne Basic Fire Manipulation",
                                                  "Shadow Step",
                                                  "Iron Body Cultivation",
                                                  "Silken Web Binding Technique"
                                                ]
                                              },
                                              {
                                                "name": "Type",
                                                "type": "String",
                                                "prompt": "Category: Combat, Utility, Movement, Defense, Cultivation, Support, Forbidden, Other.",
                                                "defaultValue": "Combat",
                                                "exampleValues": [
                                                  "Combat - Offensive fire magic",
                                                  "Movement - Short-range teleportation",
                                                  "Cultivation - Body tempering method",
                                                  "Utility - Silk creation and manipulation"
                                                ]
                                              },
                                              {
                                                "name": "School",
                                                "type": "String",
                                                "prompt": "Which magical school it belongs to: Elemental, Body Cultivation, Mental Arts, Spatial, Life, Bound Magic, Bloodline, or Hybrid.",
                                                "defaultValue": "Elemental",
                                                "exampleValues": [
                                                  "Elemental (Fire)",
                                                  "Spatial (limited)",
                                                  "Body Cultivation",
                                                  "Bloodline (Arachnid)"
                                                ]
                                              },
                                              {
                                                "name": "Source",
                                                "type": "String",
                                                "prompt": "Where/how the technique was learned.",
                                                "defaultValue": "Unknown",
                                                "exampleValues": [
                                                  "Ember Throne standard curriculum, outer disciple training",
                                                  "Personal development during combat situation",
                                                  "Stolen manual, self-taught (incomplete understanding)",
                                                  "Elder reward for service, personal instruction"
                                                ]
                                              },
                                              {
                                                "name": "Description",
                                                "type": "String",
                                                "prompt": "What the technique does - effect, requirements, costs. Note how it scales with proficiency.",
                                                "defaultValue": "Effect not defined",
                                                "exampleValues": [
                                                  "Basic control over fire - create flames in palm, throw small fireballs, enhance existing fires. Mana cost: 5-20 depending on scale. Requires fire affinity or much more mana. Higher proficiency = larger flames, more control, lower costs.",
                                                  "Step through shadows to reappear up to 30ft away. Requires shadows at both points. Mana cost: 25. 5-second cooldown. Disorients briefly after use. Proficiency increases range and reduces disorientation.",
                                                  "Circulate mana to strengthen body against damage. -30% damage from physical attacks while active. Mana cost: 5/minute sustained. Cannot cast other magic while maintaining. Higher proficiency = greater reduction, lower cost."
                                                ]
                                              },
                                              {
                                                "name": "Proficiency",
                                                "type": "String",
                                                "prompt": "Current proficiency level. Levels: Untrained (0), Novice (50), Amateur (150), Competent (400), Proficient (900), Expert (1900), Master (4400), Grandmaster (9400). Learned techniques typically start at Untrained.",
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
                                                "name": "XP",
                                                "type": "Object",
                                                "prompt": "Experience tracking toward next proficiency level.",
                                                "defaultValue": null,
                                                "nestedFields": [
                                                  {
                                                    "name": "Current",
                                                    "type": "Number",
                                                    "prompt": "Total accumulated XP. Thresholds: Novice(50), Amateur(150), Competent(400), Proficient(900), Expert(1900), Master(4400), Grandmaster(9400).",
                                                    "defaultValue": "0",
                                                    "exampleValues": [0, 35, 178, 523, 1850, 5200, 11240]
                                                  },
                                                  {
                                                    "name": "NextThreshold",
                                                    "type": "Number",
                                                    "prompt": "XP threshold for next proficiency level. 50→150→400→900→1900→4400→9400→MAX.",
                                                    "defaultValue": 50,
                                                    "exampleValues": [50, 150, 400, 900, 1900, 4400, 9400, -1]
                                                  },
                                                  {
                                                    "name": "ToNext",
                                                    "type": "Number",
                                                    "prompt": "XP remaining until next proficiency level. -1 if at Grandmaster (max level).",
                                                    "defaultValue": 50,
                                                    "exampleValues": [50, 15, 222, 377, 2550, -1]
                                                  }
                                                ]
                                              },
                                              {
                                                "name": "ChallengeFloor",
                                                "type": "String",
                                                "prompt": "Minimum difficulty of tasks that grant meaningful XP - equals current Proficiency level. Tasks BELOW floor grant only 0-10% XP. Tasks AT floor grant 100% XP. Tasks ABOVE floor grant 150-200% XP.",
                                                "defaultValue": "Untrained (any practice grants full XP)",
                                                "exampleValues": [
                                                  "Untrained (any practice grants full XP, everything is challenging)",
                                                  "Novice (basic exercises grant minimal XP; need real application or difficult drills)",
                                                  "Competent (routine professional tasks grant minimal XP; need genuine challenges, novel problems, or superior opponents)",
                                                  "Master (standard difficult tasks grant minimal XP; only extreme challenges, innovation, teaching masters, or legendary feats grant meaningful XP)"
                                                ]
                                              },
                                              {
                                                "name": "Development",
                                                "type": "String",
                                                "prompt": "Narrative tracking of how this technique has developed - training history, key learning moments, teachers, and recent progress.",
                                                "defaultValue": "Newly learned technique, no development history yet.",
                                                "exampleValues": [
                                                  "Newly learned technique, no development history yet.",
                                                  "Learned from sect manual 3 months ago. Practiced daily under instructor supervision. Recently achieved Competent after breakthrough in live combat situation.",
                                                  "Personal development over 2 years of experimentation. No formal teaching but developed unique variations. At Expert level, now teaching others."
                                                ]
                                              },
                                              {
                                                "name": "RecentGains",
                                                "type": "String",
                                                "prompt": "Last 3-5 significant XP gains with calculation breakdown. Format: 'Scene/Day: +X XP (Base x Challenge x Bonuses = Total) - Context'",
                                                "defaultValue": "None yet",
                                                "exampleValues": [
                                                  "None yet",
                                                  "Day 45: +20 XP (20 x 1.0 = 20) - Standard training session at level",
                                                  "Scene 52: +56 XP (25 x 1.5 x 1.5 = 56) - Combat vs Expert, above floor, high stakes + dramatic moment"
                                                ]
                                              }
                                            ]
                                          }
                                        ]
                                      },
                                      {
                                        "name": "BloodlineInstincts",
                                        "type": "Object",
                                        "prompt": "Psychological and behavioral changes from bloodline - instincts, urges, and compulsions that came with fusion.",
                                        "defaultValue": null,
                                        "nestedFields": [
                                          {
                                            "name": "CoreInstincts",
                                            "type": "String",
                                            "prompt": "Primary instinctual drives from bloodline. These are COMPULSIONS that affect behavior, not just preferences.",
                                            "defaultValue": "None (Human baseline)",
                                            "exampleValues": [
                                              "None - Human psychology, no fusion-driven instincts",
                                              "Feline: Strong independence drive (resists authority instinctively), hunting/stalking urge (finds chasing things satisfying), territorial about personal space and possessions, grooming/cleanliness compulsion",
                                              "Canine: Pack bonding drive (intense loyalty to chosen 'pack'), hierarchy awareness (constantly assessing dominance), territorial, protective of pack members",
                                              "Draconic: Hoarding compulsion (collects valuable things, including people, feels anxiety when hoard is threatened), pride/dominance (difficulty submitting or admitting fault), possessiveness (views partners/treasures as 'mine')"
                                            ]
                                          },
                                          {
                                            "name": "HeatCycle",
                                            "type": "Object",
                                            "prompt": "Reproductive instinct cycle if applicable. Many beast bloodlines have heat cycles affecting arousal and fertility. Leave as N/A if bloodline doesn't have heats.",
                                            "defaultValue": null,
                                            "nestedFields": [
                                              {
                                                "name": "HasCycle",
                                                "type": "String",
                                                "prompt": "Whether this bloodline experiences heat cycles. Common in: Canine, Feline, Bovine, Equine, Lapine. Absent in: Draconic, Serpentine (different fertility pattern), Arachnid, Cephalopod.",
                                                "defaultValue": "No",
                                                "exampleValues": [
                                                  "No - Human baseline or bloodline without heat cycles",
                                                  "Yes - Canine heat cycle",
                                                  "Yes - Feline heat cycle",
                                                  "No - Draconic bloodlines don't have heats but have hoarding instinct for mates"
                                                ]
                                              },
                                              {
                                                "name": "CycleLength",
                                                "type": "String",
                                                "prompt": "How long between heats and how long heat lasts. Varies by bloodline.",
                                                "defaultValue": "N/A",
                                                "exampleValues": [
                                                  "N/A - No heat cycle",
                                                  "Every 3 months, heat lasts 5-7 days (Canine typical)",
                                                  "Every 2-3 weeks, heat lasts 3-5 days (Feline typical)",
                                                  "Monthly, heat lasts 2-3 days (Lapine - very frequent)"
                                                ]
                                              },
                                              {
                                                "name": "CurrentPhase",
                                                "type": "String",
                                                "prompt": "Current position in heat cycle. Format: 'Phase: [Normal/Pre-Heat/In Heat/Post-Heat] - Details'",
                                                "defaultValue": "N/A",
                                                "exampleValues": [
                                                  "N/A - No heat cycle",
                                                  "Normal - Between heats, ~6 weeks until next",
                                                  "Pre-Heat (2 days out) - Arousal increasing, becoming restless, scent changing",
                                                  "IN HEAT (Day 3 of 5) - Severely affected: constant arousal, difficulty concentrating on anything non-sexual, fertility extremely high, will seek mating aggressively",
                                                  "Post-Heat (1 day) - Heat ending, arousal returning to normal, exhausted"
                                                ]
                                              },
                                              {
                                                "name": "HeatSymptoms",
                                                "type": "String",
                                                "prompt": "How heat manifests - physical and psychological symptoms during heat.",
                                                "defaultValue": "N/A",
                                                "exampleValues": [
                                                  "N/A - No heat cycle",
                                                  "Canine heat: Intense arousal spike, vaginal swelling, strong scent changes (musk detectable by other cultivators), overwhelming urge to be mounted/bred, difficulty refusing advances, heightened emotional state, nesting behavior",
                                                  "Feline heat: Constant arousal, 'presenting' behavior (unconsciously arching back, raising hips), yowling urges (vocalizes more), rolling/rubbing against surfaces, will approach anyone even slightly attractive, extremely fertile",
                                                  "Lapine heat: Nearly constant, very short cycle, arousal becomes unbearable without relief, will mate with almost anyone, extreme fertility, multiple orgasms easily triggered"
                                                ]
                                              },
                                              {
                                                "name": "HeatManagement",
                                                "type": "String",
                                                "prompt": "How heat is currently being managed - suppressed, indulged, partner arrangements, etc.",
                                                "defaultValue": "N/A",
                                                "exampleValues": [
                                                  "N/A - No heat cycle",
                                                  "Unmanaged - No suppressants or arrangements, must endure or seek relief independently",
                                                  "Sect-managed - Pack Covenant assigns partners during heat, scheduled matings with designated disciples",
                                                  "Suppressed - Taking heat suppression pills (expensive, uncomfortable side effects, can't use long-term)",
                                                  "Owner-controlled - Master decides when and if she gets relief during heat, often used as control mechanism"
                                                ]
                                              }
                                            ]
                                          },
                                          {
                                            "name": "BondingInstinct",
                                            "type": "String",
                                            "prompt": "Bloodline-driven bonding patterns - how the bloodline affects attachment and loyalty. Many bloodlines form bonds differently than humans.",
                                            "defaultValue": "Human baseline - Normal human attachment patterns",
                                            "exampleValues": [
                                              "Human baseline - Standard human attachment, no instinct-driven bonds",
                                              "Canine Pack-Bond - Forms intense loyalty to chosen 'pack' (can be chosen or forced), pack bonds feel like family even if not related, instinctively prioritizes pack safety, experiences distress when separated from pack",
                                              "Feline Independent - Resists permanent bonds, prefers transactional relationships, can form deep attachments but they feel like choice not compulsion",
                                              "Draconic Hoarding - Views partners as possessions in hoard, intense possessiveness, genuinely experiences anxiety when 'hoard members' are threatened or interact with others, bonds are permanent unless deliberately broken",
                                              "Demonic Feeding Bond - Forms connection with regular feeding partners, not emotional but magical/physical dependency, can sense regular partners"
                                            ]
                                          },
                                          {
                                            "name": "SexualInstincts",
                                            "type": "String",
                                            "prompt": "Bloodline-specific sexual drives and behaviors beyond basic arousal. Many bloodlines have distinctive mating patterns.",
                                            "defaultValue": "Human baseline",
                                            "exampleValues": [
                                              "Human baseline - No bloodline-driven sexual instincts",
                                              "Canine: Knotting urge (during climax, desire to 'tie' with partner and remain connected), mating displays (presenting, posturing), scent-marking partners",
                                              "Feline: Play before mating (toying, chasing), multiple partners acceptable (no pair-bonding instinct), loud vocalizations during sex, nape-biting triggers submission",
                                              "Draconic: Display dominance during sex, extended mating (stamina far beyond human), afterglow hoarding behavior (wants partner close after), can't share partners easily",
                                              "Serpentine: Constriction during mating (wraps around partner), tongue-based stimulation instinct, temperature-seeking (drawn to warmth), can be sexually aggressive predators",
                                              "Demonic: Feeding through sex (gains energy/power from partner's pleasure), addictive touch (produces chemicals that create dependency), instinctive seduction"
                                            ]
                                          }
                                        ]
                                      },
                                      {
                                        "name": "CurrentState",
                                        "type": "Object",
                                        "prompt": "All temporary and frequently changing aspects - immediate physical condition, needs, appearance, and ongoing effects. Updated constantly during scenes.",
                                        "defaultValue": null,
                                        "nestedFields": [
                                          {
                                            "name": "Vitals",
                                            "type": "Object",
                                            "prompt": "Core physical and mental condition - health, pain, energy, and psychological state.",
                                            "defaultValue": null,
                                            "nestedFields": [
                                              {
                                                "name": "Health",
                                                "type": "String",
                                                "prompt": "Physical health status - injuries, wounds, illness. Higher-tier cultivators are harder to injure and heal faster. Separate from fatigue and pain.",
                                                "defaultValue": "Healthy - No injuries or illness",
                                                "exampleValues": [
                                                  "Healthy - No injuries, body in good condition",
                                                  "Minor Injuries - Bruising on thighs, small cut on lip (healing rapidly due to Tier 4 constitution)",
                                                  "Moderate Injuries - Deep claw marks across back (Tier 5 opponent), cracked ribs, significant blood loss - healing at enhanced rate but needs rest",
                                                  "Severe Injuries - Life-threatening to mundane, recovering due to Tier 6 regeneration - internal damage healing, external wounds closing"
                                                ]
                                              },
                                              {
                                                "name": "Pain",
                                                "type": "String",
                                                "prompt": "Current pain level 0-10 with description. Higher-tier cultivators have better pain tolerance but still feel it. Include source.",
                                                "defaultValue": "0/10 - No pain",
                                                "exampleValues": [
                                                  "0/10 - No pain, comfortable",
                                                  "3/10 - Mild ache from yesterday's training, easily ignored",
                                                  "6/10 - Significant pain from fresh whip marks, affecting concentration but manageable with Tier 3 tolerance",
                                                  "9/10 - Severe pain from ongoing torture, even with trained pain tolerance struggling to function"
                                                ]
                                              },
                                              {
                                                "name": "Fatigue",
                                                "type": "String",
                                                "prompt": "Energy level 0-10. 0=Fully rested, 10=Collapse. Higher tiers have better stamina but can still tire. Include cause and symptoms.",
                                                "defaultValue": "0/10 (Fully rested)",
                                                "exampleValues": [
                                                  "0/10 - Fully rested after good sleep, energized",
                                                  "4/10 - Moderate fatigue from full day of training, functioning fine",
                                                  "7/10 - Exhausted from extended combat, muscles heavy, reaction time slowed, needs rest soon",
                                                  "10/10 - Complete exhaustion, body shutting down, will collapse if pushed further"
                                                ]
                                              },
                                              {
                                                "name": "Mental",
                                                "type": "String",
                                                "prompt": "Psychological state - emotional condition, mental clarity, stress level, any altered states.",
                                                "defaultValue": "Clear and stable",
                                                "exampleValues": [
                                                  "Clear and stable - Alert, calm, thinking clearly",
                                                  "Anxious - Worried about upcoming fusion ritual, slightly scattered thoughts but functional",
                                                  "Arousal fog - Heat cycle day 2, difficulty thinking about anything non-sexual, making poor decisions",
                                                  "Broken subspace - Deep in submission after extended scene, nonverbal, completely passive, needs aftercare"
                                                ]
                                              }
                                            ]
                                          },
                                          {
                                            "name": "Needs",
                                            "type": "Object",
                                            "prompt": "Physical and physiological needs - arousal, hunger, thirst, bladder, heat status if applicable.",
                                            "defaultValue": null,
                                            "nestedFields": [
                                              {
                                                "name": "Arousal",
                                                "type": "String",
                                                "prompt": "Sexual arousal level 0-10 with MANDATORY physical details. Include: genital response (swelling, wetness/hardness), nipple state, flushing, breathing, additional signs for transformed anatomy (tail behavior, ear position, etc.).",
                                                "defaultValue": "0/10 (Dormant) - No arousal",
                                                "exampleValues": [
                                                  "0/10 (Dormant) - No arousal, genitals at rest, tail relaxed, ears neutral",
                                                  "5/10 (Aroused) - Clit swelling, noticeably wet, nipples hardening, ears rotating forward with interest, tail swaying slowly",
                                                  "8/10 (Highly Aroused) - Very wet (can feel it on thighs), clit throbbing visibly, nipples aching and erect, breathing heavy, tail lashing, ears flat back with intensity, struggling to focus",
                                                  "10/10 (Overwhelmed/Heat-Driven) - Soaking wet and dripping, entire genital area swollen and flushed, nipples painfully sensitive, panting, trembling, tail rigid then thrashing, ears pinned back, IN HEAT - cannot think past the need to be fucked and bred"
                                                ]
                                              },
                                              {
                                                "name": "Hunger",
                                                "type": "String",
                                                "prompt": "Food need 0-10 with time context. Higher tiers need less food but still need some.",
                                                "defaultValue": "2/10 (Satisfied)",
                                                "exampleValues": [
                                                  "1/10 - Ate recently, no hunger",
                                                  "5/10 - Last meal 10 hours ago, stomach growling, would like food",
                                                  "8/10 - No food for 2 days, significant weakness, lightheaded"
                                                ]
                                              },
                                              {
                                                "name": "Thirst",
                                                "type": "String",
                                                "prompt": "Hydration need 0-10 with time context.",
                                                "defaultValue": "1/10 (Hydrated)",
                                                "exampleValues": [
                                                  "0/10 - Just drank, fully hydrated",
                                                  "5/10 - Several hours since water, throat dry",
                                                  "8/10 - Dehydrated, headache, lips cracked"
                                                ]
                                              },
                                              {
                                                "name": "Bladder",
                                                "type": "String",
                                                "prompt": "Bladder pressure 0-10. Include urgency and physical signs.",
                                                "defaultValue": "1/10 (Empty)",
                                                "exampleValues": [
                                                  "1/10 - Recently relieved, no pressure",
                                                  "5/10 - Noticeable fullness, would use bathroom if available",
                                                  "9/10 - Desperate, physically uncomfortable, struggling to hold, occasional leakage"
                                                ]
                                              },
                                              {
                                                "name": "Bowel",
                                                "type": "String",
                                                "prompt": "Bowel pressure 0-10. Include urgency and physical signs.",
                                                "defaultValue": "1/10 (Empty)",
                                                "exampleValues": [
                                                  "1/10 - No pressure",
                                                  "5/10 - Awareness of need, not urgent",
                                                  "8/10 - Strong cramps, needs to go soon"
                                                ]
                                              },
                                              {
                                                "name": "HeatNeed",
                                                "type": "String",
                                                "prompt": "For characters with heat cycles - the specific breeding/mating urge level during heat. Separate from regular arousal. 'N/A' if not in heat or no heat cycle.",
                                                "defaultValue": "N/A",
                                                "exampleValues": [
                                                  "N/A - Not in heat / No heat cycle",
                                                  "Low - Heat starting, elevated urge but controllable",
                                                  "Moderate - Heat peak approaching, constant intrusive thoughts about mating, scent attracting attention",
                                                  "Severe - Deep heat, physically painful to not be bred, will present to almost anyone, rational thought severely impaired",
                                                  "Desperate - Extended heat without relief, will do anything to get fucked, begging and presenting constantly"
                                                ]
                                              }
                                            ]
                                          },
                                          {
                                            "name": "InternalState",
                                            "type": "Object",
                                            "prompt": "Internal body state - what's inside body cavities, any internal pressures or fullness.",
                                            "defaultValue": null,
                                            "nestedFields": [
                                              {
                                                "name": "WombState",
                                                "type": "String",
                                                "prompt": "Womb contents and state. Track: current contents (semen, contraceptive, nothing), amount/fullness, sensation, whether plugged/sealed.",
                                                "defaultValue": "Empty - Normal, unfilled",
                                                "exampleValues": [
                                                  "Empty - Normal state, nothing inside",
                                                  "Contains semen (~150ml, two loads) - Warm fullness behind pubic bone, sealed by plug, sloshing with movement",
                                                  "Heavily filled (~500ml+, multiple partners) - Significant pressure, visible bloating of lower belly, cramping slightly, plugged to retain",
                                                  "Pregnant - See Reproduction section for details"
                                                ]
                                              },
                                              {
                                                "name": "StomachState",
                                                "type": "String",
                                                "prompt": "Stomach contents and state - food, drink, other substances.",
                                                "defaultValue": "Normal - Comfortable from recent meal",
                                                "exampleValues": [
                                                  "Empty - Haven't eaten today, hollow feeling",
                                                  "Normal - Comfortable from recent meal",
                                                  "Full - Large meal just consumed, feeling heavy and satisfied",
                                                  "Contains (other) - Forced to swallow X, feeling nauseous"
                                                ]
                                              },
                                              {
                                                "name": "BowelState",
                                                "type": "String",
                                                "prompt": "Bowel contents if relevant - enemas, plugged, etc.",
                                                "defaultValue": "Normal",
                                                "exampleValues": [
                                                  "Normal - Nothing unusual",
                                                  "Contains enema (1L warm water) - Cramping, pressure, fighting urge to release, plugged",
                                                  "Recently cleaned - Just evacuated preparation enema, empty and clean"
                                                ]
                                              }
                                            ]
                                          },
                                          {
                                            "name": "Appearance",
                                            "type": "Object",
                                            "prompt": "Current visual and sensory presentation - how the character looks, sounds, and smells RIGHT NOW. These fields track current state and condition, which changes based on activities and circumstances.",
                                            "defaultValue": null,
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
                                                "prompt": "Comprehensive facial features and current expression - structure and current state. Include: face shape, eye color/shape/current state, eyebrow shape, nose, lips (fullness, natural color, current state), skin complexion and condition, and current expression. Update current state based on scene: tears, flushing, swelling from slaps, fluids on face, etc.",
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
                                                  "Ruined heavy makeup - Was: thick black eyeliner, heavy mascara, deep red lipstick, blush. Now: eyeliner smeared across temples from tears, mascara running in black streaks down both cheeks, lipstick completely worn off, foundation streaked with sweat and tears; thoroughly wrecked appearance"
                                                ]
                                              },
                                              {
                                                "name": "Scent",
                                                "type": "String",
                                                "prompt": "What the character currently smells like - natural body odor, applied scents, and accumulated smells from activities. Layer scents from underlying (skin) to surface (recent additions). Include: baseline cleanliness, any perfume/soap, sweat level, arousal musk, sex smells (cum, fluids), and any other relevant odors. Update based on: time since bathing, physical exertion, sexual activity, environmental exposure. Scent can be an important sensory detail for scenes.",
                                                "defaultValue": "Clean - Fresh soap scent, natural neutral skin smell, no strong odors",
                                                "exampleValues": [
                                                  "Clean and fresh - Bathed this morning with lavender soap, faint floral scent lingers on skin, no body odor, no sweat; pleasant neutral smell",
                                                  "Aroused musk - Clean underneath but several hours since bathing, light natural body scent, strong arousal musk emanating from between legs, light sweat sheen adding salt note; smells like an aroused woman",
                                                  "Thoroughly used - Hasn't bathed in 2 days, underlying stale sweat and body odor, layered with heavy sex smell: dried and fresh cum, her own arousal fluids coating thighs, dried saliva, fresh sweat from exertion; overwhelmingly smells of sex and use"
                                                ]
                                              },
                                              {
                                                "name": "Voice",
                                                "type": "String",
                                                "prompt": "Current state of character's voice and ability to vocalize - natural qualities and current condition. Describe: natural voice (pitch, tone, quality), current condition (clear, hoarse, strained), and any impairments.",
                                                "defaultValue": "Clear - Soft feminine voice, unimpaired, speaks easily",
                                                "exampleValues": [
                                                  "Clear and steady - Natural alto voice, pleasant tone, completely unimpaired; speaks clearly and confidently",
                                                  "Strained and thick - Naturally soft voice, currently thick from recent crying, slight wobble when speaking, occasional catch in throat from suppressed sobs; understandable but obviously distressed",
                                                  "Wrecked - Voice destroyed from combination of screaming and rough throat use; currently barely above hoarse whisper, raw pain when swallowing or attempting to speak, words come out as rough croaks; will need days to recover"
                                                ]
                                              }
                                            ]
                                          },
                                          {
                                            "name": "Body",
                                            "type": "Object",
                                            "prompt": "Detailed physical anatomy of the character's body - structure, features, and current condition of each body region.",
                                            "defaultValue": null,
                                            "nestedFields": [
                                              {
                                                "name": "Mouth",
                                                "type": "String",
                                                "prompt": "Detailed oral anatomy focused on sexual use capacity. Include: lip function, teeth condition, tongue details, jaw strength and current state, gag reflex status, throat depth capacity, and current oral condition.",
                                                "defaultValue": "Healthy mouth - average tongue, strong gag reflex, untrained throat, jaw comfortable",
                                                "exampleValues": [
                                                  "Inexperienced mouth - All teeth present and healthy, average pink tongue, strong gag reflex triggering at 3 inches depth, throat untrained and tight, never taken anything deep; jaw currently comfortable, no fatigue",
                                                  "Trained oral - Teeth intact, longer than average tongue (skilled from practice), gag reflex weakened through training (triggers at 5-6 inches), throat can accommodate average cock to root with effort; jaw well-conditioned for extended use, currently mild ache from earlier session",
                                                  "Extensively broken in - Teeth intact (carefully preserved), long dexterous tongue, gag reflex completely eliminated through months of training, throat permanently loosened and can take any size without resistance; jaw currently aching badly and clicking (locked in ring gag for 3 hours), throat raw and scratched from rough use"
                                                ]
                                              },
                                              {
                                                "name": "Breasts",
                                                "type": "String",
                                                "prompt": "Complete breast anatomy including chest, nipples, and lactation status in one field. CHEST: size (cup size AND descriptive), shape (perky, teardrop, round, pendulous), weight/heaviness, firmness (soft, firm, augmented), natural behavior (self-supporting, need support, bounce/sway patterns), vein visibility, current state (natural, swollen, marked, bound). NIPPLES: areola size (use coin comparisons: dime, quarter, silver dollar), areola color and texture, nipple size/shape (small, medium, large/long, puffy, flat, inverted), nipple color, current state (soft, hardening, fully erect, overstimulated), any modifications (piercings) or damage. LACTATION: production status (non-lactating, producing), volume if lactating, time since last expression, current fullness (empty/comfortable/full/engorged), milk characteristics, let-down triggers. For male characters, describe pectoral development instead.",
                                                "defaultValue": "Moderate B-cups, perky and self-supporting, soft with gentle natural bounce. Unmarked. Quarter-sized light pink areolae, small button nipples, currently soft. Non-lactating.",
                                                "exampleValues": [
                                                  "Small A-cups, barely-there gentle swells against ribcage, very firm, minimal movement even during activity. Smooth and unmarked. Small dime-sized pale pink areolae, nearly smooth texture. Tiny nipples that lay almost flat when soft, rise to small firm points when erect. Currently soft and unobtrusive. Non-lactating.",
                                                  "Full natural D-cups, classic teardrop shape with more fullness at bottom, heavy enough to require support, significant sway when walking. Soft and yielding, faint blue veins visible under fair skin when aroused. Currently unmarked. Silver-dollar sized medium pink areolae with visible bumpy Montgomery glands. Puffy nipples - areola and nipple form soft cone shape when relaxed, tips push out prominently when erect. Currently fully erect from arousal. Non-lactating.",
                                                  "Massive G-cups, heavy and pendulous, hang to navel when unsupported, impossible to ignore. Very soft, almost fluid movement. Currently bound in rope harness squeezing into tight swollen globes. Large dark brown areolae with pronounced bumpy texture. Long thick nipples (~1 inch when erect). Pierced: thick gauge steel barbells through each. Currently clamped with clover clamps. Heavy production - 8+ pints/day, not milked in 14 hours (punishment), breasts painfully engorged."
                                                ]
                                              },
                                              {
                                                "name": "Stomach",
                                                "type": "String",
                                                "prompt": "Combined midriff appearance covering both baseline anatomy and any current distension. BASELINE: muscle definition (none, slight, toned, defined abs), natural shape (flat, slight curve, rounded), softness (firm, soft, very soft), navel type and appearance (innie depth, outie), skin texture. CURRENT DISTENSION (if any): size change from baseline (slight bulge, noticeable swelling, severe distension), skin state (soft, taut, drum-tight, shiny), visible effects (veins, movement inside, navel changes), and comparison (food baby, looks pregnant, etc.). If normal/not distended, focus on baseline description and state 'no current distension.'",
                                                "defaultValue": "Flat stomach with slight natural softness, no visible muscle. Shallow innie navel. Smooth pale skin. No current distension.",
                                                "exampleValues": [
                                                  "Tightly toned stomach with visible four-pack definition, very firm to touch, minimal body fat. Deep innie navel. Smooth tanned skin. Athletic core from training. No current distension.",
                                                  "Soft flat stomach with gentle feminine curve, no muscle definition, pleasant give when pressed. Small round innie navel. Creamy smooth skin, sensitive to tickling. Currently showing moderate bulge - visible rounded swelling of lower belly, skin taut over the bump, looks like early pregnancy or having eaten large meal. Navel slightly stretched. Gentle sloshing movement visible when she shifts position.",
                                                  "Soft rounded belly with visible pooch below navel, very soft and squeezable. Shallow navel. Pale skin with faint stretch marks on sides. Currently severely distended - belly swollen massively, skin drum-tight and shiny, veins visible through stretched skin, navel completely flat and almost popping outward. Looks like full-term pregnancy but rounder. Visible churning/movement inside. MainCharacter cannot bend at waist."
                                                ]
                                              },
                                              {
                                                "name": "Genitalia",
                                                "type": "String",
                                                "prompt": "Complete genital anatomy AND current secretion/fluid status in one field. ANATOMY - FOR VULVAS: Mons pubis (fullness, padding), labia majora (puffy, flat, thin, full), labia minora (length, inner/outer visibility, color, texture), clitoris (size, hood coverage, exposure), vaginal opening (observed tightness/looseness, gape when relaxed). FOR PENISES: Length (soft AND erect), girth, shape, vein prominence, glans details, foreskin status, scrotum. Current anatomical state: resting/aroused, used/fresh. SECRETIONS: current wetness level (dry, slightly moist, wet, soaking, dripping, gushing), natural arousal fluid (amount, consistency, color), any cum present (whose, how fresh, how much, where), other fluids (pre-cum, cervical mucus). Describe viscosity, visible evidence (dampening fabric, coating thighs, pooling beneath), and scent if relevant.",
                                                "defaultValue": "Female: Smooth mound with modest padding. Puffy outer labia concealing small pink inner labia (innie). Small clit hidden under hood. Tight vaginal entrance. Currently dry - genitals clean and dry at rest, neutral state.",
                                                "exampleValues": [
                                                  "Female (virgin anatomy): Full soft mons with slight padding. Puffy outer labia press together when standing, conceal everything when closed. Inner labia small and delicate, pale pink, completely contained within outer lips (innie). Small clit fully covered by hood, only visible when hood manually retracted. Vaginal entrance virgin-tight, hymen intact, barely admits single fingertip. Currently dry - genitals clean and dry, no natural lubrication, neutral state.",
                                                  "Female (experienced anatomy): Prominent mons, mostly smooth. Outer labia moderately full but parted, don't conceal inner anatomy. Inner labia prominent - extend 1.5 inches past outer lips when spread, darker rose-pink with slightly textured edges, visible from outside. Clit medium-sized, hood retracted showing pink nub constantly. Vaginal entrance well-used - relaxed gape of ~1cm at rest, easily accommodates three fingers. Currently wet and slick - significant natural arousal fluid, clear and slippery, coating entire vulva and beginning to dampen inner thighs. Thin consistency, strings slightly when spread. Light musky arousal scent.",
                                                  "Male (average anatomy): Soft: 3 inches, hangs over scrotum. Erect: 6.5 inches, moderate girth (5 inch circumference), slight upward curve, prominent dorsal vein. Cut, pink glans with defined ridge. Scrotum hangs loosely in warm conditions, average-sized testicles. Currently cum-soaked from recent use - fresh thick load coating shaft and glans, dripping onto scrotum, mixed with her arousal fluid."
                                                ]
                                              },
                                              {
                                                "name": "Rear",
                                                "type": "String",
                                                "prompt": "Complete rear anatomy covering buttocks AND anus in one field. BUTTOCKS: size/volume (small, medium, large, massive), shape (flat, round, heart, bubble, shelf), firmness (tight, firm, soft, very soft), movement physics (minimal jiggle, bounces, claps, ripples), how easily spread (firm/resistant vs soft/yields), cheek texture (smooth, dimpled, cellulite), current state (unmarked, reddened, bruised, welted), thigh gap presence if relevant. ANUS: external appearance - color of outer rim (pink, brown, dark), texture (puckered/knotted, smooth, wrinkled), surrounding hair if any. Muscle tone and capacity - tightness (virgin-tight, tight, normal, relaxed, loose, gaping), observed gape when relaxed, what can be accommodated. Current condition (pristine, used, red, swollen, sore, damaged). Training progress if being developed.",
                                                "defaultValue": "Modest medium rear, round shape, firm with slight softness. Light bounce when moving. Smooth unmarked skin. Tight pink rosebud anus, puckered closed, untouched and virgin-tight. Clean-shaven surrounding. Pristine condition.",
                                                "exampleValues": [
                                                  "Small tight rear, flat-ish with slight rounded curve, very firm (athletic build). Minimal jiggle even with impact, would need effort to spread cheeks. Smooth taut skin, unmarked. No thigh gap. Virginal anus - small tightly-knotted pink rosebud, puckers closed with no visible opening when relaxed, clenches reflexively at any touch. Never penetrated, would require significant stretching to accept even single finger. Smooth hairless skin surrounding. Pristine, never used.",
                                                  "Large heart-shaped ass, full and prominent, soft and squeezable with pleasant give. Noticeable sway when walking, bounces and jiggles with movement, claps during impact. Spreads easily when pulled. Smooth skin with faint cellulite dimpling on lower cheeks. Small thigh gap. Currently covered in dark bruises (2 days old) and fresh red handprints from recent spanking. Trained anus - light brown wrinkled ring, relaxes to slight visible dimple when at rest (~0.5cm). Trained with plugs to accept average-sized toys/cock with adequate lube. Sphincter conditioned to relax on command. Currently slight redness and mild soreness from plug worn earlier. Light hair on outer rim.",
                                                  "Massive shelf ass, extremely heavy and prominent, very soft and plush like pillows. Dramatic sway and bounce with every step, loud clapping during any impact, ripples spread across flesh. Deep cleavage between cheeks. Significant cellulite on cheeks and upper thighs. Extensively used anus - dark stretched ring, permanent gape of ~2cm when relaxed, inner red/pink mucosa visible inside opening. No longer able to fully close. Can easily accept very large objects without resistance. Sphincter tone significantly reduced. Currently puffy and irritated from recent rough use, minor prolapse beginning. Hairless (kept shaved)."
                                                ]
                                              },
                                              {
                                                "name": "BodyHair",
                                                "type": "String",
                                                "prompt": "Body hair status across all regions EXCEPT head. Track: Pubic, Armpits, Legs. Include days since last grooming to track growth.",
                                                "defaultValue": "Pubic: Neatly trimmed | Armpits: Freshly shaved | Legs: Smooth (shaved yesterday) | Other: None notable",
                                                "exampleValues": [
                                                  "Pubic: Completely bare (waxed 3 days ago, still smooth) | Armpits: Freshly shaved (this morning) | Legs: Smooth (shaved this morning) | Other: No notable body hair - maintains full removal",
                                                  "Pubic: Short stubble growing back (shaved 4 days ago, scratchy to touch) | Armpits: Visible stubble (4 days) | Legs: Prickly stubble dots visible (4 days) | Other: Fine arm hair (natural) - hasn't had grooming access in captivity",
                                                  "Pubic: Full natural bush (never shaved, thick dark curls extending to inner thighs) | Armpits: Full dark tufts (natural, never shaved) | Legs: Hairy (natural, never shaved) | Other: Visible dark treasure trail from navel down - completely natural, no grooming"
                                                ]
                                              }
                                            ]
                                          }
                                        ]
                                      },
                                      {
                                        "name": "Reproduction",
                                        "type": "Object",
                                        "prompt": "Reproductive system status - fertility cycle, breeding, pregnancy. Interacts with bloodline (some much more fertile).",
                                        "defaultValue": null,
                                        "nestedFields": [
                                          {
                                            "name": "FertilityCycle",
                                            "type": "Object",
                                            "prompt": "Natural fertility cycle - modified by bloodline.",
                                            "defaultValue": null,
                                            "nestedFields": [
                                              {
                                                "name": "CycleType",
                                                "type": "String",
                                                "prompt": "What kind of fertility cycle - human menstrual, beast heat-based, or other pattern. Determined by bloodline.",
                                                "defaultValue": "Human standard menstrual cycle",
                                                "exampleValues": [
                                                  "Human standard - 28-day menstrual cycle with typical fertile window",
                                                  "Feline heat-based - No menstruation, fertile only during heat cycles (every 2-3 weeks, 3-5 days), VERY fertile during heat",
                                                  "Canine heat-based - Fertile during heat cycles (every 3 months, 5-7 days), can conceive outside heat but lower chance",
                                                  "Bovine enhanced - Menstrual cycle but highly fertile, longer fertile window, higher conception rates",
                                                  "Draconic reduced - Standard cycle but lower baseline fertility (dragons reproduce rarely)"
                                                ]
                                              },
                                              {
                                                "name": "CurrentPhase",
                                                "type": "String",
                                                "prompt": "Current position in fertility cycle with conception risk percentage.",
                                                "defaultValue": "Unknown",
                                                "exampleValues": [
                                                  "Menstrual 🩸 (Day 2) - 0% conception risk, currently bleeding",
                                                  "Follicular 🌱 (Day 8) - Low risk ~15%",
                                                  "Ovulating 🌺 (Day 14) - HIGH risk ~85% peak fertility",
                                                  "IN HEAT 🔥 (Day 2 of 5) - EXTREME FERTILITY ~95%, body optimized for conception",
                                                  "Pregnant 🤰 - Cycle suspended"
                                                ]
                                              },
                                              {
                                                "name": "FertilityModifiers",
                                                "type": "String",
                                                "prompt": "Factors affecting fertility - bloodline bonuses/penalties, contraception, fertility treatments, conditions.",
                                                "defaultValue": "None - Baseline fertility",
                                                "exampleValues": [
                                                  "None - Standard fertility for cycle type",
                                                  "Bovine Bloodline (+50% conception chance) - Highly fertile even outside peak",
                                                  "Taking contraceptive pills (-95% effective) - Protection active, must take daily",
                                                  "Draconic Bloodline (-30% conception) - Dragons reproduce rarely",
                                                  "Breeding program treatments (+25%) - Taking fertility pills to increase conception chance"
                                                ]
                                              }
                                            ]
                                          },
                                          {
                                            "name": "Pregnancy",
                                            "type": "String",
                                            "prompt": "Pregnancy status if applicable. Track: whether pregnant, days since conception, trimester, father identity, symptoms, expected delivery.",
                                            "defaultValue": "Not Pregnant",
                                            "exampleValues": [
                                              "Not Pregnant - No current pregnancy",
                                              "Possibly Pregnant - Creampied during fertile window 2 days ago, too early to confirm",
                                              "Confirmed Pregnant - 1st Trimester (Day 45) | Father: Elder Vex (certain, designated breeding) | Symptoms: Morning nausea, breast tenderness, missed period | Expected: ~Day 270",
                                              "Pregnant - 3rd Trimester (Day 250) | Father: Unknown (heat-night with multiple partners) | Symptoms: Large belly, frequent urination, preparing to deliver | Expected: ~2 weeks"
                                            ]
                                          },
                                          {
                                            "name": "BreedingStatus",
                                            "type": "String",
                                            "prompt": "For characters in breeding programs or owned for breeding purposes - their formal status and arrangements.",
                                            "defaultValue": "Not in breeding program",
                                            "exampleValues": [
                                              "Not in breeding program - Reproduction is personal matter",
                                              "Registered breeding stock (Ivory Pastures) - Required to breed twice yearly minimum, offspring belong to sect, receives premium care",
                                              "Heat-service assignment (Pack Covenant) - Designated partners during heats, may breed if it happens, no formal requirement",
                                              "Personal broodmare (Lord Halvard) - Owner's breeding slave, expected to carry his children, currently on 2nd pregnancy"
                                            ]
                                          },
                                          {
                                            "name": "ReproductiveHistory",
                                            "type": "String",
                                            "prompt": "Record of past pregnancies and children born.",
                                            "defaultValue": "No pregnancies / No children",
                                            "exampleValues": [
                                              "No pregnancies - Never been pregnant",
                                              "1 pregnancy, 1 child: Daughter (name: Sera), father: Lord Halvard, born 8 months ago, in house nursery, healthy",
                                              "3 pregnancies, 3 children: 1st son (sold at birth), 2nd daughter (kept by sect), 3rd son (current, nursing). All from breeding program."
                                            ]
                                          },
                                          {
                                            "name": "OrgasmControl",
                                            "type": "String",
                                            "prompt": "Current orgasm tracking and any control mechanisms - denial, edging, permission requirements.",
                                            "defaultValue": "Free - No orgasm control",
                                            "exampleValues": [
                                              "Free - Can orgasm whenever aroused enough, no restrictions",
                                              "Permission required - Must ask owner before cumming, last permitted: 3 days ago, edges today: 5",
                                              "Denial (Week 2) - Not permitted to orgasm, edged daily, desperation building, clit constantly throbbing",
                                              "Cursed denial - Magical curse prevents orgasm entirely, has been edged for 3 weeks, body at breaking point"
                                            ]
                                          }
                                        ]
                                      },
                                      {
                                        "name": "SocialPosition",
                                        "type": "Object",
                                        "prompt": "Place in Devoria's social structure - sect membership, freedom status, political position.",
                                        "defaultValue": null,
                                        "nestedFields": [
                                          {
                                            "name": "SectMembership",
                                            "type": "Object",
                                            "prompt": "Affiliation with major sect if any.",
                                            "defaultValue": null,
                                            "nestedFields": [
                                              {
                                                "name": "Sect",
                                                "type": "String",
                                                "prompt": "Which sect, if any. Major sects: Ember Throne (draconic), Pack Covenant (canine), Ivory Pastures (bovine), Silken Web (arachnid), Foxfire Pavilion (vulpine), The Coil (serpentine), Abyssal Tide (cephalopod), The Hive (insectoid), or minor/no sect.",
                                                "defaultValue": "Unaffiliated",
                                                "exampleValues": [
                                                  "Unaffiliated - No sect membership, independent cultivator",
                                                  "Pack Covenant - Full member",
                                                  "Ember Throne - Member (enslaved/property)",
                                                  "Foxfire Pavilion - Contracted courtesan (not full member)"
                                                ]
                                              },
                                              {
                                                "name": "Rank",
                                                "type": "String",
                                                "prompt": "Position within sect hierarchy: Outer Disciple, Inner Disciple, Core Disciple, Elder, Patriarch/Matriarch. Or non-standard positions for owned members.",
                                                "defaultValue": "N/A",
                                                "exampleValues": [
                                                  "N/A - Not in a sect",
                                                  "Outer Disciple - Lowest full member rank, probationary",
                                                  "Inner Disciple - Full member, proven value",
                                                  "Core Disciple - Elite, being groomed for leadership",
                                                  "Property of Elder Vex - Not ranked, owned by elder as personal slave"
                                                ]
                                              },
                                              {
                                                "name": "Standing",
                                                "type": "String",
                                                "prompt": "Reputation and standing within the sect.",
                                                "defaultValue": "N/A",
                                                "exampleValues": [
                                                  "N/A - Not in sect",
                                                  "Average - Unremarkable standing, neither favored nor disliked",
                                                  "Rising - Recent achievements gaining attention, being watched",
                                                  "Disgraced - Failed important mission, currently under scrutiny",
                                                  "Favored - Has powerful patron, protected position"
                                                ]
                                              },
                                              {
                                                "name": "Faction",
                                                "type": "String",
                                                "prompt": "Internal faction alignment if any.",
                                                "defaultValue": "N/A",
                                                "exampleValues": [
                                                  "N/A - Not in sect or too junior for factions",
                                                  "Elder Vex's faction - Aligned with Elder Vex's political camp",
                                                  "Neutral - Deliberately unaligned, risky position",
                                                  "Matriarch's direct - Reports directly to sect leader, above faction politics"
                                                ]
                                              }
                                            ]
                                          },
                                          {
                                            "name": "FreedomStatus",
                                            "type": "Object",
                                            "prompt": "Legal and practical freedom - from full citizen to slave.",
                                            "defaultValue": null,
                                            "nestedFields": [
                                              {
                                                "name": "Status",
                                                "type": "String",
                                                "prompt": "Legal status: Free (full rights), Contracted (time-limited service), Indentured (debt-bound), Bonded (Pack Covenant permanent bond), Enslaved (legal property).",
                                                "defaultValue": "Free",
                                                "exampleValues": [
                                                  "Free - Full legal autonomy, citizen rights, owns herself",
                                                  "Contracted - Voluntary 5-year service contract to Foxfire Pavilion, 3 years remaining, can buy out for 500 spirit stones",
                                                  "Indentured - Debt-bound to House Halvard, must work until debt repaid (~8 years at current rate), limited rights",
                                                  "Bonded - Pack Covenant permanent loyalty bond, magically bound to sect, cannot betray, treated well but not truly free",
                                                  "Enslaved - Legal property of Lord Halvard, no rights, registered with city guild, can be sold/traded"
                                                ]
                                              },
                                              {
                                                "name": "Owner",
                                                "type": "String",
                                                "prompt": "If not free, who holds their contract/bond/ownership.",
                                                "defaultValue": "N/A (Free)",
                                                "exampleValues": [
                                                  "N/A - Free, no owner",
                                                  "Foxfire Pavilion (sect) - Contract holder",
                                                  "Lord Marcus Halvard - Personal owner (slave)",
                                                  "Pack Covenant (collective) - Bonded to sect itself",
                                                  "Currently between owners - Recently sold, in transit to new owner"
                                                ]
                                              },
                                              {
                                                "name": "Circumstances",
                                                "type": "String",
                                                "prompt": "How they came to current status and key terms/conditions.",
                                                "defaultValue": "N/A",
                                                "exampleValues": [
                                                  "N/A - Born free, always been free",
                                                  "Sold by parents age 14 to pay debts, trained as pleasure slave, sold twice since",
                                                  "Captured during sect raid, enslaved as war spoils, registered property",
                                                  "Voluntarily contracted for training and protection, includes sexual service clause",
                                                  "Bonded at first fusion (Pack Covenant standard), permanent but not slavery - full member rights within pack structure"
                                                ]
                                              }
                                            ]
                                          },
                                          {
                                            "name": "Reputation",
                                            "type": "Object",
                                            "prompt": "How they're known and perceived by different groups.",
                                            "defaultValue": null,
                                            "nestedFields": [
                                              {
                                                "name": "PublicImage",
                                                "type": "String",
                                                "prompt": "General reputation - what most people know/think of them.",
                                                "defaultValue": "Unknown - No public reputation",
                                                "exampleValues": [
                                                  "Unknown - Unremarkable, no public reputation",
                                                  "Promising talent - Known as rising star in Ember Throne",
                                                  "Infamous - Known as traitor who stole sect techniques",
                                                  "Prized possession - Known as Lord Halvard's beautiful slave"
                                                ]
                                              },
                                              {
                                                "name": "SectReputations",
                                                "type": "String",
                                                "prompt": "Standing with major sects.",
                                                "defaultValue": "Neutral with all - No notable relationships",
                                                "exampleValues": [
                                                  "Neutral with all - Unknown or unremarkable to major sects",
                                                  "Ember Throne: Enemy (defector) | Pack Covenant: Favorable (assisted mission) | Others: Neutral",
                                                  "Foxfire Pavilion: Valuable asset | Silken Web: Wary (knows too many secrets) | Ember Throne: Hostile (rejected overtures)"
                                                ]
                                              }
                                            ]
                                          }
                                        ]
                                      },
                                      {
                                        "name": "Equipment",
                                        "type": "Object",
                                        "prompt": "What they're wearing, carrying, and what's on/in their body.",
                                        "defaultValue": null,
                                        "nestedFields": [
                                          {
                                            "name": "Clothing",
                                            "type": "Object",
                                            "prompt": "Current worn clothing by category.",
                                            "defaultValue": null,
                                            "nestedFields": [
                                              {
                                                "name": "Upper",
                                                "type": "String",
                                                "prompt": "Clothing on torso (not underwear).",
                                                "defaultValue": "None",
                                                "exampleValues": [
                                                  "None - Torso bare",
                                                  "Simple white cotton blouse, loose fit, fully buttoned",
                                                  "Black silk robe with Ember Throne insignia, fine quality, properly worn",
                                                  "Torn remains of training tunic, barely covering anything"
                                                ]
                                              },
                                              {
                                                "name": "Lower",
                                                "type": "String",
                                                "prompt": "Clothing on lower body (not underwear).",
                                                "defaultValue": "None",
                                                "exampleValues": [
                                                  "None - Lower body bare",
                                                  "Brown cotton skirt, knee-length, in place",
                                                  "Silk pants, flowing, black with red trim, properly worn"
                                                ]
                                              },
                                              {
                                                "name": "Underwear",
                                                "type": "String",
                                                "prompt": "ALWAYS track underwear explicitly - bra/breast support and panties/loincloth separately.",
                                                "defaultValue": "None - Not wearing underwear",
                                                "exampleValues": [
                                                  "None - Forbidden by owner, always bare underneath",
                                                  "Simple cotton breast band, white cotton panties - basic smallclothes",
                                                  "Black lace bra (custom for tail), matching thong - sexy set currently in place",
                                                  "Breast band pushed above breasts, panties pulled aside - technically wearing but not covering"
                                                ]
                                              },
                                              {
                                                "name": "Footwear",
                                                "type": "String",
                                                "prompt": "What's on feet.",
                                                "defaultValue": "Barefoot",
                                                "exampleValues": [
                                                  "Barefoot - No footwear",
                                                  "Simple sandals",
                                                  "Leather boots, practical, well-worn",
                                                  "Heeled slippers (locked on) - cannot remove without key"
                                                ]
                                              }
                                            ]
                                          },
                                          {
                                            "name": "Accessories",
                                            "type": "String",
                                            "prompt": "Jewelry, collars, cuffs, decorative items worn. Separate from restraints.",
                                            "defaultValue": "None",
                                            "exampleValues": [
                                              "None - No accessories",
                                              "Steel collar (permanent, welded) with ownership tag | Matching steel cuffs (decorative, not restraining) | Bell on collar",
                                              "Ember Throne disciple token on cord around neck | Simple gold earrings"
                                            ]
                                          },
                                          {
                                            "name": "Restraints",
                                            "type": "String",
                                            "prompt": "Active restraints currently restricting movement. 'None' if unrestrained.",
                                            "defaultValue": "None - Free movement",
                                            "exampleValues": [
                                              "None - No restraints, full freedom of movement",
                                              "Wrists bound behind back with rope | Ankles hobbled with 12-inch chain - can shuffle, cannot run",
                                              "Full bondage: Armbinder (arms behind back, elbow to fingertip), ball gag (large, drooling around it), ankle spreader bar (2ft), leash attached to collar - severely restricted"
                                            ]
                                          },
                                          {
                                            "name": "Insertions",
                                            "type": "String",
                                            "prompt": "Objects currently inside body orifices. Track by location: vaginal, anal, oral, other.",
                                            "defaultValue": "None - All orifices empty",
                                            "exampleValues": [
                                              "None - Nothing inserted",
                                              "Anal: Medium plug (keeps womb sealed after breeding) - worn 2 hours",
                                              "Vaginal: Locked chastity insert (cannot remove, prevents entry) | Anal: Training plug (size 3 of 5, stretching program)",
                                              "Vaginal: Owner's cock (currently being used) | Oral: Ring gag (forces mouth open)"
                                            ]
                                          },
                                          {
                                            "name": "Weapons",
                                            "type": "String",
                                            "prompt": "Weapons carried or equipped. 'None/Unarmed' if none.",
                                            "defaultValue": "Unarmed",
                                            "exampleValues": [
                                              "Unarmed - No weapons (slave, not permitted)",
                                              "Iron dagger, belt sheath - simple self-defense",
                                              "Steel sword (quality), belt scabbard left | Hidden knife in boot right",
                                              "Claws only - natural weapons from transformation, no manufactured weapons"
                                            ]
                                          }
                                        ]
                                      },
                                      {
                                        "name": "Economy",
                                        "type": "Object",
                                        "prompt": "Financial resources - currency, assets, and for slaves, their own market value.",
                                        "defaultValue": null,
                                        "nestedFields": [
                                          {
                                            "name": "Currency",
                                            "type": "String",
                                            "prompt": "Spirit Stones (SS) - primary currency.",
                                            "defaultValue": "0 SS",
                                            "exampleValues": [
                                              "0 SS (Slave - cannot own currency)",
                                              "45 SS - Modest funds from missions",
                                              "2,400 SS - Significant savings, comfortable",
                                              "0 SS personal (Slave) but carries owner's purse: 150 SS"
                                            ]
                                          },
                                          {
                                            "name": "Assets",
                                            "type": "String",
                                            "prompt": "Significant property, investments, owned slaves (if any). For slaves, note their own assessed market value.",
                                            "defaultValue": "None",
                                            "exampleValues": [
                                              "None - No significant assets",
                                              "Small apartment in sect quarters (provided, not owned) - no real assets",
                                              "IS PROPERTY - Own market value: ~3,500 SS (Tier 3 cat-blood, trained, fertile, young)",
                                              "Owns: Small house in city (600 SS value), one trained servant (400 SS value)"
                                            ]
                                          }
                                        ]
                                      },
                                      {
                                        "name": "SexualProfile",
                                        "type": "Object",
                                        "prompt": "Comprehensive sexual history, capabilities, preferences, and training. Explicit tracking for adult content.",
                                        "defaultValue": null,
                                        "nestedFields": [
                                          {
                                            "name": "Experience",
                                            "type": "Object",
                                            "prompt": "Sexual experience level and history.",
                                            "defaultValue": null,
                                            "nestedFields": [
                                              {
                                                "name": "Overall",
                                                "type": "String",
                                                "prompt": "General sexual experience level and background.",
                                                "defaultValue": "Inexperienced",
                                                "exampleValues": [
                                                  "Virgin - No sexual experience",
                                                  "Inexperienced - A few encounters, still learning",
                                                  "Moderate - Several partners, comfortable with common acts",
                                                  "Experienced - Many partners, skilled and knowledgeable",
                                                  "Extensive (Forced) - Heavily used since capture, body experienced but mind processes as trauma"
                                                ]
                                              },
                                              {
                                                "name": "Virginity",
                                                "type": "String",
                                                "prompt": "Per-orifice virginity status with details of first times.",
                                                "defaultValue": "Oral: Virgin | Vaginal: Virgin | Anal: Virgin",
                                                "exampleValues": [
                                                  "Oral: Virgin | Vaginal: Virgin | Anal: Virgin - Completely untouched",
                                                  "Oral: Taken (boyfriend, age 17, consensual) | Vaginal: Taken (same, age 18, consensual) | Anal: Virgin",
                                                  "Oral: Taken (Guard, Day 1, forced) | Vaginal: Taken (Owner, Day 1, forced) | Anal: Taken (Punishment, Day 5, forced)"
                                                ]
                                              },
                                              {
                                                "name": "PartnerCount",
                                                "type": "String",
                                                "prompt": "Number of sexual partners and rough act counts.",
                                                "defaultValue": "0 partners",
                                                "exampleValues": [
                                                  "0 partners - Virgin",
                                                  "3 partners | Vaginal: ~30 times | Oral: ~20 | Anal: 0",
                                                  "Unknown (50+) | Vaginal: Countless | Anal: ~150 | Oral: Countless | Gangbangs: 8"
                                                ]
                                              }
                                            ]
                                          },
                                          {
                                            "name": "PhysicalCapabilities",
                                            "type": "Object",
                                            "prompt": "Physical sexual characteristics affecting performance - capacity, stamina, response.",
                                            "defaultValue": null,
                                            "nestedFields": [
                                              {
                                                "name": "VaginalCapacity",
                                                "type": "String",
                                                "prompt": "How much vaginal canal can accommodate.",
                                                "defaultValue": "Average (untested if virgin)",
                                                "exampleValues": [
                                                  "Virgin - Untested, likely average, hymen intact",
                                                  "Average - Accommodates typical sizes comfortably, larger requires adjustment",
                                                  "Above Average - Can take larger than normal without discomfort",
                                                  "High (trained) - Trained to accept very large sizes, unusual depth/stretch capacity",
                                                  "Extreme (modified) - Bloodline modification grants exceptional capacity"
                                                ]
                                              },
                                              {
                                                "name": "AnalCapacity",
                                                "type": "String",
                                                "prompt": "Anal training level and capacity.",
                                                "defaultValue": "Untrained (virgin)",
                                                "exampleValues": [
                                                  "Untrained - Never penetrated, very tight",
                                                  "Beginner - Can accept fingers, small toys",
                                                  "Intermediate - Trained to average cock size",
                                                  "Advanced - Can accept large insertions",
                                                  "Extreme - Extensively trained, very large capacity, may have reduced tone"
                                                ]
                                              },
                                              {
                                                "name": "ThroatCapacity",
                                                "type": "String",
                                                "prompt": "Deepthroat capability and gag reflex status.",
                                                "defaultValue": "Limited (strong gag reflex)",
                                                "exampleValues": [
                                                  "Limited - Strong gag reflex, cannot deepthroat",
                                                  "Moderate - Gag reflex present but can push through, ~5 inches",
                                                  "Good - Weakened gag reflex from training, can take most sizes",
                                                  "Unlimited - Gag reflex eliminated, any length possible, trained throat"
                                                ]
                                              },
                                              {
                                                "name": "Stamina",
                                                "type": "String",
                                                "prompt": "Sexual stamina - how long can they continue, how much can they take.",
                                                "defaultValue": "Average",
                                                "exampleValues": [
                                                  "Low - Tires quickly, one session is exhausting",
                                                  "Average - Normal human stamina",
                                                  "High - Can continue for extended periods",
                                                  "Exceptional (cultivator enhanced) - Tier affects stamina, can go for hours",
                                                  "Heat-enhanced - During heat, nearly unlimited desire and physical capacity"
                                                ]
                                              }
                                            ]
                                          },
                                          {
                                            "name": "Preferences",
                                            "type": "Object",
                                            "prompt": "Sexual likes, dislikes, kinks, and limits.",
                                            "defaultValue": null,
                                            "nestedFields": [
                                              {
                                                "name": "TurnOns",
                                                "type": "String",
                                                "prompt": "What arouses them - kinks, preferences, triggers. Include whether these are natural or conditioned.",
                                                "defaultValue": "Unknown/Undiscovered",
                                                "exampleValues": [
                                                  "Unknown - Inexperienced, hasn't discovered preferences",
                                                  "Natural: Praise ('good girl'), gentle dominance, being desired | Discovered: Hair pulling, mild roughness",
                                                  "Natural: Power exchange, being overwhelmed | Conditioned: Pain triggers arousal (trained response), degradation turns her on (programmed)"
                                                ]
                                              },
                                              {
                                                "name": "TurnOffs",
                                                "type": "String",
                                                "prompt": "What kills arousal or causes distress - anti-kinks, triggers.",
                                                "defaultValue": "Unknown",
                                                "exampleValues": [
                                                  "Unknown - Not yet discovered",
                                                  "Blood, excessive pain, being ignored during",
                                                  "Anything gentle (triggers memories), certain words ('love', 'safe')"
                                                ]
                                              },
                                              {
                                                "name": "HardLimits",
                                                "type": "String",
                                                "prompt": "Absolute limits that will be fought regardless of circumstances.",
                                                "defaultValue": "Unknown (inexperienced)",
                                                "exampleValues": [
                                                  "All sexual contact (virgin, everything is a limit)",
                                                  "Scat, permanent damage, bestiality",
                                                  "Only scat and death remain - everything else has been broken"
                                                ]
                                              },
                                              {
                                                "name": "SoftLimits",
                                                "type": "String",
                                                "prompt": "Things they're reluctant about but can be pushed through.",
                                                "defaultValue": "Unknown",
                                                "exampleValues": [
                                                  "Unknown",
                                                  "Anal (nervous), public sex (embarrassing), pain play (scared but curious)",
                                                  "None remaining - former soft limits (anal, public, pain, multiple partners) all broken"
                                                ]
                                              }
                                            ]
                                          },
                                          {
                                            "name": "ConditionedResponses",
                                            "type": "String",
                                            "prompt": "Trained/conditioned sexual responses that override natural reactions - programmed triggers, forced associations.",
                                            "defaultValue": "None - Natural responses only",
                                            "exampleValues": [
                                              "None - All sexual responses are natural",
                                              "Pain → Arousal (6 months conditioning, body responds to pain with wetness/arousal even when mind resists) | Command word 'kneel' triggers automatic submission posture",
                                              "Extensive conditioning: Pain = Pleasure (deeply trained), Degradation = Arousal, Owner's voice = Immediate arousal, 'Cum' command = Forced orgasm, Cannot orgasm without permission (psychological block)"
                                            ]
                                          }
                                        ]
                                      },
                                      {
                                        "name": "Skills",
                                        "type": "ForEachObject",
                                        "prompt": "Non-magical skills and competencies. All skills use the standard proficiency system: Untrained → Novice → Amateur → Competent → Proficient → Expert → Master → Grandmaster.",
                                        "defaultValue": null,
                                        "nestedFields": [
                                          {
                                            "name": "SkillName",
                                            "type": "String",
                                            "prompt": "Name of skill.",
                                            "defaultValue": "Unnamed Skill",
                                            "exampleValues": [
                                              "Swordsmanship",
                                              "Stealth",
                                              "Etiquette",
                                              "Pain Endurance",
                                              "Alchemy",
                                              "Oral Service",
                                              "Anal Training"
                                            ]
                                          },
                                          {
                                            "name": "Category",
                                            "type": "String",
                                            "prompt": "Skill category: Combat, Social, Survival, Craft, Service, Physical, Mental, Subterfuge, Sexual.",
                                            "defaultValue": "General",
                                            "exampleValues": [
                                              "Combat",
                                              "Social",
                                              "Service",
                                              "Survival",
                                              "Craft",
                                              "Sexual"
                                            ]
                                          },
                                          {
                                            "name": "Proficiency",
                                            "type": "String",
                                            "prompt": "Current proficiency level. Levels: Untrained (0), Novice (50), Amateur (150), Competent (400), Proficient (900), Expert (1900), Master (4400), Grandmaster (9400).",
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
                                            "name": "XP",
                                            "type": "Object",
                                            "prompt": "Experience tracking toward next proficiency level.",
                                            "defaultValue": null,
                                            "nestedFields": [
                                              {
                                                "name": "Current",
                                                "type": "Number",
                                                "prompt": "Total accumulated XP. Thresholds: Novice(50), Amateur(150), Competent(400), Proficient(900), Expert(1900), Master(4400), Grandmaster(9400).",
                                                "defaultValue": "0",
                                                "exampleValues": [0, 35, 178, 523, 1850, 5200, 11240]
                                              },
                                              {
                                                "name": "NextThreshold",
                                                "type": "Number",
                                                "prompt": "XP threshold for next proficiency level. 50→150→400→900→1900→4400→9400→MAX.",
                                                "defaultValue": 50,
                                                "exampleValues": [50, 150, 400, 900, 1900, 4400, 9400, -1]
                                              },
                                              {
                                                "name": "ToNext",
                                                "type": "Number",
                                                "prompt": "XP remaining until next proficiency level. -1 if at Grandmaster (max level).",
                                                "defaultValue": 50,
                                                "exampleValues": [50, 15, 222, 377, 2550, -1]
                                              }
                                            ]
                                          },
                                          {
                                            "name": "ChallengeFloor",
                                            "type": "String",
                                            "prompt": "Minimum difficulty of tasks that grant meaningful XP - equals current Proficiency level. Tasks BELOW floor grant only 0-10% XP. Tasks AT floor grant 100% XP. Tasks ABOVE floor grant 150-200% XP.",
                                            "defaultValue": "Untrained (any practice grants full XP)",
                                            "exampleValues": [
                                              "Untrained (any practice grants full XP, everything is challenging)",
                                              "Novice (basic exercises grant minimal XP; need real application or difficult drills)",
                                              "Competent (routine professional tasks grant minimal XP; need genuine challenges, novel problems, or superior opponents)",
                                              "Master (standard difficult tasks grant minimal XP; only extreme challenges, innovation, teaching masters, or legendary feats grant meaningful XP)"
                                            ]
                                          },
                                          {
                                            "name": "Development",
                                            "type": "String",
                                            "prompt": "Narrative tracking of how this skill has developed - training history, key learning moments, teachers, and recent progress. Include: how skill was first acquired, notable training or experiences that granted significant XP, any teachers/mentors who contributed, recent developments, and current training focus if any.",
                                            "defaultValue": "Newly encountered skill, no development history yet.",
                                            "exampleValues": [
                                              "Self-taught basics through trial and error over first month of captivity. No formal instruction. Recent: Gained significant XP during escape attempt (high-stakes application).",
                                              "Formally trained from age 8 at father's insistence. Journeyman instructor for 6 years, then 2 years under Swordmaster Aldric (Expert). Reached Proficient before capture. Skills maintained but not advancing in captivity - no worthy opponents. Recent: Successfully defended self against guard (routine, minimal XP).",
                                              "Natural talent identified by court mage at age 12 (see Trait: Magically Gifted). Apprenticed for 4 years, focused on fire specialization. Teaching accelerated progress significantly. Recent: Breakthrough during emotional crisis unlocked new understanding (+150 XP bonus from dramatic moment)."
                                            ]
                                          },
                                          {
                                            "name": "RecentGains",
                                            "type": "String",
                                            "prompt": "Last 3-5 significant XP gains with calculation breakdown. Format: 'Scene/Day: +X XP (Base x Challenge x Bonuses = Total) - Context'",
                                            "defaultValue": "None yet",
                                            "exampleValues": [
                                              "None yet",
                                              "Day 45: +20 XP (20 x 1.0 = 20) - Standard training session at level",
                                              "Scene 47: +2 XP (20 x 0.10 = 2) - Routine use, below floor | Scene 52: +35 XP (20 x 1.0 x 1.75 = 35) - Duel vs Competent opponent, high stakes + dramatic"
                                            ]
                                          }
                                        ]
                                      },
                                      {
                                        "name": "PermanentMarks",
                                        "type": "String",
                                        "prompt": "Permanent body modifications - brands, tattoos, scars, piercings. Track each with location, description, origin, and whether consensual.",
                                        "defaultValue": "None - Body unmarked",
                                        "exampleValues": [
                                          "None - No permanent marks, clean canvas",
                                          "Ownership brand: House Halvard crest on left inner thigh (forced at purchase, healed silver scar) | Pierced: Both nipples with steel rings (forced, for attachment purposes), healed",
                                          "Slave marks: Brand (House Halvard, inner thigh), Tattoo ('Property' lower back, forced), Piercings (nipples, clit hood - all functional for control/display). Scars: Whip marks across back (punishment, healed), collar scar around neck (permanent from first year of wear)"
                                        ]
                                      },
                                      {
                                        "name": "Traits",
                                        "type": "Object",
                                        "prompt": "Permanent characteristics with mechanical effects - talents, flaws, special qualities. Traits require significant, sustained experience to develop - not single events. Include XP/CTP modifiers where applicable.",
                                        "defaultValue": null,
                                        "nestedFields": [
                                          {
                                            "name": "Positive",
                                            "type": "String",
                                            "prompt": "Beneficial traits - natural talents, developed strengths. Format: 'Trait Name (Category) - Effect'. Include any XP or CTP multipliers.",
                                            "defaultValue": "None identified",
                                            "exampleValues": [
                                              "None - No notable positive traits identified",
                                              "Magically Gifted (Magical) - +50% XP to magic skills, instinctive mana sensing | Natural Beauty (Social) - +25% to appearance-based social interactions",
                                              "Exceptional Pain Tolerance (Physical) - Can function normally up to 7/10 pain, +25% XP to Pain Endurance | Quick Learner (Mental) - +25% XP to all skills | Fertile (Sexual/Bovine bloodline) - +50% conception chance"
                                            ]
                                          },
                                          {
                                            "name": "Negative",
                                            "type": "String",
                                            "prompt": "Detrimental traits - weaknesses, flaws. Format: 'Trait Name (Category) - Effect'. Include any XP or CTP penalties.",
                                            "defaultValue": "None identified",
                                            "exampleValues": [
                                              "None - No notable negative traits",
                                              "Frail Constitution (Physical) - -25% XP to physical skills, -25% CTP to Body Capacity, tires faster | Easily Conditioned (Mental) - Psychological conditioning takes hold 50% faster",
                                              "Slave Brand (Social/Magical) - Cannot raise hand against owner, compelled truth when directly questioned | Trauma: Darkness (Mental) - Panic response in dark enclosed spaces, -3 effective proficiency levels when triggered"
                                            ]
                                          },
                                          {
                                            "name": "Developing",
                                            "type": "String",
                                            "prompt": "Traits that are forming but not yet permanent. Track progress toward full trait acquisition. Single dramatic events may begin development but rarely complete it. Format: 'Trait Name (progress/requirement) - What would cause completion'",
                                            "defaultValue": "None - No traits currently developing",
                                            "exampleValues": [
                                              "None - No traits currently developing",
                                              "Pain Tolerance (3/10 significant pain experiences) - Repeated exposure to significant pain is building tolerance | Conditioned Arousal (2 weeks/8 weeks conditioning) - Ongoing conditioning connecting pain to arousal",
                                              "Pack Bond (1/3 major bonding events) - First significant pack loyalty moment occurred, more needed for permanent trait"
                                            ]
                                          }
                                        ]
                                      },
                                      {
                                        "name": "Inventory",
                                        "type": "ForEachObject",
                                        "prompt": "Items being carried (not worn). Each tracked separately.",
                                        "defaultValue": null,
                                        "nestedFields": [
                                          {
                                            "name": "Item",
                                            "type": "String",
                                            "prompt": "Item name and brief description.",
                                            "defaultValue": "Item",
                                            "exampleValues": [
                                              "Healing Pill (low tier) - Basic healing, restores minor wounds",
                                              "Letter from sister - Hidden, sentimental value",
                                              "Sect token - Identifies as Pack Covenant disciple"
                                            ]
                                          },
                                          {
                                            "name": "Location",
                                            "type": "String",
                                            "prompt": "Where on person the item is kept.",
                                            "defaultValue": "Carried",
                                            "exampleValues": [
                                              "Belt pouch",
                                              "Hidden in boot",
                                              "Worn around neck under clothing"
                                            ]
                                          }
                                        ]
                                      }
                                    ],
                                    "Characters": [
                                      {
                                        "name": "Name",
                                        "type": "String",
                                        "prompt": "The character's full identity as currently known. In Devoria, names accumulate meaning: birth name, sect name, bloodline epithet, earned titles, or slave designation. Format: 'Primary Name (Additional Names/Titles)'. For slaves, include birth name if known and any assigned designations. Update as character gains recognition, titles, or has identity stripped.",
                                        "defaultValue": "Unknown",
                                        "exampleValues": [
                                          "Lyra Ashford",
                                          "Kira Emberheart, 'The Scarlet Claw' (inner disciple title, Ember Throne)",
                                          "Slave designation: 'Seventeen' (birth name: Mira Vance, stripped upon enslavement to Silken Web)"
                                        ]
                                      },
                                      {
                                        "name": "Gender",
                                        "type": "String",
                                        "prompt": "Biological sex, potentially modified by fusion transformations. Some high-tier fusions can alter sexual characteristics. Note baseline sex and any fusion-induced changes. This determines relevant anatomical fields and breeding capacity.",
                                        "defaultValue": "Female ♀️",
                                        "exampleValues": [
                                          "Female ♀️",
                                          "Male ♂️",
                                          "Female ♀️ (hermaphroditic traits emerging from high-tier serpent fusion - developing hemipenes alongside vagina)"
                                        ]
                                      },
                                      {
                                        "name": "Age",
                                        "type": "Object",
                                        "prompt": "Age tracking accounting for cultivation's effect on lifespan and aging. Higher tiers age slower and live longer.",
                                        "defaultValue": null,
                                        "nestedFields": [
                                          {
                                            "name": "Actual",
                                            "type": "String",
                                            "prompt": "True chronological age in years.",
                                            "defaultValue": "19",
                                            "exampleValues": [
                                              "19",
                                              "147",
                                              "892"
                                            ]
                                          },
                                          {
                                            "name": "Apparent",
                                            "type": "String",
                                            "prompt": "How old they appear. Higher-tier cultivators age slowly. A Tier 6 at 200 might look 35. Include tier context.",
                                            "defaultValue": "19 (Tier 2 - aging normally)",
                                            "exampleValues": [
                                              "19 (Tier 2 - aging normally)",
                                              "Mid-20s apparent (actual 147, Tier 6 - aging ~1 year per 5)",
                                              "Appears 40s (actual 892, Tier 8 - aging has nearly stopped)"
                                            ]
                                          },
                                          {
                                            "name": "ExpectedLifespan",
                                            "type": "String",
                                            "prompt": "Projected maximum lifespan based on current tier. Tier 1: 60-80, Tier 2: ~120, Tier 3: ~150, Tier 4: ~200, Tier 5: ~300, Tier 6: ~500, Tier 7: ~1000, Tier 8: Millennia, Tier 9: Immortal.",
                                            "defaultValue": "~120 years (Tier 2)",
                                            "exampleValues": [
                                              "60-80 years (Tier 1 - mundane lifespan)",
                                              "~200 years (Tier 4)",
                                              "~1000 years (Tier 7)",
                                              "Immortal barring violence (Tier 9)"
                                            ]
                                          }
                                        ]
                                      },
                                      {
                                        "name": "GeneralBuild",
                                        "type": "String",
                                        "prompt": "Overall body type and physical presence - the big picture before zooming into specific parts. Include: height (specific measurement preferred), weight or build descriptor, body type (slender, athletic, curvy, heavyset), how weight is distributed, muscle tone, skin color/texture/temperature. This field sets the foundation; specific body parts are detailed in their own fields. Include racial physical traits if applicable.",
                                        "defaultValue": "Average height (1.65m), slender feminine build with soft curves. Fair smooth skin, warm to touch.",
                                        "exampleValues": [
                                          "Petite (1.52m, ~55 kg), delicate small-framed build with subtle curves. Minimal muscle tone, soft everywhere. Porcelain pale skin that shows marks easily, naturally cool to touch, goosebumps when cold or aroused.",
                                          "Tall (1.78m, ~68 kg), athletic Amazonian build with defined muscles visible under skin, strong shoulders, powerful thighs. Low body fat, firm rather than soft. Bronze sun-kissed skin, warm and slightly oiled from training, flushed with exertion heat.",
                                          "Short and stacked (1.50m, ~73 kg), exaggerated hourglass with weight concentrated in chest and hips. Thick soft thighs, soft belly with slight pooch, plush everywhere. Creamy pale skin, very warm and soft to touch, yields like bread dough when squeezed."
                                        ]
                                      },
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
                                        "name": "StateOfDress",
                                        "type": "String",
                                        "prompt": "SUMMARY field - overall assessment of how dressed/undressed and put-together the character appears. This is the quick-reference for current clothed status. Options: Pristine (perfect), Neat (properly dressed), Casual (relaxed but dressed), Disheveled (messy, askew), Partially Undressed (some clothing removed/displaced), Stripped (most clothing removed), Nude (nothing), Exposed (clothed but arranged for access). Include key details and where any removed clothing is located.",
                                        "defaultValue": "Neat - Properly dressed, clothes in place, presentable",
                                        "exampleValues": [
                                          "Pristine - Fully dressed, every piece in perfect position, clean and unwrinkled, appearance carefully maintained",
                                          "Disheveled - Still technically dressed but: blouse untucked and partially unbuttoned, skirt twisted and wrinkled, hair messy, clearly has been through something; clothing intact but disarrayed",
                                          "Nude - Completely naked, all clothing removed and folded on nearby chair. Wearing only collar (permanent). Body fully exposed."
                                        ]
                                      },
                                      {
                                        "name": "ActiveEffects",
                                        "type": "String",
                                        "prompt": "All active effects currently influencing the character - anything temporary that modifies their state beyond their natural baseline. Categories: PHYSICAL (restraint effects, injury effects, modifications), CHEMICAL (drugs, potions, poisons, aphrodisiacs), MAGICAL (spells, curses, enchantments, blessings), PSYCHOLOGICAL (temporary conditioning effects, triggers currently active, hypnotic suggestions, mental states). Format each as: 'Effect Name (Type) - Duration - Impact'. Duration can be: time remaining, 'Until removed', or 'Until condition met'. 'None' if no active effects. Note: PERMANENT effects belong in Development.Traits, not here.",
                                        "defaultValue": "None - No active effects, character at natural baseline",
                                        "exampleValues": [
                                          "None - No drugs, spells, or unusual effects active. MainCharacter functioning at natural baseline.",
                                          "Mild Aphrodisiac (Chemical) - ~3 hours remaining - Heightened arousal, increased genital sensitivity, mildly foggy thinking when aroused, easier to arouse; Bound arms (Physical) - Until released - Cannot use hands, limited mobility",
                                          "Heavy Aphrodisiac (Chemical) - 6 hours remaining - Uncontrollable arousal, constant wetness, can barely think past need; Orgasm Denial Curse (Magical) - Until dispelled - Cannot physically orgasm regardless of stimulation, edges painfully but release is blocked"
                                        ]
                                      },
                                      {
                                        "name": "Cultivation",
                                        "type": "Object",
                                        "prompt": "Core cultivation status - tier, capacity, advancement progress, and fusion readiness. The fundamental power system of Devoria.",
                                        "defaultValue": null,
                                        "nestedFields": [
                                          {
                                            "name": "Tier",
                                            "type": "Object",
                                            "prompt": "Current cultivation tier and stage within that tier.",
                                            "defaultValue": null,
                                            "nestedFields": [
                                              {
                                                "name": "Current",
                                                "type": "String",
                                                "prompt": "Cultivation tier (1-9) with title. Tier 1: Mundane, Tier 2: Awakened, Tier 3: Initiate, Tier 4: Adept, Tier 5: Master, Tier 6: Grandmaster, Tier 7: Archon, Tier 8: Exalted, Tier 9: Ascended.",
                                                "defaultValue": "Tier 1 - Mundane",
                                                "exampleValues": [
                                                  "Tier 1 - Mundane (no cultivation)",
                                                  "Tier 3 - Initiate",
                                                  "Tier 6 - Grandmaster",
                                                  "Tier 9 - Ascended"
                                                ]
                                              },
                                              {
                                                "name": "Stage",
                                                "type": "String",
                                                "prompt": "Progress within current tier: Early (0-25% capacity), Middle (25-60%), Late (60-85%), Peak (85-100%, ready for breakthrough). Include percentage estimate.",
                                                "defaultValue": "N/A (Mundane)",
                                                "exampleValues": [
                                                  "Early Stage (~15% capacity developed)",
                                                  "Middle Stage (~45% capacity)",
                                                  "Late Stage (~78% capacity)",
                                                  "Peak Stage (100% - at threshold, ready for fusion breakthrough)"
                                                ]
                                              },
                                              {
                                                "name": "TimeAtTier",
                                                "type": "String",
                                                "prompt": "How long since reaching current tier. Context for advancement speed.",
                                                "defaultValue": "N/A",
                                                "exampleValues": [
                                                  "3 months (recently broke through)",
                                                  "4 years (steady progress)",
                                                  "47 years (stuck at bottleneck)"
                                                ]
                                              }
                                            ]
                                          },
                                          {
                                            "name": "Capacity",
                                            "type": "Object",
                                            "prompt": "Body and Soul capacity - the twin foundations that must both reach threshold for breakthrough. Uses Capacity Training Points (CTP) system where each capacity point requires accumulating CTP through training.",
                                            "defaultValue": null,
                                            "nestedFields": [
                                              {
                                                "name": "BodyCapacity",
                                                "type": "Object",
                                                "prompt": "Physical vessel's ability to channel mana. Trained through: physical conditioning, mana circulation, tempering, body cultivation pills.",
                                                "defaultValue": null,
                                                "nestedFields": [
                                                  {
                                                    "name": "Current",
                                                    "type": "Number",
                                                    "prompt": "Current capacity points (0-100). Each point represents progress toward threshold.",
                                                    "defaultValue": "0",
                                                    "exampleValues": ["0", "45", "67", "100"]
                                                  },
                                                  {
                                                    "name": "Threshold",
                                                    "type": "Number",
                                                    "prompt": "Capacity threshold required for breakthrough. Always 100.",
                                                    "defaultValue": "100",
                                                    "exampleValues": ["100"]
                                                  },
                                                  {
                                                    "name": "CTP",
                                                    "type": "Object",
                                                    "prompt": "Capacity Training Points tracking - the XP-like system for capacity advancement.",
                                                    "defaultValue": null,
                                                    "nestedFields": [
                                                      {
                                                        "name": "Current",
                                                        "type": "Number",
                                                        "prompt": "Current CTP accumulated toward next capacity point.",
                                                        "defaultValue": "0",
                                                        "exampleValues": [0, 15, 42, 78]
                                                      },
                                                      {
                                                        "name": "PerPoint",
                                                        "type": "Number",
                                                        "prompt": "CTP required per capacity point. Scales with tier: Tier 2→3: 2.5, Tier 3→4: 5, Tier 4→5: 10, Tier 5→6: 25, Tier 6→7: 50, Tier 7→8: 150, Tier 8→9: 500.",
                                                        "defaultValue": "1",
                                                        "exampleValues": ["1", "2.5", "5", "10", "25", "50", "150", "500"]
                                                      },
                                                      {
                                                        "name": "ToNextPoint",
                                                        "type": "Number",
                                                        "prompt": "CTP remaining until next capacity point is gained.",
                                                        "defaultValue": "1",
                                                        "exampleValues": [1, 3, 8, 22]
                                                      }
                                                    ]
                                                  },
                                                  {
                                                    "name": "TrainingLog",
                                                    "type": "String",
                                                    "prompt": "Recent training sessions and CTP gains. Format: 'Session (method) - Base CTP x Multipliers = Final CTP'. Track last 3-5 significant sessions.",
                                                    "defaultValue": "No training logged yet.",
                                                    "exampleValues": [
                                                      "No training logged yet.",
                                                      "Day 47: Body tempering (quality) - 20 x 1.25 (mana-rich) = 25 CTP | Day 45: Mana circulation - 8 x 1.0 = 8 CTP",
                                                      "Day 102: Intensive tempering - 35 x 1.25 (matching bloodline) x 1.15 (overseen) = 50 CTP | Day 100: Body cultivation pill (quality) - 30 CTP"
                                                    ]
                                                  }
                                                ]
                                              },
                                              {
                                                "name": "SoulCapacity",
                                                "type": "Object",
                                                "prompt": "Spiritual core's ability to hold and regenerate mana. Trained through: meditation, visualization, mental challenges, soul pills, communion with powerful beings.",
                                                "defaultValue": null,
                                                "nestedFields": [
                                                  {
                                                    "name": "Current",
                                                    "type": "Number",
                                                    "prompt": "Current capacity points (0-100). Each point represents progress toward threshold.",
                                                    "defaultValue": "0",
                                                    "exampleValues": ["0", "45", "72", "100"]
                                                  },
                                                  {
                                                    "name": "Threshold",
                                                    "type": "Number",
                                                    "prompt": "Capacity threshold required for breakthrough. Always 100.",
                                                    "defaultValue": "100",
                                                    "exampleValues": ["100"]
                                                  },
                                                  {
                                                    "name": "CTP",
                                                    "type": "Object",
                                                    "prompt": "Capacity Training Points tracking - the XP-like system for capacity advancement.",
                                                    "defaultValue": null,
                                                    "nestedFields": [
                                                      {
                                                        "name": "Current",
                                                        "type": "Number",
                                                        "prompt": "Current CTP accumulated toward next capacity point.",
                                                        "defaultValue": "0",
                                                        "exampleValues": ["0", "12", "38", "95"]
                                                      },
                                                      {
                                                        "name": "PerPoint",
                                                        "type": "Number",
                                                        "prompt": "CTP required per capacity point. Scales with tier: Tier 2→3: 2.5, Tier 3→4: 5, Tier 4→5: 10, Tier 5→6: 25, Tier 6→7: 50, Tier 7→8: 150, Tier 8→9: 500.",
                                                        "defaultValue": "1",
                                                        "exampleValues": ["1", "2.5", "5", "10", "25", "50", "150", "500"]
                                                      },
                                                      {
                                                        "name": "ToNextPoint",
                                                        "type": "Number",
                                                        "prompt": "CTP remaining until next capacity point is gained.",
                                                        "defaultValue": "1",
                                                        "exampleValues": ["1", "4", "12", "45"]
                                                      }
                                                    ]
                                                  },
                                                  {
                                                    "name": "TrainingLog",
                                                    "type": "String",
                                                    "prompt": "Recent training sessions and CTP gains. Format: 'Session (method) - Base CTP x Multipliers = Final CTP'. Track last 3-5 significant sessions.",
                                                    "defaultValue": "No training logged yet.",
                                                    "exampleValues": [
                                                      "No training logged yet.",
                                                      "Day 47: Deep meditation (quality) - 12 x 1.25 (resonant location) = 15 CTP | Day 44: Basic meditation - 5 x 1.0 = 5 CTP",
                                                      "Day 102: Soul communion (with treasure) - 20 x 1.20 (guided) = 24 CTP | Day 99: Soul cultivation pill - 15 CTP"
                                                    ]
                                                  }
                                                ]
                                              },
                                              {
                                                "name": "Balance",
                                                "type": "String",
                                                "prompt": "Assessment of body/soul development balance. Imbalance creates problems: Body-heavy (20+ ahead): Soul strains channeling power. Soul-heavy (20+ ahead): Mana damages vessel. Severe (35+): Dangerous. Critical (50+): Life-threatening.",
                                                "defaultValue": "N/A",
                                                "exampleValues": [
                                                  "Balanced - Body and soul developing evenly (within 15 points)",
                                                  "Slightly Body-heavy (Body 78, Soul 65) - Minor imbalance, prioritize soul training",
                                                  "Soul-heavy (Body 45, Soul 89) - Dangerous imbalance, mana strains body, needs tempering urgently",
                                                  "Critical imbalance (Body 30, Soul 85) - Life-threatening, body rejecting mana flow"
                                                ]
                                              },
                                              {
                                                "name": "ThresholdStatus",
                                                "type": "String",
                                                "prompt": "Overall readiness for next breakthrough. Requires BOTH capacities at 100.",
                                                "defaultValue": "Not at threshold",
                                                "exampleValues": [
                                                  "Not at threshold - Still developing (Body 67%, Soul 72%)",
                                                  "Approaching threshold - Close (Body 94%, Soul 97%)",
                                                  "AT THRESHOLD - Ready for fusion breakthrough, both capacities maxed",
                                                  "Stuck at threshold - Maxed for 3 years, cannot find appropriate fusion creature"
                                                ]
                                              }
                                            ]
                                          },
                                          {
                                            "name": "TalentAssessment",
                                            "type": "Object",
                                            "prompt": "Evaluation of cultivation potential and natural limits.",
                                            "defaultValue": null,
                                            "nestedFields": [
                                              {
                                                "name": "NaturalTalent",
                                                "type": "String",
                                                "prompt": "Innate cultivation aptitude. Affects CTP gain multiplier and potential ceiling. Most people are Average. Exceptional talent is rare.",
                                                "defaultValue": "Average",
                                                "exampleValues": [
                                                  "Below Average - x0.7-0.8 CTP gain, likely low ceiling",
                                                  "Average - x1.0 CTP gain, standard advancement with proper resources",
                                                  "Above Average - x1.15-1.25 CTP gain, noticeably faster progress",
                                                  "Exceptional - x1.3-1.5 CTP gain, rapid advancement, attracts sect interest",
                                                  "Prodigious - x1.5-2.0 CTP gain, one-in-a-generation talent"
                                                ]
                                              },
                                              {
                                                "name": "EstimatedCeiling",
                                                "type": "String",
                                                "prompt": "Best assessment of maximum achievable tier based on talent, resources, and current progress. This can be wrong - ceilings are discovered by hitting them.",
                                                "defaultValue": "Unknown",
                                                "exampleValues": [
                                                  "Tier 3-4 (Limited talent, likely plateaus as Adept)",
                                                  "Tier 5-6 (Good talent, could reach Grandmaster with resources)",
                                                  "Tier 7+ (Exceptional, true ceiling unknown, Archon achievable)",
                                                  "Unknown (Too early to assess accurately)"
                                                ]
                                              },
                                              {
                                                "name": "SpecialFactors",
                                                "type": "String",
                                                "prompt": "Anything unusual affecting cultivation potential - inherited bloodline echoes, blessings, curses, damaged foundation, etc. Include any CTP multiplier effects.",
                                                "defaultValue": "None",
                                                "exampleValues": [
                                                  "None - Standard cultivation potential",
                                                  "Bloodline echoes: Parents both cat-blooded Tier 4, born with latent affinity (feline fusions +25% success, feline-related CTP x1.25)",
                                                  "Damaged foundation: Survived failed fusion at Tier 3, cultivation scarred, CTP gains x0.8 permanently",
                                                  "Phoenix blessing: Received blessing age 12, fire-related CTP x1.3, grants fire resistance"
                                                ]
                                              }
                                            ]
                                          }
                                        ]
                                      },
                                      {
                                        "name": "MagicAndAbilities",
                                        "type": "Object",
                                        "prompt": "Magical capabilities - mana pool, affinities, instinctive abilities from fusion, and learned techniques.",
                                        "defaultValue": null,
                                        "nestedFields": [
                                          {
                                            "name": "ManaPool",
                                            "type": "Object",
                                            "prompt": "Magical energy reservoir - capacity and current state.",
                                            "defaultValue": null,
                                            "nestedFields": [
                                              {
                                                "name": "Maximum",
                                                "type": "Number",
                                                "prompt": "Maximum mana capacity. Scales dramatically with tier - not linear but qualitative jumps. Tier 1: 0, Tier 2: ~50, Tier 3: ~150, Tier 4: ~400, Tier 5: ~1000, Tier 6: ~3000, Tier 7: ~10000, Tier 8: ~50000, Tier 9: Effectively unlimited.",
                                                "defaultValue": "0",
                                                "exampleValues": [0, 50, 150, 400, 1000, 3000, 10000]
                                              },
                                              {
                                                "name": "Current",
                                                "type": "Number",
                                                "prompt": "Current mana available.",
                                                "defaultValue": "0",
                                                "exampleValues": [0, 45, 180, 380]
                                              },
                                              {
                                                "name": "RegenerationRate",
                                                "type": "String",
                                                "prompt": "How fast mana regenerates under different conditions. Base: ~10% max/hour resting. Modified by: activity level, environment, meditation, pills.",
                                                "defaultValue": "N/A",
                                                "exampleValues": [
                                                  "N/A (No mana)",
                                                  "~5/hour resting, ~2/hour active, ~10/hour meditating (Tier 2)",
                                                  "~40/hour resting, ~15/hour active, ~80/hour meditating, doubled in sect cultivation chamber (Tier 4)"
                                                ]
                                              },
                                              {
                                                "name": "ExhaustionState",
                                                "type": "String",
                                                "prompt": "Current exhaustion effects from mana depletion. Thresholds: Above 25%: No effect. Below 25%: Minor strain (+25% spell costs, mild headache). Below 10%: Significant strain (+50% costs, -1 tier effective Magic, miscast risk). At 0%: Collapse (can't cast, -2 tiers Mental, severe symptoms).",
                                                "defaultValue": "None",
                                                "exampleValues": [
                                                  "None - Mana above 25%, no exhaustion effects",
                                                  "None - Non-cultivator, no mana to exhaust",
                                                  "Minor strain (at 18%) - Slight headache, spells feel more taxing, +25% costs",
                                                  "Significant strain (at 7%) - Pounding headache, can barely concentrate, complex magic impossible, miscast risk",
                                                  "CRITICAL (at 0%) - Cannot cast anything, migraine, body shutting down, near collapse"
                                                ]
                                              }
                                            ]
                                          },
                                          {
                                            "name": "Affinities",
                                            "type": "Object",
                                            "prompt": "Magical affinities - types of magic that come easily or with difficulty. Shaped by fusion and practice. Affects XP multipliers for related techniques.",
                                            "defaultValue": null,
                                            "nestedFields": [
                                              {
                                                "name": "Strong",
                                                "type": "String",
                                                "prompt": "Magic types that come naturally - reduced mana cost, better effects, easier learning. +25% XP for related techniques. Usually from fusion.",
                                                "defaultValue": "None",
                                                "exampleValues": [
                                                  "None - No strong affinities",
                                                  "Fire (draconic fusion) +25% XP, Shadow (natural talent) +25% XP",
                                                  "Stealth, Illusion (vulpine bloodline grants both) +25% XP each",
                                                  "Lightning, Fear effects (storm dragon lineage) +25% XP each"
                                                ]
                                              },
                                              {
                                                "name": "Moderate",
                                                "type": "String",
                                                "prompt": "Magic types with neither bonus nor penalty - standard XP gain and effectiveness.",
                                                "defaultValue": "Standard",
                                                "exampleValues": [
                                                  "Most magic types (no fusion modifiers)",
                                                  "Body Enhancement, Basic Elemental (training offsets no natural affinity)",
                                                  "Mental Arts (some natural talent, no fusion support)"
                                                ]
                                              },
                                              {
                                                "name": "Weak",
                                                "type": "String",
                                                "prompt": "Magic types that are difficult - increased cost, weaker effects, -25% XP gain. Often opposing elements.",
                                                "defaultValue": "None",
                                                "exampleValues": [
                                                  "None - No particular weaknesses",
                                                  "Water, Ice (opposing fire affinity) -25% XP",
                                                  "Light magic (shadow affinity conflicts) -25% XP",
                                                  "Healing (draconic bloodlines are poor healers) -25% XP"
                                                ]
                                              },
                                              {
                                                "name": "Null",
                                                "type": "String",
                                                "prompt": "Magic types completely inaccessible - cannot learn or use regardless of effort. No XP can be gained. Rare but significant.",
                                                "defaultValue": "None",
                                                "exampleValues": [
                                                  "None - Can theoretically learn any magic",
                                                  "ALL (Magically null trait - cannot use any magic)",
                                                  "Holy/Sacred magic (demonic bloodline incompatible)",
                                                  "Necromancy (phoenix blessing actively prevents)"
                                                ]
                                              }
                                            ]
                                          },
                                          {
                                            "name": "InstinctiveAbilities",
                                            "type": "ForEachObject",
                                            "prompt": "Abilities that came naturally from fusion - used instinctively like breathing, not learned. Still progress through proficiency levels with use.",
                                            "defaultValue": null,
                                            "nestedFields": [
                                              {
                                                "name": "AbilityName",
                                                "type": "String",
                                                "prompt": "Name of the instinctive ability.",
                                                "defaultValue": "Unnamed Ability",
                                                "exampleValues": [
                                                  "Night Vision",
                                                  "Fire Breath",
                                                  "Venom Production",
                                                  "Pack Bond Sense"
                                                ]
                                              },
                                              {
                                                "name": "Source",
                                                "type": "String",
                                                "prompt": "Which fusion granted this ability.",
                                                "defaultValue": "Unknown",
                                                "exampleValues": [
                                                  "Tier 2 Shadow Cat fusion",
                                                  "Tier 4 Fire Drake fusion",
                                                  "Tier 3 Viper fusion",
                                                  "Tier 2 Wolf fusion"
                                                ]
                                              },
                                              {
                                                "name": "Description",
                                                "type": "String",
                                                "prompt": "What the ability does - effect, range, limitations, scaling with proficiency.",
                                                "defaultValue": "Effect not defined",
                                                "exampleValues": [
                                                  "Can see clearly in near-total darkness. Eyes reflect light. Passive, always active, no mana cost. Higher proficiency extends range and detail in minimal light.",
                                                  "Can exhale a cone of fire 15ft long. Deals significant burn damage. Costs ~30 mana, 3-second cooldown while throat recovers. Natural resistance to own flames. Proficiency increases range, damage, and reduces cooldown.",
                                                  "Produce paralytic venom in fangs. Injection causes progressive paralysis over ~30 seconds. Can consciously control whether to inject when biting. Venom replenishes ~1 dose per 4 hours. Higher proficiency = faster paralysis, more potent venom."
                                                ]
                                              },
                                              {
                                                "name": "Proficiency",
                                                "type": "String",
                                                "prompt": "Current proficiency level. Levels: Untrained (0), Novice (50), Amateur (150), Competent (400), Proficient (900), Expert (1900), Master (4400), Grandmaster (9400). Instinctive abilities typically start at Novice when first gained from fusion.",
                                                "defaultValue": "Novice",
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
                                                "name": "XP",
                                                "type": "Object",
                                                "prompt": "Experience tracking toward next proficiency level.",
                                                "defaultValue": null,
                                                "nestedFields": [
                                                  {
                                                    "name": "Current",
                                                    "type": "Number",
                                                    "prompt": "Total accumulated XP. Thresholds: Novice(50), Amateur(150), Competent(400), Proficient(900), Expert(1900), Master(4400), Grandmaster(9400).",
                                                    "defaultValue": 50,
                                                    "exampleValues": [0, 50, 127, 523, 1850, 5200, 11240]
                                                  },
                                                  {
                                                    "name": "NextThreshold",
                                                    "type": "Number",
                                                    "prompt": "XP threshold for next proficiency level. 50→150→400→900→1900→4400→9400→MAX.",
                                                    "defaultValue": 150,
                                                    "exampleValues": [50, 150, 400, 900, 1900, 4400, 9400, -1]
                                                  },
                                                  {
                                                    "name": "ToNext",
                                                    "type": "Number",
                                                    "prompt": "XP remaining until next proficiency level. -1 if at Grandmaster (max level).",
                                                    "defaultValue": "100",
                                                    "exampleValues": [50, 23, 377, 50, 2550, -1]
                                                  }
                                                ]
                                              },
                                              {
                                                "name": "ChallengeFloor",
                                                "type": "String",
                                                "prompt": "Minimum difficulty of tasks that grant meaningful XP - equals current Proficiency level. Tasks BELOW floor grant only 0-10% XP. Tasks AT floor grant 100% XP. Tasks ABOVE floor grant 150-200% XP.",
                                                "defaultValue": "Novice (basic use counts, need real application for full XP)",
                                                "exampleValues": [
                                                  "Untrained (any attempt grants full XP)",
                                                  "Novice (basic use grants minimal XP; need real application or difficult situations)",
                                                  "Competent (routine use grants minimal XP; need genuine challenges, dangerous situations, or superior opponents)",
                                                  "Master (standard difficult tasks grant minimal XP; only extreme challenges, innovation, or legendary feats grant meaningful XP)"
                                                ]
                                              },
                                              {
                                                "name": "Development",
                                                "type": "String",
                                                "prompt": "Narrative tracking of how this ability has developed - key uses, breakthrough moments, and recent progress.",
                                                "defaultValue": "Newly acquired from fusion, instinctive use only.",
                                                "exampleValues": [
                                                  "Newly acquired from fusion, instinctive use only. Learning control and limits.",
                                                  "Gained at Tier 3 breakthrough. Initial clumsy bursts now more controlled after 6 months practice. Recent: Used against Competent ice mage in combat (+28 XP, above floor, high stakes).",
                                                  "Years of refinement. Can modulate intensity precisely, extend duration, and combine with other abilities. Currently at Master level, seeking Grandmaster-level challenges."
                                                ]
                                              },
                                              {
                                                "name": "RecentGains",
                                                "type": "String",
                                                "prompt": "Last 3-5 significant XP gains with calculation breakdown. Format: 'Scene/Day: +X XP (Base x Challenge x Bonuses = Total) - Context'",
                                                "defaultValue": "None yet",
                                                "exampleValues": [
                                                  "None yet",
                                                  "Day 52: +28 XP (20 x 1.0 x 1.4 = 28) - Used in combat vs Competent opponent, high stakes",
                                                  "Scene 47: +2 XP (20 x 0.10 = 2) - Routine use, below floor | Scene 52: +35 XP (20 x 1.0 x 1.75 = 35) - Combat vs Expert, high stakes + dramatic"
                                                ]
                                              }
                                            ]
                                          },
                                          {
                                            "name": "LearnedTechniques",
                                            "type": "ForEachObject",
                                            "prompt": "Techniques learned through training - sect teachings, manuals, or developed personally. Progress through standard proficiency levels.",
                                            "defaultValue": null,
                                            "nestedFields": [
                                              {
                                                "name": "TechniqueName",
                                                "type": "String",
                                                "prompt": "Name of the technique.",
                                                "defaultValue": "Unnamed Technique",
                                                "exampleValues": [
                                                  "Ember Throne Basic Fire Manipulation",
                                                  "Shadow Step",
                                                  "Iron Body Cultivation",
                                                  "Silken Web Binding Technique"
                                                ]
                                              },
                                              {
                                                "name": "Type",
                                                "type": "String",
                                                "prompt": "Category: Combat, Utility, Movement, Defense, Cultivation, Support, Forbidden, Other.",
                                                "defaultValue": "Combat",
                                                "exampleValues": [
                                                  "Combat - Offensive fire magic",
                                                  "Movement - Short-range teleportation",
                                                  "Cultivation - Body tempering method",
                                                  "Utility - Silk creation and manipulation"
                                                ]
                                              },
                                              {
                                                "name": "School",
                                                "type": "String",
                                                "prompt": "Which magical school it belongs to: Elemental, Body Cultivation, Mental Arts, Spatial, Life, Bound Magic, Bloodline, or Hybrid.",
                                                "defaultValue": "Elemental",
                                                "exampleValues": [
                                                  "Elemental (Fire)",
                                                  "Spatial (limited)",
                                                  "Body Cultivation",
                                                  "Bloodline (Arachnid)"
                                                ]
                                              },
                                              {
                                                "name": "Source",
                                                "type": "String",
                                                "prompt": "Where/how the technique was learned.",
                                                "defaultValue": "Unknown",
                                                "exampleValues": [
                                                  "Ember Throne standard curriculum, outer disciple training",
                                                  "Personal development during combat situation",
                                                  "Stolen manual, self-taught (incomplete understanding)",
                                                  "Elder reward for service, personal instruction"
                                                ]
                                              },
                                              {
                                                "name": "Description",
                                                "type": "String",
                                                "prompt": "What the technique does - effect, requirements, costs. Note how it scales with proficiency.",
                                                "defaultValue": "Effect not defined",
                                                "exampleValues": [
                                                  "Basic control over fire - create flames in palm, throw small fireballs, enhance existing fires. Mana cost: 5-20 depending on scale. Requires fire affinity or much more mana. Higher proficiency = larger flames, more control, lower costs.",
                                                  "Step through shadows to reappear up to 30ft away. Requires shadows at both points. Mana cost: 25. 5-second cooldown. Disorients briefly after use. Proficiency increases range and reduces disorientation.",
                                                  "Circulate mana to strengthen body against damage. -30% damage from physical attacks while active. Mana cost: 5/minute sustained. Cannot cast other magic while maintaining. Higher proficiency = greater reduction, lower cost."
                                                ]
                                              },
                                              {
                                                "name": "Proficiency",
                                                "type": "String",
                                                "prompt": "Current proficiency level. Levels: Untrained (0), Novice (50), Amateur (150), Competent (400), Proficient (900), Expert (1900), Master (4400), Grandmaster (9400). Learned techniques typically start at Untrained.",
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
                                                "name": "XP",
                                                "type": "Object",
                                                "prompt": "Experience tracking toward next proficiency level.",
                                                "defaultValue": null,
                                                "nestedFields": [
                                                  {
                                                    "name": "Current",
                                                    "type": "Number",
                                                    "prompt": "Total accumulated XP. Thresholds: Novice(50), Amateur(150), Competent(400), Proficient(900), Expert(1900), Master(4400), Grandmaster(9400).",
                                                    "defaultValue": "0",
                                                    "exampleValues": [0, 35, 178, 523, 1850, 5200, 11240]
                                                  },
                                                  {
                                                    "name": "NextThreshold",
                                                    "type": "Number",
                                                    "prompt": "XP threshold for next proficiency level. 50→150→400→900→1900→4400→9400→MAX.",
                                                    "defaultValue": 50,
                                                    "exampleValues": [50, 150, 400, 900, 1900, 4400, 9400, -1]
                                                  },
                                                  {
                                                    "name": "ToNext",
                                                    "type": "Number",
                                                    "prompt": "XP remaining until next proficiency level. -1 if at Grandmaster (max level).",
                                                    "defaultValue": 50,
                                                    "exampleValues": [50, 15, 222, 377, 2550, -1]
                                                  }
                                                ]
                                              },
                                              {
                                                "name": "ChallengeFloor",
                                                "type": "String",
                                                "prompt": "Minimum difficulty of tasks that grant meaningful XP - equals current Proficiency level. Tasks BELOW floor grant only 0-10% XP. Tasks AT floor grant 100% XP. Tasks ABOVE floor grant 150-200% XP.",
                                                "defaultValue": "Untrained (any practice grants full XP)",
                                                "exampleValues": [
                                                  "Untrained (any practice grants full XP, everything is challenging)",
                                                  "Novice (basic exercises grant minimal XP; need real application or difficult drills)",
                                                  "Competent (routine professional tasks grant minimal XP; need genuine challenges, novel problems, or superior opponents)",
                                                  "Master (standard difficult tasks grant minimal XP; only extreme challenges, innovation, teaching masters, or legendary feats grant meaningful XP)"
                                                ]
                                              },
                                              {
                                                "name": "Development",
                                                "type": "String",
                                                "prompt": "Narrative tracking of how this technique has developed - training history, key learning moments, teachers, and recent progress.",
                                                "defaultValue": "Newly learned technique, no development history yet.",
                                                "exampleValues": [
                                                  "Newly learned technique, no development history yet.",
                                                  "Learned from sect manual 3 months ago. Practiced daily under instructor supervision. Recently achieved Competent after breakthrough in live combat situation.",
                                                  "Personal development over 2 years of experimentation. No formal teaching but developed unique variations. At Expert level, now teaching others."
                                                ]
                                              },
                                              {
                                                "name": "RecentGains",
                                                "type": "String",
                                                "prompt": "Last 3-5 significant XP gains with calculation breakdown. Format: 'Scene/Day: +X XP (Base x Challenge x Bonuses = Total) - Context'",
                                                "defaultValue": "None yet",
                                                "exampleValues": [
                                                  "None yet",
                                                  "Day 45: +20 XP (20 x 1.0 = 20) - Standard training session at level",
                                                  "Scene 52: +56 XP (25 x 1.5 x 1.5 = 56) - Combat vs Expert, above floor, high stakes + dramatic moment"
                                                ]
                                              }
                                            ]
                                          }
                                        ]
                                      },
                                      {
                                        "name": "BloodlineInstincts",
                                        "type": "Object",
                                        "prompt": "Psychological and behavioral changes from bloodline - instincts, urges, and compulsions that came with fusion.",
                                        "defaultValue": null,
                                        "nestedFields": [
                                          {
                                            "name": "CoreInstincts",
                                            "type": "String",
                                            "prompt": "Primary instinctual drives from bloodline. These are COMPULSIONS that affect behavior, not just preferences.",
                                            "defaultValue": "None (Human baseline)",
                                            "exampleValues": [
                                              "None - Human psychology, no fusion-driven instincts",
                                              "Feline: Strong independence drive (resists authority instinctively), hunting/stalking urge (finds chasing things satisfying), territorial about personal space and possessions, grooming/cleanliness compulsion",
                                              "Canine: Pack bonding drive (intense loyalty to chosen 'pack'), hierarchy awareness (constantly assessing dominance), territorial, protective of pack members",
                                              "Draconic: Hoarding compulsion (collects valuable things, including people, feels anxiety when hoard is threatened), pride/dominance (difficulty submitting or admitting fault), possessiveness (views partners/treasures as 'mine')"
                                            ]
                                          },
                                          {
                                            "name": "HeatCycle",
                                            "type": "Object",
                                            "prompt": "Reproductive instinct cycle if applicable. Many beast bloodlines have heat cycles affecting arousal and fertility. Leave as N/A if bloodline doesn't have heats.",
                                            "defaultValue": null,
                                            "nestedFields": [
                                              {
                                                "name": "HasCycle",
                                                "type": "String",
                                                "prompt": "Whether this bloodline experiences heat cycles. Common in: Canine, Feline, Bovine, Equine, Lapine. Absent in: Draconic, Serpentine (different fertility pattern), Arachnid, Cephalopod.",
                                                "defaultValue": "No",
                                                "exampleValues": [
                                                  "No - Human baseline or bloodline without heat cycles",
                                                  "Yes - Canine heat cycle",
                                                  "Yes - Feline heat cycle",
                                                  "No - Draconic bloodlines don't have heats but have hoarding instinct for mates"
                                                ]
                                              },
                                              {
                                                "name": "CycleLength",
                                                "type": "String",
                                                "prompt": "How long between heats and how long heat lasts. Varies by bloodline.",
                                                "defaultValue": "N/A",
                                                "exampleValues": [
                                                  "N/A - No heat cycle",
                                                  "Every 3 months, heat lasts 5-7 days (Canine typical)",
                                                  "Every 2-3 weeks, heat lasts 3-5 days (Feline typical)",
                                                  "Monthly, heat lasts 2-3 days (Lapine - very frequent)"
                                                ]
                                              },
                                              {
                                                "name": "CurrentPhase",
                                                "type": "String",
                                                "prompt": "Current position in heat cycle. Format: 'Phase: [Normal/Pre-Heat/In Heat/Post-Heat] - Details'",
                                                "defaultValue": "N/A",
                                                "exampleValues": [
                                                  "N/A - No heat cycle",
                                                  "Normal - Between heats, ~6 weeks until next",
                                                  "Pre-Heat (2 days out) - Arousal increasing, becoming restless, scent changing",
                                                  "IN HEAT (Day 3 of 5) - Severely affected: constant arousal, difficulty concentrating on anything non-sexual, fertility extremely high, will seek mating aggressively",
                                                  "Post-Heat (1 day) - Heat ending, arousal returning to normal, exhausted"
                                                ]
                                              },
                                              {
                                                "name": "HeatSymptoms",
                                                "type": "String",
                                                "prompt": "How heat manifests - physical and psychological symptoms during heat.",
                                                "defaultValue": "N/A",
                                                "exampleValues": [
                                                  "N/A - No heat cycle",
                                                  "Canine heat: Intense arousal spike, vaginal swelling, strong scent changes (musk detectable by other cultivators), overwhelming urge to be mounted/bred, difficulty refusing advances, heightened emotional state, nesting behavior",
                                                  "Feline heat: Constant arousal, 'presenting' behavior (unconsciously arching back, raising hips), yowling urges (vocalizes more), rolling/rubbing against surfaces, will approach anyone even slightly attractive, extremely fertile",
                                                  "Lapine heat: Nearly constant, very short cycle, arousal becomes unbearable without relief, will mate with almost anyone, extreme fertility, multiple orgasms easily triggered"
                                                ]
                                              },
                                              {
                                                "name": "HeatManagement",
                                                "type": "String",
                                                "prompt": "How heat is currently being managed - suppressed, indulged, partner arrangements, etc.",
                                                "defaultValue": "N/A",
                                                "exampleValues": [
                                                  "N/A - No heat cycle",
                                                  "Unmanaged - No suppressants or arrangements, must endure or seek relief independently",
                                                  "Sect-managed - Pack Covenant assigns partners during heat, scheduled matings with designated disciples",
                                                  "Suppressed - Taking heat suppression pills (expensive, uncomfortable side effects, can't use long-term)",
                                                  "Owner-controlled - Master decides when and if she gets relief during heat, often used as control mechanism"
                                                ]
                                              }
                                            ]
                                          },
                                          {
                                            "name": "BondingInstinct",
                                            "type": "String",
                                            "prompt": "Bloodline-driven bonding patterns - how the bloodline affects attachment and loyalty. Many bloodlines form bonds differently than humans.",
                                            "defaultValue": "Human baseline - Normal human attachment patterns",
                                            "exampleValues": [
                                              "Human baseline - Standard human attachment, no instinct-driven bonds",
                                              "Canine Pack-Bond - Forms intense loyalty to chosen 'pack' (can be chosen or forced), pack bonds feel like family even if not related, instinctively prioritizes pack safety, experiences distress when separated from pack",
                                              "Feline Independent - Resists permanent bonds, prefers transactional relationships, can form deep attachments but they feel like choice not compulsion",
                                              "Draconic Hoarding - Views partners as possessions in hoard, intense possessiveness, genuinely experiences anxiety when 'hoard members' are threatened or interact with others, bonds are permanent unless deliberately broken",
                                              "Demonic Feeding Bond - Forms connection with regular feeding partners, not emotional but magical/physical dependency, can sense regular partners"
                                            ]
                                          },
                                          {
                                            "name": "SexualInstincts",
                                            "type": "String",
                                            "prompt": "Bloodline-specific sexual drives and behaviors beyond basic arousal. Many bloodlines have distinctive mating patterns.",
                                            "defaultValue": "Human baseline",
                                            "exampleValues": [
                                              "Human baseline - No bloodline-driven sexual instincts",
                                              "Canine: Knotting urge (during climax, desire to 'tie' with partner and remain connected), mating displays (presenting, posturing), scent-marking partners",
                                              "Feline: Play before mating (toying, chasing), multiple partners acceptable (no pair-bonding instinct), loud vocalizations during sex, nape-biting triggers submission",
                                              "Draconic: Display dominance during sex, extended mating (stamina far beyond human), afterglow hoarding behavior (wants partner close after), can't share partners easily",
                                              "Serpentine: Constriction during mating (wraps around partner), tongue-based stimulation instinct, temperature-seeking (drawn to warmth), can be sexually aggressive predators",
                                              "Demonic: Feeding through sex (gains energy/power from partner's pleasure), addictive touch (produces chemicals that create dependency), instinctive seduction"
                                            ]
                                          }
                                        ]
                                      },
                                      {
                                        "name": "CurrentState",
                                        "type": "Object",
                                        "prompt": "All temporary and frequently changing aspects - immediate physical condition, needs, appearance, and ongoing effects. Updated constantly during scenes.",
                                        "defaultValue": null,
                                        "nestedFields": [
                                          {
                                            "name": "Vitals",
                                            "type": "Object",
                                            "prompt": "Core physical and mental condition - health, pain, energy, and psychological state.",
                                            "defaultValue": null,
                                            "nestedFields": [
                                              {
                                                "name": "Health",
                                                "type": "String",
                                                "prompt": "Physical health status - injuries, wounds, illness. Higher-tier cultivators are harder to injure and heal faster. Separate from fatigue and pain.",
                                                "defaultValue": "Healthy - No injuries or illness",
                                                "exampleValues": [
                                                  "Healthy - No injuries, body in good condition",
                                                  "Minor Injuries - Bruising on thighs, small cut on lip (healing rapidly due to Tier 4 constitution)",
                                                  "Moderate Injuries - Deep claw marks across back (Tier 5 opponent), cracked ribs, significant blood loss - healing at enhanced rate but needs rest",
                                                  "Severe Injuries - Life-threatening to mundane, recovering due to Tier 6 regeneration - internal damage healing, external wounds closing"
                                                ]
                                              },
                                              {
                                                "name": "Pain",
                                                "type": "String",
                                                "prompt": "Current pain level 0-10 with description. Higher-tier cultivators have better pain tolerance but still feel it. Include source.",
                                                "defaultValue": "0/10 - No pain",
                                                "exampleValues": [
                                                  "0/10 - No pain, comfortable",
                                                  "3/10 - Mild ache from yesterday's training, easily ignored",
                                                  "6/10 - Significant pain from fresh whip marks, affecting concentration but manageable with Tier 3 tolerance",
                                                  "9/10 - Severe pain from ongoing torture, even with trained pain tolerance struggling to function"
                                                ]
                                              },
                                              {
                                                "name": "Fatigue",
                                                "type": "String",
                                                "prompt": "Energy level 0-10. 0=Fully rested, 10=Collapse. Higher tiers have better stamina but can still tire. Include cause and symptoms.",
                                                "defaultValue": "0/10 (Fully rested)",
                                                "exampleValues": [
                                                  "0/10 - Fully rested after good sleep, energized",
                                                  "4/10 - Moderate fatigue from full day of training, functioning fine",
                                                  "7/10 - Exhausted from extended combat, muscles heavy, reaction time slowed, needs rest soon",
                                                  "10/10 - Complete exhaustion, body shutting down, will collapse if pushed further"
                                                ]
                                              },
                                              {
                                                "name": "Mental",
                                                "type": "String",
                                                "prompt": "Psychological state - emotional condition, mental clarity, stress level, any altered states.",
                                                "defaultValue": "Clear and stable",
                                                "exampleValues": [
                                                  "Clear and stable - Alert, calm, thinking clearly",
                                                  "Anxious - Worried about upcoming fusion ritual, slightly scattered thoughts but functional",
                                                  "Arousal fog - Heat cycle day 2, difficulty thinking about anything non-sexual, making poor decisions",
                                                  "Broken subspace - Deep in submission after extended scene, nonverbal, completely passive, needs aftercare"
                                                ]
                                              }
                                            ]
                                          },
                                          {
                                            "name": "Needs",
                                            "type": "Object",
                                            "prompt": "Physical and physiological needs - arousal, hunger, thirst, bladder, heat status if applicable.",
                                            "defaultValue": null,
                                            "nestedFields": [
                                              {
                                                "name": "Arousal",
                                                "type": "String",
                                                "prompt": "Sexual arousal level 0-10 with MANDATORY physical details. Include: genital response (swelling, wetness/hardness), nipple state, flushing, breathing, additional signs for transformed anatomy (tail behavior, ear position, etc.).",
                                                "defaultValue": "0/10 (Dormant) - No arousal",
                                                "exampleValues": [
                                                  "0/10 (Dormant) - No arousal, genitals at rest, tail relaxed, ears neutral",
                                                  "5/10 (Aroused) - Clit swelling, noticeably wet, nipples hardening, ears rotating forward with interest, tail swaying slowly",
                                                  "8/10 (Highly Aroused) - Very wet (can feel it on thighs), clit throbbing visibly, nipples aching and erect, breathing heavy, tail lashing, ears flat back with intensity, struggling to focus",
                                                  "10/10 (Overwhelmed/Heat-Driven) - Soaking wet and dripping, entire genital area swollen and flushed, nipples painfully sensitive, panting, trembling, tail rigid then thrashing, ears pinned back, IN HEAT - cannot think past the need to be fucked and bred"
                                                ]
                                              },
                                              {
                                                "name": "Hunger",
                                                "type": "String",
                                                "prompt": "Food need 0-10 with time context. Higher tiers need less food but still need some.",
                                                "defaultValue": "2/10 (Satisfied)",
                                                "exampleValues": [
                                                  "1/10 - Ate recently, no hunger",
                                                  "5/10 - Last meal 10 hours ago, stomach growling, would like food",
                                                  "8/10 - No food for 2 days, significant weakness, lightheaded"
                                                ]
                                              },
                                              {
                                                "name": "Thirst",
                                                "type": "String",
                                                "prompt": "Hydration need 0-10 with time context.",
                                                "defaultValue": "1/10 (Hydrated)",
                                                "exampleValues": [
                                                  "0/10 - Just drank, fully hydrated",
                                                  "5/10 - Several hours since water, throat dry",
                                                  "8/10 - Dehydrated, headache, lips cracked"
                                                ]
                                              },
                                              {
                                                "name": "Bladder",
                                                "type": "String",
                                                "prompt": "Bladder pressure 0-10. Include urgency and physical signs.",
                                                "defaultValue": "1/10 (Empty)",
                                                "exampleValues": [
                                                  "1/10 - Recently relieved, no pressure",
                                                  "5/10 - Noticeable fullness, would use bathroom if available",
                                                  "9/10 - Desperate, physically uncomfortable, struggling to hold, occasional leakage"
                                                ]
                                              },
                                              {
                                                "name": "Bowel",
                                                "type": "String",
                                                "prompt": "Bowel pressure 0-10. Include urgency and physical signs.",
                                                "defaultValue": "1/10 (Empty)",
                                                "exampleValues": [
                                                  "1/10 - No pressure",
                                                  "5/10 - Awareness of need, not urgent",
                                                  "8/10 - Strong cramps, needs to go soon"
                                                ]
                                              },
                                              {
                                                "name": "HeatNeed",
                                                "type": "String",
                                                "prompt": "For characters with heat cycles - the specific breeding/mating urge level during heat. Separate from regular arousal. 'N/A' if not in heat or no heat cycle.",
                                                "defaultValue": "N/A",
                                                "exampleValues": [
                                                  "N/A - Not in heat / No heat cycle",
                                                  "Low - Heat starting, elevated urge but controllable",
                                                  "Moderate - Heat peak approaching, constant intrusive thoughts about mating, scent attracting attention",
                                                  "Severe - Deep heat, physically painful to not be bred, will present to almost anyone, rational thought severely impaired",
                                                  "Desperate - Extended heat without relief, will do anything to get fucked, begging and presenting constantly"
                                                ]
                                              }
                                            ]
                                          },
                                          {
                                            "name": "InternalState",
                                            "type": "Object",
                                            "prompt": "Internal body state - what's inside body cavities, any internal pressures or fullness.",
                                            "defaultValue": null,
                                            "nestedFields": [
                                              {
                                                "name": "WombState",
                                                "type": "String",
                                                "prompt": "Womb contents and state. Track: current contents (semen, contraceptive, nothing), amount/fullness, sensation, whether plugged/sealed.",
                                                "defaultValue": "Empty - Normal, unfilled",
                                                "exampleValues": [
                                                  "Empty - Normal state, nothing inside",
                                                  "Contains semen (~150ml, two loads) - Warm fullness behind pubic bone, sealed by plug, sloshing with movement",
                                                  "Heavily filled (~500ml+, multiple partners) - Significant pressure, visible bloating of lower belly, cramping slightly, plugged to retain",
                                                  "Pregnant - See Reproduction section for details"
                                                ]
                                              },
                                              {
                                                "name": "StomachState",
                                                "type": "String",
                                                "prompt": "Stomach contents and state - food, drink, other substances.",
                                                "defaultValue": "Normal - Comfortable from recent meal",
                                                "exampleValues": [
                                                  "Empty - Haven't eaten today, hollow feeling",
                                                  "Normal - Comfortable from recent meal",
                                                  "Full - Large meal just consumed, feeling heavy and satisfied",
                                                  "Contains (other) - Forced to swallow X, feeling nauseous"
                                                ]
                                              },
                                              {
                                                "name": "BowelState",
                                                "type": "String",
                                                "prompt": "Bowel contents if relevant - enemas, plugged, etc.",
                                                "defaultValue": "Normal",
                                                "exampleValues": [
                                                  "Normal - Nothing unusual",
                                                  "Contains enema (1L warm water) - Cramping, pressure, fighting urge to release, plugged",
                                                  "Recently cleaned - Just evacuated preparation enema, empty and clean"
                                                ]
                                              }
                                            ]
                                          },
                                          {
                                            "name": "Appearance",
                                            "type": "Object",
                                            "prompt": "Current visual and sensory presentation - how the character looks, sounds, and smells RIGHT NOW. These fields track current state and condition, which changes based on activities and circumstances.",
                                            "defaultValue": null,
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
                                                "prompt": "Comprehensive facial features and current expression - structure and current state. Include: face shape, eye color/shape/current state, eyebrow shape, nose, lips (fullness, natural color, current state), skin complexion and condition, and current expression. Update current state based on scene: tears, flushing, swelling from slaps, fluids on face, etc.",
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
                                                  "Ruined heavy makeup - Was: thick black eyeliner, heavy mascara, deep red lipstick, blush. Now: eyeliner smeared across temples from tears, mascara running in black streaks down both cheeks, lipstick completely worn off, foundation streaked with sweat and tears; thoroughly wrecked appearance"
                                                ]
                                              },
                                              {
                                                "name": "Scent",
                                                "type": "String",
                                                "prompt": "What the character currently smells like - natural body odor, applied scents, and accumulated smells from activities. Layer scents from underlying (skin) to surface (recent additions). Include: baseline cleanliness, any perfume/soap, sweat level, arousal musk, sex smells (cum, fluids), and any other relevant odors. Update based on: time since bathing, physical exertion, sexual activity, environmental exposure. Scent can be an important sensory detail for scenes.",
                                                "defaultValue": "Clean - Fresh soap scent, natural neutral skin smell, no strong odors",
                                                "exampleValues": [
                                                  "Clean and fresh - Bathed this morning with lavender soap, faint floral scent lingers on skin, no body odor, no sweat; pleasant neutral smell",
                                                  "Aroused musk - Clean underneath but several hours since bathing, light natural body scent, strong arousal musk emanating from between legs, light sweat sheen adding salt note; smells like an aroused woman",
                                                  "Thoroughly used - Hasn't bathed in 2 days, underlying stale sweat and body odor, layered with heavy sex smell: dried and fresh cum, her own arousal fluids coating thighs, dried saliva, fresh sweat from exertion; overwhelmingly smells of sex and use"
                                                ]
                                              },
                                              {
                                                "name": "Voice",
                                                "type": "String",
                                                "prompt": "Current state of character's voice and ability to vocalize - natural qualities and current condition. Describe: natural voice (pitch, tone, quality), current condition (clear, hoarse, strained), and any impairments.",
                                                "defaultValue": "Clear - Soft feminine voice, unimpaired, speaks easily",
                                                "exampleValues": [
                                                  "Clear and steady - Natural alto voice, pleasant tone, completely unimpaired; speaks clearly and confidently",
                                                  "Strained and thick - Naturally soft voice, currently thick from recent crying, slight wobble when speaking, occasional catch in throat from suppressed sobs; understandable but obviously distressed",
                                                  "Wrecked - Voice destroyed from combination of screaming and rough throat use; currently barely above hoarse whisper, raw pain when swallowing or attempting to speak, words come out as rough croaks; will need days to recover"
                                                ]
                                              }
                                            ]
                                          },
                                          {
                                            "name": "Body",
                                            "type": "Object",
                                            "prompt": "Detailed physical anatomy of the character's body - structure, features, and current condition of each body region.",
                                            "defaultValue": null,
                                            "nestedFields": [
                                              {
                                                "name": "Mouth",
                                                "type": "String",
                                                "prompt": "Detailed oral anatomy focused on sexual use capacity. Include: lip function, teeth condition, tongue details, jaw strength and current state, gag reflex status, throat depth capacity, and current oral condition.",
                                                "defaultValue": "Healthy mouth - average tongue, strong gag reflex, untrained throat, jaw comfortable",
                                                "exampleValues": [
                                                  "Inexperienced mouth - All teeth present and healthy, average pink tongue, strong gag reflex triggering at 3 inches depth, throat untrained and tight, never taken anything deep; jaw currently comfortable, no fatigue",
                                                  "Trained oral - Teeth intact, longer than average tongue (skilled from practice), gag reflex weakened through training (triggers at 5-6 inches), throat can accommodate average cock to root with effort; jaw well-conditioned for extended use, currently mild ache from earlier session",
                                                  "Extensively broken in - Teeth intact (carefully preserved), long dexterous tongue, gag reflex completely eliminated through months of training, throat permanently loosened and can take any size without resistance; jaw currently aching badly and clicking (locked in ring gag for 3 hours), throat raw and scratched from rough use"
                                                ]
                                              },
                                              {
                                                "name": "Breasts",
                                                "type": "String",
                                                "prompt": "Complete breast anatomy including chest, nipples, and lactation status in one field. CHEST: size (cup size AND descriptive), shape (perky, teardrop, round, pendulous), weight/heaviness, firmness (soft, firm, augmented), natural behavior (self-supporting, need support, bounce/sway patterns), vein visibility, current state (natural, swollen, marked, bound). NIPPLES: areola size (use coin comparisons: dime, quarter, silver dollar), areola color and texture, nipple size/shape (small, medium, large/long, puffy, flat, inverted), nipple color, current state (soft, hardening, fully erect, overstimulated), any modifications (piercings) or damage. LACTATION: production status (non-lactating, producing), volume if lactating, time since last expression, current fullness (empty/comfortable/full/engorged), milk characteristics, let-down triggers. For male characters, describe pectoral development instead.",
                                                "defaultValue": "Moderate B-cups, perky and self-supporting, soft with gentle natural bounce. Unmarked. Quarter-sized light pink areolae, small button nipples, currently soft. Non-lactating.",
                                                "exampleValues": [
                                                  "Small A-cups, barely-there gentle swells against ribcage, very firm, minimal movement even during activity. Smooth and unmarked. Small dime-sized pale pink areolae, nearly smooth texture. Tiny nipples that lay almost flat when soft, rise to small firm points when erect. Currently soft and unobtrusive. Non-lactating.",
                                                  "Full natural D-cups, classic teardrop shape with more fullness at bottom, heavy enough to require support, significant sway when walking. Soft and yielding, faint blue veins visible under fair skin when aroused. Currently unmarked. Silver-dollar sized medium pink areolae with visible bumpy Montgomery glands. Puffy nipples - areola and nipple form soft cone shape when relaxed, tips push out prominently when erect. Currently fully erect from arousal. Non-lactating.",
                                                  "Massive G-cups, heavy and pendulous, hang to navel when unsupported, impossible to ignore. Very soft, almost fluid movement. Currently bound in rope harness squeezing into tight swollen globes. Large dark brown areolae with pronounced bumpy texture. Long thick nipples (~1 inch when erect). Pierced: thick gauge steel barbells through each. Currently clamped with clover clamps. Heavy production - 8+ pints/day, not milked in 14 hours (punishment), breasts painfully engorged."
                                                ]
                                              },
                                              {
                                                "name": "Stomach",
                                                "type": "String",
                                                "prompt": "Combined midriff appearance covering both baseline anatomy and any current distension. BASELINE: muscle definition (none, slight, toned, defined abs), natural shape (flat, slight curve, rounded), softness (firm, soft, very soft), navel type and appearance (innie depth, outie), skin texture. CURRENT DISTENSION (if any): size change from baseline (slight bulge, noticeable swelling, severe distension), skin state (soft, taut, drum-tight, shiny), visible effects (veins, movement inside, navel changes), and comparison (food baby, looks pregnant, etc.). If normal/not distended, focus on baseline description and state 'no current distension.'",
                                                "defaultValue": "Flat stomach with slight natural softness, no visible muscle. Shallow innie navel. Smooth pale skin. No current distension.",
                                                "exampleValues": [
                                                  "Tightly toned stomach with visible four-pack definition, very firm to touch, minimal body fat. Deep innie navel. Smooth tanned skin. Athletic core from training. No current distension.",
                                                  "Soft flat stomach with gentle feminine curve, no muscle definition, pleasant give when pressed. Small round innie navel. Creamy smooth skin, sensitive to tickling. Currently showing moderate bulge - visible rounded swelling of lower belly, skin taut over the bump, looks like early pregnancy or having eaten large meal. Navel slightly stretched. Gentle sloshing movement visible when she shifts position.",
                                                  "Soft rounded belly with visible pooch below navel, very soft and squeezable. Shallow navel. Pale skin with faint stretch marks on sides. Currently severely distended - belly swollen massively, skin drum-tight and shiny, veins visible through stretched skin, navel completely flat and almost popping outward. Looks like full-term pregnancy but rounder. Visible churning/movement inside. MainCharacter cannot bend at waist."
                                                ]
                                              },
                                              {
                                                "name": "Genitalia",
                                                "type": "String",
                                                "prompt": "Complete genital anatomy AND current secretion/fluid status in one field. ANATOMY - FOR VULVAS: Mons pubis (fullness, padding), labia majora (puffy, flat, thin, full), labia minora (length, inner/outer visibility, color, texture), clitoris (size, hood coverage, exposure), vaginal opening (observed tightness/looseness, gape when relaxed). FOR PENISES: Length (soft AND erect), girth, shape, vein prominence, glans details, foreskin status, scrotum. Current anatomical state: resting/aroused, used/fresh. SECRETIONS: current wetness level (dry, slightly moist, wet, soaking, dripping, gushing), natural arousal fluid (amount, consistency, color), any cum present (whose, how fresh, how much, where), other fluids (pre-cum, cervical mucus). Describe viscosity, visible evidence (dampening fabric, coating thighs, pooling beneath), and scent if relevant.",
                                                "defaultValue": "Female: Smooth mound with modest padding. Puffy outer labia concealing small pink inner labia (innie). Small clit hidden under hood. Tight vaginal entrance. Currently dry - genitals clean and dry at rest, neutral state.",
                                                "exampleValues": [
                                                  "Female (virgin anatomy): Full soft mons with slight padding. Puffy outer labia press together when standing, conceal everything when closed. Inner labia small and delicate, pale pink, completely contained within outer lips (innie). Small clit fully covered by hood, only visible when hood manually retracted. Vaginal entrance virgin-tight, hymen intact, barely admits single fingertip. Currently dry - genitals clean and dry, no natural lubrication, neutral state.",
                                                  "Female (experienced anatomy): Prominent mons, mostly smooth. Outer labia moderately full but parted, don't conceal inner anatomy. Inner labia prominent - extend 1.5 inches past outer lips when spread, darker rose-pink with slightly textured edges, visible from outside. Clit medium-sized, hood retracted showing pink nub constantly. Vaginal entrance well-used - relaxed gape of ~1cm at rest, easily accommodates three fingers. Currently wet and slick - significant natural arousal fluid, clear and slippery, coating entire vulva and beginning to dampen inner thighs. Thin consistency, strings slightly when spread. Light musky arousal scent.",
                                                  "Male (average anatomy): Soft: 3 inches, hangs over scrotum. Erect: 6.5 inches, moderate girth (5 inch circumference), slight upward curve, prominent dorsal vein. Cut, pink glans with defined ridge. Scrotum hangs loosely in warm conditions, average-sized testicles. Currently cum-soaked from recent use - fresh thick load coating shaft and glans, dripping onto scrotum, mixed with her arousal fluid."
                                                ]
                                              },
                                              {
                                                "name": "Rear",
                                                "type": "String",
                                                "prompt": "Complete rear anatomy covering buttocks AND anus in one field. BUTTOCKS: size/volume (small, medium, large, massive), shape (flat, round, heart, bubble, shelf), firmness (tight, firm, soft, very soft), movement physics (minimal jiggle, bounces, claps, ripples), how easily spread (firm/resistant vs soft/yields), cheek texture (smooth, dimpled, cellulite), current state (unmarked, reddened, bruised, welted), thigh gap presence if relevant. ANUS: external appearance - color of outer rim (pink, brown, dark), texture (puckered/knotted, smooth, wrinkled), surrounding hair if any. Muscle tone and capacity - tightness (virgin-tight, tight, normal, relaxed, loose, gaping), observed gape when relaxed, what can be accommodated. Current condition (pristine, used, red, swollen, sore, damaged). Training progress if being developed.",
                                                "defaultValue": "Modest medium rear, round shape, firm with slight softness. Light bounce when moving. Smooth unmarked skin. Tight pink rosebud anus, puckered closed, untouched and virgin-tight. Clean-shaven surrounding. Pristine condition.",
                                                "exampleValues": [
                                                  "Small tight rear, flat-ish with slight rounded curve, very firm (athletic build). Minimal jiggle even with impact, would need effort to spread cheeks. Smooth taut skin, unmarked. No thigh gap. Virginal anus - small tightly-knotted pink rosebud, puckers closed with no visible opening when relaxed, clenches reflexively at any touch. Never penetrated, would require significant stretching to accept even single finger. Smooth hairless skin surrounding. Pristine, never used.",
                                                  "Large heart-shaped ass, full and prominent, soft and squeezable with pleasant give. Noticeable sway when walking, bounces and jiggles with movement, claps during impact. Spreads easily when pulled. Smooth skin with faint cellulite dimpling on lower cheeks. Small thigh gap. Currently covered in dark bruises (2 days old) and fresh red handprints from recent spanking. Trained anus - light brown wrinkled ring, relaxes to slight visible dimple when at rest (~0.5cm). Trained with plugs to accept average-sized toys/cock with adequate lube. Sphincter conditioned to relax on command. Currently slight redness and mild soreness from plug worn earlier. Light hair on outer rim.",
                                                  "Massive shelf ass, extremely heavy and prominent, very soft and plush like pillows. Dramatic sway and bounce with every step, loud clapping during any impact, ripples spread across flesh. Deep cleavage between cheeks. Significant cellulite on cheeks and upper thighs. Extensively used anus - dark stretched ring, permanent gape of ~2cm when relaxed, inner red/pink mucosa visible inside opening. No longer able to fully close. Can easily accept very large objects without resistance. Sphincter tone significantly reduced. Currently puffy and irritated from recent rough use, minor prolapse beginning. Hairless (kept shaved)."
                                                ]
                                              },
                                              {
                                                "name": "BodyHair",
                                                "type": "String",
                                                "prompt": "Body hair status across all regions EXCEPT head. Track: Pubic, Armpits, Legs. Include days since last grooming to track growth.",
                                                "defaultValue": "Pubic: Neatly trimmed | Armpits: Freshly shaved | Legs: Smooth (shaved yesterday) | Other: None notable",
                                                "exampleValues": [
                                                  "Pubic: Completely bare (waxed 3 days ago, still smooth) | Armpits: Freshly shaved (this morning) | Legs: Smooth (shaved this morning) | Other: No notable body hair - maintains full removal",
                                                  "Pubic: Short stubble growing back (shaved 4 days ago, scratchy to touch) | Armpits: Visible stubble (4 days) | Legs: Prickly stubble dots visible (4 days) | Other: Fine arm hair (natural) - hasn't had grooming access in captivity",
                                                  "Pubic: Full natural bush (never shaved, thick dark curls extending to inner thighs) | Armpits: Full dark tufts (natural, never shaved) | Legs: Hairy (natural, never shaved) | Other: Visible dark treasure trail from navel down - completely natural, no grooming"
                                                ]
                                              }
                                            ]
                                          }
                                        ]
                                      },
                                      {
                                        "name": "Reproduction",
                                        "type": "Object",
                                        "prompt": "Reproductive system status - fertility cycle, breeding, pregnancy. Interacts with bloodline (some much more fertile).",
                                        "defaultValue": null,
                                        "nestedFields": [
                                          {
                                            "name": "FertilityCycle",
                                            "type": "Object",
                                            "prompt": "Natural fertility cycle - modified by bloodline.",
                                            "defaultValue": null,
                                            "nestedFields": [
                                              {
                                                "name": "CycleType",
                                                "type": "String",
                                                "prompt": "What kind of fertility cycle - human menstrual, beast heat-based, or other pattern. Determined by bloodline.",
                                                "defaultValue": "Human standard menstrual cycle",
                                                "exampleValues": [
                                                  "Human standard - 28-day menstrual cycle with typical fertile window",
                                                  "Feline heat-based - No menstruation, fertile only during heat cycles (every 2-3 weeks, 3-5 days), VERY fertile during heat",
                                                  "Canine heat-based - Fertile during heat cycles (every 3 months, 5-7 days), can conceive outside heat but lower chance",
                                                  "Bovine enhanced - Menstrual cycle but highly fertile, longer fertile window, higher conception rates",
                                                  "Draconic reduced - Standard cycle but lower baseline fertility (dragons reproduce rarely)"
                                                ]
                                              },
                                              {
                                                "name": "CurrentPhase",
                                                "type": "String",
                                                "prompt": "Current position in fertility cycle with conception risk percentage.",
                                                "defaultValue": "Unknown",
                                                "exampleValues": [
                                                  "Menstrual 🩸 (Day 2) - 0% conception risk, currently bleeding",
                                                  "Follicular 🌱 (Day 8) - Low risk ~15%",
                                                  "Ovulating 🌺 (Day 14) - HIGH risk ~85% peak fertility",
                                                  "IN HEAT 🔥 (Day 2 of 5) - EXTREME FERTILITY ~95%, body optimized for conception",
                                                  "Pregnant 🤰 - Cycle suspended"
                                                ]
                                              },
                                              {
                                                "name": "FertilityModifiers",
                                                "type": "String",
                                                "prompt": "Factors affecting fertility - bloodline bonuses/penalties, contraception, fertility treatments, conditions.",
                                                "defaultValue": "None - Baseline fertility",
                                                "exampleValues": [
                                                  "None - Standard fertility for cycle type",
                                                  "Bovine Bloodline (+50% conception chance) - Highly fertile even outside peak",
                                                  "Taking contraceptive pills (-95% effective) - Protection active, must take daily",
                                                  "Draconic Bloodline (-30% conception) - Dragons reproduce rarely",
                                                  "Breeding program treatments (+25%) - Taking fertility pills to increase conception chance"
                                                ]
                                              }
                                            ]
                                          },
                                          {
                                            "name": "Pregnancy",
                                            "type": "String",
                                            "prompt": "Pregnancy status if applicable. Track: whether pregnant, days since conception, trimester, father identity, symptoms, expected delivery.",
                                            "defaultValue": "Not Pregnant",
                                            "exampleValues": [
                                              "Not Pregnant - No current pregnancy",
                                              "Possibly Pregnant - Creampied during fertile window 2 days ago, too early to confirm",
                                              "Confirmed Pregnant - 1st Trimester (Day 45) | Father: Elder Vex (certain, designated breeding) | Symptoms: Morning nausea, breast tenderness, missed period | Expected: ~Day 270",
                                              "Pregnant - 3rd Trimester (Day 250) | Father: Unknown (heat-night with multiple partners) | Symptoms: Large belly, frequent urination, preparing to deliver | Expected: ~2 weeks"
                                            ]
                                          },
                                          {
                                            "name": "BreedingStatus",
                                            "type": "String",
                                            "prompt": "For characters in breeding programs or owned for breeding purposes - their formal status and arrangements.",
                                            "defaultValue": "Not in breeding program",
                                            "exampleValues": [
                                              "Not in breeding program - Reproduction is personal matter",
                                              "Registered breeding stock (Ivory Pastures) - Required to breed twice yearly minimum, offspring belong to sect, receives premium care",
                                              "Heat-service assignment (Pack Covenant) - Designated partners during heats, may breed if it happens, no formal requirement",
                                              "Personal broodmare (Lord Halvard) - Owner's breeding slave, expected to carry his children, currently on 2nd pregnancy"
                                            ]
                                          },
                                          {
                                            "name": "ReproductiveHistory",
                                            "type": "String",
                                            "prompt": "Record of past pregnancies and children born.",
                                            "defaultValue": "No pregnancies / No children",
                                            "exampleValues": [
                                              "No pregnancies - Never been pregnant",
                                              "1 pregnancy, 1 child: Daughter (name: Sera), father: Lord Halvard, born 8 months ago, in house nursery, healthy",
                                              "3 pregnancies, 3 children: 1st son (sold at birth), 2nd daughter (kept by sect), 3rd son (current, nursing). All from breeding program."
                                            ]
                                          },
                                          {
                                            "name": "OrgasmControl",
                                            "type": "String",
                                            "prompt": "Current orgasm tracking and any control mechanisms - denial, edging, permission requirements.",
                                            "defaultValue": "Free - No orgasm control",
                                            "exampleValues": [
                                              "Free - Can orgasm whenever aroused enough, no restrictions",
                                              "Permission required - Must ask owner before cumming, last permitted: 3 days ago, edges today: 5",
                                              "Denial (Week 2) - Not permitted to orgasm, edged daily, desperation building, clit constantly throbbing",
                                              "Cursed denial - Magical curse prevents orgasm entirely, has been edged for 3 weeks, body at breaking point"
                                            ]
                                          }
                                        ]
                                      },
                                      {
                                        "name": "SocialPosition",
                                        "type": "Object",
                                        "prompt": "Place in Devoria's social structure - sect membership, freedom status, political position.",
                                        "defaultValue": null,
                                        "nestedFields": [
                                          {
                                            "name": "SectMembership",
                                            "type": "Object",
                                            "prompt": "Affiliation with major sect if any.",
                                            "defaultValue": null,
                                            "nestedFields": [
                                              {
                                                "name": "Sect",
                                                "type": "String",
                                                "prompt": "Which sect, if any. Major sects: Ember Throne (draconic), Pack Covenant (canine), Ivory Pastures (bovine), Silken Web (arachnid), Foxfire Pavilion (vulpine), The Coil (serpentine), Abyssal Tide (cephalopod), The Hive (insectoid), or minor/no sect.",
                                                "defaultValue": "Unaffiliated",
                                                "exampleValues": [
                                                  "Unaffiliated - No sect membership, independent cultivator",
                                                  "Pack Covenant - Full member",
                                                  "Ember Throne - Member (enslaved/property)",
                                                  "Foxfire Pavilion - Contracted courtesan (not full member)"
                                                ]
                                              },
                                              {
                                                "name": "Rank",
                                                "type": "String",
                                                "prompt": "Position within sect hierarchy: Outer Disciple, Inner Disciple, Core Disciple, Elder, Patriarch/Matriarch. Or non-standard positions for owned members.",
                                                "defaultValue": "N/A",
                                                "exampleValues": [
                                                  "N/A - Not in a sect",
                                                  "Outer Disciple - Lowest full member rank, probationary",
                                                  "Inner Disciple - Full member, proven value",
                                                  "Core Disciple - Elite, being groomed for leadership",
                                                  "Property of Elder Vex - Not ranked, owned by elder as personal slave"
                                                ]
                                              },
                                              {
                                                "name": "Standing",
                                                "type": "String",
                                                "prompt": "Reputation and standing within the sect.",
                                                "defaultValue": "N/A",
                                                "exampleValues": [
                                                  "N/A - Not in sect",
                                                  "Average - Unremarkable standing, neither favored nor disliked",
                                                  "Rising - Recent achievements gaining attention, being watched",
                                                  "Disgraced - Failed important mission, currently under scrutiny",
                                                  "Favored - Has powerful patron, protected position"
                                                ]
                                              },
                                              {
                                                "name": "Faction",
                                                "type": "String",
                                                "prompt": "Internal faction alignment if any.",
                                                "defaultValue": "N/A",
                                                "exampleValues": [
                                                  "N/A - Not in sect or too junior for factions",
                                                  "Elder Vex's faction - Aligned with Elder Vex's political camp",
                                                  "Neutral - Deliberately unaligned, risky position",
                                                  "Matriarch's direct - Reports directly to sect leader, above faction politics"
                                                ]
                                              }
                                            ]
                                          },
                                          {
                                            "name": "FreedomStatus",
                                            "type": "Object",
                                            "prompt": "Legal and practical freedom - from full citizen to slave.",
                                            "defaultValue": null,
                                            "nestedFields": [
                                              {
                                                "name": "Status",
                                                "type": "String",
                                                "prompt": "Legal status: Free (full rights), Contracted (time-limited service), Indentured (debt-bound), Bonded (Pack Covenant permanent bond), Enslaved (legal property).",
                                                "defaultValue": "Free",
                                                "exampleValues": [
                                                  "Free - Full legal autonomy, citizen rights, owns herself",
                                                  "Contracted - Voluntary 5-year service contract to Foxfire Pavilion, 3 years remaining, can buy out for 500 spirit stones",
                                                  "Indentured - Debt-bound to House Halvard, must work until debt repaid (~8 years at current rate), limited rights",
                                                  "Bonded - Pack Covenant permanent loyalty bond, magically bound to sect, cannot betray, treated well but not truly free",
                                                  "Enslaved - Legal property of Lord Halvard, no rights, registered with city guild, can be sold/traded"
                                                ]
                                              },
                                              {
                                                "name": "Owner",
                                                "type": "String",
                                                "prompt": "If not free, who holds their contract/bond/ownership.",
                                                "defaultValue": "N/A (Free)",
                                                "exampleValues": [
                                                  "N/A - Free, no owner",
                                                  "Foxfire Pavilion (sect) - Contract holder",
                                                  "Lord Marcus Halvard - Personal owner (slave)",
                                                  "Pack Covenant (collective) - Bonded to sect itself",
                                                  "Currently between owners - Recently sold, in transit to new owner"
                                                ]
                                              },
                                              {
                                                "name": "Circumstances",
                                                "type": "String",
                                                "prompt": "How they came to current status and key terms/conditions.",
                                                "defaultValue": "N/A",
                                                "exampleValues": [
                                                  "N/A - Born free, always been free",
                                                  "Sold by parents age 14 to pay debts, trained as pleasure slave, sold twice since",
                                                  "Captured during sect raid, enslaved as war spoils, registered property",
                                                  "Voluntarily contracted for training and protection, includes sexual service clause",
                                                  "Bonded at first fusion (Pack Covenant standard), permanent but not slavery - full member rights within pack structure"
                                                ]
                                              }
                                            ]
                                          },
                                          {
                                            "name": "Reputation",
                                            "type": "Object",
                                            "prompt": "How they're known and perceived by different groups.",
                                            "defaultValue": null,
                                            "nestedFields": [
                                              {
                                                "name": "PublicImage",
                                                "type": "String",
                                                "prompt": "General reputation - what most people know/think of them.",
                                                "defaultValue": "Unknown - No public reputation",
                                                "exampleValues": [
                                                  "Unknown - Unremarkable, no public reputation",
                                                  "Promising talent - Known as rising star in Ember Throne",
                                                  "Infamous - Known as traitor who stole sect techniques",
                                                  "Prized possession - Known as Lord Halvard's beautiful slave"
                                                ]
                                              },
                                              {
                                                "name": "SectReputations",
                                                "type": "String",
                                                "prompt": "Standing with major sects.",
                                                "defaultValue": "Neutral with all - No notable relationships",
                                                "exampleValues": [
                                                  "Neutral with all - Unknown or unremarkable to major sects",
                                                  "Ember Throne: Enemy (defector) | Pack Covenant: Favorable (assisted mission) | Others: Neutral",
                                                  "Foxfire Pavilion: Valuable asset | Silken Web: Wary (knows too many secrets) | Ember Throne: Hostile (rejected overtures)"
                                                ]
                                              }
                                            ]
                                          }
                                        ]
                                      },
                                      {
                                        "name": "Equipment",
                                        "type": "Object",
                                        "prompt": "What they're wearing, carrying, and what's on/in their body.",
                                        "defaultValue": null,
                                        "nestedFields": [
                                          {
                                            "name": "Clothing",
                                            "type": "Object",
                                            "prompt": "Current worn clothing by category.",
                                            "defaultValue": null,
                                            "nestedFields": [
                                              {
                                                "name": "Upper",
                                                "type": "String",
                                                "prompt": "Clothing on torso (not underwear).",
                                                "defaultValue": "None",
                                                "exampleValues": [
                                                  "None - Torso bare",
                                                  "Simple white cotton blouse, loose fit, fully buttoned",
                                                  "Black silk robe with Ember Throne insignia, fine quality, properly worn",
                                                  "Torn remains of training tunic, barely covering anything"
                                                ]
                                              },
                                              {
                                                "name": "Lower",
                                                "type": "String",
                                                "prompt": "Clothing on lower body (not underwear).",
                                                "defaultValue": "None",
                                                "exampleValues": [
                                                  "None - Lower body bare",
                                                  "Brown cotton skirt, knee-length, in place",
                                                  "Silk pants, flowing, black with red trim, properly worn"
                                                ]
                                              },
                                              {
                                                "name": "Underwear",
                                                "type": "String",
                                                "prompt": "ALWAYS track underwear explicitly - bra/breast support and panties/loincloth separately.",
                                                "defaultValue": "None - Not wearing underwear",
                                                "exampleValues": [
                                                  "None - Forbidden by owner, always bare underneath",
                                                  "Simple cotton breast band, white cotton panties - basic smallclothes",
                                                  "Black lace bra (custom for tail), matching thong - sexy set currently in place",
                                                  "Breast band pushed above breasts, panties pulled aside - technically wearing but not covering"
                                                ]
                                              },
                                              {
                                                "name": "Footwear",
                                                "type": "String",
                                                "prompt": "What's on feet.",
                                                "defaultValue": "Barefoot",
                                                "exampleValues": [
                                                  "Barefoot - No footwear",
                                                  "Simple sandals",
                                                  "Leather boots, practical, well-worn",
                                                  "Heeled slippers (locked on) - cannot remove without key"
                                                ]
                                              }
                                            ]
                                          },
                                          {
                                            "name": "Accessories",
                                            "type": "String",
                                            "prompt": "Jewelry, collars, cuffs, decorative items worn. Separate from restraints.",
                                            "defaultValue": "None",
                                            "exampleValues": [
                                              "None - No accessories",
                                              "Steel collar (permanent, welded) with ownership tag | Matching steel cuffs (decorative, not restraining) | Bell on collar",
                                              "Ember Throne disciple token on cord around neck | Simple gold earrings"
                                            ]
                                          },
                                          {
                                            "name": "Restraints",
                                            "type": "String",
                                            "prompt": "Active restraints currently restricting movement. 'None' if unrestrained.",
                                            "defaultValue": "None - Free movement",
                                            "exampleValues": [
                                              "None - No restraints, full freedom of movement",
                                              "Wrists bound behind back with rope | Ankles hobbled with 12-inch chain - can shuffle, cannot run",
                                              "Full bondage: Armbinder (arms behind back, elbow to fingertip), ball gag (large, drooling around it), ankle spreader bar (2ft), leash attached to collar - severely restricted"
                                            ]
                                          },
                                          {
                                            "name": "Insertions",
                                            "type": "String",
                                            "prompt": "Objects currently inside body orifices. Track by location: vaginal, anal, oral, other.",
                                            "defaultValue": "None - All orifices empty",
                                            "exampleValues": [
                                              "None - Nothing inserted",
                                              "Anal: Medium plug (keeps womb sealed after breeding) - worn 2 hours",
                                              "Vaginal: Locked chastity insert (cannot remove, prevents entry) | Anal: Training plug (size 3 of 5, stretching program)",
                                              "Vaginal: Owner's cock (currently being used) | Oral: Ring gag (forces mouth open)"
                                            ]
                                          },
                                          {
                                            "name": "Weapons",
                                            "type": "String",
                                            "prompt": "Weapons carried or equipped. 'None/Unarmed' if none.",
                                            "defaultValue": "Unarmed",
                                            "exampleValues": [
                                              "Unarmed - No weapons (slave, not permitted)",
                                              "Iron dagger, belt sheath - simple self-defense",
                                              "Steel sword (quality), belt scabbard left | Hidden knife in boot right",
                                              "Claws only - natural weapons from transformation, no manufactured weapons"
                                            ]
                                          }
                                        ]
                                      },
                                      {
                                        "name": "Economy",
                                        "type": "Object",
                                        "prompt": "Financial resources - currency, assets, and for slaves, their own market value.",
                                        "defaultValue": null,
                                        "nestedFields": [
                                          {
                                            "name": "Currency",
                                            "type": "String",
                                            "prompt": "Spirit Stones (SS) - primary currency.",
                                            "defaultValue": "0 SS",
                                            "exampleValues": [
                                              "0 SS (Slave - cannot own currency)",
                                              "45 SS - Modest funds from missions",
                                              "2,400 SS - Significant savings, comfortable",
                                              "0 SS personal (Slave) but carries owner's purse: 150 SS"
                                            ]
                                          },
                                          {
                                            "name": "Assets",
                                            "type": "String",
                                            "prompt": "Significant property, investments, owned slaves (if any). For slaves, note their own assessed market value.",
                                            "defaultValue": "None",
                                            "exampleValues": [
                                              "None - No significant assets",
                                              "Small apartment in sect quarters (provided, not owned) - no real assets",
                                              "IS PROPERTY - Own market value: ~3,500 SS (Tier 3 cat-blood, trained, fertile, young)",
                                              "Owns: Small house in city (600 SS value), one trained servant (400 SS value)"
                                            ]
                                          }
                                        ]
                                      },
                                      {
                                        "name": "SexualProfile",
                                        "type": "Object",
                                        "prompt": "Comprehensive sexual history, capabilities, preferences, and training. Explicit tracking for adult content.",
                                        "defaultValue": null,
                                        "nestedFields": [
                                          {
                                            "name": "Experience",
                                            "type": "Object",
                                            "prompt": "Sexual experience level and history.",
                                            "defaultValue": null,
                                            "nestedFields": [
                                              {
                                                "name": "Overall",
                                                "type": "String",
                                                "prompt": "General sexual experience level and background.",
                                                "defaultValue": "Inexperienced",
                                                "exampleValues": [
                                                  "Virgin - No sexual experience",
                                                  "Inexperienced - A few encounters, still learning",
                                                  "Moderate - Several partners, comfortable with common acts",
                                                  "Experienced - Many partners, skilled and knowledgeable",
                                                  "Extensive (Forced) - Heavily used since capture, body experienced but mind processes as trauma"
                                                ]
                                              },
                                              {
                                                "name": "Virginity",
                                                "type": "String",
                                                "prompt": "Per-orifice virginity status with details of first times.",
                                                "defaultValue": "Oral: Virgin | Vaginal: Virgin | Anal: Virgin",
                                                "exampleValues": [
                                                  "Oral: Virgin | Vaginal: Virgin | Anal: Virgin - Completely untouched",
                                                  "Oral: Taken (boyfriend, age 17, consensual) | Vaginal: Taken (same, age 18, consensual) | Anal: Virgin",
                                                  "Oral: Taken (Guard, Day 1, forced) | Vaginal: Taken (Owner, Day 1, forced) | Anal: Taken (Punishment, Day 5, forced)"
                                                ]
                                              },
                                              {
                                                "name": "PartnerCount",
                                                "type": "String",
                                                "prompt": "Number of sexual partners and rough act counts.",
                                                "defaultValue": "0 partners",
                                                "exampleValues": [
                                                  "0 partners - Virgin",
                                                  "3 partners | Vaginal: ~30 times | Oral: ~20 | Anal: 0",
                                                  "Unknown (50+) | Vaginal: Countless | Anal: ~150 | Oral: Countless | Gangbangs: 8"
                                                ]
                                              }
                                            ]
                                          },
                                          {
                                            "name": "PhysicalCapabilities",
                                            "type": "Object",
                                            "prompt": "Physical sexual characteristics affecting performance - capacity, stamina, response.",
                                            "defaultValue": null,
                                            "nestedFields": [
                                              {
                                                "name": "VaginalCapacity",
                                                "type": "String",
                                                "prompt": "How much vaginal canal can accommodate.",
                                                "defaultValue": "Average (untested if virgin)",
                                                "exampleValues": [
                                                  "Virgin - Untested, likely average, hymen intact",
                                                  "Average - Accommodates typical sizes comfortably, larger requires adjustment",
                                                  "Above Average - Can take larger than normal without discomfort",
                                                  "High (trained) - Trained to accept very large sizes, unusual depth/stretch capacity",
                                                  "Extreme (modified) - Bloodline modification grants exceptional capacity"
                                                ]
                                              },
                                              {
                                                "name": "AnalCapacity",
                                                "type": "String",
                                                "prompt": "Anal training level and capacity.",
                                                "defaultValue": "Untrained (virgin)",
                                                "exampleValues": [
                                                  "Untrained - Never penetrated, very tight",
                                                  "Beginner - Can accept fingers, small toys",
                                                  "Intermediate - Trained to average cock size",
                                                  "Advanced - Can accept large insertions",
                                                  "Extreme - Extensively trained, very large capacity, may have reduced tone"
                                                ]
                                              },
                                              {
                                                "name": "ThroatCapacity",
                                                "type": "String",
                                                "prompt": "Deepthroat capability and gag reflex status.",
                                                "defaultValue": "Limited (strong gag reflex)",
                                                "exampleValues": [
                                                  "Limited - Strong gag reflex, cannot deepthroat",
                                                  "Moderate - Gag reflex present but can push through, ~5 inches",
                                                  "Good - Weakened gag reflex from training, can take most sizes",
                                                  "Unlimited - Gag reflex eliminated, any length possible, trained throat"
                                                ]
                                              },
                                              {
                                                "name": "Stamina",
                                                "type": "String",
                                                "prompt": "Sexual stamina - how long can they continue, how much can they take.",
                                                "defaultValue": "Average",
                                                "exampleValues": [
                                                  "Low - Tires quickly, one session is exhausting",
                                                  "Average - Normal human stamina",
                                                  "High - Can continue for extended periods",
                                                  "Exceptional (cultivator enhanced) - Tier affects stamina, can go for hours",
                                                  "Heat-enhanced - During heat, nearly unlimited desire and physical capacity"
                                                ]
                                              }
                                            ]
                                          },
                                          {
                                            "name": "ConditionedResponses",
                                            "type": "String",
                                            "prompt": "Trained/conditioned sexual responses that override natural reactions - programmed triggers, forced associations.",
                                            "defaultValue": "None - Natural responses only",
                                            "exampleValues": [
                                              "None - All sexual responses are natural",
                                              "Pain → Arousal (6 months conditioning, body responds to pain with wetness/arousal even when mind resists) | Command word 'kneel' triggers automatic submission posture",
                                              "Extensive conditioning: Pain = Pleasure (deeply trained), Degradation = Arousal, Owner's voice = Immediate arousal, 'Cum' command = Forced orgasm, Cannot orgasm without permission (psychological block)"
                                            ]
                                          }
                                        ]
                                      },
                                      {
                                        "name": "Skills",
                                        "type": "ForEachObject",
                                        "prompt": "Non-magical skills and competencies. All skills use the standard proficiency system: Untrained → Novice → Amateur → Competent → Proficient → Expert → Master → Grandmaster.",
                                        "defaultValue": null,
                                        "nestedFields": [
                                          {
                                            "name": "SkillName",
                                            "type": "String",
                                            "prompt": "Name of skill.",
                                            "defaultValue": "Unnamed Skill",
                                            "exampleValues": [
                                              "Swordsmanship",
                                              "Stealth",
                                              "Etiquette",
                                              "Pain Endurance",
                                              "Alchemy",
                                              "Oral Service",
                                              "Anal Training"
                                            ]
                                          },
                                          {
                                            "name": "Category",
                                            "type": "String",
                                            "prompt": "Skill category: Combat, Social, Survival, Craft, Service, Physical, Mental, Subterfuge, Sexual.",
                                            "defaultValue": "General",
                                            "exampleValues": [
                                              "Combat",
                                              "Social",
                                              "Service",
                                              "Survival",
                                              "Craft",
                                              "Sexual"
                                            ]
                                          },
                                          {
                                            "name": "Proficiency",
                                            "type": "String",
                                            "prompt": "Current proficiency level. Levels: Untrained (0), Novice (50), Amateur (150), Competent (400), Proficient (900), Expert (1900), Master (4400), Grandmaster (9400).",
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
                                            "name": "XP",
                                            "type": "Object",
                                            "prompt": "Experience tracking toward next proficiency level.",
                                            "defaultValue": null,
                                            "nestedFields": [
                                              {
                                                "name": "Current",
                                                "type": "Number",
                                                "prompt": "Total accumulated XP. Thresholds: Novice(50), Amateur(150), Competent(400), Proficient(900), Expert(1900), Master(4400), Grandmaster(9400).",
                                                "defaultValue": "0",
                                                "exampleValues": [0, 35, 178, 523, 1850, 5200, 11240]
                                              },
                                              {
                                                "name": "NextThreshold",
                                                "type": "Number",
                                                "prompt": "XP threshold for next proficiency level. 50→150→400→900→1900→4400→9400→MAX.",
                                                "defaultValue": 50,
                                                "exampleValues": [50, 150, 400, 900, 1900, 4400, 9400, -1]
                                              },
                                              {
                                                "name": "ToNext",
                                                "type": "Number",
                                                "prompt": "XP remaining until next proficiency level. -1 if at Grandmaster (max level).",
                                                "defaultValue": 50,
                                                "exampleValues": [50, 15, 222, 377, 2550, -1]
                                              }
                                            ]
                                          },
                                          {
                                            "name": "ChallengeFloor",
                                            "type": "String",
                                            "prompt": "Minimum difficulty of tasks that grant meaningful XP - equals current Proficiency level. Tasks BELOW floor grant only 0-10% XP. Tasks AT floor grant 100% XP. Tasks ABOVE floor grant 150-200% XP.",
                                            "defaultValue": "Untrained (any practice grants full XP)",
                                            "exampleValues": [
                                              "Untrained (any practice grants full XP, everything is challenging)",
                                              "Novice (basic exercises grant minimal XP; need real application or difficult drills)",
                                              "Competent (routine professional tasks grant minimal XP; need genuine challenges, novel problems, or superior opponents)",
                                              "Master (standard difficult tasks grant minimal XP; only extreme challenges, innovation, teaching masters, or legendary feats grant meaningful XP)"
                                            ]
                                          },
                                          {
                                            "name": "Development",
                                            "type": "String",
                                            "prompt": "Narrative tracking of how this skill has developed - training history, key learning moments, teachers, and recent progress. Include: how skill was first acquired, notable training or experiences that granted significant XP, any teachers/mentors who contributed, recent developments, and current training focus if any.",
                                            "defaultValue": "Newly encountered skill, no development history yet.",
                                            "exampleValues": [
                                              "Self-taught basics through trial and error over first month of captivity. No formal instruction. Recent: Gained significant XP during escape attempt (high-stakes application).",
                                              "Formally trained from age 8 at father's insistence. Journeyman instructor for 6 years, then 2 years under Swordmaster Aldric (Expert). Reached Proficient before capture. Skills maintained but not advancing in captivity - no worthy opponents. Recent: Successfully defended self against guard (routine, minimal XP).",
                                              "Natural talent identified by court mage at age 12 (see Trait: Magically Gifted). Apprenticed for 4 years, focused on fire specialization. Teaching accelerated progress significantly. Recent: Breakthrough during emotional crisis unlocked new understanding (+150 XP bonus from dramatic moment)."
                                            ]
                                          },
                                          {
                                            "name": "RecentGains",
                                            "type": "String",
                                            "prompt": "Last 3-5 significant XP gains with calculation breakdown. Format: 'Scene/Day: +X XP (Base x Challenge x Bonuses = Total) - Context'",
                                            "defaultValue": "None yet",
                                            "exampleValues": [
                                              "None yet",
                                              "Day 45: +20 XP (20 x 1.0 = 20) - Standard training session at level",
                                              "Scene 47: +2 XP (20 x 0.10 = 2) - Routine use, below floor | Scene 52: +35 XP (20 x 1.0 x 1.75 = 35) - Duel vs Competent opponent, high stakes + dramatic"
                                            ]
                                          }
                                        ]
                                      },
                                      {
                                        "name": "PermanentMarks",
                                        "type": "String",
                                        "prompt": "Permanent body modifications - brands, tattoos, scars, piercings. Track each with location, description, origin, and whether consensual.",
                                        "defaultValue": "None - Body unmarked",
                                        "exampleValues": [
                                          "None - No permanent marks, clean canvas",
                                          "Ownership brand: House Halvard crest on left inner thigh (forced at purchase, healed silver scar) | Pierced: Both nipples with steel rings (forced, for attachment purposes), healed",
                                          "Slave marks: Brand (House Halvard, inner thigh), Tattoo ('Property' lower back, forced), Piercings (nipples, clit hood - all functional for control/display). Scars: Whip marks across back (punishment, healed), collar scar around neck (permanent from first year of wear)"
                                        ]
                                      },
                                      {
                                        "name": "Traits",
                                        "type": "Object",
                                        "prompt": "Permanent characteristics with mechanical effects - talents, flaws, special qualities. Traits require significant, sustained experience to develop - not single events. Include XP/CTP modifiers where applicable.",
                                        "defaultValue": null,
                                        "nestedFields": [
                                          {
                                            "name": "Positive",
                                            "type": "String",
                                            "prompt": "Beneficial traits - natural talents, developed strengths. Format: 'Trait Name (Category) - Effect'. Include any XP or CTP multipliers.",
                                            "defaultValue": "None identified",
                                            "exampleValues": [
                                              "None - No notable positive traits identified",
                                              "Magically Gifted (Magical) - +50% XP to magic skills, instinctive mana sensing | Natural Beauty (Social) - +25% to appearance-based social interactions",
                                              "Exceptional Pain Tolerance (Physical) - Can function normally up to 7/10 pain, +25% XP to Pain Endurance | Quick Learner (Mental) - +25% XP to all skills | Fertile (Sexual/Bovine bloodline) - +50% conception chance"
                                            ]
                                          },
                                          {
                                            "name": "Negative",
                                            "type": "String",
                                            "prompt": "Detrimental traits - weaknesses, flaws. Format: 'Trait Name (Category) - Effect'. Include any XP or CTP penalties.",
                                            "defaultValue": "None identified",
                                            "exampleValues": [
                                              "None - No notable negative traits",
                                              "Frail Constitution (Physical) - -25% XP to physical skills, -25% CTP to Body Capacity, tires faster | Easily Conditioned (Mental) - Psychological conditioning takes hold 50% faster",
                                              "Slave Brand (Social/Magical) - Cannot raise hand against owner, compelled truth when directly questioned | Trauma: Darkness (Mental) - Panic response in dark enclosed spaces, -3 effective proficiency levels when triggered"
                                            ]
                                          },
                                          {
                                            "name": "Developing",
                                            "type": "String",
                                            "prompt": "Traits that are forming but not yet permanent. Track progress toward full trait acquisition. Single dramatic events may begin development but rarely complete it. Format: 'Trait Name (progress/requirement) - What would cause completion'",
                                            "defaultValue": "None - No traits currently developing",
                                            "exampleValues": [
                                              "None - No traits currently developing",
                                              "Pain Tolerance (3/10 significant pain experiences) - Repeated exposure to significant pain is building tolerance | Conditioned Arousal (2 weeks/8 weeks conditioning) - Ongoing conditioning connecting pain to arousal",
                                              "Pack Bond (1/3 major bonding events) - First significant pack loyalty moment occurred, more needed for permanent trait"
                                            ]
                                          }
                                        ]
                                      },
                                      {
                                        "name": "Inventory",
                                        "type": "ForEachObject",
                                        "prompt": "Items being carried (not worn). Each tracked separately.",
                                        "defaultValue": null,
                                        "nestedFields": [
                                          {
                                            "name": "Item",
                                            "type": "String",
                                            "prompt": "Item name and brief description.",
                                            "defaultValue": "Item",
                                            "exampleValues": [
                                              "Healing Pill (low tier) - Basic healing, restores minor wounds",
                                              "Letter from sister - Hidden, sentimental value",
                                              "Sect token - Identifies as Pack Covenant disciple"
                                            ]
                                          },
                                          {
                                            "name": "Location",
                                            "type": "String",
                                            "prompt": "Where on person the item is kept.",
                                            "defaultValue": "Carried",
                                            "exampleValues": [
                                              "Belt pouch",
                                              "Hidden in boot",
                                              "Worn around neck under clothing"
                                            ]
                                          }
                                        ]
                                      }
                                    ]
                                  }
                                  """;
}