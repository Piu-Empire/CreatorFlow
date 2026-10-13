using CreatorFlow.Data;
using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Repositories.Interfaces;

namespace CreatorFlow.Repositories.Implementations;

/// <summary>Cài đặt Npgsql cho IPlatformRepository (bảng platforms, content_platforms).</summary>
public class PlatformRepository : IPlatformRepository
{
    private readonly IDbSession _session;

    public PlatformRepository(IDbSession session) => _session = session;

    public List<Platform> GetActive()
    {
        using var cmd = _session.CreateCommand(
            "SELECT platform_id, code, name, is_active FROM platforms WHERE is_active ORDER BY platform_id");

        var list = new List<Platform>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Platform
            {
                PlatformId = reader.GetInt64(0),
                Code = reader.GetString(1),
                Name = reader.GetString(2),
                IsActive = reader.GetBoolean(3),
            });
        }
        return list;
    }

    public List<ContentPlatform> GetByContentId(long contentId)
    {
        using var cmd = _session.CreateCommand(@"
SELECT cp.content_platform_id, cp.content_id, cp.platform_id, p.name, cp.publication_status::text,
       cp.post_url, cp.planned_publish_at, cp.published_at
FROM content_platforms cp
JOIN platforms p ON p.platform_id = cp.platform_id
WHERE cp.content_id = @contentId
ORDER BY cp.platform_id");
        cmd.Parameters.AddWithValue("contentId", contentId);

        var list = new List<ContentPlatform>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new ContentPlatform
            {
                ContentPlatformId = reader.GetInt64(0),
                ContentId = reader.GetInt64(1),
                PlatformId = reader.GetInt64(2),
                PlatformName = reader.GetString(3),
                PublicationStatus = PostgresEnumMapper.Parse<PublicationStatus>(reader.GetString(4)),
                PostUrl = reader.IsDBNull(5) ? null : reader.GetString(5),
                PlannedPublishAt = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                PublishedAt = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
            });
        }
        return list;
    }
}