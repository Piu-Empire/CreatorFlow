using CreatorFlow.Data;
using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Repositories.Interfaces;
using NpgsqlTypes;

namespace CreatorFlow.Repositories.Implementations;

/// <summary>
/// Cài đặt Npgsql cho IContentScriptRepository (cột contents.script, kiểu TEXT nên không giới hạn độ dài ở DB).
/// updated_at do trigger set_updated_at cập nhật.
/// </summary>
public class ContentScriptRepository : IContentScriptRepository
{
    private readonly IDbSession _session;

    public ContentScriptRepository(IDbSession session) => _session = session;

    public ContentScript? GetByContentId(long contentId)
    {
        using var cmd = _session.CreateCommand(
            "SELECT content_id, project_id, status::text, created_by, script FROM contents WHERE content_id = @id");
        cmd.Parameters.AddWithValue("id", contentId);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return null;

        return new ContentScript
        {
            ContentId = reader.GetInt64(0),
            ProjectId = reader.GetInt64(1),
            Status = PostgresEnumMapper.Parse<ContentStatus>(reader.GetString(2)),
            CreatedByUserId = reader.GetInt64(3),
            Script = reader.IsDBNull(4) ? null : reader.GetString(4),
        };
    }

    public bool TryUpdateScript(long contentId, string? expectedScript, string? newScript)
    {
        // IS NOT DISTINCT FROM so sánh được cả trường hợp script đang NULL.
        using var cmd = _session.CreateCommand(
            "UPDATE contents SET script = @script WHERE content_id = @id AND script IS NOT DISTINCT FROM @expected");
        cmd.Parameters.AddWithValue("id", contentId);
        cmd.Parameters.Add("script", NpgsqlDbType.Text).Value = (object?)newScript ?? DBNull.Value;
        cmd.Parameters.Add("expected", NpgsqlDbType.Text).Value = (object?)expectedScript ?? DBNull.Value;
        return cmd.ExecuteNonQuery() == 1;
    }
}
