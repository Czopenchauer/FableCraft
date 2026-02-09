using FableCraft.AppHost;

using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// Check if we should use an external PostgreSQL (e.g., from Docker Compose)
var useExternalDb = builder.Configuration["FableCraft:UseExternalDatabase"] == "true";

IResourceBuilder<IResourceWithConnectionString> serverDatabase = builder
    .AddPostgres("fablecraftdb-npgsql", port: 6999)
    .WithImage("postgres", "18.0")
    .WithDataVolumeForV18("fablecraft-postgres-data")
    .AddDatabase("fablecraftdb", "fablecraftdb");

var graphRagLlmMaxTokens = builder.Configuration["FableCraft:Server:GraphRag:MaxTokens"] ?? "";
var graphRagLlmApiKey = builder.Configuration["FableCraft:GraphRag:LLM:ApiKey"]!;
var graphRagLlmModel = builder.Configuration["FableCraft:GraphRag:LLM:Model"]!;
var graphRagLlmProvider = builder.Configuration["FableCraft:GraphRag:LLM:Provider"]!;
var graphRagLlmEndpoint = builder.Configuration["FableCraft:GraphRag:LLM:BaseUrl"] ?? "";
var graphRagLlmApiVersion = builder.Configuration["FableCraft:GraphRag:LLM:ApiVersion"] ?? "";
var llmRateLimitEnabled = builder.Configuration["FableCraft:GraphRag:LLM:RateLimitEnabled"] ?? "";
var llmRateLimitRequests = builder.Configuration["FableCraft:GraphRag:LLM:RateLimitRequests"] ?? "";
var llmRateLimitInterval = builder.Configuration["FableCraft:GraphRag:LLM:RateLimitInterval"] ?? "";

// GraphRag Embedding Configuration
var embeddingProvider = builder.Configuration["FableCraft:GraphRag:Embedding:Provider"]!;
var embeddingModel = builder.Configuration["FableCraft:GraphRag:Embedding:Model"]!;
var embeddingEndpoint = builder.Configuration["FableCraft:GraphRag:Embedding:BaseUrl"] ?? "";
var embeddingApiVersion = builder.Configuration["FableCraft:GraphRag:Embedding:ApiVersion"] ?? "";
var embeddingDimensions = builder.Configuration["FableCraft:GraphRag:Embedding:Dimensions"] ?? "";
var embeddingMaxTokens = builder.Configuration["FableCraft:GraphRag:Embedding:MaxTokens"] ?? "";
var embeddingApiKey = builder.Configuration["FableCraft:GraphRag:Embedding:ApiKey"] ?? "";
var embeddingBatchSize = builder.Configuration["FableCraft:GraphRag:Embedding:BatchSize"] ?? "";
var huggingFaceTokenizer = builder.Configuration["FableCraft:GraphRag:HuggingFaceTokenizer"] ?? "";

var graphRagApi = builder
    .AddPythonApp("graph-rag-api", "../GraphRag", "api.py")
    .WithHttpEndpoint(env: "PORT", port: 8111, name: "graphRagApi")
    .WithExternalHttpEndpoints()
    .WithOtlpExporter()
    .WithEnvironment("LANGFUSE_PUBLIC_KEY", "default")
    .WithEnvironment("LANGFUSE_SECRET_KEY", "default")
    .WithEnvironment("LLM_ENDPOINT", "http://localhost:3000")
    .WithEnvironment("LITELLM_LOG", "INFO")
    .WithEnvironment("TOKENIZERS_PARALLELISM", "true")
    .WithEnvironment("LLM_API_KEY", graphRagLlmApiKey)
    .WithEnvironment("LLM_MODEL", graphRagLlmModel)
    .WithEnvironment("LLM_PROVIDER", graphRagLlmProvider)
    .WithEnvironment("LLM_ENDPOINT", graphRagLlmEndpoint)
    .WithEnvironment("LLM_API_VERSION", graphRagLlmApiVersion)
    .WithEnvironment("LLM_MAX_TOKENS", graphRagLlmMaxTokens)
    // .WithEnvironment("LLM_RATE_LIMIT_ENABLED", llmRateLimitEnabled)
    // .WithEnvironment("LLM_RATE_LIMIT_REQUESTS", llmRateLimitRequests)
    //.WithEnvironment("LLM_RATE_LIMIT_INTERVAL", llmRateLimitInterval)
    .WithEnvironment("EMBEDDING_PROVIDER", embeddingProvider)
    .WithEnvironment("EMBEDDING_MODEL", embeddingModel)
    .WithEnvironment("EMBEDDING_ENDPOINT", embeddingEndpoint)
    // .WithEnvironment("EMBEDDING_API_VERSION", embeddingApiVersion)
    .WithEnvironment("EMBEDDING_DIMENSIONS", embeddingDimensions)
    .WithEnvironment("EMBEDDING_API_KEY", embeddingApiKey)
    // .WithEnvironment("HUGGINGFACE_TOKENIZER", huggingFaceTokenizer)
    .WithEnvironment("ENABLE_BACKEND_ACCESS_CONTROL", "true")
    .WithEnvironment("TELEMETRY_DISABLED", "true")
    .WithEnvironment("VISUALISATION_PATH", @$"{TryGetSolutionDirectoryInfo().FullName}\visualization")
    .WithEnvironment("DATA_ROOT_DIRECTORY", @$"{TryGetSolutionDirectoryInfo().FullName}\cognee\data\")
    .WithEnvironment("SYSTEM_ROOT_DIRECTORY", @$"{TryGetSolutionDirectoryInfo().FullName}\cognee\system\");

var promptPath = @$"{TryGetSolutionDirectoryInfo().FullName}\Prompts\Default\";

var serverBuilder = builder
    .AddProject<FableCraft_Server>("fablecraft-server")
    .WithReference(graphRagApi)
    .WithReference(serverDatabase)
    .WithEnvironment("FABLECRAFT_EXPORTER_SEQ_TRACE_ENDPOINT", "http://localhost:5341/ingest/otlp/v1/traces")
    .WithEnvironment("FABLECRAFT_EXPORTER_SEQ_LOG_ENDPOINT", "http://localhost:5341")
    .WithEnvironment("FABLECRAFT_DATA_STORE", @$"{TryGetSolutionDirectoryInfo().FullName}\data-store")
    .WithEnvironment("FABLECRAFT_LOG_PATH", @$"{TryGetSolutionDirectoryInfo().FullName}\logs\")
    .WithEnvironment("GraphService__LogsHostPath", @$"{TryGetSolutionDirectoryInfo().FullName}\logs\graphrag")
    .WithEnvironment("DEFAULT_PROMPT_PATH", promptPath)
    .WaitFor(graphRagApi);

// Only wait for database if Aspire is managing it (not external)
if (!useExternalDb)
{
    serverBuilder.WaitFor(serverDatabase);
}

var server = serverBuilder;

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