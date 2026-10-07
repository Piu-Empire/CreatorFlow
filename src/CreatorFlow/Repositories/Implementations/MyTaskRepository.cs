using CreatorFlow.Data;
using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Repositories.Interfaces;
using Npgsql;
using NpgsqlTypes;

namespace CreatorFlow.Repositories.Implementations;

/// <summary>
/// Cài đặt Npgsql cho IMyTaskRepository. Viết theo database/01_schema.sql (cột snake_case, enum UPPER_SNAKE_CASE).
/// Một Content có thể có nhiều dòng content_assignments (mỗi Creator một dòng, tiến độ + deadline riêng).
/// Lọc ngay ở SQL theo assignee_id + project_id để không bao giờ kéo task của người khác / project khác lên.
/// Deadline hiển thị = content_assignments.deadline (riêng), rơi về contents.deadline (chung) nếu chưa đặt riêng.
/// </summary>
public class MyTaskRepository : IMyTaskRepository
{
    private const string SelectSql = @"
SELECT
    ca.assignment_id,
    c.content_id,
    c.project_id,
    'CNT-' || LPAD(c.content_id::text, 3, '0') AS code,
    c.title,
    c.status::text,
    c.priority::text,
    ca.status::text,
    ca.progress_percent,
    COALESCE(ca.deadline, c.deadline) AS deadline,
    ab.display_name,
    (SELECT STRING_AGG(p.name, ' • ' ORDER BY p.name)
       FROM content_platforms cp JOIN platforms p ON p.platform_id = cp.platform_id
       WHERE cp.content_id = c.content_id) AS platforms,
    ca.assignee_id,
    (SELECT COUNT(*) FROM content_assignments t
       WHERE t.content_id = c.content_id AND t.status <> 'CANCELLED') AS team_total,
    (SELECT COUNT(*) FROM content_assignments t
       WHERE t.content_id = c.content_id AND t.status = 'COMPLETED') AS team_done
FROM content_assignments ca
JOIN contents c ON c.content_id = ca.content_id
LEFT JOIN users ab ON ab.user_id = ca.assigned_by";

    private readonly IDbSession _session;

    public MyTaskRepository(IDbSession session) => _session = session;

    public List<MyTaskItem> GetMyTasks(long projectId, long assigneeUserId)
    {
        const string sql = SelectSql + @"
WHERE ca.assignee_id = @userId
  AND c.project_id = @projectId
  AND ca.status <> 'CANCELLED'
  AND c.status <> 'ARCHIVED'
ORDER BY c.content_id";

        using var cmd = _session.CreateCommand(sql);
        cmd.Parameters.AddWithValue("userId", assigneeUserId);
        cmd.Parameters.AddWithValue("projectId", projectId);
        return ReadAll(cmd);
    }

    public MyTaskItem? GetAssignment(long contentId, long assigneeUserId)
    {
        const string sql = SelectSql + @"
WHERE ca.content_id = @contentId
  AND ca.assignee_id = @userId
  AND ca.status <> 'CANCELLED'
ORDER BY ca.assigned_at DESC
LIMIT 1";

        using var cmd = _session.CreateCommand(sql);
        cmd.Parameters.AddWithValue("contentId", contentId);
        cmd.Parameters.AddWithValue("userId", assigneeUserId);
        return ReadAll(cmd).FirstOrDefault();
    }

    public List<MyTaskItem> GetAssignments(long contentId)
    {
        const string sql = SelectSql + @"
WHERE ca.content_id = @contentId
  AND ca.status <> 'CANCELLED'
ORDER BY ca.assigned_at, ca.assignment_id";

        using var cmd = _session.CreateCommand(sql);
        cmd.Parameters.AddWithValue("contentId", contentId);
        return ReadAll(cmd);
    }

