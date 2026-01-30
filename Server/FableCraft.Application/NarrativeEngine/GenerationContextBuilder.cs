using System.Text.Json;

using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;

namespace FableCraft.Application.NarrativeEngine;

internal interface IGenerationContextBuilder
{
    Task<GenerationContext> BuildRegenerationContextAsync(Guid adventureId, Scene scene, CancellationToken ct);

    Task<GenerationContext> BuildEnrichmentContextAsync(Guid adventureId, CancellationToken ct);

    Task<(GenerationContext Context, GenerationProcessStep Step)> GetOrCreateGenerationContextAsync(
        Guid adventureId,
        string playerAction,
        CancellationToken ct);
}

internal sealed class GenerationContextBuilder(ApplicationDbContext dbContext) : IGenerationContextBuilder
{
    private const int NumberOfScenesToInclude = 20;

    public async Task<GenerationContext> BuildRegenerationContextAsync(
        Guid adventureId,
        Scene scene,
        CancellationToken ct)
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

        // Get previous scenes for context (skip the current scene being regenerated)
        var scenes = await dbContext.Scenes
            .AsSplitQuery()
            .Where(x => x.AdventureId == adventureId && x.SequenceNumber < scene.SequenceNumber)
            .Include(x => x.CharacterActions)
            .Include(x => x.CharacterMemories)
            .Include(x => x.CharacterRelationships)
            .Include(x => x.CharacterSceneRewrites)
            .OrderByDescending(x => x.SequenceNumber)
            .Take(NumberOfScenesToInclude)
            .ToListAsync(ct);

        var lorebooks = await dbContext.Scenes
            .AsSplitQuery()
            .Where(x => x.AdventureId == adventureId && x.SequenceNumber < scene.SequenceNumber)
            .OrderByDescending(x => x.SequenceNumber)
            .Take(1)
            .Include(x => x.Lorebooks)
            .SelectMany(x => x.Lorebooks)
            .ToArrayAsync(ct);

        var createdLore = lorebooks.Where(x => x.Category == nameof(LorebookCategory.Lore)).ToArray();
        var createdLocations = lorebooks.Where(x => x.Category == nameof(LorebookCategory.Location)).ToArray();
        var createdItems = lorebooks.Where(x => x.Category == nameof(LorebookCategory.Item)).ToArray();

        // Skip the most recent character state as that's the one being regenerated
        var (existingCharContext, newCharContext) = await GetCharactersForRegenerationAsync(scene.Id, adventureId, ct);

        var context = new GenerationContext
        {
            AdventureId = adventureId,
            PlayerAction = scene.CharacterActions.FirstOrDefault(x => x.Selected)?.ActionDescription ?? string.Empty,
            NewSceneId = scene.Id,
            NewResolution = scene.Metadata.ResolutionOutput,
            NewScene = new GeneratedScene
            {
                Scene = scene.NarrativeText,
                Choices = scene.CharacterActions.Select(x => x.ActionDescription).ToArray()
            },
            NewTracker = scene.Metadata.Tracker != null
                ? new Tracker
                {
                    Scene = scene.Metadata.Tracker.Scene,
                    MainCharacter = scene.Metadata.Tracker.MainCharacter
                }
                : null,
            CharacterUpdates = scene.CharacterStates.Where(c => c.SequenceNumber != 0).Select(cs => new CharacterContext
            {
                CharacterId = cs.CharacterId,
                Name = cs.CharacterStats.Name!,
                Description = existingCharContext.Single(x => x.CharacterId == cs.CharacterId)
                    .Description,
                CharacterState = cs.CharacterStats,
                CharacterTracker = cs.Tracker,
                CharacterMemories = scene.CharacterMemories.Where(x => x.CharacterId == cs.CharacterId)
                    .Select(x => new MemoryContext
                    {
                        MemoryContent = x.Summary,
                        Salience = x.Salience,
                        Data = x.Data,
                        SceneTracker = x.SceneTracker
                    })
                    .ToList(),
                Relationships = scene.CharacterRelationships.Where(x => x.CharacterId == cs.CharacterId)
                    .Select(x => new CharacterRelationshipContext
                    {
                        TargetCharacterName = x.TargetCharacterName,
                        Data = x.Data,
                        UpdateTime = x.UpdateTime,
                        SequenceNumber = x.SequenceNumber,
                        Dynamic = x.Dynamic!
                    })
                    .ToList(),
                SceneRewrites = scene.CharacterSceneRewrites.Where(x => x.CharacterId == cs.CharacterId)
                    .Select(x => new CharacterSceneContext
                    {
                        Content = x.Content,
                        SceneTracker = x.SceneTracker,
                        SequenceNumber = x.SequenceNumber
                    })
                    .ToList(),
                Importance = existingCharContext.Single(ac => ac.CharacterId == cs.CharacterId)
                    .Importance,
                SimulationMetadata = cs.SimulationMetadata,
                IsDead = cs.IsDead
            }).ToList(),
            // Sequence number 0 indicates newly introduced characters in this scene
            NewCharacters = newCharContext,
            NewLore = scene.Lorebooks.Where(x => x.Category == nameof(LorebookCategory.Lore))
                .Select(lb => JsonSerializer.Deserialize<GeneratedLore>(lb.Content)!).ToList(),
            NewLocations = scene.Lorebooks.Where(x => x.Category == nameof(LorebookCategory.Location))
                .Select(lb => JsonSerializer.Deserialize<LocationGenerationResult>(lb.Content)!).ToArray(),
            NewItems = scene.Lorebooks.Where(x => x.Category == nameof(LorebookCategory.Item))
                .Select(lb => JsonSerializer.Deserialize<GeneratedItem>(lb.Content)!).ToArray()
        };

