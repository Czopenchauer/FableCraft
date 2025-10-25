namespace FableCraft.AppHost;

public static class Postgres18BuilderExtensions
{
    public static IResourceBuilder<PostgresServerResource> WithDataVolumeForV18(
        this IResourceBuilder<PostgresServerResource> builder, string? name = null, bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithVolume(name ?? VolumeNameGenerator.Generate(builder, "data"),
            "/var/lib/postgresql/18/docker", isReadOnly);
    }
}