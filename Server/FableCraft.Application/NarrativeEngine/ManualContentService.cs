using System.Text.Json;

using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Application.NarrativeEngine.Workflow;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Serilog;

namespace FableCraft.Application.NarrativeEngine;

public enum ManualContentKind
{
    Character,
    Location,
    Item,
    Lore
}

public sealed record ManualContentInput(
    ManualContentKind Kind,
    string Name,
    string Details,
    string? Importance,
    string? PowerLevel,
    string? Category);

public sealed record ManualContentOutput(string Kind, Guid? Id, string Name, string Summary);

public sealed record ManualContentDraftOutput(string Kind, string Name, string Summary, JsonElement RawJson);

public sealed record ManualContentConfirmInput(
    ManualContentKind Kind,
    JsonElement RawJson);

public sealed class ManualContentService(
    ApplicationDbContext dbContext,
    ILogger logger,
    IServiceProvider serviceProvider)
{
    private const int NumberOfScenesToInclude = 20;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public async Task<ManualContentOutput> CreateAsync(
        Guid adventureId,
        ManualContentInput input,
        CancellationToken cancellationToken)
    {
        var draft = await DraftAsync(adventureId, input, cancellationToken);
        return await ConfirmAsync(adventureId, new ManualContentConfirmInput(input.Kind, draft.RawJson), cancellationToken);
    }

    public async Task<ManualContentDraftOutput> DraftAsync(
        Guid adventureId,
        ManualContentInput input,
        CancellationToken cancellationToken)
    {
        var latestScene = await GetLatestScene(adventureId, cancellationToken);
        var context = await BuildContext(adventureId, latestScene, cancellationToken);

        return input.Kind switch
        {
            ManualContentKind.Lore => await DraftLore(context, input, latestScene, cancellationToken),
            ManualContentKind.Location => await DraftLocation(context, input, latestScene, cancellationToken),
            ManualContentKind.Item => await DraftItem(context, input, latestScene, cancellationToken),
            ManualContentKind.Character => await DraftCharacter(context, input, latestScene, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(input), input.Kind, "Unknown content kind")
        };
    }

    public async Task<ManualContentOutput> ConfirmAsync(
        Guid adventureId,
        ManualContentConfirmInput input,
        CancellationToken cancellationToken)
    {
        var latestScene = await GetLatestScene(adventureId, cancellationToken);
        var rawJson = input.RawJson;

        var output = input.Kind switch
        {
            ManualContentKind.Lore => PersistLore(latestScene, rawJson),
            ManualContentKind.Location => PersistLocation(latestScene, rawJson),
            ManualContentKind.Item => PersistItem(latestScene, rawJson),
            ManualContentKind.Character => PersistCharacter(latestScene, rawJson),
            _ => throw new ArgumentOutOfRangeException(nameof(input), input.Kind, "Unknown content kind")
        };

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.Information("Manually created {Kind} '{Name}' for adventure {AdventureId} on scene {SceneId}",
            input.Kind, output.Name, adventureId, latestScene.Id);
        return output;
    }

    private async Task<Scene> GetLatestScene(Guid adventureId, CancellationToken cancellationToken)
    {
        var latestScene = await dbContext.Scenes
            .Where(x => x.AdventureId == adventureId)
            .Include(x => x.CharacterActions)
            .OrderByDescending(x => x.SequenceNumber)
            .FirstOrDefaultAsync(cancellationToken);

        return latestScene ?? throw new InvalidOperationException("Adventure has no scene to attach created content to");
    }

    private async Task<ManualContentDraftOutput> DraftLore(
        GenerationContext context, ManualContentInput input, Scene scene, CancellationToken ct)
    {
        var request = BuildLoreRequest(input);
        var result = await serviceProvider.GetRequiredService<LoreCrafter>().Invoke(context, request, context.SceneContext, ct);

        var payload = new DraftPayload(nameof(ManualContentKind.Lore), result);
        var rawJson = JsonSerializer.SerializeToElement(payload, JsonOptions);
        return new ManualContentDraftOutput(nameof(ManualContentKind.Lore), result.Title, result.Description, rawJson);
    }

    private async Task<ManualContentDraftOutput> DraftLocation(
        GenerationContext context, ManualContentInput input, Scene scene, CancellationToken ct)
    {
        var request = BuildLocationRequest(input);
        var result = await serviceProvider.GetRequiredService<LocationCrafter>().Invoke(context, request, context.SceneContext, ct);

        var payload = new DraftPayload(nameof(ManualContentKind.Location), result);
        var rawJson = JsonSerializer.SerializeToElement(payload, JsonOptions);
        return new ManualContentDraftOutput(nameof(ManualContentKind.Location), result.Title, result.Description, rawJson);
    }

    private async Task<ManualContentDraftOutput> DraftItem(
        GenerationContext context, ManualContentInput input, Scene scene, CancellationToken ct)
    {
        var request = BuildItemRequest(input);
        var result = await serviceProvider.GetRequiredService<ItemCrafter>().Invoke(context, request, context.SceneContext, ct);

        var payload = new DraftPayload(nameof(ManualContentKind.Item), result);
        var rawJson = JsonSerializer.SerializeToElement(payload, JsonOptions);
        return new ManualContentDraftOutput(nameof(ManualContentKind.Item), result.Name, result.Description, rawJson);
    }

    private async Task<ManualContentDraftOutput> DraftCharacter(
        GenerationContext context, ManualContentInput input, Scene scene, CancellationToken ct)
    {
        var importance = CharacterImportanceConverter.FromString(
            string.IsNullOrWhiteSpace(input.Importance) ? "significant" : input.Importance);

        var request = new CharacterRequest
        {
            Importance = importance,
            AdditionalData =
            {
                ["name"] = input.Name,
                ["request"] = input.Details
            }
        };

        if (importance == CharacterImportance.Background)
        {
            var profile = await serviceProvider.GetRequiredService<PartialProfileCrafter>().Invoke(context, request, context.SceneContext, ct);
            var bgPayload = new DraftPayload("BackgroundCharacter", profile)
            {
                AdditionalData = new Dictionary<string, object?>
                {
                    ["lastLocation"] = context.NewTracker?.Scene?.Location ?? "Unknown",
                    ["lastSeenTime"] = context.NewTracker?.Scene?.Time ?? context.AdventureStartTime
                }
            };
            var bgRawJson = JsonSerializer.SerializeToElement(bgPayload, JsonOptions);
            return new ManualContentDraftOutput(nameof(ManualContentKind.Character), profile.Name, profile.Description, bgRawJson);
        }

        var sceneTracker = context.NewTracker?.Scene
            ?? throw new InvalidOperationException("Scene must be enriched before creating a full-profile character");

        var character = await serviceProvider.GetRequiredService<CharacterCrafter>().Invoke(context, request, context.SceneContext, ct);
        var experientialOutput = await serviceProvider.GetRequiredService<ExperientialNarratorAgent>().Invoke(context, character, sceneTracker, ct);
        var assessorOutput = await serviceProvider.GetRequiredService<ClinicalAssessorAgent>().Invoke(context, character, experientialOutput.SceneRewrite, sceneTracker, ct);

        if (assessorOutput.Identity != null)
        {
            character.CharacterState = assessorOutput.Identity;
        }

        character.SceneRewrites =
        [
            new CharacterSceneContext
            {
                Content = experientialOutput.SceneRewrite,
                SceneTracker = sceneTracker,
                SequenceNumber = 0
            }
        ];
        character.IsDead = experientialOutput.IsDead;

        var payload = new DraftPayload("FullCharacter", character);
        var rawJson = JsonSerializer.SerializeToElement(payload, JsonOptions);
        return new ManualContentDraftOutput(nameof(ManualContentKind.Character), character.Name, character.Description, rawJson);
    }

    private ManualContentOutput PersistLore(Scene scene, JsonElement rawJson)
    {
        var payload = rawJson.Deserialize<DraftPayload>(JsonOptions)!;
        var result = payload.Data.Deserialize<GeneratedLore>(JsonOptions)!;
        var entry = AddLorebookEntry(scene, result.Title, result.Description, result.ToJsonString(), LorebookCategory.Lore);
        return new ManualContentOutput(nameof(ManualContentKind.Lore), entry.Id, result.Title, result.Description);
    }

    private ManualContentOutput PersistLocation(Scene scene, JsonElement rawJson)
    {
        var payload = rawJson.Deserialize<DraftPayload>(JsonOptions)!;
        var result = payload.Data.Deserialize<LocationGenerationResult>(JsonOptions)!;
        var entry = AddLorebookEntry(scene, result.Title, result.Description, result.ToJsonString(), LorebookCategory.Location);
        return new ManualContentOutput(nameof(ManualContentKind.Location), entry.Id, result.Title, result.Description);
    }

    private ManualContentOutput PersistItem(Scene scene, JsonElement rawJson)
    {
        var payload = rawJson.Deserialize<DraftPayload>(JsonOptions)!;
        var result = payload.Data.Deserialize<GeneratedItem>(JsonOptions)!;
        var entry = AddLorebookEntry(scene, result.Name, result.Description, result.ToJsonString(), LorebookCategory.Item);
        return new ManualContentOutput(nameof(ManualContentKind.Item), entry.Id, result.Name, result.Description);
    }

    private ManualContentOutput PersistCharacter(Scene scene, JsonElement rawJson)
    {
        var payload = rawJson.Deserialize<DraftPayload>(JsonOptions)!;

        if (payload.SubKind == "BackgroundCharacter")
        {
            var profile = payload.Data.Deserialize<GeneratedPartialProfile>(JsonOptions)!;
            var lastLocation = payload.AdditionalData?.TryGetValue("lastLocation", out var locObj) == true
                ? locObj?.ToString() ?? "Unknown"
                : "Unknown";
            var lastSeenTime = payload.AdditionalData?.TryGetValue("lastSeenTime", out var timeObj) == true
                ? timeObj?.ToString() ?? scene.Metadata?.Tracker?.Scene?.Time ?? "Unknown"
                : scene.Metadata?.Tracker?.Scene?.Time ?? "Unknown";

            dbContext.BackgroundCharacters.Add(new BackgroundCharacter
            {
                AdventureId = scene.AdventureId,
                SceneId = scene.Id,
                Name = profile.Name,
                Identity = profile.Identity,
                Description = $"{profile.Description}\n{profile.Identity}\n{profile.AdditionalData.ToJsonString()}",
                LastLocation = lastLocation,
                LastSeenTime = lastSeenTime,
                ConvertedToFull = false,
                Version = 0
            });

            var entry = AddLorebookEntry(scene, profile.Name, profile.Description, $"{profile.Description}\n{profile.Identity}\n{profile.AdditionalData.ToJsonString()}", LorebookCategory.BackgroundCharacter);
            return new ManualContentOutput(nameof(ManualContentKind.Character), entry.Id, profile.Name, profile.Description);
        }

        var character = payload.Data.Deserialize<CharacterContext>(JsonOptions)!;
        var entity = CharacterPersistence.BuildNewCharacter(character, scene, scene.AdventureId);
        dbContext.Characters.Add(entity);

        return new ManualContentOutput(nameof(ManualContentKind.Character), entity.Id, character.Name, character.Description);
    }

    private static LoreRequest BuildLoreRequest(ManualContentInput input)
    {
        var request = new LoreRequest
        {
            AdditionalData =
            {
                ["name"] = input.Name,
                ["request"] = input.Details
            }
        };
        if (!string.IsNullOrWhiteSpace(input.Category))
        {
            request.AdditionalData["category"] = input.Category;
        }
        return request;
    }

    private static LocationRequest BuildLocationRequest(ManualContentInput input)
    {
        var request = new LocationRequest
        {
            AdditionalData =
            {
                ["name"] = input.Name,
                ["request"] = input.Details
            }
        };
        if (!string.IsNullOrWhiteSpace(input.Importance))
        {
            request.AdditionalData["importance"] = input.Importance;
        }
        return request;
    }

    private static ItemRequest BuildItemRequest(ManualContentInput input)
    {
        var request = new ItemRequest
        {
            AdditionalData =
            {
                ["name"] = input.Name,
                ["request"] = input.Details
            }
        };
        if (!string.IsNullOrWhiteSpace(input.PowerLevel))
        {
            request.AdditionalData["power_level"] = input.PowerLevel;
        }
        return request;
    }

    private LorebookEntry AddLorebookEntry(Scene scene, string? title, string description, string content, LorebookCategory category)
    {
        var entry = new LorebookEntry
        {
            AdventureId = scene.AdventureId,
            SceneId = scene.Id,
            Title = title,
            Description = description,
            Content = content,
            Category = category.ToString(),
            ContentType = ContentType.json
        };
        dbContext.LorebookEntries.Add(entry);
        return entry;
    }

    private async Task<GenerationContext> BuildContext(Guid adventureId, Scene latestScene, CancellationToken ct)
    {
        var adventure = await dbContext.Adventures
            .Where(x => x.Id == adventureId)
            .Include(x => x.AgentLlmPresets)
            .ThenInclude(x => x.LlmPreset)
            .Select(x => new
            {
                x.TrackerStructure,
                x.MainCharacter,
                x.AgentLlmPresets,
                PromptPaths = x.PromptPath,
                x.AdventureStartTime
            })
            .SingleAsync(ct);

        var scenes = await dbContext.Scenes
            .Where(x => x.AdventureId == adventureId)
            .Include(x => x.CharacterActions)
            .OrderByDescending(x => x.SequenceNumber)
            .Skip(1)
            .Take(NumberOfScenesToInclude)
            .ToListAsync(ct);

        var adventureCharacters = await GetCharacters(adventureId, ct);

        var lorebooks = await dbContext.Scenes
            .AsSplitQuery()
            .Where(x => x.AdventureId == adventureId)
            .OrderByDescending(x => x.SequenceNumber)
            .Take(1)
            .Include(x => x.Lorebooks)
            .SelectMany(x => x.Lorebooks)
            .ToArrayAsync(ct);

        var createdLore = lorebooks.Where(x => x.Category == nameof(LorebookCategory.Lore)).ToArray();
        var createdLocations = lorebooks.Where(x => x.Category == nameof(LorebookCategory.Location)).ToArray();
        var createdItems = lorebooks.Where(x => x.Category == nameof(LorebookCategory.Item)).ToArray();

        var context = new GenerationContext
        {
            AdventureId = adventureId,
            PlayerAction = latestScene.CharacterActions.FirstOrDefault(x => x.Selected)?.ActionDescription ?? string.Empty,
            NewSceneId = latestScene.Id,
            NewResolution = latestScene.Metadata.ResolutionOutput,
            NewScene = new GeneratedScene
            {
                Scene = latestScene.NarrativeText,
                Choices = latestScene.CharacterActions.Select(x => x.ActionDescription).ToArray()
            },
            NewTracker = latestScene.Metadata.Tracker
        };

        context.SetupRequiredFields(
            scenes.Select(SceneContext.CreateFromScene).ToArray(),
            adventure.TrackerStructure,
            adventure.MainCharacter,
            adventureCharacters,
            adventure.AgentLlmPresets.ToArray(),
            adventure.PromptPaths,
            adventure.AdventureStartTime,
            createdLore,
            createdLocations,
            createdItems,
            []);

        return context;
    }

    private async Task<List<CharacterContext>> GetCharacters(Guid adventureId, CancellationToken ct)
    {
        var existingCharacters = await dbContext.Characters
            .AsSplitQuery()
            .Where(x => x.AdventureId == adventureId)
            .Include(x => x.CharacterStates.OrderByDescending(cs => cs.SequenceNumber).Take(1))
            .Include(x => x.CharacterMemories)
            .Include(x => x.CharacterRelationships)
            .Include(x => x.CharacterSceneRewrites.OrderByDescending(c => c.SequenceNumber).Take(20))
            .ToListAsync(ct);

        return existingCharacters
            .Select(x => new CharacterContext
            {
                Description = x.Description,
                Name = x.Name,
                CharacterState = x.CharacterStates.Single().CharacterStats,
                CharacterTracker = x.CharacterStates.Single().Tracker,
                CharacterId = x.Id,
                Relationships = x.CharacterRelationships
                    .GroupBy(r => r.TargetCharacterName)
                    .Select(g => g.OrderByDescending(r => r.SequenceNumber).First())
                    .Select(y => new CharacterRelationshipContext
                    {
                        TargetCharacterName = y.TargetCharacterName,
                        Data = y.Data,
                        UpdateTime = y.UpdateTime,
                        SequenceNumber = y.SequenceNumber,
                        Dynamic = y.Dynamic!
                    })
                    .ToList(),
                SceneRewrites = x.CharacterSceneRewrites.Select(y => new CharacterSceneContext
                {
                    Content = y.Content,
                    SceneTracker = y.SceneTracker,
                    SequenceNumber = y.SequenceNumber
                }).ToList(),
                Importance = x.Importance,
                SimulationMetadata = x.CharacterStates.Single().SimulationMetadata,
                IsDead = false
            }).ToList();
    }
}

internal sealed class DraftPayload
{
    private static readonly JsonSerializerOptions PayloadOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public string SubKind { get; init; } = null!;
    public JsonElement Data { get; init; }
    public Dictionary<string, object?>? AdditionalData { get; init; }

    public DraftPayload() { }

    public DraftPayload(string subKind, object data)
    {
        SubKind = subKind;
        Data = JsonSerializer.SerializeToElement(data, PayloadOptions);
    }
}