        var previousBackgroundCharacters = await GetBackgroundCharactersFromPreviousSceneAsync(adventureId, context.ContextGathered?.BackgroundRoster ?? [], ct);

        context.SetupRequiredFields(
            scenes.Select(SceneContext.CreateFromScene).ToArray(),
            adventure.TrackerStructure,
            adventure.MainCharacter,
            existingCharContext,
            adventure.AgentLlmPresets.ToArray(),
            adventure.PromptPaths,
            adventure.AdventureStartTime,
            createdLore,
            createdLocations,
            createdItems,
            previousBackgroundCharacters);

        return context;
    }

    public async Task<GenerationContext> BuildEnrichmentContextAsync(
        Guid adventureId,
        CancellationToken ct)
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

        // Skip the most recent scene as that's the one being enriched, and it has separate field
        var scenes = await dbContext.Scenes
            .Where(x => x.AdventureId == adventureId)
            .Include(x => x.CharacterActions)
            .OrderByDescending(x => x.SequenceNumber)
            .Take(NumberOfScenesToInclude)
            .Skip(1)
            .ToListAsync(ct);

        var generationProcess = await dbContext.GenerationProcesses.FirstAsync(x => x.AdventureId == adventureId, ct);
        var generationContext = generationProcess.GetContextAs<GenerationContext>();
        var adventureCharacters = await GetCharactersAsync(adventureId, ct);
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

        var previousBackgroundCharacters = await GetBackgroundCharactersFromPreviousSceneAsync(adventureId, generationContext.ContextGathered?.BackgroundRoster ?? [], ct);
        generationContext.SetupRequiredFields(
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
            previousBackgroundCharacters);
        return generationContext;
    }

    public async Task<(GenerationContext Context, GenerationProcessStep Step)> GetOrCreateGenerationContextAsync(
        Guid adventureId,
        string playerAction,
        CancellationToken ct)
    {
        var adventure = await dbContext
            .Adventures
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

        var scenes = await dbContext
            .Scenes
            .Where(x => x.AdventureId == adventureId)
            .Include(x => x.CharacterActions)
            .OrderByDescending(x => x.SequenceNumber)
            .Take(NumberOfScenesToInclude)
            .ToListAsync(ct);
        var adventureCharacters = await GetCharactersAsync(adventureId, ct);
        var generationProcess = await dbContext.GenerationProcesses.Where(x => x.AdventureId == adventureId).FirstOrDefaultAsync(ct);
        (GenerationContext newContext, GenerationProcess process) context = (null, generationProcess)!;
        if (generationProcess != null)
        {
            context.newContext = generationProcess.GetContextAs<GenerationContext>();
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (context.newContext == null || context.newContext.PlayerAction != playerAction)
            {
                await dbContext.GenerationProcesses
                    .Where(x => x.AdventureId == adventureId)
                    .ExecuteDeleteAsync(ct);
                context = await CreateNewProcess();
            }
        }
        else
        {
            context = await CreateNewProcess();
        }

        if (context.newContext == null)
        {
            throw new InvalidOperationException("Failed to deserialize generation context from the database.");
        }

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

        var previousBackgroundCharacters = await GetBackgroundCharactersFromPreviousSceneAsync(adventureId, context.newContext.ContextGathered?.BackgroundRoster ?? [], ct);

        List<ExtraLoreContext>? extraLoreEntries = null;
        if (scenes.Count == 0)
        {
            extraLoreEntries = await dbContext.LorebookEntries
                .Where(x => x.AdventureId == adventureId)
                .Select(x => new ExtraLoreContext(x.Title ?? x.Description, x.Content, x.Category))
                .ToListAsync(ct);
        }

        context.newContext.SetupRequiredFields(
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
            previousBackgroundCharacters,
            extraLoreEntries);
        return (context.newContext, context.process!.Step);

        async Task<(GenerationContext newContext, GenerationProcess process)> CreateNewProcess()
        {
            var newContext = new GenerationContext
            {
                AdventureId = adventureId,
                PlayerAction = playerAction,
                NewSceneId = Guid.NewGuid()
            };
            var process = new GenerationProcess
            {
                AdventureId = adventureId,
                Context = newContext.ToJsonString(),
                Step = GenerationProcessStep.NotStarted
            };
            await dbContext.GenerationProcesses.AddAsync(process, ct);
            await dbContext.SaveChangesAsync(ct);
            return (newContext, process);
        }
    }

    private async Task<List<CharacterContext>> GetCharactersAsync(Guid adventureId, CancellationToken ct)
    {
        var existingCharacters = await dbContext
            .Characters
            .AsSplitQuery()
            .Where(x => x.AdventureId == adventureId)
            .Include(x => x.CharacterStates.OrderByDescending(cs => cs.SequenceNumber).Take(1))
            .Include(x => x.CharacterMemories)
            .Include(x => x.CharacterRelationships)
            .Include(x => x.CharacterSceneRewrites.OrderByDescending(c => c.SequenceNumber).Take(20))
            .ToListAsync(ct);

        return existingCharacters
            .Where(x => x.CharacterStates.Count == 1 && !x.CharacterStates.Single().IsDead)
            .Select(x => new CharacterContext
            {
                Description = x.Description,
                Name = x.Name,
                CharacterState = x.CharacterStates.Single()
                    .CharacterStats,
                CharacterTracker = x.CharacterStates.Single()
                    .Tracker,
                CharacterId = x.Id,
                CharacterMemories = x.CharacterMemories.Select(y => new MemoryContext
                    {
                        MemoryContent = y.Summary,
                        Salience = y.Salience,
                        Data = y.Data,
                        SceneTracker = y.SceneTracker
                    })
                    .ToList(),
                // Group by target and take latest relationship per target character
                Relationships = x.CharacterRelationships
                    .GroupBy(r => r.TargetCharacterName)
                    .Select(g => g.OrderByDescending(r => r.SequenceNumber)
                        .First())
                    .Select(y => new CharacterRelationshipContext
                    {
                        TargetCharacterName = y!.TargetCharacterName,
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
                    })
                    .ToList(),
                Importance = x.Importance,
                SimulationMetadata = x.CharacterStates.Single()
                    .SimulationMetadata,
                IsDead = x.CharacterStates.Single().IsDead,
            }).ToList();
    }

    private async Task<List<BackgroundCharacter>> GetBackgroundCharactersFromPreviousSceneAsync(
        Guid adventureId,
        string[] backgroundCharacters,
        CancellationToken ct)
    {
        return await dbContext.BackgroundCharacters
            .Where(x => x.AdventureId == adventureId
                        && backgroundCharacters.Contains(x.Name) && !x.ConvertedToFull)
            .ToListAsync(ct);
    }

    private async Task<(List<CharacterContext> Existing, List<CharacterContext> New)> GetCharactersForRegenerationAsync(
        Guid sceneId,
        Guid adventureId,
        CancellationToken ct)
    {
        var existingCharacters = await dbContext
            .Characters
            .AsSplitQuery()
            .Where(x => x.AdventureId == adventureId && x.IntroductionScene != sceneId)
            .Include(x => x.CharacterStates.Where(cs => cs.SceneId != sceneId).OrderByDescending(cs => cs.SequenceNumber).Take(1))
            .Include(x => x.CharacterMemories.Where(z => z.SceneId != sceneId))
            .Include(x => x.CharacterRelationships)
            .Include(x => x.CharacterSceneRewrites.Where(z => z.SceneId != sceneId).OrderByDescending(c => c.SequenceNumber).Take(20))
            .ToListAsync(ct);

        var newlyCreatedCharacters = await dbContext
            .Characters
            .AsSplitQuery()
            .Where(x => x.AdventureId == adventureId && x.IntroductionScene == sceneId)
            .Include(x => x.CharacterStates)
            .Include(x => x.CharacterMemories)
            .Include(x => x.CharacterRelationships)
            .Include(x => x.CharacterSceneRewrites)
            .ToListAsync(ct);

        var existingCharContext = existingCharacters
            .Where(x => x.CharacterStates.Count == 1 && !x.CharacterStates.Single().IsDead)
            .Select(x => new CharacterContext
            {
                Description = x
                    .Description,
                Name = x.Name,
                CharacterState = x.CharacterStates.Single()
                    .CharacterStats,
                CharacterTracker = x.CharacterStates.Single()
                    .Tracker,
                CharacterId = x.Id,
                CharacterMemories = x.CharacterMemories.Select(y => new MemoryContext
                    {
                        MemoryContent = y.Summary,
                        Salience = y.Salience,
                        Data = y.Data,
                        SceneTracker = y.SceneTracker
                    })
                    .ToList(),
                // Group by target and take latest relationship per target (excluding the scene being regenerated)
                Relationships = x.CharacterRelationships
                    .Where(r => r.SceneId != sceneId)
                    .GroupBy(r => r.TargetCharacterName)
                    .Select(g => g.OrderByDescending(r => r.SequenceNumber)
                        .FirstOrDefault())
                    .Where(r => r != null)
                    .Select(y => new CharacterRelationshipContext
                    {
                        TargetCharacterName = y!.TargetCharacterName,
                        Data = y.Data,
                        UpdateTime = y.UpdateTime,
                        SequenceNumber = y.SequenceNumber,
                        Dynamic = y.Dynamic!
                    })
                    .ToList()!,
                SceneRewrites = x.CharacterSceneRewrites.Select(y => new CharacterSceneContext
                    {
                        Content = y.Content,
                        SceneTracker = y.SceneTracker,
                        SequenceNumber = y.SequenceNumber
                    })
                    .ToList(),
                Importance = x.Importance,
                SimulationMetadata = x.CharacterStates.Single()
                    .SimulationMetadata,
                IsDead = x.CharacterStates.Single().IsDead,
            }).ToList();

        var newCharContext = newlyCreatedCharacters
            .Where(x => !x.CharacterStates.Single().IsDead)
            .Select(x => new CharacterContext
            {
                Description = x
                    .Description,
                Name = x.Name,
                CharacterState = x.CharacterStates.Single()
                    .CharacterStats,
                CharacterTracker = x.CharacterStates.Single()
                    .Tracker,
                CharacterId = x.Id,
                CharacterMemories = x.CharacterMemories.Select(y => new MemoryContext
                    {
                        MemoryContent = y.Summary,
                        Salience = y.Salience,
                        Data = y.Data,
                        SceneTracker = y.SceneTracker
                    })
                    .ToList(),
                Relationships = x.CharacterRelationships
                    .Select(y => new CharacterRelationshipContext
                    {
                        TargetCharacterName = y!.TargetCharacterName,
                        Data = y.Data,
                        UpdateTime = y.UpdateTime,
                        SequenceNumber = y.SequenceNumber,
                        Dynamic = y.Dynamic!
                    })
                    .ToList()!,
                SceneRewrites = x.CharacterSceneRewrites.Select(y => new CharacterSceneContext
                    {
                        Content = y.Content,
                        SceneTracker = y.SceneTracker,
                        SequenceNumber = y.SequenceNumber
                    })
                    .ToList(),
                Importance = x.Importance,
                SimulationMetadata = x.CharacterStates.Single()
                    .SimulationMetadata,
                IsDead = false
            }).ToList();

        return (existingCharContext, newCharContext);
    }
}
