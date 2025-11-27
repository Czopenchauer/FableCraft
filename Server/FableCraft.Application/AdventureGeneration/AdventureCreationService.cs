using System.Text.Json;

using FableCraft.Application.Exceptions;
using FableCraft.Application.Model;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Queue;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using OpenAI.Chat;

using Polly;
using Polly.Retry;

using Serilog;

using ChatMessageContent = Microsoft.SemanticKernel.ChatMessageContent;
using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.AdventureGeneration;

public class AdventureCreationStatus
{
    public required Guid AdventureId { get; init; }

    public required Dictionary<string, string> ComponentStatuses { get; init; }
}

public interface IAdventureCreationService
{
    Task<AdventureCreationStatus> CreateAdventureAsync(AdventureDto adventureDto, CancellationToken cancellationToken);

    Task<AdventureCreationStatus> GetAdventureCreationStatusAsync(Guid worldId, CancellationToken cancellationToken);

    Task DeleteAdventureAsync(Guid adventureId, CancellationToken cancellationToken);

    Task<IEnumerable<AdventureListItemDto>> GetAllAdventuresAsync(CancellationToken cancellationToken);
}

internal class AdventureCreationService : IAdventureCreationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger _logger;
    private readonly IMessageDispatcher _messageDispatcher;
    private readonly IRagBuilder _ragBuilder;
    private readonly TimeProvider _timeProvider;

    public AdventureCreationService(
        ApplicationDbContext dbContext,
        IMessageDispatcher messageDispatcher,
        TimeProvider timeProvider,
        ILogger logger,
        IRagBuilder ragBuilder)
    {
        _dbContext = dbContext;
        _messageDispatcher = messageDispatcher;
        _timeProvider = timeProvider;
        _logger = logger;
        _ragBuilder = ragBuilder;
    }

    public async Task<AdventureCreationStatus> CreateAdventureAsync(AdventureDto adventureDto,
        CancellationToken cancellationToken)
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();

        var adventure = new Adventure
        {
            Name = adventureDto.Name,
            CreatedAt = now,
            FirstSceneGuidance = adventureDto.FirstSceneDescription,
            LastPlayedAt = null,
            AuthorNotes = adventureDto.AuthorNotes,
            MainCharacter = new MainCharacter
            {
                Name = adventureDto.Character.Name,
                Description = adventureDto.Character.Description,
            },
            Lorebook = adventureDto.Lorebook.Select(entry => new LorebookEntry
                {
                    Description = entry.Description,
                    Content = entry.Content,
                    Category = entry.Category
                })
                .ToList(),
            TrackerStructure = JsonSerializer.Deserialize<TrackerStructure>(TrackerStructure,
                new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true
                })!,
        };

        _dbContext.Adventures.Add(adventure);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _messageDispatcher.PublishAsync(new AddAdventureToKnowledgeGraphCommand { AdventureId = adventure.Id },
            cancellationToken);

        return await GetAdventureCreationStatusAsync(adventure.Id, cancellationToken);
    }

    public async Task<AdventureCreationStatus> GetAdventureCreationStatusAsync(Guid adventureId,
        CancellationToken cancellationToken)
    {
        // Shit query due to materialization but whatever
        var adventure = await _dbContext.Adventures
            .Include(w => w.MainCharacter)
            .Include(w => w.Lorebook)
            .Include(x => x.Scenes)
            .Select(x => new
            {
                x.Id,
                CharacterId = x.MainCharacter.Id,
                Lorebooks = x.Lorebook.Select(y => new
                {
                    LorebookId = y.Id,
                    y.Category
                }),
                SceneIds = x.Scenes.Select(s => s.Id)
            })
            .FirstOrDefaultAsync(w => w.Id == adventureId, cancellationToken);

        if (adventure == null)
        {
            throw new AdventureNotFoundException(adventureId);
        }

        var lorebookStatuses = await _dbContext.Chunks
            .Where(x => adventure.Lorebooks.Select(y => y.LorebookId).Contains(x.EntityId))
            .Join(_dbContext.LorebookEntries,
                chunk => chunk.EntityId,
                lorebook => lorebook.Id,
                (chunk, lorebook) => new { lorebook.Category, chunk.ProcessingStatus })
            .GroupBy(x => x.Category)
            .Select(g => new
            {
                Category = g.Key,
                Status = g.All(s => s.ProcessingStatus == ProcessingStatus.Completed) ? nameof(ProcessingStatus.Completed) :
                    g.Any(s => s.ProcessingStatus == ProcessingStatus.Failed) ? nameof(ProcessingStatus.Failed) :
                    g.Any(s => s.ProcessingStatus == ProcessingStatus.InProgress) ? nameof(ProcessingStatus.InProgress) :
                    nameof(ProcessingStatus.Pending)
            })
            .ToDictionaryAsync(x => x.Category, x => x.Status, cancellationToken);

        foreach (var category in adventure.Lorebooks.Select(x => x.Category).Except(lorebookStatuses.Keys))
        {
            lorebookStatuses.Add(category, nameof(ProcessingStatus.Pending));
        }

        var characterStatus = await _dbContext.Chunks
                                  .Where(x => x.EntityId == adventure.CharacterId)
                                  .GroupBy(x => x.EntityId)
                                  .Select(g => g.All(s => s.ProcessingStatus == ProcessingStatus.Completed) ? nameof(ProcessingStatus.Completed) :
                                      g.Any(s => s.ProcessingStatus == ProcessingStatus.Failed) ? nameof(ProcessingStatus.Failed) :
                                      g.Any(s => s.ProcessingStatus == ProcessingStatus.InProgress) ? nameof(ProcessingStatus.InProgress) :
                                      nameof(ProcessingStatus.Pending))
                                  .FirstOrDefaultAsync(cancellationToken)
                              ?? nameof(ProcessingStatus.Pending);

        var status = new Dictionary<string, string>(lorebookStatuses)
        {
            ["Character"] = characterStatus
        };

        status.Add("Creating first scene", adventure.SceneIds.Any() ? nameof(ProcessingStatus.Completed) : nameof(ProcessingStatus.Pending));

        return new AdventureCreationStatus
        {
            AdventureId = adventureId,
            ComponentStatuses = status
        };
    }

    public async Task DeleteAdventureAsync(Guid adventureId, CancellationToken cancellationToken)
    {
        Adventure? adventure = await _dbContext.Adventures
            .Include(w => w.MainCharacter)
            .Include(w => w.Lorebook)
            .Include(x => x.Scenes)
            .ThenInclude(x => x.CharacterActions)
            .FirstOrDefaultAsync(w => w.Id == adventureId, cancellationToken);

        if (adventure == null)
        {
            throw new AdventureNotFoundException(adventureId);
        }

        try
        {
            await _ragBuilder.DeleteDatasetAsync(adventure.Id.ToString(), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error(ex,
                "Failed to delete adventure {adventureId} from knowledge graph.",
                adventure.Id);
            // We don't throw here to ensure the adventure is deleted from DB even if RAG deletion fails? 
            // The original code threw exception. So I should probably throw.
            throw;
        }

        _dbContext.Adventures.Remove(adventure);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<AdventureListItemDto>> GetAllAdventuresAsync(CancellationToken cancellationToken)
    {
        var adventures = await _dbContext.Adventures
            .Include(a => a.Scenes)
            .OrderByDescending(a => a.LastPlayedAt)
            .Select(a => new AdventureListItemDto
            {
                AdventureId = a.Id,
                Name = a.Name,
                LastScenePreview = a.Scenes
                    .OrderByDescending(s => s.SequenceNumber)
                    .Select(s => s.NarrativeText.Length > 200
                        ? s.NarrativeText.Substring(0, 200)
                        : s.NarrativeText)
                    .FirstOrDefault(),
                Created = a.CreatedAt,
                LastPlayed = a.LastPlayedAt
            })
            .ToListAsync(cancellationToken);

        return adventures;
    }

    private const string TrackerStructure = """
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
}