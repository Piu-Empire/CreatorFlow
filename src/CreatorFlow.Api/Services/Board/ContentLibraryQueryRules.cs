using CreatorFlow.Contracts.Board;

namespace CreatorFlow.Api.Services.Board;

/// <summary>Validation, AND-filters and sorting for the Content Library API response.</summary>
public static class ContentLibraryQueryRules
{
    private static readonly HashSet<string> SortFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "title", "platform", "status", "creator", "priority", "deadline", "tag",
    };

    public static bool TryValidate(ContentLibraryQuery query, out string? field, out string? message)
    {
        ArgumentNullException.ThrowIfNull(query);
        field = null;
        message = null;

        if (query.CreatorId is <= 0)
        {
            field = nameof(query.CreatorId);
            message = "CreatorId phải lớn hơn 0.";
        }
        else if (query.DeadlineFrom.HasValue && query.DeadlineTo.HasValue && query.DeadlineFrom > query.DeadlineTo)
        {
            field = nameof(query.DeadlineTo);
            message = "DeadlineTo phải bằng hoặc sau DeadlineFrom.";
        }
        else if (query.HasDeadline == false && (query.DeadlineFrom.HasValue || query.DeadlineTo.HasValue))
        {
            field = nameof(query.HasDeadline);
            message = "Không thể kết hợp lọc Content chưa có deadline với khoảng ngày deadline.";
        }
        else if (!string.IsNullOrWhiteSpace(query.SortBy) && !SortFields.Contains(query.SortBy.Trim()))
        {
            field = nameof(query.SortBy);
            message = "SortBy chỉ nhận title, platform, status, creator, priority, deadline hoặc tag.";
        }

        return message is null;
    }

    public static IReadOnlyList<BoardCardResponse> Apply(
        IEnumerable<BoardCardResponse> cards,
        ContentLibraryQuery query)
    {
        ArgumentNullException.ThrowIfNull(cards);
        ArgumentNullException.ThrowIfNull(query);

        IEnumerable<BoardCardResponse> filtered = cards;
        if (!string.IsNullOrWhiteSpace(query.Title))
            filtered = filtered.Where(card => card.Title.Contains(query.Title.Trim(), StringComparison.OrdinalIgnoreCase) ||
                                               card.Code.Contains(query.Title.Trim(), StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(query.Platform))
            filtered = filtered.Where(card => card.Platforms.Any(value => EqualsIgnoreCase(value, query.Platform)));
        if (!string.IsNullOrWhiteSpace(query.Status))
            filtered = filtered.Where(card => EqualsIgnoreCase(card.Status, query.Status));
        if (query.CreatorId.HasValue)
            filtered = filtered.Where(card => card.Assignees.Any(creator => creator.UserId == query.CreatorId.Value));
        if (!string.IsNullOrWhiteSpace(query.Priority))
            filtered = filtered.Where(card => EqualsIgnoreCase(card.Priority, query.Priority));
        if (query.HasDeadline.HasValue)
            filtered = filtered.Where(card => card.Deadline.HasValue == query.HasDeadline.Value);
        if (query.DeadlineFrom.HasValue)
            filtered = filtered.Where(card => card.Deadline.HasValue && card.Deadline.Value >= query.DeadlineFrom.Value);
        if (query.DeadlineTo.HasValue)
            filtered = filtered.Where(card => card.Deadline.HasValue && card.Deadline.Value <= query.DeadlineTo.Value);
        if (!string.IsNullOrWhiteSpace(query.Tag))
            filtered = filtered.Where(card => card.Tags.Any(value => EqualsIgnoreCase(value, query.Tag)));

        string? sortBy = string.IsNullOrWhiteSpace(query.SortBy) ? null : query.SortBy.Trim().ToLowerInvariant();
        if (sortBy is null)
            return filtered.OrderByDescending(card => card.ContentId).ToArray();

        if (sortBy == "deadline")
        {
            IOrderedEnumerable<BoardCardResponse> byDeadline = filtered.OrderBy(card => !card.Deadline.HasValue);
            byDeadline = query.SortDescending == true
                ? byDeadline.ThenByDescending(card => card.Deadline)
                : byDeadline.ThenBy(card => card.Deadline);
            return byDeadline.ThenBy(card => card.ContentId).ToArray();
        }

        if (sortBy == "priority")
        {
            IOrderedEnumerable<BoardCardResponse> byPriority = query.SortDescending == true
                ? filtered.OrderByDescending(card => PriorityRank(card.Priority))
                : filtered.OrderBy(card => PriorityRank(card.Priority));
            return byPriority.ThenBy(card => card.ContentId).ToArray();
        }

        Func<BoardCardResponse, string> key = sortBy switch
        {
            "title" => card => card.Title,
            "platform" => card => FirstSorted(card.Platforms),
            "status" => card => card.Status,
            "creator" => card => FirstSorted(card.Assignees.Select(creator => creator.DisplayName)),
            "tag" => card => FirstSorted(card.Tags),
            _ => throw new InvalidOperationException("The query must be validated before sorting."),
        };

        IOrderedEnumerable<BoardCardResponse> ordered = query.SortDescending == true
            ? filtered.OrderByDescending(key, StringComparer.OrdinalIgnoreCase)
            : filtered.OrderBy(key, StringComparer.OrdinalIgnoreCase);
        return ordered.ThenBy(card => card.ContentId).ToArray();
    }

    private static bool EqualsIgnoreCase(string? value, string? query) =>
        string.Equals(value?.Trim(), query?.Trim(), StringComparison.OrdinalIgnoreCase);

    private static string FirstSorted(IEnumerable<string> values) => values
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
        .FirstOrDefault() ?? string.Empty;

    private static int PriorityRank(string? priority) => priority?.Trim().ToUpperInvariant() switch
    {
        "LOW" => 0,
        "MEDIUM" => 1,
        "HIGH" => 2,
        "URGENT" => 3,
        _ => -1,
    };
}
