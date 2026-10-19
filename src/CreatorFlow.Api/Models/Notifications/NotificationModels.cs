namespace CreatorFlow.Api.Models.Notifications;

/// <summary>Loại thông báo, khớp enum notification_type trong database/01_schema.sql.</summary>
public enum NotificationType
{
    Assignment,
    ReviewSubmitted,
    ReviewApproved,
    ReviewRejected,
    Feedback,
    StatusChanged,
    DeadlineNear,
    Overdue,
    Invitation,
    System,
}

/// <summary>
/// Một dòng notifications cần ghi. DedupeKey là khoá nghiệp vụ chống tạo trùng vô hạn;
/// database có unique index có điều kiện trên cột này (database/09_notifications.sql).
/// </summary>
public sealed record NotificationInsert(
    long RecipientUserId,
    long? ProjectId,
    long? ContentId,
    NotificationType Type,
    string Title,
    string Message,
    string DedupeKey);

/// <summary>Một dòng notifications đọc từ database để trả về API.</summary>
public sealed record NotificationRecord(
    long NotificationId,
    string Type,
    string Title,
    string Message,
    bool IsRead,
    DateTime CreatedAt,
    DateTime? ReadAt,
    long? ProjectId,
    long? ContentId,
    string? ContentCode);

/// <summary>Assignment đang hiệu lực và đang tới hạn/trễ của một User (nguồn sinh thông báo deadline).</summary>
public sealed record DueAssignment(
    long ContentId,
    long ProjectId,
    string ContentCode,
    string ContentTitle,
    DateOnly Deadline);

/// <summary>
/// Khoá chống trùng cho notifications.dedupe_key.
/// Luôn gồm người nhận vì một sự kiện có thể có nhiều người nhận (Owner + Manager, nhiều Creator).
/// </summary>
public static class NotificationKeys
{
    public static string Assignment(long contentId, long recipientUserId, long assignmentId) =>
        $"ASSIGNMENT:{contentId}:{recipientUserId}:{assignmentId}";

    public static string Review(NotificationType type, long contentId, long recipientUserId, int reviewNo) =>
        $"{ToDbEnum(type)}:{contentId}:{recipientUserId}:{reviewNo}";

    public static string Deadline(NotificationType type, long contentId, long recipientUserId, DateOnly today) =>
        $"{ToDbEnum(type)}:{contentId}:{recipientUserId}:{today:yyyy-MM-dd}";

    /// <summary>NotificationType.DeadlineNear → "DEADLINE_NEAR".</summary>
    internal static string ToDbEnum<TEnum>(TEnum value) where TEnum : struct, Enum
    {
        string name = value.ToString();
        var builder = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i])) builder.Append('_');
            builder.Append(char.ToUpperInvariant(name[i]));
        }
        return builder.ToString();
    }
}