    public void UpdateProgress(long assignmentId, int percent, AssignmentStatus status)
    {
        const string sql = @"
UPDATE content_assignments
SET progress_percent = @percent,
    status = @status::assignment_status,
    completed_at = CASE WHEN @completed THEN COALESCE(completed_at, CURRENT_TIMESTAMP) ELSE NULL END
WHERE assignment_id = @id";

        using var cmd = _session.CreateCommand(sql);
        cmd.Parameters.AddWithValue("percent", NpgsqlDbType.Smallint, (short)percent);
        cmd.Parameters.AddWithValue("status", NpgsqlDbType.Text, PostgresEnumMapper.ToDatabaseValue(status));
        cmd.Parameters.AddWithValue("completed", NpgsqlDbType.Boolean, status == AssignmentStatus.Completed);
        cmd.Parameters.AddWithValue("id", NpgsqlDbType.Bigint, assignmentId);
        cmd.ExecuteNonQuery();
    }

    public void UpdateAssignmentDeadline(long assignmentId, DateTime? deadline)
    {
        using var cmd = _session.CreateCommand(
            "UPDATE content_assignments SET deadline = @deadline WHERE assignment_id = @id");
        cmd.Parameters.AddWithValue("deadline", NpgsqlDbType.TimestampTz, ToDbDeadline(deadline));
        cmd.Parameters.AddWithValue("id", NpgsqlDbType.Bigint, assignmentId);
        cmd.ExecuteNonQuery();
    }

    public void AddAssignment(long contentId, long assigneeUserId, long assignedByUserId, DateTime? deadline)
    {
        using var cmd = _session.CreateCommand(
            "INSERT INTO content_assignments (content_id, assignee_id, assigned_by, status, progress_percent, deadline) " +
            "VALUES (@contentId, @assignee, @by, 'ASSIGNED', 0, @deadline)");
        cmd.Parameters.AddWithValue("contentId", NpgsqlDbType.Bigint, contentId);
        cmd.Parameters.AddWithValue("assignee", NpgsqlDbType.Bigint, assigneeUserId);
        cmd.Parameters.AddWithValue("by", NpgsqlDbType.Bigint, assignedByUserId);
        cmd.Parameters.AddWithValue("deadline", NpgsqlDbType.TimestampTz, ToDbDeadline(deadline));
        cmd.ExecuteNonQuery();
    }

    public void CancelAssignment(long assignmentId)
    {
        using var cmd = _session.CreateCommand(
            "UPDATE content_assignments SET status = 'CANCELLED' WHERE assignment_id = @id");
        cmd.Parameters.AddWithValue("id", NpgsqlDbType.Bigint, assignmentId);
        cmd.ExecuteNonQuery();
    }

    private static List<MyTaskItem> ReadAll(NpgsqlCommand cmd)
    {
        var items = new List<MyTaskItem>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            items.Add(Map(reader));
        return items;
    }

    /// <summary>timestamptz yêu cầu DateTime có Kind rõ ràng → coi deadline là 00:00 giờ máy rồi đổi sang UTC.</summary>
    private static object ToDbDeadline(DateTime? deadline) =>
        deadline.HasValue
            ? DateTime.SpecifyKind(deadline.Value.Date, DateTimeKind.Local).ToUniversalTime()
            : DBNull.Value;

    private static MyTaskItem Map(NpgsqlDataReader reader) => new()
    {
        AssignmentId = reader.GetInt64(0),
        ContentId = reader.GetInt64(1),
        ProjectId = reader.GetInt64(2),
        ContentCode = reader.GetString(3),
        ContentTitle = reader.GetString(4),
        ContentStage = PostgresEnumMapper.Parse<ContentStatus>(reader.GetString(5)),
        Priority = PostgresEnumMapper.Parse<Priority>(reader.GetString(6)),
        Status = PostgresEnumMapper.Parse<AssignmentStatus>(reader.GetString(7)),
        ProgressPercent = reader.GetInt16(8),
        // timestamptz trả về UTC → đổi sang giờ máy để so sánh "quá hạn / hôm nay" đúng ngày.
        Deadline = reader.IsDBNull(9) ? null : reader.GetDateTime(9).ToLocalTime(),
        AssignedByName = reader.IsDBNull(10) ? null : reader.GetString(10),
        Platforms = reader.IsDBNull(11)
            ? new List<string>()
            : reader.GetString(11).Split(" • ", StringSplitOptions.RemoveEmptyEntries).ToList(),
        AssigneeUserId = reader.GetInt64(12),
        TeamTotal = (int)reader.GetInt64(13),
        TeamDone = (int)reader.GetInt64(14),
    };
}