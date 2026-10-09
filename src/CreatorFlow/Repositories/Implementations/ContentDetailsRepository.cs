using CreatorFlow.Data;
using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Repositories.Interfaces;
using Npgsql;
using NpgsqlTypes;

namespace CreatorFlow.Repositories.Implementations;

/// <summary>
/// Cài đặt Npgsql cho IContentDetailsRepository: ghi bảng contents và đồng bộ content_platforms.
/// Chạy trong transaction hiện hành của session (ContentService đã Begin()).
/// Lưu đủ thông tin lập kế hoạch (SCRUM-32): title, description, script, content_type, priority, deadline, planned_publish_at.
/// Chưa lưu Sprint / EstimatedDuration / Assignee vì schema chưa có cột tương ứng
/// (assignee thuộc content_assignments — phần Workflow/Assignment).
/// </summary>
public class ContentDetailsRepository : IContentDetailsRepository
{
    private readonly IDbSession _session;

    public ContentDetailsRepository(IDbSession session) => _session = session;

    public long Create(long projectId, ContentStatus status, ContentDraft draft, long createdByUserId)
    {
        using var cmd = _session.CreateCommand(@"
INSERT INTO contents (project_id, title, description, script, content_type, priority, status, deadline, planned_publish_at, created_by)
VALUES (@projectId, @title, @description, @script, @contentType, @priority::content_priority, @status::content_status, @deadline, @plannedPublishAt, @createdBy)
RETURNING content_id");
        cmd.Parameters.AddWithValue("projectId", projectId);
        cmd.Parameters.AddWithValue("status", PostgresEnumMapper.ToDatabaseValue(status));
        cmd.Parameters.AddWithValue("createdBy", createdByUserId);
        AddPlanningParameters(cmd, draft);

        long contentId = (long)cmd.ExecuteScalar()!;
        SyncPlatforms(contentId, draft.Platforms);
        return contentId;
    }

    public void Update(long contentId, ContentDraft draft)
    {
        using (var cmd = _session.CreateCommand(@"
UPDATE contents
SET title = @title, description = @description, script = @script, content_type = @contentType,
    priority = @priority::content_priority, deadline = @deadline, planned_publish_at = @plannedPublishAt
WHERE content_id = @id"))
        {
            AddPlanningParameters(cmd, draft);
            cmd.Parameters.AddWithValue("id", contentId);
            cmd.ExecuteNonQuery();
        }

        SyncPlatforms(contentId, draft.Platforms);
    }

    public Content? GetDetail(long contentId)
    {
        using var cmd = _session.CreateCommand(@"
SELECT content_id, project_id, source_idea_id, title, description, script, content_type,
       priority::text, status::text, deadline, planned_publish_at, created_by, created_at, updated_at
FROM contents
WHERE content_id = @id");
        cmd.Parameters.AddWithValue("id", contentId);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return null;

        return new Content
        {
            ContentId = reader.GetInt64(0),
            ProjectId = reader.GetInt64(1),
            SourceIdeaId = reader.IsDBNull(2) ? null : reader.GetInt64(2),
            Title = reader.GetString(3),
            Description = reader.IsDBNull(4) ? null : reader.GetString(4),
            Script = reader.IsDBNull(5) ? null : reader.GetString(5),
            ContentType = reader.IsDBNull(6) ? null : reader.GetString(6),
            Priority = PostgresEnumMapper.Parse<Priority>(reader.GetString(7)),
            Status = PostgresEnumMapper.Parse<ContentStatus>(reader.GetString(8)),
            Deadline = reader.IsDBNull(9) ? null : FromDbDate(reader.GetDateTime(9)),
            PlannedPublishAt = reader.IsDBNull(10) ? null : FromDbDate(reader.GetDateTime(10)),
            CreatedBy = reader.GetInt64(11),
            CreatedAt = reader.GetDateTime(12),
            UpdatedAt = reader.GetDateTime(13),
        };
    }

    /// <summary>Các tham số lập kế hoạch dùng chung cho INSERT và UPDATE (cùng tên với câu SQL tương ứng).</summary>
    private static void AddPlanningParameters(NpgsqlCommand cmd, ContentDraft draft)
    {
        cmd.Parameters.AddWithValue("title", draft.Title);
        cmd.Parameters.Add("description", NpgsqlDbType.Text).Value = ToDbText(draft.Description);
        cmd.Parameters.Add("script", NpgsqlDbType.Text).Value = ToDbText(draft.Script);
        cmd.Parameters.Add("contentType", NpgsqlDbType.Varchar).Value = ToDbText(draft.ContentType);
        cmd.Parameters.AddWithValue("priority", PostgresEnumMapper.ToDatabaseValue(draft.Priority));
        cmd.Parameters.Add("deadline", NpgsqlDbType.TimestampTz).Value = ToDbDate(draft.Deadline);
        cmd.Parameters.Add("plannedPublishAt", NpgsqlDbType.TimestampTz).Value = ToDbDate(draft.PlannedPublishAt);
    }

    /// <summary>
    /// Đồng bộ content_platforms theo kiểu "thêm cái thiếu, xóa cái bỏ chọn".
    /// KHÔNG xóa hết rồi chèn lại: content_metrics có ON DELETE CASCADE từ content_platforms,
    /// xóa-chèn lại sẽ làm mất toàn bộ metrics của các platform được giữ nguyên.
    /// </summary>
    private void SyncPlatforms(long contentId, List<string> platformNames)
    {
        string[] names = platformNames.ToArray();

        using (var add = _session.CreateCommand(@"
INSERT INTO content_platforms (content_id, platform_id)
SELECT @contentId, p.platform_id FROM platforms p
WHERE p.name = ANY(@names) AND p.is_active
ON CONFLICT (content_id, platform_id) DO NOTHING"))
        {
            add.Parameters.AddWithValue("contentId", contentId);
            add.Parameters.Add("names", NpgsqlDbType.Array | NpgsqlDbType.Varchar).Value = names;
            add.ExecuteNonQuery();
        }

        using (var remove = _session.CreateCommand(@"
DELETE FROM content_platforms cp
USING platforms p
WHERE cp.content_id = @contentId AND p.platform_id = cp.platform_id AND p.name <> ALL(@names)"))
        {
            remove.Parameters.AddWithValue("contentId", contentId);
            remove.Parameters.Add("names", NpgsqlDbType.Array | NpgsqlDbType.Varchar).Value = names;
            remove.ExecuteNonQuery();
        }
    }

    /// <summary>Deadline / ngày dự kiến đăng chỉ là ngày: lưu 00:00 UTC để đọc lại không bị lệch ngày theo múi giờ (Npgsql chỉ nhận DateTime UTC cho timestamptz).</summary>
    private static object ToDbDate(DateTime? date) =>
        date.HasValue ? DateTime.SpecifyKind(date.Value.Date, DateTimeKind.Utc) : DBNull.Value;

    /// <summary>Đọc lại ngày đã lưu ở dạng 00:00 UTC thành ngày thuần (Kind Unspecified), để UI/ToLocalTime về sau không làm lệch ngày.</summary>
    private static DateTime FromDbDate(DateTime value) =>
        DateTime.SpecifyKind(value.Date, DateTimeKind.Unspecified);

    /// <summary>Chuỗi rỗng lưu thành NULL để cột script không lẫn giữa "chưa nhập" và "rỗng".</summary>
    private static object ToDbText(string? text) =>
        string.IsNullOrEmpty(text) ? DBNull.Value : text;
}