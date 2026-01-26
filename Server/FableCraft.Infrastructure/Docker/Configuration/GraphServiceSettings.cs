namespace FableCraft.Infrastructure.Docker.Configuration;

/// <summary>
/// Configuration settings for the knowledge graph service container.
/// </summary>
public sealed class GraphServiceSettings
{
    public const string SectionName = "GraphService";

    /// <summary>
    /// Docker image name for the graph service.
    /// </summary>
    public string ImageName { get; set; } = "graph-rag-api:latest";

    /// <summary>
    /// Container name for the graph service.
    /// </summary>
    public string ContainerName { get; set; } = "graph-rag-api";

    /// <summary>
    /// Docker network name for service communication.
    /// </summary>
    public string NetworkName { get; set; } = "fablecraft-network";

    /// <summary>
    /// Host port to expose the graph service on.
    /// </summary>
    public int Port { get; set; } = 8111;

    /// <summary>
    /// Port the graph service listens on inside the container.
    /// </summary>
    public int ContainerPort { get; set; } = 8111;

    /// <summary>
    /// Health check endpoint path.
    /// </summary>
    public string HealthEndpoint { get; set; } = "/health";

    /// <summary>
    /// Host to use for health checks. Defaults to container name.
    /// Set to "localhost" when running tests from the host machine.
    /// </summary>
    public string? HealthCheckHost { get; set; }

    /// <summary>
    /// Timeout in seconds for health checks.
    /// </summary>
    public int HealthCheckTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Path for visualization output (relative, used in non-Docker scenarios).
    /// </summary>
    public string VisualizationPath { get; set; } = "./visualization";

    /// <summary>
    /// Absolute host path for visualization output (for Docker volume mounts).
    /// When set, this takes precedence over VisualizationPath for container creation.
    /// </summary>
    public string? VisualizationHostPath { get; set; }

    /// <summary>
    /// Path for data store (relative, used in non-Docker scenarios).
    /// </summary>
    public string DataStorePath { get; set; } = "./data-store";

    /// <summary>
    /// Absolute host path for data store (for Docker volume mounts).
    /// When set, this takes precedence over DataStorePath for container creation.
    /// </summary>
    public string? DataStoreHostPath { get; set; }

    /// <summary>
    /// Path for volume exports (relative, used in non-Docker scenarios).
    /// </summary>
    public string ExportsPath { get; set; } = "./exports";

    /// <summary>
    /// Absolute host path for exports (for Docker volume mounts).
    /// When set, this takes precedence over ExportsPath for container creation.
    /// </summary>
    public string? ExportsHostPath { get; set; }

    /// <summary>
    /// Gets the effective data store path for Docker volume mounts.
    /// Returns the absolute host path if configured, otherwise falls back to relative path.
    /// </summary>
    public string GetEffectiveDataStorePath() => DataStoreHostPath ?? DataStorePath;

    /// <summary>
    /// Gets the effective visualization path for Docker volume mounts.
    /// </summary>
    public string GetEffectiveVisualizationPath() => VisualizationHostPath ?? VisualizationPath;

    /// <summary>
    /// Gets the effective exports path for Docker volume mounts.
    /// </summary>
    public string GetEffectiveExportsPath() => ExportsHostPath ?? ExportsPath;

    /// <summary>
    /// Volume name prefix for worldbook templates.
    /// </summary>
    public string WorldbookVolumePrefix { get; set; } = "kg-worldbook-";

    /// <summary>
    /// Volume name prefix for adventure contexts.
    /// </summary>
    public string AdventureVolumePrefix { get; set; } = "kg-adventure-";

    /// <summary>
    /// Volume mount path inside the graph service container.
    /// Mounts the entire cognee folder to include both data and system directories.
    /// </summary>
    public string VolumeMountPath { get; set; } = "/app/cognee";

    /// <summary>
    /// Gets the volume name for a worldbook template.
    /// </summary>
    public string GetWorldbookVolumeName(Guid worldbookId) =>
        $"{WorldbookVolumePrefix}{worldbookId}";

    /// <summary>
    /// Gets the volume name for an adventure context.
    /// </summary>
    public string GetAdventureVolumeName(Guid adventureId) =>
        $"{AdventureVolumePrefix}{adventureId}";
}
