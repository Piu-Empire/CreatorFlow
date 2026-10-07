using CreatorFlow.Models.Enums;
using CreatorFlow.Data;
﻿using CreatorFlow.Data;
using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Repositories.Interfaces;
using Npgsql;
using NpgsqlTypes;

namespace CreatorFlow.Repositories.Implementations;

/// <summary>
/// Cài đặt Npgsql cho IContentDetailsRepository: ghi bảng contents và đồng bộ content_platforms.
/// Chạy trong transaction hiện hành của session (ContentService đã Begin()).
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
INSERT INTO contents (project_id, title, description, priority, status, deadline, created_by)
VALUES (@projectId, @title, @description, @priority::content_priority, @status::content_status, @deadline, @createdBy)
RETURNING content_id");
        cmd.Parameters.AddWithValue("projectId", projectId);
        cmd.Parameters.AddWithValue("title", draft.Title);
        cmd.Parameters.AddWithValue("description", draft.Description);
        cmd.Parameters.AddWithValue("priority", PostgresEnumMapper.ToDatabaseValue(draft.Priority));
        cmd.Parameters.AddWithValue("status", PostgresEnumMapper.ToDatabaseValue(status));
        cmd.Parameters.Add("deadline", NpgsqlDbType.TimestampTz).Value = ToDbDeadline(draft.Deadline);
        cmd.Parameters.AddWithValue("createdBy", createdByUserId);

        long contentId = (long)cmd.ExecuteScalar()!;
        SyncPlatforms(contentId, draft.Platforms);
        return contentId;
    }

    public void Update(long contentId, ContentDraft draft)
    {
        using (var cmd = _session.CreateCommand(@"
UPDATE contents
SET title = @title, description = @description, priority = @priority::content_priority, deadline = @deadline
WHERE content_id = @id"))
        {
            cmd.Parameters.AddWithValue("title", draft.Title);
            cmd.Parameters.AddWithValue("description", draft.Description);
            cmd.Parameters.AddWithValue("priority", PostgresEnumMapper.ToDatabaseValue(draft.Priority));
            cmd.Parameters.Add("deadline", NpgsqlDbType.TimestampTz).Value = ToDbDeadline(draft.Deadline);
            cmd.Parameters.AddWithValue("id", contentId);
            cmd.ExecuteNonQuery();
        }

        SyncPlatforms(contentId, draft.Platforms);
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

    /// <summary>Deadline chỉ là ngày: lưu 00:00 UTC để đọc lại không bị lệch ngày theo múi giờ (Npgsql chỉ nhận DateTime UTC cho timestamptz).</summary>
    private static object ToDbDeadline(DateTime? deadline) =>
        deadline.HasValue ? DateTime.SpecifyKind(deadline.Value.Date, DateTimeKind.Utc) : DBNull.Value;
}