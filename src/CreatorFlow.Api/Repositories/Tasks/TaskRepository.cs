using CreatorFlow.Api.Models.Tasks;
using CreatorFlow.Data;
using Npgsql;
using NpgsqlTypes;

namespace CreatorFlow.Api.Repositories.Tasks;

/// <summary>
/// Cài đặt Npgsql cho ITaskRepository, viết theo database/01_schema.sql (cột snake_case, enum UPPER_SNAKE_CASE).
/// Một Content có thể có nhiều dòng content_assignments (mỗi Creator một dòng, tiến độ + deadline riêng).
/// Deadline trả về = content_assignments.deadline (riêng), rơi về contents.deadline (chung) nếu chưa đặt riêng.
/// Deadline là NGÀY: ghi xuống timestamptz lúc 00:00 UTC, đọc lên lấy ngày theo UTC.
/// </summary>
public sealed class TaskRepository(IDbConnectionFactory connectionFactory) : ITaskRepository
{
    private const string AssignmentSelect = """
        SELECT
            ca.assignment_id,
            c.content_id,
            c.project_id,
            ca.assignee_id,
            'CNT-' || LPAD(c.content_id::text, 3, '0') AS code,
            c.title,
            c.status::text,
            c.priority::text,
            ca.status::text,
            ca.progress_percent,
            COALESCE(ca.deadline, c.deadline) AS deadline,
            ab.display_name,
            (SELECT STRING_AGG(p.name, '|' ORDER BY p.name)
               FROM content_platforms cp JOIN platforms p ON p.platform_id = cp.platform_id
               WHERE cp.content_id = c.content_id) AS platforms,
            (SELECT COUNT(*) FROM content_assignments t
               WHERE t.content_id = c.content_id AND t.status <> 'CANCELLED') AS team_total,
            (SELECT COUNT(*) FROM content_assignments t
               WHERE t.content_id = c.content_id AND t.status = 'COMPLETED') AS team_done
        FROM content_assignments ca
        JOIN contents c ON c.content_id = ca.content_id
        LEFT JOIN users ab ON ab.user_id = ca.assigned_by
        """;

    public async Task<ProjectRole?> GetRoleAsync(long projectId, long userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT role::text
            FROM project_members
            WHERE project_id = @project_id AND user_id = @user_id AND is_active;
            """;

        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("project_id", NpgsqlDbType.Bigint, projectId);
        command.Parameters.AddWithValue("user_id", NpgsqlDbType.Bigint, userId);

        object? value = await command.ExecuteScalarAsync(cancellationToken);
        return value is string text ? ParseEnum<ProjectRole>(text) : null;
    }

    public async Task<IReadOnlyList<ProjectMember>> GetMembersAsync(long projectId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT u.user_id, u.display_name, pm.role::text
            FROM project_members pm
            JOIN users u ON u.user_id = pm.user_id
            WHERE pm.project_id = @project_id AND pm.is_active AND u.account_status = 'ACTIVE'
            ORDER BY u.display_name, u.user_id;
            """;

        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("project_id", NpgsqlDbType.Bigint, projectId);

        var members = new List<ProjectMember>();
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            members.Add(new ProjectMember(reader.GetInt64(0), reader.GetString(1), ParseEnum<ProjectRole>(reader.GetString(2))));
        return members;
    }

