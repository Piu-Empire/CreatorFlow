using CreatorFlow.Api.Models.Notifications;
using CreatorFlow.Data;
using Npgsql;
using NpgsqlTypes;

namespace CreatorFlow.Api.Repositories.Notifications;

/// <summary>
/// Cài đặt Npgsql cho INotificationRepository, viết theo database/01_schema.sql
/// (thêm cột dedupe_key ở database/09_notifications.sql).
/// Ghi thông báo dùng ON CONFLICT (dedupe_key) DO NOTHING nên gọi lại nhiều lần vẫn không tạo trùng.
/// </summary>
public sealed class NotificationRepository(IDbConnectionFactory connectionFactory) : INotificationRepository
{
    private const string InsertSql = """
        INSERT INTO notifications (recipient_user_id, project_id, content_id, type, title, message, dedupe_key)
        VALUES (@recipient_user_id, @project_id, @content_id, @type::notification_type, @title, @message, @dedupe_key)
        ON CONFLICT (dedupe_key) WHERE dedupe_key IS NOT NULL DO NOTHING
        RETURNING notification_id;
        """;

    private const string SelectSql = """
        SELECT n.notification_id, n.type::text, n.title, n.message, n.is_read, n.created_at, n.read_at,
               n.project_id, n.content_id,
               CASE WHEN n.content_id IS NULL THEN NULL
                    ELSE 'CNT-' || LPAD(n.content_id::text, 3, '0') END
        FROM notifications n
        WHERE n.recipient_user_id = @recipient_user_id
          AND (@unread_only = FALSE OR n.is_read = FALSE)
        ORDER BY n.created_at DESC, n.notification_id DESC
        LIMIT @limit;
        """;

    // Hạn của assignment ưu tiên deadline riêng, giống GetBoardAssigneesAsync của TaskRepository.
    private const string DueAssignmentsSql = """
        SELECT ca.content_id, c.project_id, c.title,
               'CNT-' || LPAD(c.content_id::text, 3, '0'),
               (COALESCE(ca.deadline, c.deadline) AT TIME ZONE 'UTC')::date
        FROM content_assignments ca
        JOIN contents c ON c.content_id = ca.content_id
        WHERE ca.assignee_id = @user_id
          AND ca.status NOT IN ('COMPLETED', 'CANCELLED')
          AND c.status NOT IN ('PUBLISHED', 'ARCHIVED')
          AND COALESCE(ca.deadline, c.deadline) IS NOT NULL
          AND (COALESCE(ca.deadline, c.deadline) AT TIME ZONE 'UTC')::date <= @due_on
        ORDER BY 5, ca.content_id;
        """;

    public async Task<bool> InsertAsync(NotificationInsert row, CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(InsertSql, connection);
        command.Parameters.AddWithValue("recipient_user_id", NpgsqlDbType.Bigint, row.RecipientUserId);
        command.Parameters.AddWithValue("project_id", NpgsqlDbType.Bigint, (object?)row.ProjectId ?? DBNull.Value);
        command.Parameters.AddWithValue("content_id", NpgsqlDbType.Bigint, (object?)row.ContentId ?? DBNull.Value);
        command.Parameters.AddWithValue("type", NpgsqlDbType.Text, NotificationKeys.ToDbEnum(row.Type));
        command.Parameters.AddWithValue("title", NpgsqlDbType.Varchar, row.Title);
        command.Parameters.AddWithValue("message", NpgsqlDbType.Text, row.Message);
        command.Parameters.AddWithValue("dedupe_key", NpgsqlDbType.Varchar, row.DedupeKey);

        // Không có dòng trả về nghĩa là dedupe_key đã tồn tại → không tạo thông báo trùng.
        return await command.ExecuteScalarAsync(cancellationToken) is not null;
    }

    public async Task<IReadOnlyList<NotificationRecord>> GetForUserAsync(
        long userId, bool unreadOnly, int limit, CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(SelectSql, connection);
        command.Parameters.AddWithValue("recipient_user_id", NpgsqlDbType.Bigint, userId);
        command.Parameters.AddWithValue("unread_only", NpgsqlDbType.Boolean, unreadOnly);
        command.Parameters.AddWithValue("limit", NpgsqlDbType.Integer, limit);

        var items = new List<NotificationRecord>();
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new NotificationRecord(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetBoolean(4),
                reader.GetDateTime(5),
                reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                reader.IsDBNull(7) ? null : reader.GetInt64(7),
                reader.IsDBNull(8) ? null : reader.GetInt64(8),
                reader.IsDBNull(9) ? null : reader.GetString(9)));
        }
        return items;
    }

    public async Task<int> CountUnreadAsync(long userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(*) FROM notifications
            WHERE recipient_user_id = @recipient_user_id AND is_read = FALSE;
            """;

        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("recipient_user_id", NpgsqlDbType.Bigint, userId);
        object? value = await command.ExecuteScalarAsync(cancellationToken);
        return value is long count ? (int)count : 0;
    }

    public async Task<DateTime?> MarkReadAsync(
        long notificationId, long userId, DateTime readAtUtc, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE notifications
            SET read_at = COALESCE(read_at, @read_at),
                is_read = TRUE
            WHERE notification_id = @notification_id AND recipient_user_id = @recipient_user_id
            RETURNING read_at;
            """;

        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("notification_id", NpgsqlDbType.Bigint, notificationId);
        command.Parameters.AddWithValue("recipient_user_id", NpgsqlDbType.Bigint, userId);
        command.Parameters.AddWithValue("read_at", NpgsqlDbType.TimestampTz, readAtUtc);

        // Không có dòng trả về: thông báo không tồn tại hoặc không thuộc người gọi (404 ở service).
        object? value = await command.ExecuteScalarAsync(cancellationToken);
        return value is DateTime readAt ? readAt : null;
    }

    public async Task<int> MarkAllReadAsync(long userId, DateTime readAtUtc, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE notifications
            SET read_at = @read_at, is_read = TRUE
            WHERE recipient_user_id = @recipient_user_id AND is_read = FALSE;
            """;

        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("recipient_user_id", NpgsqlDbType.Bigint, userId);
        command.Parameters.AddWithValue("read_at", NpgsqlDbType.TimestampTz, readAtUtc);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DueAssignment>> GetDueAssignmentsForUserAsync(
        long userId, DateOnly dueOn, CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(DueAssignmentsSql, connection);
        command.Parameters.AddWithValue("user_id", NpgsqlDbType.Bigint, userId);
        command.Parameters.AddWithValue("due_on", NpgsqlDbType.Date, dueOn);

        var items = new List<DueAssignment>();
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new DueAssignment(
                reader.GetInt64(0),
                reader.GetInt64(1),
                reader.GetString(3),
                reader.GetString(2),
                DateOnly.FromDateTime(reader.GetDateTime(4))));
        }
        return items;
    }
}
