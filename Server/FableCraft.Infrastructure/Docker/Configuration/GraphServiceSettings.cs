using FableCraft.Infrastructure.Persistence.Entities;

using Microsoft.Extensions.Configuration;

namespace FableCraft.Infrastructure.Docker.Configuration;

/// <summary>
/// Configuration settings for the knowledge graph service container.
/// </summary>
internal sealed class GraphServiceSettings
{
    public const string SectionName = "GraphService";

    /// <summary>
    /// Time of inactivity after which the container will be removed
    /// </summary>
    public TimeSpan EvictionTime { get; set; }

    /// <summary>
    /// Docker image name for the graph service.
    /// </summary>
    public string ImageName { get; set; } = "graph-rag-api:latest";

    /// <summary>
    /// Docker network name for service communication.
    /// </summary>
    public string NetworkName { get; set; } = "fablecraft-network";

    /// <summary>
    /// Port the graph service listens on inside the container.
    /// </summary>
    public int ContainerPort { get; set; } = 8111;

    /// <summary>
    /// Health check endpoint path.
    /// </summary>
    public string HealthEndpoint { get; set; } = "/health";

    public string BuildHealthCheck(int port, string name) => $"{GetContainerBaseUrl(port, name)}{HealthEndpoint}";

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
    /// Path for container logs (relative, used in non-Docker scenarios).
    /// </summary>
    public string LogsPath { get; set; } = "./logs/graphrag";

    /// <summary>
    /// Absolute host path for container logs (for Docker volume mounts).
    /// When set, this takes precedence over LogsPath.
    /// </summary>
    public string? LogsHostPath { get; set; }

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
    /// Gets the effective logs path for container log dumps.
    /// </summary>
    public string GetEffectiveLogsPath() => LogsHostPath ?? LogsPath;

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

    /// <summary>
    /// Maximum number of concurrent graph service containers.
    /// When exceeded, least recently used containers are evicted.
    /// </summary>
    public int MaxConcurrentContainers { get; set; } = 10;

    /// <summary>
    /// Base port for dynamic port allocation.
    /// Containers will be assigned ports starting from this value.
    /// </summary>
    public int BasePort { get; set; } = 8111;

    /// <summary>
    /// Prefix for adventure-specific container names.
    /// </summary>
    public string ContainerNamePrefix { get; set; } = "graph-rag-";

    /// <summary>
    /// Gets a unique container name for an adventure.
    /// Uses GUID without dashes, truncated to fit Docker's 63-char limit.
    /// </summary>
    public string GetContainerName(string name)
    {
        var fullName = $"{ContainerNamePrefix}{name}";
        return fullName.Length > 63 ? fullName[..63] : fullName;
    }

    /// <summary>
    /// Gets the base URL for an adventure's container given its assigned port.
    /// </summary>
    public string GetContainerBaseUrl(int port, string name) =>
        $"http://{name}:{port}";

    /// <summary>
    /// Builds environment variables from configuration/environment variables.
    /// Used as fallback when no GraphRagSettings are provided.
    /// </summary>
    public Dictionary<string, string> GetEnvVariable(IConfiguration config)
    {
        return GetEnvVariable(config, graphRagSettings: null);
    }

