using CreatorFlow.Models.Enums;
using CreatorFlow.Data;
using CreatorFlow.Models;
using CreatorFlow.Repositories.Interfaces;

namespace CreatorFlow.Repositories.Implementations;

/// <summary>Cài đặt Npgsql cho IContentRepository (bảng contents).</summary>
public class ContentRepository : IContentRepository
{
    private readonly IDbSession _session;

    public ContentRepository(IDbSession session) => _session = session;

    public Content GetById(long contentId)
    {
        using var cmd = _session.CreateCommand(
            "SELECT content_id, project_id, source_idea_id, title, status::text, created_by, updated_at " +
            "FROM contents WHERE content_id = @id");
        cmd.Parameters.AddWithValue("id", contentId);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return null!; // WorkflowService.GetContentOrThrow tự kiểm tra null và ném lỗi nghiệp vụ phù hợp.

        return new Content
        {
            Id = reader.GetInt64(0),
            ProjectId = reader.GetInt64(1),
            IdeaId = reader.IsDBNull(2) ? null : reader.GetInt64(2),
            Title = reader.GetString(3),
            Status = PostgresEnumMapper.Parse<ContentStatus>(reader.GetString(4)),
            CreatedByUserId = reader.GetInt64(5),
            UpdatedAt = reader.GetDateTime(6),
        };
    }

    /// <summary>Chạy trong transaction hiện hành của session (nếu WorkflowService đang Begin()). updated_at do trigger set_updated_at cập nhật.</summary>
    public void UpdateStatus(long contentId, ContentStatus newStatus)
    {
        using var cmd = _session.CreateCommand(
            "UPDATE contents SET status = @status::content_status WHERE content_id = @id");
        cmd.Parameters.AddWithValue("status", PostgresEnumMapper.ToDatabaseValue(newStatus));
        cmd.Parameters.AddWithValue("id", contentId);
        cmd.ExecuteNonQuery();
    }
}