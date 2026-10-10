using CreatorFlow.Data;
using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Repositories.Interfaces;
using Npgsql;
using NpgsqlTypes;

namespace CreatorFlow.Repositories.Implementations;

/// <summary>
/// Cài đặt Npgsql cho IIdeaRepository: bảng ideas + idea_tags + tags.
/// - Tìm kiếm / lọc chạy ngay trong SQL (theo Project, trạng thái, tag, từ khóa) để vẫn nhanh khi có nhiều Idea.
/// - Tag nằm ở bảng tags của đúng Project của Idea; đồng bộ theo kiểu "thêm cái thiếu, bỏ cái không còn".
/// Ghi dữ liệu chạy trong transaction hiện hành của session (IdeaService tự Begin/Commit).
/// </summary>
public class IdeaRepository : IIdeaRepository
{
    private const string SelectIdeas = @"
SELECT i.idea_id, i.project_id, i.title, COALESCE(i.description, ''), COALESCE(i.note, ''), i.status::text,
       i.created_by, u.display_name, i.created_at, i.updated_at,
       ARRAY(SELECT t.name FROM idea_tags it JOIN tags t ON t.tag_id = it.tag_id
             WHERE it.idea_id = i.idea_id ORDER BY t.name)::text[] AS tags,
       (SELECT c.content_id FROM contents c WHERE c.source_idea_id = i.idea_id ORDER BY c.content_id LIMIT 1) AS converted_content_id
FROM ideas i
JOIN users u ON u.user_id = i.created_by";

    private readonly IDbSession _session;

    public IdeaRepository(IDbSession session) => _session = session;