    /// <summary>
    /// Builds environment variables from GraphRagSettings entity if provided,
    /// otherwise falls back to configuration/environment variables.
    /// </summary>
    public Dictionary<string, string> GetEnvVariable(IConfiguration config, GraphRagSettings? graphRagSettings)
    {
        var environment = new Dictionary<string, string>();

        if (graphRagSettings != null)
        {
            environment["LLM_API_KEY"] = graphRagSettings.LlmApiKey;
            environment["LLM_MODEL"] = graphRagSettings.LlmModel;
            environment["LLM_PROVIDER"] = graphRagSettings.LlmProvider;

            if (!string.IsNullOrEmpty(graphRagSettings.LlmEndpoint))
                environment["LLM_ENDPOINT"] = graphRagSettings.LlmEndpoint;

            if (!string.IsNullOrEmpty(graphRagSettings.LlmApiVersion))
                environment["LLM_API_VERSION"] = graphRagSettings.LlmApiVersion;

            environment["LLM_MAX_TOKENS"] = graphRagSettings.LlmMaxTokens.ToString();
            environment["LLM_RATE_LIMIT_ENABLED"] = graphRagSettings.LlmRateLimitEnabled.ToString().ToLowerInvariant();
            environment["LLM_RATE_LIMIT_REQUESTS"] = graphRagSettings.LlmRateLimitRequests.ToString();
            environment["LLM_RATE_LIMIT_INTERVAL"] = graphRagSettings.LlmRateLimitInterval.ToString();

            environment["EMBEDDING_PROVIDER"] = graphRagSettings.EmbeddingProvider;
            environment["EMBEDDING_MODEL"] = graphRagSettings.EmbeddingModel;

            if (!string.IsNullOrEmpty(graphRagSettings.EmbeddingEndpoint))
                environment["EMBEDDING_ENDPOINT"] = graphRagSettings.EmbeddingEndpoint;

            if (!string.IsNullOrEmpty(graphRagSettings.EmbeddingApiVersion))
                environment["EMBEDDING_API_VERSION"] = graphRagSettings.EmbeddingApiVersion;

            var embeddingApiKey = !string.IsNullOrEmpty(graphRagSettings.EmbeddingApiKey)
                ? graphRagSettings.EmbeddingApiKey
                : graphRagSettings.LlmApiKey;
            environment["EMBEDDING_API_KEY"] = embeddingApiKey;

            environment["EMBEDDING_DIMENSIONS"] = graphRagSettings.EmbeddingDimensions.ToString();
            environment["EMBEDDING_MAX_TOKENS"] = graphRagSettings.EmbeddingMaxTokens.ToString();
            environment["EMBEDDING_BATCH_SIZE"] = graphRagSettings.EmbeddingBatchSize.ToString();

            if (!string.IsNullOrEmpty(graphRagSettings.HuggingfaceTokenizer))
                environment["HUGGINGFACE_TOKENIZER"] = graphRagSettings.HuggingfaceTokenizer;
        }
        else
        {
            AddEnvVar(environment, "LLM_API_KEY", "LLM_API_KEY");
            AddEnvVar(environment, "LLM_MODEL", "LLM_MODEL");
            AddEnvVar(environment, "LLM_PROVIDER", "LLM_PROVIDER");
            AddEnvVar(environment, "LLM_ENDPOINT", "LLM_ENDPOINT");
            AddEnvVar(environment, "LLM_API_VERSION", "LLM_API_VERSION");
            AddEnvVar(environment, "LLM_MAX_TOKENS", "LLM_MAX_TOKENS");
            AddEnvVar(environment, "LLM_RATE_LIMIT_ENABLED", "LLM_RATE_LIMIT_ENABLED");
            AddEnvVar(environment, "LLM_RATE_LIMIT_REQUESTS", "LLM_RATE_LIMIT_REQUESTS");
            AddEnvVar(environment, "LLM_RATE_LIMIT_INTERVAL", "LLM_RATE_LIMIT_INTERVAL");

            AddEnvVar(environment, "EMBEDDING_PROVIDER", "EMBEDDING_PROVIDER");
            AddEnvVar(environment, "EMBEDDING_MODEL", "EMBEDDING_MODEL");
            AddEnvVar(environment, "EMBEDDING_ENDPOINT", "EMBEDDING_ENDPOINT");
            AddEnvVar(environment, "EMBEDDING_API_VERSION", "EMBEDDING_API_VERSION");
            AddEnvVar(environment, "EMBEDDING_DIMENSIONS", "EMBEDDING_DIMENSIONS");
            AddEnvVar(environment, "EMBEDDING_MAX_TOKENS", "EMBEDDING_MAX_TOKENS");
            AddEnvVar(environment, "EMBEDDING_BATCH_SIZE", "EMBEDDING_BATCH_SIZE");
            AddEnvVar(environment, "HUGGINGFACE_TOKENIZER", "HUGGINGFACE_TOKENIZER");

            void AddEnvVar(Dictionary<string, string> env, string key, string envVarName)
            {
                var value = config[key]
                            ?? Environment.GetEnvironmentVariable(envVarName);

                if (!string.IsNullOrEmpty(value))
                {
                    env[key] = value;
                }
            }
        }

        environment["DATA_ROOT_DIRECTORY"] = $"{VolumeMountPath}/data";
        environment["SYSTEM_ROOT_DIRECTORY"] = $"{VolumeMountPath}/system";

        return environment;
    }
}