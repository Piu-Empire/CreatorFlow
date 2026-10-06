using CreatorFlow.Models.Enums;
using CreatorFlow.Data;
using CreatorFlow.Models;
using CreatorFlow.Repositories.Interfaces;

namespace CreatorFlow.Repositories.Implementations;

/// <summary>
/// Cài đặt Npgsql cho IBoardRepository. Đây là view-model cho Board (mục 4 UX spec) nên
/// 1 câu SELECT gộp thẳng contents + content_platforms/platforms + content_tags/tags +
/// content_assignments (lấy người được giao gần nhất) + kiểm tra có Review PENDING hay không,
/// thay vì load rời từng bảng rồi ghép ở tầng C# (đơn giản và đủ nhanh cho quy mô 1 Project).
/// </summary>
public class BoardRepository : IBoardRepository
{
    private readonly IDbSession _session;

    public BoardRepository(IDbSession session) => _session = session;

    public List<ContentBoardCard> GetBoardCards(long projectId)
    {
        const string sql = @"
SELECT
    c.content_id,
    c.project_id,
    'CNT-' || LPAD(c.content_id::text, 3, '0') AS code,
    c.title,
    c.status::text,
    c.priority::text,
    c.deadline,
    (SELECT STRING_AGG(p.name, ' • ' ORDER BY p.name)
       FROM content_platforms cp JOIN platforms p ON p.platform_id = cp.platform_id
       WHERE cp.content_id = c.content_id) AS platforms,
    (SELECT STRING_AGG(t.name, ',' ORDER BY t.name)
       FROM content_tags ct JOIN tags t ON t.tag_id = ct.tag_id
       WHERE ct.content_id = c.content_id) AS tags,
    (SELECT u.display_name
       FROM content_assignments ca JOIN users u ON u.user_id = ca.assignee_id
       WHERE ca.content_id = c.content_id
       ORDER BY ca.assigned_at DESC LIMIT 1) AS assignee_name,
    EXISTS (SELECT 1 FROM reviews r WHERE r.content_id = c.content_id AND r.status = 'PENDING') AS has_pending_review,
    COALESCE(c.description, '') AS description
FROM contents c
WHERE c.project_id = @projectId
ORDER BY c.content_id";

        using var cmd = _session.CreateCommand(sql);
        cmd.Parameters.AddWithValue("projectId", projectId);

        var cards = new List<ContentBoardCard>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            cards.Add(new ContentBoardCard
            {
                ContentId = reader.GetInt64(0),
                ProjectId = reader.GetInt64(1),
                Code = reader.GetString(2),
                Title = reader.GetString(3),
                Status = PostgresEnumMapper.Parse<ContentStatus>(reader.GetString(4)),
                Priority = PostgresEnumMapper.Parse<Priority>(reader.GetString(5)),
                Deadline = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                Platforms = reader.IsDBNull(7)
                    ? new List<string>()
                    : reader.GetString(7).Split(" • ", StringSplitOptions.RemoveEmptyEntries).ToList(),
                Tags = reader.IsDBNull(8)
                    ? new List<string>()
                    : reader.GetString(8).Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                AssigneeName = reader.IsDBNull(9) ? null : reader.GetString(9),
                HasPendingReview = reader.GetBoolean(10),
                Description = reader.GetString(11),
            });
        }

        return cards;
    }
}