    public List<Idea> GetByProject(long projectId, IdeaFilter filter)
    {
        string search = (filter.SearchText ?? string.Empty).Trim();
        string? tag = string.IsNullOrWhiteSpace(filter.Tag) ? null : filter.Tag.Trim();

        using var cmd = _session.CreateCommand(SelectIdeas + @"
WHERE i.project_id = @projectId
  AND (@status::idea_status IS NULL OR i.status = @status::idea_status)
  AND (@tag::text IS NULL OR EXISTS (
        SELECT 1 FROM idea_tags it JOIN tags t ON t.tag_id = it.tag_id
        WHERE it.idea_id = i.idea_id AND LOWER(t.name) = LOWER(@tag::text)))
  AND (@pattern::text IS NULL
        OR i.title ILIKE @pattern::text ESCAPE '\'
        OR COALESCE(i.description, '') ILIKE @pattern::text ESCAPE '\'
        OR COALESCE(i.note, '') ILIKE @pattern::text ESCAPE '\'
        OR EXISTS (SELECT 1 FROM idea_tags it JOIN tags t ON t.tag_id = it.tag_id
                   WHERE it.idea_id = i.idea_id AND t.name ILIKE @pattern::text ESCAPE '\'))
ORDER BY i.updated_at DESC, i.idea_id DESC");
        cmd.Parameters.AddWithValue("projectId", projectId);
        cmd.Parameters.Add("status", NpgsqlDbType.Text).Value =
            filter.Status.HasValue ? (object)PostgresEnumMapper.ToDatabaseValue(filter.Status.Value) : DBNull.Value;
        cmd.Parameters.Add("tag", NpgsqlDbType.Text).Value = (object?)tag ?? DBNull.Value;
        cmd.Parameters.Add("pattern", NpgsqlDbType.Text).Value =
            search.Length == 0 ? DBNull.Value : (object)("%" + EscapeLike(search) + "%");

        return ReadIdeas(cmd);
    }

    public Idea? GetById(long ideaId)
    {
        using var cmd = _session.CreateCommand(SelectIdeas + " WHERE i.idea_id = @id");
        cmd.Parameters.AddWithValue("id", ideaId);
        return ReadIdeas(cmd).FirstOrDefault();
    }

    public List<string> GetProjectTags(long projectId)
    {
        using var cmd = _session.CreateCommand("SELECT name FROM tags WHERE project_id = @projectId ORDER BY name");
        cmd.Parameters.AddWithValue("projectId", projectId);

        var names = new List<string>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            names.Add(reader.GetString(0));
        return names;
    }

    public long Create(long projectId, IdeaDraft draft, long createdByUserId)
    {
        long ideaId;
        using (var cmd = _session.CreateCommand(@"
INSERT INTO ideas (project_id, title, description, note, status, created_by)
VALUES (@projectId, @title, @description, @note, @status::idea_status, @createdBy)
RETURNING idea_id"))
        {
            cmd.Parameters.AddWithValue("projectId", projectId);
            AddEditableParameters(cmd, draft);
            cmd.Parameters.AddWithValue("createdBy", createdByUserId);
            ideaId = (long)cmd.ExecuteScalar()!;
        }

        SyncTags(ideaId, draft.Tags);
        return ideaId;
    }

    public void Update(long ideaId, IdeaDraft draft)
    {
        using (var cmd = _session.CreateCommand(@"
UPDATE ideas
SET title = @title, description = @description, note = @note, status = @status::idea_status
WHERE idea_id = @id"))
        {
            AddEditableParameters(cmd, draft);
            cmd.Parameters.AddWithValue("id", ideaId);
            cmd.ExecuteNonQuery();
        }

        SyncTags(ideaId, draft.Tags);
    }

    public long? ConvertToContent(long ideaId, string contentType, long convertedByUserId)
    {
        long projectId;
        string title;
        string? description;

        // Đặt Converted trước và chỉ khi Idea còn Draft/Backlog: hai người cùng chuyển một Idea thì chỉ một người thành công.
        using (var mark = _session.CreateCommand(@"
UPDATE ideas SET status = @converted::idea_status
WHERE idea_id = @id AND status IN (@draft::idea_status, @backlog::idea_status)
RETURNING project_id, title, description"))
        {
            mark.Parameters.AddWithValue("id", ideaId);
            mark.Parameters.AddWithValue("converted", PostgresEnumMapper.ToDatabaseValue(IdeaStatus.Converted));
            mark.Parameters.AddWithValue("draft", PostgresEnumMapper.ToDatabaseValue(IdeaStatus.Draft));
            mark.Parameters.AddWithValue("backlog", PostgresEnumMapper.ToDatabaseValue(IdeaStatus.Backlog));

            using var reader = mark.ExecuteReader();
            if (!reader.Read())
                return null;

            projectId = reader.GetInt64(0);
            title = reader.GetString(1);
            description = reader.IsDBNull(2) ? null : reader.GetString(2);
        }

        long contentId;
        using (var insert = _session.CreateCommand(@"
INSERT INTO contents (project_id, source_idea_id, title, description, content_type, priority, status, created_by)
VALUES (@projectId, @ideaId, @title, @description, @contentType, @priority::content_priority, @status::content_status, @createdBy)
RETURNING content_id"))
        {
            insert.Parameters.AddWithValue("projectId", projectId);
            insert.Parameters.AddWithValue("ideaId", ideaId);
            insert.Parameters.AddWithValue("title", title);
            insert.Parameters.Add("description", NpgsqlDbType.Text).Value = ToDbText(description);
            insert.Parameters.Add("contentType", NpgsqlDbType.Varchar).Value = ToDbText(contentType);
            insert.Parameters.AddWithValue("priority", PostgresEnumMapper.ToDatabaseValue(Priority.Medium));
            insert.Parameters.AddWithValue("status", PostgresEnumMapper.ToDatabaseValue(ContentStatus.Idea));
            insert.Parameters.AddWithValue("createdBy", convertedByUserId);
            contentId = (long)insert.ExecuteScalar()!;
        }

        // Tag của Idea và Content dùng chung bảng tags của Project nên chỉ cần nối thêm content_tags.
        using (var tags = _session.CreateCommand(@"
INSERT INTO content_tags (content_id, tag_id)
SELECT @contentId, it.tag_id FROM idea_tags it WHERE it.idea_id = @ideaId
ON CONFLICT (content_id, tag_id) DO NOTHING"))
        {
            tags.Parameters.AddWithValue("contentId", contentId);
            tags.Parameters.AddWithValue("ideaId", ideaId);
            tags.ExecuteNonQuery();
        }

        return contentId;
    }

    public void Delete(long ideaId)
    {
        using var cmd = _session.CreateCommand("DELETE FROM ideas WHERE idea_id = @id");
        cmd.Parameters.AddWithValue("id", ideaId);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Đảm bảo mọi tag đã có trong bảng tags của Project chứa Idea, rồi thêm/bớt dòng idea_tags cho khớp danh sách.
    /// Project lấy từ chính Idea nên không thể gắn nhầm tag của Project khác.
    /// </summary>
    private void SyncTags(long ideaId, List<string> tagNames)
    {
        string[] names = tagNames.ToArray();

        using (var ensure = _session.CreateCommand(@"
INSERT INTO tags (project_id, name)
SELECT i.project_id, n
FROM ideas i, unnest(@names) AS n
WHERE i.idea_id = @ideaId
ON CONFLICT (project_id, name) DO NOTHING"))
        {
            ensure.Parameters.AddWithValue("ideaId", ideaId);
            ensure.Parameters.Add("names", NpgsqlDbType.Array | NpgsqlDbType.Varchar).Value = names;
            ensure.ExecuteNonQuery();
        }

        using (var add = _session.CreateCommand(@"
INSERT INTO idea_tags (idea_id, tag_id)
SELECT i.idea_id, t.tag_id
FROM ideas i
JOIN tags t ON t.project_id = i.project_id AND t.name = ANY(@names)
WHERE i.idea_id = @ideaId
ON CONFLICT (idea_id, tag_id) DO NOTHING"))
        {
            add.Parameters.AddWithValue("ideaId", ideaId);
            add.Parameters.Add("names", NpgsqlDbType.Array | NpgsqlDbType.Varchar).Value = names;
            add.ExecuteNonQuery();
        }

        using (var remove = _session.CreateCommand(@"
DELETE FROM idea_tags it
USING tags t
WHERE it.idea_id = @ideaId AND t.tag_id = it.tag_id AND t.name <> ALL(@names)"))
        {
            remove.Parameters.AddWithValue("ideaId", ideaId);
            remove.Parameters.Add("names", NpgsqlDbType.Array | NpgsqlDbType.Varchar).Value = names;
            remove.ExecuteNonQuery();
        }
    }

    /// <summary>Các tham số dùng chung cho INSERT và UPDATE (cùng tên với câu SQL tương ứng). Chuỗi rỗng lưu thành NULL.</summary>
    private static void AddEditableParameters(NpgsqlCommand cmd, IdeaDraft draft)
    {
        cmd.Parameters.AddWithValue("title", draft.Title);
        cmd.Parameters.Add("description", NpgsqlDbType.Text).Value = ToDbText(draft.Description);
        cmd.Parameters.Add("note", NpgsqlDbType.Text).Value = ToDbText(draft.Note);
        cmd.Parameters.AddWithValue("status", PostgresEnumMapper.ToDatabaseValue(draft.Status));
    }

    private static List<Idea> ReadIdeas(NpgsqlCommand cmd)
    {
        var ideas = new List<Idea>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            ideas.Add(new Idea
            {
                IdeaId = reader.GetInt64(0),
                ProjectId = reader.GetInt64(1),
                Title = reader.GetString(2),
                Description = reader.GetString(3),
                Note = reader.GetString(4),
                Status = PostgresEnumMapper.Parse<IdeaStatus>(reader.GetString(5)),
                CreatedByUserId = reader.GetInt64(6),
                CreatedByName = reader.GetString(7),
                CreatedAt = reader.GetDateTime(8).ToLocalTime(),
                UpdatedAt = reader.GetDateTime(9).ToLocalTime(),
                Tags = reader.GetFieldValue<string[]>(10).ToList(),
                ConvertedContentId = reader.IsDBNull(11) ? null : reader.GetInt64(11),
            });
        }
        return ideas;
    }

    /// <summary>Thoát ký tự đặc biệt của LIKE (\ % _) để từ khóa được tìm đúng nghĩa đen.</summary>
    private static string EscapeLike(string text) =>
        text.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    private static object ToDbText(string? text) =>
        string.IsNullOrEmpty(text) ? DBNull.Value : text;
}