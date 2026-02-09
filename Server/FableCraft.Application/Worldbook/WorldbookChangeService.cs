using FableCraft.Application.Model.Worldbook;
using FableCraft.Infrastructure.Persistence.Entities;

namespace FableCraft.Application.Worldbook;

/// <summary>
/// Service for detecting and managing changes to worldbook lorebooks.
/// </summary>
public class WorldbookChangeService
{
    /// <summary>
    /// Calculates the change status for a lorebook based on its snapshot.
    /// </summary>
    public LorebookChangeStatus GetChangeStatus(Lorebook lorebook, LorebookSnapshot? snapshot)
    {
        if (lorebook.IsDeleted)
        {
            return LorebookChangeStatus.Deleted;
        }

        if (snapshot == null)
        {
            return LorebookChangeStatus.Added;
        }

        if (HasChanges(lorebook, snapshot))
        {
            return LorebookChangeStatus.Modified;
        }

        return LorebookChangeStatus.None;
    }

    private bool HasChanges(Lorebook lorebook, LorebookSnapshot snapshot)
    {
        return lorebook.Title != snapshot.Title ||
               lorebook.Content != snapshot.Content ||
               lorebook.Category != snapshot.Category ||
               lorebook.ContentType != snapshot.ContentType;
    }

    /// <summary>
    /// Calculates a summary of pending changes for a worldbook.
    /// </summary>
    public PendingChangeSummaryDto CalculatePendingChangeSummary(
        IEnumerable<Lorebook> lorebooks,
        IEnumerable<LorebookSnapshot> snapshots)
    {
        var snapshotByLorebookId = snapshots.ToDictionary(s => s.LorebookId);

        var added = 0;
        var modified = 0;
        var deleted = 0;

        foreach (var lorebook in lorebooks)
        {
            snapshotByLorebookId.TryGetValue(lorebook.Id, out var snapshot);
            var status = GetChangeStatus(lorebook, snapshot);

            switch (status)
            {
                case LorebookChangeStatus.Added:
                    added++;
                    break;
                case LorebookChangeStatus.Modified:
                    modified++;
                    break;
                case LorebookChangeStatus.Deleted:
                    deleted++;
                    break;
            }
        }

        return new PendingChangeSummaryDto
        {
            AddedCount = added,
            ModifiedCount = modified,
            DeletedCount = deleted
        };
    }

    /// <summary>
    /// Determines if a worldbook has any pending changes.
    /// </summary>
    public bool HasPendingChanges(
        IEnumerable<Lorebook> lorebooks,
        IEnumerable<LorebookSnapshot> snapshots)
    {
        var summary = CalculatePendingChangeSummary(lorebooks, snapshots);
        return summary.AddedCount > 0 || summary.ModifiedCount > 0 || summary.DeletedCount > 0;
    }

    /// <summary>
    /// Creates a LorebookResponseDto with change status.
    /// </summary>
    public LorebookResponseDto ToResponseDto(Lorebook lorebook, LorebookSnapshot? snapshot)
    {
        return new LorebookResponseDto
        {
            Id = lorebook.Id,
            WorldbookId = lorebook.WorldbookId,
            Title = lorebook.Title,
            Content = lorebook.Content,
            Category = lorebook.Category,
            ContentType = lorebook.ContentType,
            IsDeleted = lorebook.IsDeleted,
            ChangeStatus = GetChangeStatus(lorebook, snapshot)
        };
    }
}
