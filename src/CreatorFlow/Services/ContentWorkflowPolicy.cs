using CreatorFlow.Models;
using CreatorFlow.Models.Enums;

namespace CreatorFlow.Services;

public static class ContentWorkflowPolicy
{
    public static bool CanTransition(ContentStatus from, ContentStatus to)
    {
        return (from, to) switch
        {
            (ContentStatus.Idea, ContentStatus.Script) => true,
            (ContentStatus.Script, ContentStatus.Production) => true,
            (ContentStatus.Production, ContentStatus.Editing) => true,
            (ContentStatus.Editing, ContentStatus.Review) => true,
            (ContentStatus.Review, ContentStatus.Editing) => true,
            (ContentStatus.Review, ContentStatus.Ready) => true,
            (ContentStatus.Ready, ContentStatus.Published) => true,
            _ => false
        };
    }

    public static bool CanArchive(ProjectRole role, ContentStatus currentStatus)
    {
        return IsOwnerOrManager(role) && currentStatus != ContentStatus.Archived;
    }

    public static bool CanRestore(ProjectRole role, ContentStatus currentStatus)
    {
        return IsOwnerOrManager(role) && currentStatus == ContentStatus.Archived;
    }

    public static ContentStatus GetRestoreStatus(
        IEnumerable<ContentStatusHistory> statusHistory)
    {
        ArgumentNullException.ThrowIfNull(statusHistory);

        ContentStatusHistory? latestArchive = statusHistory
            .Where(entry =>
                entry.ToStatus == ContentStatus.Archived
                && entry.FromStatus is not null
                && entry.FromStatus != ContentStatus.Archived)
            .OrderByDescending(entry => entry.ChangedAt)
            .ThenByDescending(entry => entry.HistoryId)
            .FirstOrDefault();

        return latestArchive?.FromStatus
            ?? throw new InvalidOperationException(
                "A valid archive history entry is required to restore content.");
    }

    private static bool IsOwnerOrManager(ProjectRole role)
    {
        return role is ProjectRole.Owner or ProjectRole.Manager;
    }
}
