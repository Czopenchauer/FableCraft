using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;

namespace FableCraft.Infrastructure.Persistence.Cosmos;

public static class Extensions
{
    public static IHostApplicationBuilder AddAzureCosmosDb(this IHostApplicationBuilder builder)
    {
        const string connectionName = "fablecraft-cosmos";
        builder.AddAzureCosmosClient(connectionName);
        builder.AddAzureCosmosDatabase(connectionName,
            settings =>
            {
                settings.DatabaseName = "fablecraft";
            },
            options =>
            {
                options.ConnectionMode = ConnectionMode.Gateway;
            });

        return builder;
    }
}