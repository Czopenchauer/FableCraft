using System.Text.Json;
using System.Text.Json.Serialization;

using FableCraft.Application.AdventureGeneration;
using FableCraft.Application.Model;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Queue;

using Microsoft.AspNetCore.Http;

namespace FableCraft.Application.AdventureImport;

public class ImportAdventure
{
    public IFormFile Lorebook { get; set; }
    
    public IFormFile Adventure { get; set; }
    
    public IFormFile Character { get; set; }
    
    public string AdventureName { get; set; }
}

public class AdventureImportService
{
    private readonly static JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    
    private readonly ApplicationDbContext _context;
    private readonly TimeProvider _timeProvider;
    private readonly IMessageDispatcher _dispatcher;

    public AdventureImportService(ApplicationDbContext context, TimeProvider timeProvider, IMessageDispatcher dispatcher)
    {
        _context = context;
        _timeProvider = timeProvider;
        _dispatcher = dispatcher;
    }

    public async Task<Adventure> ImportAdventureAsync(
        ImportAdventure importAdventure,
        CancellationToken cancellationToken)
    {
        string lorebookJson;
        using (var reader = new StreamReader(importAdventure.Lorebook.OpenReadStream()))
        {
            lorebookJson = await reader.ReadToEndAsync(cancellationToken);
        }

        string adventureJson;
        using (var reader = new StreamReader(importAdventure.Adventure.OpenReadStream()))
        {
            adventureJson = await reader.ReadToEndAsync(cancellationToken);
        }

        string characterJson;
        using (var reader = new StreamReader(importAdventure.Character.OpenReadStream()))
        {
            characterJson = await reader.ReadToEndAsync(cancellationToken);
        }

        var character = ParseCharacterAsync(characterJson);
        var lorebook = ParseLorebookAsync(lorebookJson);
        var scenes = ParseAdventureMessagesAsync(adventureJson);
        var now = _timeProvider.GetUtcNow();
        var adventure = new Adventure
        {
            Name = importAdventure.AdventureName,
            AuthorNotes = string.Empty,
            Character = character,
            Lorebook = lorebook,
            FirstSceneGuidance = string.Empty,
            Scenes = scenes,
            CreatedAt = now
        };

        await _context.Adventures.AddAsync(adventure, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await _dispatcher.PublishAsync(new AddAdventureToKnowledgeGraphCommand { AdventureId = adventure.Id },
            cancellationToken);

        return adventure;
    }

    private Character ParseCharacterAsync(string characterJson)
    {
        var importedChar = JsonSerializer.Deserialize<ImportCharacterDto>(characterJson, JsonOptions);

        if (importedChar == null)
        {
            throw new InvalidOperationException("Failed to deserialize character JSON");
        }

        var character = new Character()
        {
            Name = importedChar.Name,
            Description = importedChar.Description,
            Background = string.Empty
        };

        return character;
    }

    private List<LorebookEntry> ParseLorebookAsync(string lorebookJson)
    {
        var importedLorebook = JsonSerializer.Deserialize<ImportLorebookDto>(lorebookJson, JsonOptions);

        if (importedLorebook?.Entries == null)
        {
            throw new InvalidOperationException("Failed to deserialize lorebook JSON or no entries found");
        }

        var lorebookEntries = new List<LorebookEntry>();

        foreach (var (_, entry) in importedLorebook.Entries.OrderBy(e => e.Value.Order))
        {
            if (entry.Disable)
            {
                continue;
            }

            var lorebookEntry = new LorebookEntry()
            {
                Description = entry.Comment,
                Content = entry.Content,
                Category = entry.Comment,
                Priority = entry.Order
            };

            lorebookEntries.Add(lorebookEntry);
        }

        return lorebookEntries;
    }

    private List<Scene> ParseAdventureMessagesAsync(
        string adventureJson)
    {
        var scenes = new List<Scene>();

        var lines = adventureJson.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var sceneSequenceNumber = 0;
        foreach (var line in lines.Chunk(2))
        {
            var trimmedLine = line[0].Trim();
            if (string.IsNullOrEmpty(trimmedLine))
            {
                continue;
            }

            var message = JsonSerializer.Deserialize<AdventureMessageDto>(trimmedLine, JsonOptions);
            if (message != null)
            {
                var scene = new Scene
                {
                    SequenceNumber = sceneSequenceNumber,
                    NarrativeText = message.Mes,
                    SceneStateJson = JsonSerializer.Serialize(message.Tracker),
                    CharacterActions = line.Skip(1).Select(x => new CharacterAction
                    {
                        ActionDescription = JsonSerializer.Deserialize<AdventureMessageDto>(x.Trim(), JsonOptions)!.Mes,
                        Selected = true
                    }).ToList()
                };

                sceneSequenceNumber++;

                scenes.Add(scene);
            }
        }

        return scenes;
    }
}