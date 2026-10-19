using CreatorFlow.Api.Models.Notifications;
using CreatorFlow.Api.Repositories.Notifications;

namespace CreatorFlow.Api.Tests.Notifications;

/// <summary>
/// INotificationRepository giả lập đúng ngữ nghĩa database: chống trùng theo DedupeKey,
/// đọc chỉ dòng của chính người nhận, MarkRead chỉ dòng của chính người gọi.
/// </summary>
internal class FakeNotificationRepository : INotificationRepository
{
    private sealed record Row(long Id, NotificationInsert Insert, bool IsRead, DateTime? ReadAt, DateTime CreatedAt);

    private readonly List<Row> _rows = new();
    private long _nextId = 1;

    public Dictionary<long, string> Emails { get; } = new(); // userId -> email (cho đường gửi email)
    public List<(long NotificationId, long UserId, DateTime ReadAt)> MarkReadCalls { get; } = new();
    public int MarkAllReadCalls { get; private set; }
    public bool ThrowOnInsert { get; set; }
    public List<(DueAssignment Due, long UserId)> DueAssignments { get; set; } = new();

    public IReadOnlyList<NotificationInsert> Insets => _rows.Select(r => r.Insert).ToList();

    /// <summary>Ghi thẳng một dòng vào "database" giả (bỏ qua dedupe) để chuẩn bị dữ liệu đọc.</summary>
    public void Seed(long recipientUserId, NotificationType type, string title, string message,
        bool isRead = false, DateTime? createdAt = null, string? dedupeKey = null, long? projectId = 1, long? contentId = 10)
    {
        DateTime created = createdAt ?? new DateTime(2026, 10, 8, 9, 0, 0, DateTimeKind.Utc);
        _rows.Add(new Row(_nextId++,
            new NotificationInsert(recipientUserId, projectId, contentId, type, title, message,
                dedupeKey ?? $"{ToDb(type)}:{recipientUserId}:{_rows.Count}"),
            isRead, isRead ? created : null, created));
    }

    public Task<bool> InsertAsync(NotificationInsert row, CancellationToken cancellationToken = default)
    {
        if (ThrowOnInsert) throw new InvalidOperationException("db down");
        if (_rows.Any(r => r.Insert.DedupeKey == row.DedupeKey))
            return Task.FromResult(false);
        _rows.Add(new Row(_nextId++, row, false, null, new DateTime(2026, 10, 9, 9, 0, 0, DateTimeKind.Utc)));
        return Task.FromResult(true);
    }

    public Task<IReadOnlyList<NotificationRecord>> GetForUserAsync(
        long userId, bool unreadOnly, int limit, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<NotificationRecord>>(_rows
            .Where(r => r.Insert.RecipientUserId == userId && (!unreadOnly || !r.IsRead))
            .OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
            .Take(limit)
            .Select(ToRecord)
            .ToList());

    public Task<int> CountUnreadAsync(long userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_rows.Count(r => r.Insert.RecipientUserId == userId && !r.IsRead));

    public Task<DateTime?> MarkReadAsync(
        long notificationId, long userId, DateTime readAtUtc, CancellationToken cancellationToken = default)
    {
        int index = _rows.FindIndex(r => r.Id == notificationId);
        if (index < 0 || _rows[index].Insert.RecipientUserId != userId)
            return Task.FromResult<DateTime?>(null);
        MarkReadCalls.Add((notificationId, userId, readAtUtc));
        Row row = _rows[index];        _rows[index] = row with { IsRead = true, ReadAt = row.ReadAt ?? readAtUtc };
        return Task.FromResult<DateTime?>(_rows[index].ReadAt);
    }

    public Task<int> MarkAllReadAsync(long userId, DateTime readAtUtc, CancellationToken cancellationToken = default)
    {
        MarkAllReadCalls++;
        int changed = 0;
        for (int i = 0; i < _rows.Count; i++)
        {
            if (_rows[i].Insert.RecipientUserId == userId && !_rows[i].IsRead)
            {
                _rows[i] = _rows[i] with { IsRead = true, ReadAt = readAtUtc };
                changed++;
            }
        }
        return Task.FromResult(changed);
    }

    public Task<IReadOnlyList<DueAssignment>> GetDueAssignmentsForUserAsync(
        long userId, DateOnly dueOn, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DueAssignment>>(DueAssignments
            .Where(d => d.UserId == userId && d.Due.Deadline <= dueOn)
            .OrderBy(d => d.Due.Deadline).ThenBy(d => d.Due.ContentId)
            .Select(d => d.Due)
            .ToList());

    private static NotificationRecord ToRecord(Row r) => new(
        r.Id, ToDb(r.Insert.Type), r.Insert.Title, r.Insert.Message,
        r.IsRead, r.CreatedAt, r.ReadAt, r.Insert.ProjectId, r.Insert.ContentId,
        r.Insert.ContentId is long contentId ? $"CNT-{contentId:000}" : null);

    /// <summary>Giống nhận dạng enum trong database để test khớp kết quả production.</summary>
    private static string ToDb(NotificationType type) => type switch
    {
        NotificationType.Assignment => "ASSIGNMENT",
        NotificationType.ReviewSubmitted => "REVIEW_SUBMITTED",
        NotificationType.ReviewApproved => "REVIEW_APPROVED",
        NotificationType.ReviewRejected => "REVIEW_REJECTED",
        NotificationType.Feedback => "FEEDBACK",
        NotificationType.StatusChanged => "STATUS_CHANGED",
        NotificationType.DeadlineNear => "DEADLINE_NEAR",
        NotificationType.Overdue => "OVERDUE",
        NotificationType.Invitation => "INVITATION",
        NotificationType.System => "SYSTEM",
    };
}
