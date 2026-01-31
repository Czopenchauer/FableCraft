namespace FableCraft.Infrastructure.Persistence.Entities;

public class Worldbook : IEntity
{
    public required string Name { get; set; }

    public List<Lorebook> Lorebooks { get; set; } = new();

    public IndexingStatus IndexingStatus { get; set; } = IndexingStatus.NotIndexed;

    public string? IndexingError { get; set; }

    public Guid Id { get; set; }

    public Guid? GraphRagSettingsId { get; set; }

    public GraphRagSettings? GraphRagSettings { get; set; }
}

public enum IndexingStatus
{
    NotIndexed = 0,
    Indexing = 1,
    Indexed = 2,
    Failed = 3
}