    public async Task<ContentInfo?> GetContentAsync(long contentId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT content_id, project_id, status::text, created_by, deadline
            FROM contents
            WHERE content_id = @content_id;
            """;

        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("content_id", NpgsqlDbType.Bigint, contentId);

        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new ContentInfo(reader.GetInt64(0), reader.GetInt64(1), ToPascal(reader.GetString(2)),
            reader.GetInt64(3), reader.IsDBNull(4) ? null : ToDate(reader.GetDateTime(4)));
    }

    public async Task<IReadOnlyList<TaskAssignment>> GetMyTasksAsync(long projectId, long userId, CancellationToken cancellationToken = default)
    {
        string sql = AssignmentSelect + """

            WHERE ca.assignee_id = @user_id
              AND c.project_id = @project_id
              AND ca.status <> 'CANCELLED'
              AND c.status <> 'ARCHIVED'
            ORDER BY c.content_id;
            """;

        return await QueryAssignmentsAsync(sql, cancellationToken,
            ("user_id", userId), ("project_id", projectId));
    }

    public async Task<TaskAssignment?> GetAssignmentAsync(long contentId, long userId, CancellationToken cancellationToken = default)
    {
        string sql = AssignmentSelect + """

            WHERE ca.content_id = @content_id
              AND ca.assignee_id = @user_id
              AND ca.status <> 'CANCELLED'
            ORDER BY ca.assigned_at DESC
            LIMIT 1;
            """;

        var rows = await QueryAssignmentsAsync(sql, cancellationToken, ("content_id", contentId), ("user_id", userId));
        return rows.Count == 0 ? null : rows[0];
    }

    public async Task<IReadOnlyList<TaskAssignment>> GetAssignmentsAsync(long contentId, CancellationToken cancellationToken = default)
    {
        string sql = AssignmentSelect + """

            WHERE ca.content_id = @content_id
              AND ca.status <> 'CANCELLED'
            ORDER BY ca.assigned_at, ca.assignment_id;
            """;

        return await QueryAssignmentsAsync(sql, cancellationToken, ("content_id", contentId));
    }

    public async Task<IReadOnlyList<BoardAssignee>> GetBoardAssigneesAsync(long projectId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                ca.content_id,
                ca.assignee_id,
                u.display_name,
                ca.status::text,
                ca.progress_percent,
                COALESCE(ca.deadline, c.deadline) AS deadline
            FROM content_assignments ca
            JOIN contents c ON c.content_id = ca.content_id
            JOIN users u ON u.user_id = ca.assignee_id
            WHERE c.project_id = @project_id
              AND ca.status <> 'CANCELLED'
            ORDER BY ca.content_id, ca.assigned_at, ca.assignment_id;
            """;

        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("project_id", NpgsqlDbType.Bigint, projectId);

        var rows = new List<BoardAssignee>();
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new BoardAssignee(
                reader.GetInt64(0),
                reader.GetInt64(1),
                reader.GetString(2),
                ParseEnum<AssignmentStatus>(reader.GetString(3)),
                reader.GetInt16(4),
                reader.IsDBNull(5) ? null : ToDate(reader.GetDateTime(5))));
        }
        return rows;
    }

    public async Task UpdateProgressAsync(long assignmentId, int percent, AssignmentStatus status, CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(UpdateProgressSql, connection);
        BindProgress(command, assignmentId, percent, status);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAssignmentDeadlineAsync(long assignmentId, DateOnly? deadline, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE content_assignments SET deadline = @deadline WHERE assignment_id = @assignment_id;";

        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("deadline", NpgsqlDbType.TimestampTz, ToDb(deadline));
        command.Parameters.AddWithValue("assignment_id", NpgsqlDbType.Bigint, assignmentId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task ApplyAssignmentsAsync(
        long contentId,
        long actorUserId,
        IReadOnlyList<long> cancelAssignmentIds,
        IReadOnlyList<DeadlineChange> deadlineChanges,
        IReadOnlyList<NewAssignment> additions,
        CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        foreach (long assignmentId in cancelAssignmentIds)
        {
            // Assignment đã hoàn thành được giữ lại làm lịch sử, không bao giờ bị huỷ.
            await using var cancel = new NpgsqlCommand("""
                UPDATE content_assignments SET status = 'CANCELLED'
                WHERE assignment_id = @assignment_id AND content_id = @content_id AND status <> 'COMPLETED';
                """, connection, transaction);
            cancel.Parameters.AddWithValue("assignment_id", NpgsqlDbType.Bigint, assignmentId);
            cancel.Parameters.AddWithValue("content_id", NpgsqlDbType.Bigint, contentId);
            await cancel.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (DeadlineChange change in deadlineChanges)
        {
            await using var update = new NpgsqlCommand("""
                UPDATE content_assignments SET deadline = @deadline
                WHERE assignment_id = @assignment_id AND content_id = @content_id;
                """, connection, transaction);
            update.Parameters.AddWithValue("deadline", NpgsqlDbType.TimestampTz, ToDb(change.Deadline));
            update.Parameters.AddWithValue("assignment_id", NpgsqlDbType.Bigint, change.AssignmentId);
            update.Parameters.AddWithValue("content_id", NpgsqlDbType.Bigint, contentId);
            await update.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (NewAssignment addition in additions)
        {
            await using var insert = new NpgsqlCommand("""
                INSERT INTO content_assignments (content_id, assignee_id, assigned_by, status, progress_percent, deadline)
                VALUES (@content_id, @assignee_id, @assigned_by, 'ASSIGNED', 0, @deadline);
                """, connection, transaction);
            insert.Parameters.AddWithValue("content_id", NpgsqlDbType.Bigint, contentId);
            insert.Parameters.AddWithValue("assignee_id", NpgsqlDbType.Bigint, addition.UserId);
            insert.Parameters.AddWithValue("assigned_by", NpgsqlDbType.Bigint, actorUserId);
            insert.Parameters.AddWithValue("deadline", NpgsqlDbType.TimestampTz, ToDb(addition.Deadline));
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var touch = new NpgsqlCommand(
            "UPDATE contents SET updated_at = CURRENT_TIMESTAMP WHERE content_id = @content_id;", connection, transaction))
        {
            touch.Parameters.AddWithValue("content_id", NpgsqlDbType.Bigint, contentId);
            await touch.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    // ------------------------------------------------------------------ helpers

    private const string UpdateProgressSql = """
        UPDATE content_assignments
        SET progress_percent = @progress_percent,
            status = @status::assignment_status,
            completed_at = CASE WHEN @completed THEN COALESCE(completed_at, CURRENT_TIMESTAMP) ELSE NULL END
        WHERE assignment_id = @assignment_id;
        """;

    private static void BindProgress(NpgsqlCommand command, long assignmentId, int percent, AssignmentStatus status)
    {
        command.Parameters.AddWithValue("progress_percent", NpgsqlDbType.Smallint, (short)percent);
        command.Parameters.AddWithValue("status", NpgsqlDbType.Text, ToDbEnum(status));
        command.Parameters.AddWithValue("completed", NpgsqlDbType.Boolean, status == AssignmentStatus.Completed);
        command.Parameters.AddWithValue("assignment_id", NpgsqlDbType.Bigint, assignmentId);
    }

    private async Task<IReadOnlyList<TaskAssignment>> QueryAssignmentsAsync(
        string sql, CancellationToken cancellationToken, params (string Name, long Value)[] parameters)
    {
        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        foreach ((string name, long value) in parameters)
            command.Parameters.AddWithValue(name, NpgsqlDbType.Bigint, value);

        var rows = new List<TaskAssignment>();
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new TaskAssignment(
                AssignmentId: reader.GetInt64(0),
                ContentId: reader.GetInt64(1),
                ProjectId: reader.GetInt64(2),
                AssigneeUserId: reader.GetInt64(3),
                ContentCode: reader.GetString(4),
                ContentTitle: reader.GetString(5),
                ContentStage: ToPascal(reader.GetString(6)),
                ContentPriority: ToPascal(reader.GetString(7)),
                Status: ParseEnum<AssignmentStatus>(reader.GetString(8)),
                ProgressPercent: reader.GetInt16(9),
                Deadline: reader.IsDBNull(10) ? null : ToDate(reader.GetDateTime(10)),
                AssignedByName: reader.IsDBNull(11) ? null : reader.GetString(11),
                Platforms: reader.IsDBNull(12)
                    ? Array.Empty<string>()
                    : reader.GetString(12).Split('|', StringSplitOptions.RemoveEmptyEntries),
                TeamTotal: (int)reader.GetInt64(13),
                TeamDone: (int)reader.GetInt64(14)));
        }
        return rows;
    }

    /// <summary>Ngày (UTC) của một timestamptz.</summary>
    private static DateOnly ToDate(DateTime value) => DateOnly.FromDateTime(value.ToUniversalTime());

    /// <summary>NGÀY → timestamptz lúc 00:00 UTC (null → DBNull).</summary>
    private static object ToDb(DateOnly? date) =>
        date.HasValue
            ? DateTime.SpecifyKind(date.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc)
            : DBNull.Value;

    /// <summary>"IN_PROGRESS" → "InProgress".</summary>
    internal static string ToPascal(string databaseValue) =>
        string.Concat(databaseValue.Split('_', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant()));

    /// <summary>AssignmentStatus.InProgress → "IN_PROGRESS".</summary>
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

    internal static TEnum ParseEnum<TEnum>(string databaseValue) where TEnum : struct, Enum =>
        Enum.Parse<TEnum>(ToPascal(databaseValue), ignoreCase: true);
}
