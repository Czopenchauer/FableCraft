using FableCraft.Infrastructure.Persistence.Entities;

namespace FableCraft.ProjectManagement.Models;

public class IndexingStatusResponse
{
    public required IndexingStatus Status { get; init; }

    public DateTimeOffset? LastIndexedAt { get; init; }

    public required List<IndexingFileStatus> PendingChanges { get; init; } = [];
}

public class IndexingFileStatus
{
    public required Guid FileId { get; init; }

    public required string FileName { get; init; } = string.Empty;

    public required bool IsNew { get; init; }

    public required bool IsModified { get; init; }
}