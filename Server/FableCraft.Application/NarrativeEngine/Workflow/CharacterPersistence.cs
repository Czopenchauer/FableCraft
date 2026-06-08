using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

namespace FableCraft.Application.NarrativeEngine.Workflow;

/// <summary>
///     Shared helpers for turning a crafted <see cref="CharacterContext" /> into persistable
///     entity graphs. Used by both the enrichment save path (<see cref="SaveSceneEnrichment" />)
///     and manual content creation (<see cref="ManualContentService" />).
/// </summary>
internal static class CharacterPersistence
{
    /// <summary>
    ///     Builds a brand-new <see cref="Character" /> entity (with its initial state,
    ///     relationships, and scene rewrites) introduced by the given scene.
    /// </summary>
    public static Character BuildNewCharacter(CharacterContext source, Scene scene, Guid adventureId)
    {
        var relationships = source.Relationships.Select(x => new CharacterRelationship
        {
            TargetCharacterName = x.TargetCharacterName,
            Data = x.Data,
            SequenceNumber = x.SequenceNumber,
            Scene = scene,
            UpdateTime = x.UpdateTime,
            Dynamic = x.Dynamic
        }).ToList();

        var sceneRewrites = source.SceneRewrites.Select(x => new CharacterSceneRewrite
        {
            Content = x.Content,
            SequenceNumber = x.SequenceNumber,
            Scene = scene,
            SceneTracker = x.SceneTracker!,
            GatheredContext = x.GatheredContext,
            StorySummary = x.StorySummary
        }).ToList();

        return new Character
        {
            AdventureId = adventureId,
            Name = source.Name,
            CharacterStates =
            [
                new CharacterState
                {
                    CharacterStats = source.CharacterState,
                    Tracker = source.CharacterTracker!,
                    SequenceNumber = 0,
                    Scene = scene,
                    IsDead = false,
                    SimulationMetadata = source.SimulationMetadata
                }
            ],
            Version = 0,
            CharacterRelationships = relationships,
            CharacterSceneRewrites = sceneRewrites,
            Importance = source.Importance,
            IntroductionScene = scene.Id,
            Scene = scene,
            Description = source.Description
        };
    }
}
