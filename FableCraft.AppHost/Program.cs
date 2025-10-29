using Aspire.Hosting.Python;

using FableCraft.AppHost;

using Projects;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<PostgresDatabaseResource> serverDatabase = builder
    .AddPostgres("fablecraftdb-npgsql", port: 6999)
    .WithImage("postgres", "18.0")
    .WithDataVolumeForV18()
    .AddDatabase("fablecraftdb", "fablecraftdb");

IResourceBuilder<ContainerResource> neo4j = builder
    .AddContainer("fablecraft-neo4j", "neo4j", "community")
    .WithVolume("neo4j-data", "/data")
    .WithHttpEndpoint(targetPort: 7474, port: 7474, name: "http")
    .WithEndpoint(targetPort: 7687, port: 7687, name: "bolt")
    .WithEnvironment("NEO4J_AUTH", "neo4j/SuperPassword")
    .WithEnvironment("NEO4J_PLUGINS", "[\"apoc\", \"graph-data-science\"]");

#pragma warning disable ASPIREHOSTINGPYTHON001
IResourceBuilder<PythonAppResource> graphRagApi = builder
    .AddPythonApp("graph-rag-api", "../GraphRag", "api.py")
    .WithHttpEndpoint(env: "PORT", port: 8111, name: "graphRagApi")
    .WithExternalHttpEndpoints()
    .WithOtlpExporter()
    .WithEnvironment(context =>
    {
        EndpointReference boltEndpoint = neo4j.GetEndpoint("bolt");
        context.EnvironmentVariables["NEO4J_URI"] = $"bolt://{boltEndpoint.Host}:{boltEndpoint.Port}";
        context.EnvironmentVariables["NEO4J_USER"] = "neo4j";
        context.EnvironmentVariables["NEO4J_PASSWORD"] = "SuperPassword";

        EndpointReference httpEndpoint = neo4j.GetEndpoint("http");
        context.EnvironmentVariables["NEO4J_HTTP_URI"] = $"http://{httpEndpoint.Host}:{httpEndpoint.Port}";
    })
    .WithRelationship(neo4j.Resource, "uses")
    .WaitFor(neo4j);
#pragma warning restore ASPIREHOSTINGPYTHON001

IResourceBuilder<ProjectResource> server = builder
    .AddProject<FableCraft_Server>("fablecraft-server")
    .WithReference(graphRagApi)
    .WithReference(serverDatabase)
    .WaitFor(graphRagApi)
    .WaitFor(serverDatabase);

builder.AddNpmApp("fablecraft-client", "../FableCraft.Client")
    .WithHttpEndpoint(env: "PORT", port: 4211)
    .WithExternalHttpEndpoints()
    .WithOtlpExporter()
    .WithReference(server)
    .WaitFor(server);

builder.Build().Run();