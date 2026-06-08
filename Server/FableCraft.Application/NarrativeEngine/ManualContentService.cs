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

/// <summary>
///     Player-supplied input for manually creating canon after a scene has been generated.
/// </summary>
public sealed record ManualContentInput(
    ManualContentKind Kind,
    string Name,
    string Details,
    string? Importance,
    string? PowerLevel,
    string? Category);

public sealed record ManualContentOutput(string Kind, Guid? Id, string Name, string Summary);

/// <summary>
///     Runs the relevant Crafter against player-supplied input and persists the result, attached to
///     the latest scene. Creation is no longer driven by the Writer — the player triggers it.
///     The crafted entity is committed to the Knowledge Graph by the existing
///     <see cref="SceneGeneratedEvent" /> flow when the player submits their next action.
/// </summary>
public sealed class ManualContentService(
    ApplicationDbContext dbContext,
    ILogger logger,
    IServiceProvider serviceProvider)
{
    private const int NumberOfScenesToInclude = 20;

    public async Task<ManualContentOutput> CreateAsync(
        Guid adventureId,
        ManualContentInput input,
        CancellationToken cancellationToken)
    {
        var latestScene = await dbContext.Scenes
            .Where(x => x.AdventureId == adventureId)
            .Include(x => x.CharacterActions)
            .OrderByDescending(x => x.SequenceNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestScene is null)
        {
            throw new InvalidOperationException("Adventure has no scene to attach created content to");
        }

        var context = await BuildContext(adventureId, latestScene, cancellationToken);

        var output = input.Kind switch
        {
            ManualContentKind.Lore => await CreateLore(context, input, latestScene, cancellationToken),
            ManualContentKind.Location => await CreateLocation(context, input, latestScene, cancellationToken),
            ManualContentKind.Item => await CreateItem(context, input, latestScene, cancellationToken),
            ManualContentKind.Character => await CreateCharacter(context, input, latestScene, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(input), input.Kind, "Unknown content kind")
        };

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.Information("Manually created {Kind} '{Name}' for adventure {AdventureId} on scene {SceneId}",
            input.Kind, input.Name, adventureId, latestScene.Id);
        return output;
    }

    private async Task<ManualContentOutput> CreateLore(
        GenerationContext context, ManualContentInput input, Scene scene, CancellationToken ct)
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

        var result = await serviceProvider.GetRequiredService<LoreCrafter>().Invoke(context, request, context.SceneContext, ct);
        var entry = AddLorebookEntry(scene, result.Title, result.Description, result.ToJsonString(), LorebookCategory.Lore);
        return new ManualContentOutput(nameof(ManualContentKind.Lore), entry.Id, result.Title, result.Description);
    }

    private async Task<ManualContentOutput> CreateLocation(
        GenerationContext context, ManualContentInput input, Scene scene, CancellationToken ct)
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

        var result = await serviceProvider.GetRequiredService<LocationCrafter>().Invoke(context, request, context.SceneContext, ct);
        var entry = AddLorebookEntry(scene, result.Title, result.Description, result.ToJsonString(), LorebookCategory.Location);
        return new ManualContentOutput(nameof(ManualContentKind.Location), entry.Id, result.Title, result.Description);
    }

    private async Task<ManualContentOutput> CreateItem(
        GenerationContext context, ManualContentInput input, Scene scene, CancellationToken ct)
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

        var result = await serviceProvider.GetRequiredService<ItemCrafter>().Invoke(context, request, context.SceneContext, ct);
        var entry = AddLorebookEntry(scene, result.Name, result.Description, result.ToJsonString(), LorebookCategory.Item);
        return new ManualContentOutput(nameof(ManualContentKind.Item), entry.Id, result.Name, result.Description);
    }

    private async Task<ManualContentOutput> CreateCharacter(
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

        // Background characters get a lightweight profile stored as a LorebookEntry (plus a
        // BackgroundCharacter entity for presence/co-location tracking).
        if (importance == CharacterImportance.Background)
        {
            var profile = await serviceProvider.GetRequiredService<PartialProfileCrafter>().Invoke(context, request, context.SceneContext, ct);

            dbContext.BackgroundCharacters.Add(new BackgroundCharacter
            {
                AdventureId = context.AdventureId,
                SceneId = scene.Id,
                Name = profile.Name,
                Identity = profile.Identity,
                Description = $"""
                              {profile.Description}
                              {profile.Identity}
                              """,
                LastLocation = context.NewTracker?.Scene?.Location ?? "Unknown",
                LastSeenTime = context.NewTracker?.Scene?.Time ?? context.AdventureStartTime,
                ConvertedToFull = false,
                Version = 0
            });

            var entry = AddLorebookEntry(scene, profile.Name, profile.Description, profile.Description, LorebookCategory.BackgroundCharacter);
            return new ManualContentOutput(nameof(ManualContentKind.Character), entry.Id, profile.Name, profile.Description);
        }

        // Arc-important / significant characters get a full profile, a subjective scene rewrite
        // (ExperientialNarrator) and a state assessment (ClinicalAssessor), then a Character entity.
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

        var entity = CharacterPersistence.BuildNewCharacter(character, scene, context.AdventureId);
        dbContext.Characters.Add(entity);

        return new ManualContentOutput(nameof(ManualContentKind.Character), entity.Id, character.Name, character.Description);
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
