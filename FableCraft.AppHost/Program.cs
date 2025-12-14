using FableCraft.AppHost;

using Projects;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

var serverDatabase = builder
    .AddPostgres("fablecraftdb-npgsql", port: 6999)
    .WithImage("postgres", "18.0")
    .WithDataVolumeForV18()
    .AddDatabase("fablecraftdb", "fablecraftdb");

var graphRagLlmMaxTokens = builder.Configuration["FableCraft:Server:GraphRag:MaxTokens"] ?? "16384";
var graphRagLlmApiKey = builder.Configuration["FableCraft:GraphRag:LLM:ApiKey"]!;
var graphRagLlmModel = builder.Configuration["FableCraft:GraphRag:LLM:Model"]!;
var graphRagLlmProvider = builder.Configuration["FableCraft:GraphRag:LLM:Provider"]!;
var graphRagLlmEndpoint = builder.Configuration["FableCraft:GraphRag:LLM:BaseUrl"] ?? "";
var graphRagLlmApiVersion = builder.Configuration["FableCraft:GraphRag:LLM:ApiVersion"] ?? "";
var llmRateLimitEnabled = builder.Configuration["FableCraft:GraphRag:LLM:RateLimitEnabled"] ?? "true";
var llmRateLimitRequests = builder.Configuration["FableCraft:GraphRag:LLM:RateLimitRequests"] ?? "60";
var llmRateLimitInterval = builder.Configuration["FableCraft:GraphRag:LLM:RateLimitInterval"] ?? "60";

// GraphRag Embedding Configuration
var embeddingProvider = builder.Configuration["FableCraft:GraphRag:Embedding:Provider"]!;
var embeddingModel = builder.Configuration["FableCraft:GraphRag:Embedding:Model"]!;
var embeddingEndpoint = builder.Configuration["FableCraft:GraphRag:Embedding:BaseUrl"] ?? "";
var embeddingApiVersion = builder.Configuration["FableCraft:GraphRag:Embedding:ApiVersion"] ?? "";
var embeddingDimensions = builder.Configuration["FableCraft:GraphRag:Embedding:Dimensions"] ?? "3072";
var embeddingMaxTokens = builder.Configuration["FableCraft:GraphRag:Embedding:MaxTokens"] ?? "8191";
var embeddingBatchSize = builder.Configuration["FableCraft:GraphRag:Embedding:BatchSize"] ?? "36";
var huggingFaceTokenizer = builder.Configuration["FableCraft:GraphRag:HuggingFaceTokenizer"] ?? "";

var cache = builder.AddAzureRedis("fablecraft-redis")
    .RunAsContainer();
var graphRagApi = builder
    .AddPythonApp("graph-rag-api", "../GraphRag", "api.py")
    .WithHttpEndpoint(env: "PORT", port: 8111, name: "graphRagApi")
    .WithExternalHttpEndpoints()
    .WithOtlpExporter()
    .WithReference(cache)
    .WithEnvironment(async context =>
    {
        EndpointReference redisEndpoint = cache.Resource.GetEndpoint("tcp");
        context.EnvironmentVariables["CACHE_HOST"] = redisEndpoint.Property(EndpointProperty.Host);
        context.EnvironmentVariables["CACHE_PORT"] = redisEndpoint.Property(EndpointProperty.Port);
        context.EnvironmentVariables["CACHE_USERNAME"] = "";
        context.EnvironmentVariables["CACHE_PASSWORD"] = (await cache.Resource.Password!.GetValueAsync(CancellationToken.None))!;
    })
    .WithEnvironment("LITELLM_LOG", "INFO")
    .WithEnvironment("TOKENIZERS_PARALLELISM", "true")
    .WithEnvironment("LLM_API_KEY", graphRagLlmApiKey)
    .WithEnvironment("LLM_MODEL", graphRagLlmModel)
    .WithEnvironment("LLM_PROVIDER", graphRagLlmProvider)
    .WithEnvironment("LLM_ENDPOINT", graphRagLlmEndpoint)
    .WithEnvironment("LLM_API_VERSION", graphRagLlmApiVersion)
    .WithEnvironment("LLM_MAX_TOKENS", graphRagLlmMaxTokens)
    .WithEnvironment("LLM_RATE_LIMIT_ENABLED", llmRateLimitEnabled)
    .WithEnvironment("LLM_RATE_LIMIT_REQUESTS", llmRateLimitRequests)
    .WithEnvironment("LLM_RATE_LIMIT_INTERVAL", llmRateLimitInterval)
    .WithEnvironment("EMBEDDING_PROVIDER", embeddingProvider)
    .WithEnvironment("EMBEDDING_MODEL", embeddingModel)
    .WithEnvironment("EMBEDDING_ENDPOINT", embeddingEndpoint)
    .WithEnvironment("EMBEDDING_API_VERSION", embeddingApiVersion)
    .WithEnvironment("EMBEDDING_DIMENSIONS", embeddingDimensions)
    .WithEnvironment("EMBEDDING_MAX_TOKENS", embeddingMaxTokens)
    .WithEnvironment("EMBEDDING_BATCH_SIZE", embeddingBatchSize)
    .WithEnvironment("HUGGINGFACE_TOKENIZER", huggingFaceTokenizer)
    .WithEnvironment("ENABLE_BACKEND_ACCESS_CONTROL", "true")
    .WithEnvironment("TELEMETRY_DISABLED", "true")
    .WithEnvironment("VISUALISATION_PATH", @$"{TryGetSolutionDirectoryInfo().FullName}\visualization")
    .WithEnvironment("DATA_ROOT_DIRECTORY", @$"{TryGetSolutionDirectoryInfo().FullName}\cognee\data\")
    .WithEnvironment("SYSTEM_ROOT_DIRECTORY", @$"{TryGetSolutionDirectoryInfo().FullName}\cognee\system\")
    .WithEnvironment("CACHING", "true");

var promptPath = @$"{TryGetSolutionDirectoryInfo().FullName}\Prompts\Default\";

var server = builder
    .AddProject<FableCraft_Server>("fablecraft-server")
    .WithReference(graphRagApi)
    .WithReference(serverDatabase)
    .WithEnvironment("FABLECRAFT_DATA_STORE", @$"{TryGetSolutionDirectoryInfo().FullName}\data-store")
    .WithEnvironment("FABLECRAFT_LOG_PATH", @$"{TryGetSolutionDirectoryInfo().FullName}\logs\")
    .WithEnvironment("DEFAULT_PROMPT_PATH", promptPath)
    .WaitFor(graphRagApi)
    .WaitFor(serverDatabase);

builder.AddNpmApp("fablecraft-client", "../FableCraft.Client")
    .WithHttpEndpoint(env: "PORT", port: 4211)
    .WithExternalHttpEndpoints()
    .WithOtlpExporter()
    .WithReference(server)
    .WaitFor(server);

builder.Build().Run();


static DirectoryInfo TryGetSolutionDirectoryInfo()
{
    var currentPath = Directory.GetCurrentDirectory();
    var directory = new DirectoryInfo(currentPath);
    while (directory != null && !directory.GetFiles("*.sln").Any())
    {
        directory = directory.Parent;
    }
    return directory!;
}