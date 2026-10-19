using CreatorFlow.Api.Models.ContentDetail;
using CreatorFlow.Api.Repositories.Tasks;
using CreatorFlow.Contracts.ContentDetail;
using CreatorFlow.Data;
using Npgsql;
using NpgsqlTypes;

namespace CreatorFlow.Api.Repositories.ContentDetail;

/// <summary>
/// Cài đặt Npgsql cho IContentDetailRepository, viết theo database/01_schema.sql
/// (cột snake_case, enum UPPER_SNAKE_CASE). Deadline / ngày dự kiến đăng lưu 00:00 UTC để đọc lại không lệch ngày.
/// Sprint / thời lượng dự kiến chưa có cột trong schema nên chưa được lưu.
/// </summary>
public sealed class ContentDetailRepository(IDbConnectionFactory connectionFactory) : IContentDetailRepository
{
    public async Task<ContentDetailRecord?> GetDetailAsync(long contentId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT c.content_id, c.project_id, c.title, COALESCE(c.description, ''), COALESCE(c.content_type, ''),
                   c.status::text, c.priority::text, c.deadline, c.planned_publish_at, c.created_by,
                   ARRAY(SELECT p.name FROM content_platforms cp JOIN platforms p ON p.platform_id = cp.platform_id
                         WHERE cp.content_id = c.content_id ORDER BY p.name)::text[] AS platforms
            FROM contents c
            WHERE c.content_id = @content_id;
            """;

        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("content_id", NpgsqlDbType.Bigint, contentId);

        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new ContentDetailRecord(
            reader.GetInt64(0),
            reader.GetInt64(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            TaskRepository.ToPascal(reader.GetString(5)),
            TaskRepository.ToPascal(reader.GetString(6)),
            reader.IsDBNull(7) ? null : ToDate(reader.GetDateTime(7)),
            reader.IsDBNull(8) ? null : ToDate(reader.GetDateTime(8)),
            reader.GetInt64(9),
            reader.GetFieldValue<string[]>(10));
    }

    public async Task<IReadOnlyList<string>> GetActivePlatformNamesAsync(CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand("SELECT name FROM platforms WHERE is_active ORDER BY platform_id;", connection);

        var names = new List<string>();
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) names.Add(reader.GetString(0));
        return names;
    }

    public async Task<IReadOnlyList<string>> GetPublishedPlatformNamesAsync(long contentId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT p.name
            FROM content_platforms cp
            JOIN platforms p ON p.platform_id = cp.platform_id
            WHERE cp.content_id = @content_id AND cp.publication_status = 'PUBLISHED';
            """;

        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("content_id", NpgsqlDbType.Bigint, contentId);

        var names = new List<string>();
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) names.Add(reader.GetString(0));
        return names;
    }

    public async Task<long> CreateAsync(NewContent content, CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);
        ContentUpdate d = content.Details;

        long contentId;
        await using (var insert = new NpgsqlCommand("""
            INSERT INTO contents (project_id, title, description, content_type, priority, status, deadline, planned_publish_at, created_by)
            VALUES (@project_id, @title, @description, @content_type, @priority::content_priority, @status::content_status,
                    @deadline, @planned_publish_at, @created_by)
            RETURNING content_id;
            """, connection, transaction))
        {
            insert.Parameters.AddWithValue("project_id", NpgsqlDbType.Bigint, content.ProjectId);
            insert.Parameters.AddWithValue("title", NpgsqlDbType.Varchar, d.Title);
            insert.Parameters.AddWithValue("description", NpgsqlDbType.Text, NullIfEmpty(d.Description));
            insert.Parameters.AddWithValue("content_type", NpgsqlDbType.Varchar, NullIfEmpty(d.ContentType));
            insert.Parameters.AddWithValue("priority", NpgsqlDbType.Text, d.PriorityDb);
            insert.Parameters.AddWithValue("status", NpgsqlDbType.Text, content.StatusDb);
            insert.Parameters.AddWithValue("deadline", NpgsqlDbType.TimestampTz, ToDb(d.Deadline));
            insert.Parameters.AddWithValue("planned_publish_at", NpgsqlDbType.TimestampTz, ToDb(d.PlannedPublishAt));
            insert.Parameters.AddWithValue("created_by", NpgsqlDbType.Bigint, content.CreatedBy);
            contentId = (long)(await insert.ExecuteScalarAsync(cancellationToken))!;
        }

        await using (var add = new NpgsqlCommand("""
            INSERT INTO content_platforms (content_id, platform_id)
            SELECT @content_id, p.platform_id FROM platforms p
            WHERE p.name = ANY(@names) AND p.is_active
            ON CONFLICT (content_id, platform_id) DO NOTHING;
            """, connection, transaction))
        {
            add.Parameters.AddWithValue("content_id", NpgsqlDbType.Bigint, contentId);
            add.Parameters.AddWithValue("names", NpgsqlDbType.Array | NpgsqlDbType.Varchar, d.Platforms.ToArray());
            await add.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var history = new NpgsqlCommand("""
            INSERT INTO content_status_history (content_id, from_status, to_status, changed_by, note)
            VALUES (@content_id, @status::content_status, @status::content_status, @created_by, @note);
            """, connection, transaction))
        {
            history.Parameters.AddWithValue("content_id", NpgsqlDbType.Bigint, contentId);
            history.Parameters.AddWithValue("status", NpgsqlDbType.Text, content.StatusDb);
            history.Parameters.AddWithValue("created_by", NpgsqlDbType.Bigint, content.CreatedBy);
            history.Parameters.AddWithValue("note", NpgsqlDbType.Text, "Tạo nội dung mới trên Production Board");
            await history.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return contentId;
    }

    public async Task UpdateAsync(long contentId, ContentUpdate update, CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var command = new NpgsqlCommand("""
            UPDATE contents
            SET title = @title, description = @description, content_type = @content_type,
                priority = @priority::content_priority, deadline = @deadline,
                planned_publish_at = @planned_publish_at, updated_at = CURRENT_TIMESTAMP
            WHERE content_id = @content_id;
            """, connection, transaction))
        {
            command.Parameters.AddWithValue("title", NpgsqlDbType.Varchar, update.Title);
            command.Parameters.AddWithValue("description", NpgsqlDbType.Text, NullIfEmpty(update.Description));
            command.Parameters.AddWithValue("content_type", NpgsqlDbType.Varchar, NullIfEmpty(update.ContentType));
            command.Parameters.AddWithValue("priority", NpgsqlDbType.Text, update.PriorityDb);
            command.Parameters.AddWithValue("deadline", NpgsqlDbType.TimestampTz, ToDb(update.Deadline));
            command.Parameters.AddWithValue("planned_publish_at", NpgsqlDbType.TimestampTz, ToDb(update.PlannedPublishAt));
            command.Parameters.AddWithValue("content_id", NpgsqlDbType.Bigint, contentId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        // Thêm cái thiếu, xóa cái bỏ chọn — KHÔNG xóa hết rồi chèn lại vì content_metrics cascade từ content_platforms.
        string[] names = update.Platforms.ToArray();
        await using (var add = new NpgsqlCommand("""
            INSERT INTO content_platforms (content_id, platform_id)
            SELECT @content_id, p.platform_id FROM platforms p
            WHERE p.name = ANY(@names) AND p.is_active
            ON CONFLICT (content_id, platform_id) DO NOTHING;
            """, connection, transaction))
        {
            add.Parameters.AddWithValue("content_id", NpgsqlDbType.Bigint, contentId);
            add.Parameters.AddWithValue("names", NpgsqlDbType.Array | NpgsqlDbType.Varchar, names);
            await add.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var remove = new NpgsqlCommand("""
            DELETE FROM content_platforms cp
            USING platforms p
            WHERE cp.content_id = @content_id AND p.platform_id = cp.platform_id AND p.name <> ALL(@names);
            """, connection, transaction))
        {
            remove.Parameters.AddWithValue("content_id", NpgsqlDbType.Bigint, contentId);
            remove.Parameters.AddWithValue("names", NpgsqlDbType.Array | NpgsqlDbType.Varchar, names);
            await remove.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<ScriptRecord?> GetScriptAsync(long contentId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT content_id, project_id, status::text, created_by, script
            FROM contents
            WHERE content_id = @content_id;
            """;

        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("content_id", NpgsqlDbType.Bigint, contentId);

        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new ScriptRecord(
            reader.GetInt64(0),
            reader.GetInt64(1),
            TaskRepository.ToPascal(reader.GetString(2)),
            reader.GetInt64(3),
            reader.IsDBNull(4) ? null : reader.GetString(4));
    }

    public async Task<bool> TryUpdateScriptAsync(long contentId, string? expectedRaw, string? newScript, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE contents
            SET script = @new_script, updated_at = CURRENT_TIMESTAMP
            WHERE content_id = @content_id AND script IS NOT DISTINCT FROM @expected;
            """;

        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("new_script", NpgsqlDbType.Text, (object?)newScript ?? DBNull.Value);
        command.Parameters.AddWithValue("expected", NpgsqlDbType.Text, (object?)expectedRaw ?? DBNull.Value);
        command.Parameters.AddWithValue("content_id", NpgsqlDbType.Bigint, contentId);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<IReadOnlyList<ActivityRecord>> GetActivityAsync(long contentId, CancellationToken cancellationToken = default)
    {
        var entries = new List<ActivityItemResponse>();
        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);

        await using (var history = new NpgsqlCommand("""
            SELECT h.changed_at, COALESCE(u.display_name, ''), h.from_status::text, h.to_status::text, h.note
            FROM content_status_history h
            LEFT JOIN users u ON u.user_id = h.changed_by
            WHERE h.content_id = @content_id;
            """, connection))
        {
            history.Parameters.AddWithValue("content_id", NpgsqlDbType.Bigint, contentId);
            await using NpgsqlDataReader reader = await history.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                string? from = reader.IsDBNull(2) ? null : TaskRepository.ToPascal(reader.GetString(2));
                string to = TaskRepository.ToPascal(reader.GetString(3));
                entries.Add(new ActivityItemResponse(
                    reader.GetDateTime(0),
                    reader.GetString(1),
                    from is null ? to : $"{from} → {to}",
                    reader.IsDBNull(4) ? null : reader.GetString(4),
                    ActivityKinds.StatusChange,
                    from,
                    to));
            }
        }

        await using (var reviews = new NpgsqlCommand("""
            SELECT r.submitted_at, COALESCE(su.display_name, ''), r.review_no, r.status::text,
                   r.reviewed_at, COALESCE(ru.display_name, ''), r.feedback
            FROM reviews r
            LEFT JOIN users su ON su.user_id = r.submitted_by
            LEFT JOIN users ru ON ru.user_id = r.reviewer_id
            WHERE r.content_id = @content_id;
            """, connection))
        {
            reviews.Parameters.AddWithValue("content_id", NpgsqlDbType.Bigint, contentId);
            await using NpgsqlDataReader reader = await reviews.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                int reviewNo = reader.GetInt32(2);
                string status = TaskRepository.ToPascal(reader.GetString(3));
                entries.Add(new ActivityItemResponse(
                    reader.GetDateTime(0),
                    reader.GetString(1),
                    $"submitted Review #{reviewNo}",
                    null,
                    ActivityKinds.ReviewSubmitted,
                    "Editing",
                    "Review",
                    reviewNo,
                    "Pending"));

                if (!reader.IsDBNull(4))
                {
                    bool approved = status == "Approved";
                    entries.Add(new ActivityItemResponse(
                        reader.GetDateTime(4),
                        reader.GetString(5),
                        approved ? $"approved Review #{reviewNo}" : $"rejected Review #{reviewNo}",
                        reader.IsDBNull(6) ? null : reader.GetString(6),
                        ActivityKinds.ReviewDecided,
                        "Review",
                        approved ? "Ready" : "Editing",
                        reviewNo,
                        status));
                }
            }
        }

        return ActivityTimeline.Compose(entries)
            .Select(i => new ActivityRecord(i.Timestamp, i.ActorName, i.Description, i.Feedback,
                i.Kind, i.FromStatus, i.ToStatus, i.ReviewNo, i.ReviewStatus))
            .ToList();
    }

    private static DateOnly ToDate(DateTime value) => DateOnly.FromDateTime(value.ToUniversalTime());

    /// <summary>Ngày thuần → 00:00 UTC (Npgsql chỉ nhận DateTime UTC cho timestamptz).</summary>
    private static object ToDb(DateOnly? date) =>
        date.HasValue ? DateTime.SpecifyKind(date.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc) : DBNull.Value;

    private static object NullIfEmpty(string text) => string.IsNullOrEmpty(text) ? DBNull.Value : text;
}
