using CreatorFlow.Data;
using CreatorFlow.Models;
using CreatorFlow.Repositories.Interfaces;

namespace CreatorFlow.Repositories.Implementations;

/// <summary>
/// Cài đặt Npgsql cho IBoardRepository. Đây là view-model cho Board (mục 4 UX spec) nên
/// 1 câu SELECT gộp thẳng Contents + ContentPlatforms/Platforms + ContentTags/Tags +
/// ContentAssignments (lấy người được giao gần nhất) + kiểm tra có Review PENDING hay không,
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
    c.id,
    c.projectid,
    'CNT-' || LPAD(c.id::text, 3, '0') AS code,
    c.title,
    c.status,
    c.priority,
    c.deadline,
    (SELECT STRING_AGG(p.name, ' • ' ORDER BY p.name)
       FROM contentplatforms cp JOIN platforms p ON p.id = cp.platformid
       WHERE cp.contentid = c.id) AS platforms,
    (SELECT STRING_AGG(t.name, ',' ORDER BY t.name)
       FROM contenttags ct JOIN tags t ON t.id = ct.tagid
       WHERE ct.contentid = c.id) AS tags,
    (SELECT u.displayname
       FROM contentassignments ca JOIN users u ON u.id = ca.assigneeuserid
       WHERE ca.contentid = c.id
       ORDER BY ca.createdat DESC LIMIT 1) AS assigneename,
    EXISTS (SELECT 1 FROM reviews r WHERE r.contentid = c.id AND r.status = 'Pending') AS haspendingreview
FROM contents c
WHERE c.projectid = @projectId
ORDER BY c.id";

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
                Status = Enum.Parse<ContentStatus>(reader.GetString(4)),
                Priority = Enum.Parse<Priority>(reader.GetString(5)),
                Deadline = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                Platforms = reader.IsDBNull(7)
                    ? new List<string>()
                    : reader.GetString(7).Split(" • ", StringSplitOptions.RemoveEmptyEntries).ToList(),
                Tags = reader.IsDBNull(8)
                    ? new List<string>()
                    : reader.GetString(8).Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                AssigneeName = reader.IsDBNull(9) ? null : reader.GetString(9),
                HasPendingReview = reader.GetBoolean(10),
            });
        }

        return cards;
    }
}