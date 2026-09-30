using CreatorFlow.Models;
using CreatorFlow.Models.Enums;

namespace CreatorFlow.Services;

public static class ContentVisibilityPolicy
{
    public static bool IsVisibleOnBoard(Content content)
    {
        ArgumentNullException.ThrowIfNull(content);
        return content.Status != ContentStatus.Archived;
    }

    public static bool IsVisibleInLibrary(Content content, ContentFilter filter)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(filter);

        return filter.Status is ContentStatus requestedStatus
            ? content.Status == requestedStatus
            : content.Status != ContentStatus.Archived;
    }
}
