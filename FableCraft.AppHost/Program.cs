using FableCraft.AppHost;

using Projects;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

var serverDatabase = builder
    .AddPostgres("fablecraftdb-npgsql", port: 6999)
    .WithImage("postgres", "18.0")
    .WithDataVolumeForV18()
    .AddDatabase("fablecraftdb", "fablecraftdb");

var llmApiKeySecret = builder.Configuration["FableCraft:Server:LLM:ApiKey"]!;
var llmModel = builder.Configuration["FableCraft:Server:LLM:Model"]!;
var llmProvider = builder.Configuration["FableCraft:Server:LLM:Provider"]!;
var llmEndpoint = builder.Configuration["FableCraft:Server:LLM:BaseUrl"] ?? "";
var llmApiVersion = builder.Configuration["FableCraft:Server:LLM:ApiVersion"] ?? "";
var llmMaxTokens = builder.Configuration["FableCraft:Server:LLM:MaxTokens"] ?? "16384";

var llmRateLimitEnabled = builder.Configuration["FableCraft:GraphRag:LLM:RateLimitEnabled"] ?? "true";
var llmRateLimitRequests = builder.Configuration["FableCraft:GraphRag:LLM:RateLimitRequests"] ?? "60";
var llmRateLimitInterval = builder.Configuration["FableCraft:GraphRag:LLM:RateLimitInterval"] ?? "60";
var embeddingProvider = builder.Configuration["FableCraft:GraphRag:Embedding:Provider"]!;
var embeddingModel = builder.Configuration["FableCraft:GraphRag:Embedding:Model"]!;
var embeddingEndpoint = builder.Configuration["FableCraft:GraphRag:Embedding:BaseUrl"] ?? "";
var embeddingApiVersion = builder.Configuration["FableCraft:GraphRag:Embedding:ApiVersion"] ?? "";
var embeddingDimensions = builder.Configuration["FableCraft:GraphRag:Embedding:Dimensions"] ?? "3072";
var embeddingMaxTokens = builder.Configuration["FableCraft:GraphRag:Embedding:MaxTokens"] ?? "8191";
var embeddingBatchSize = builder.Configuration["FableCraft:GraphRag:Embedding:BatchSize"] ?? "36";
var huggingFaceTokenizer = builder.Configuration["FableCraft:GraphRag:HuggingFaceTokenizer"] ?? "";

#pragma warning disable ASPIREHOSTINGPYTHON001
var graphRagApi = builder
    .AddPythonApp("graph-rag-api", "../GraphRag", "api.py")
    .WithHttpEndpoint(env: "PORT", port: 8111, name: "graphRagApi")
    .WithExternalHttpEndpoints()
    .WithOtlpExporter()
    .WithEnvironment("LLM_API_KEY", llmApiKeySecret)
    .WithEnvironment("LLM_MODEL", llmModel)
    .WithEnvironment("LLM_PROVIDER", llmProvider)
    .WithEnvironment("LLM_ENDPOINT", llmEndpoint)
    .WithEnvironment("LLM_API_VERSION", llmApiVersion)
    .WithEnvironment("LLM_MAX_TOKENS", llmMaxTokens)
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
    .WithEnvironment("TELEMETRY_DISABLED", "true")
    .WithEnvironment("HUGGINGFACE_TOKENIZER", huggingFaceTokenizer);
#pragma warning restore ASPIREHOSTINGPYTHON001

var server = builder
    .AddProject<FableCraft_Server>("fablecraft-server")
    .WithOtlpExporter()
    .WithReference(graphRagApi)
    .WithReference(serverDatabase)
    .WithEnvironment("FableCraft:Server:LLM:Model", llmModel)
    .WithEnvironment("FableCraft:Server:LLM:ApiKey", llmApiKeySecret)
    .WithEnvironment("FableCraft:Server:LLM:BaseUrl", llmEndpoint)
    .WaitFor(graphRagApi)
    .WaitFor(serverDatabase);

builder.AddNpmApp("fablecraft-client", "../FableCraft.Client")
    .WithHttpEndpoint(env: "PORT", port: 4211)
    .WithExternalHttpEndpoints()
    .WithOtlpExporter()
    .WithReference(server)
    .WaitFor(server);

builder.Build().Run();