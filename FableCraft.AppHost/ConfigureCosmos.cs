using Azure.Provisioning.CosmosDB;

namespace FableCraft.AppHost;

public static class ConfigureCosmos
{
    public static IResourceBuilder<AzureCosmosDBResource> AddCosmosDb(this IDistributedApplicationBuilder builder)
    {
        var cosmosAccountName = "fablecraft-cosmos";
#pragma warning disable ASPIRECOSMOSDB001
        var cosmosDb = builder
            .AddAzureCosmosDB(cosmosAccountName)
            .ConfigureInfrastructure(infra =>
            {
                var cosmosDbAccount = infra.GetProvisionableResources()
                    .OfType<CosmosDBAccount>()
                    .Single();

                cosmosDbAccount.ConsistencyPolicy = new ConsistencyPolicy
                {
                    DefaultConsistencyLevel = DefaultConsistencyLevel.BoundedStaleness
                };
                cosmosDbAccount.CapacityTotalThroughputLimit = 1000;
            })
            .RunAsPreviewEmulator(emulator =>
            {
                emulator.WithDataVolume();
                emulator.WithDataExplorer();
                emulator.ExcludeFromManifest();
                emulator.WithOtlpExporter();
            });
#pragma warning restore ASPIRECOSMOSDB001

        var cosmosDatabaseName = "fablecraft";
        var cosmosDatabase = cosmosDb.AddCosmosDatabase(cosmosDatabaseName);
        cosmosDb.ConfigureInfrastructure(infra =>
        {
            var database = infra.GetProvisionableResources()
                .OfType<CosmosDBSqlDatabase>()
                .Single(x => x.Name.Value == cosmosDatabaseName);

            database.Options.Throughput = 100;
            database.Options.AutoscaleMaxThroughput = 1000;
        });

        var adventureContainer = "adventure";
        _ = cosmosDatabase.AddContainer(adventureContainer, "/partitionKey");
        cosmosDb.ConfigureInfrastructure(infra =>
        {
            var container = infra.GetProvisionableResources()
                .OfType<CosmosDBSqlContainer>()
                .Single(x => x.Name.Value == adventureContainer);

            container.Resource.IndexingPolicy.IndexingMode = CosmosDBIndexingMode.Consistent;
        });

        return cosmosDb;
    }
}