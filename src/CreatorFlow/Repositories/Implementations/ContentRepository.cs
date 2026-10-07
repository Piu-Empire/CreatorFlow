using CreatorFlow.Models.Enums;
using CreatorFlow.Data;
using CreatorFlow.Models;
using CreatorFlow.Repositories.Interfaces;

namespace CreatorFlow.Repositories.Implementations;

/// <summary>Cài đặt Npgsql cho IContentRepository (bảng Contents — mục 5.4 bảng 10).</summary>
public class ContentRepository : IContentRepository
{
    private readonly IDbSession _session;

    public ContentRepository(IDbSession session) => _session = session;

    public Content GetById(long contentId)
    {
        using var cmd = _session.CreateCommand(
            "SELECT id, projectid, ideaid, title, status, createdbyuserid, updatedat, deadline " +
            "FROM contents WHERE id = @id");
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
            Status = Enum.Parse<ContentStatus>(reader.GetString(4)),
            CreatedByUserId = reader.GetInt64(5),
            UpdatedAt = reader.GetDateTime(6),
            Deadline = reader.IsDBNull(7) ? null : reader.GetDateTime(7).ToLocalTime(),
        };
    }

    /// <summary>Chạy trong transaction hiện hành của session (nếu WorkflowService đang Begin()).</summary>
    public void UpdateStatus(long contentId, ContentStatus newStatus)
    {
        using var cmd = _session.CreateCommand(
            "UPDATE contents SET status = @status, updatedat = now() WHERE id = @id");
        cmd.Parameters.AddWithValue("status", newStatus.ToString());
        cmd.Parameters.AddWithValue("id", contentId);
        cmd.ExecuteNonQuery();
    }
}