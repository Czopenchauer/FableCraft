namespace FableCraft.Application.Exceptions;

public sealed class SceneGenerationConcurrencyException : Exception
{
    public SceneGenerationConcurrencyException(Guid adventureId) : base($"A scene generation is already in progress for adventure {adventureId}.")
    {
    }
}

public sealed class SceneEnrichmentConcurrencyException : Exception
{
    public SceneEnrichmentConcurrencyException(Guid sceneId) : base($"A scene enrichment is already in progress for scene {sceneId}.")
    {
    }
}