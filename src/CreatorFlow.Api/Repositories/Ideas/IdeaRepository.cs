using CreatorFlow.Api.Models.Ideas;
using CreatorFlow.Data;
using Npgsql;
using NpgsqlTypes;

namespace CreatorFlow.Api.Repositories.Ideas;

/// <summary>Cài đặt Npgsql cho IIdeaRepository, cùng SQL với bản chạy trực tiếp DB của app (database/01_schema.sql + 07_idea_bank.sql).</summary>
public sealed class IdeaRepository(IDbConnectionFactory connectionFactory) : IIdeaRepository
{
    private const string SelectIdeas = """
        SELECT i.idea_id, i.project_id, i.title, COALESCE(i.description, ''), COALESCE(i.note, ''), i.status::text,
               i.created_by, u.display_name, i.created_at, i.updated_at,
               ARRAY(SELECT t.name FROM idea_tags it JOIN tags t ON t.tag_id = it.tag_id
                     WHERE it.idea_id = i.idea_id ORDER BY t.name)::text[] AS tags,
               (SELECT c.content_id FROM contents c WHERE c.source_idea_id = i.idea_id ORDER BY c.content_id LIMIT 1) AS converted_content_id
        FROM ideas i
        JOIN users u ON u.user_id = i.created_by
        """;

    public async Task<IReadOnlyList<Idea>> GetByProjectAsync(long projectId, IdeaFilter filter, CancellationToken cancellationToken = default)
    {
        string search = (filter.SearchText ?? string.Empty).Trim();
        string? tag = string.IsNullOrWhiteSpace(filter.Tag) ? null : filter.Tag.Trim();

        const string where = """

            WHERE i.project_id = @project_id
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
            ORDER BY i.updated_at DESC, i.idea_id DESC
            """;

        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(SelectIdeas + where, connection);
        command.Parameters.AddWithValue("project_id", NpgsqlDbType.Bigint, projectId);
        command.Parameters.Add("status", NpgsqlDbType.Text).Value = filter.Status.HasValue ? (object)ToDb(filter.Status.Value) : DBNull.Value;
        command.Parameters.Add("tag", NpgsqlDbType.Text).Value = (object?)tag ?? DBNull.Value;
        command.Parameters.Add("pattern", NpgsqlDbType.Text).Value = search.Length == 0 ? DBNull.Value : (object)("%" + EscapeLike(search) + "%");
        return await ReadAsync(command, cancellationToken);
    }

    public async Task<Idea?> GetByIdAsync(long ideaId, CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(SelectIdeas + " WHERE i.idea_id = @id", connection);
        command.Parameters.AddWithValue("id", NpgsqlDbType.Bigint, ideaId);
        return (await ReadAsync(command, cancellationToken)).FirstOrDefault();
    }

    public async Task<IReadOnlyList<string>> GetProjectTagsAsync(long projectId, CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand("SELECT name FROM tags WHERE project_id = @project_id ORDER BY name", connection);
        command.Parameters.AddWithValue("project_id", NpgsqlDbType.Bigint, projectId);

        var names = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) names.Add(reader.GetString(0));
        return names;
    }

    public async Task<long> CreateAsync(long projectId, IdeaDraft draft, long createdByUserId, CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using NpgsqlTransaction tx = await connection.BeginTransactionAsync(cancellationToken);

        long ideaId;
        await using (var insert = new NpgsqlCommand("""
            INSERT INTO ideas (project_id, title, description, note, status, created_by)
            VALUES (@project_id, @title, @description, @note, @status::idea_status, @created_by)
            RETURNING idea_id
            """, connection, tx))
        {
            insert.Parameters.AddWithValue("project_id", NpgsqlDbType.Bigint, projectId);
            AddEditable(insert, draft);
            insert.Parameters.AddWithValue("created_by", NpgsqlDbType.Bigint, createdByUserId);
            ideaId = (long)(await insert.ExecuteScalarAsync(cancellationToken))!;
        }

        await SyncTagsAsync(connection, tx, ideaId, draft.Tags, cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return ideaId;
    }

    public async Task UpdateAsync(long ideaId, IdeaDraft draft, CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using NpgsqlTransaction tx = await connection.BeginTransactionAsync(cancellationToken);

        await using (var update = new NpgsqlCommand("""
            UPDATE ideas SET title = @title, description = @description, note = @note, status = @status::idea_status
            WHERE idea_id = @id
            """, connection, tx))
        {
            AddEditable(update, draft);
            update.Parameters.AddWithValue("id", NpgsqlDbType.Bigint, ideaId);
            await update.ExecuteNonQueryAsync(cancellationToken);
        }

        await SyncTagsAsync(connection, tx, ideaId, draft.Tags, cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    public async Task<long?> ConvertToContentAsync(long ideaId, string contentType, long convertedByUserId, string historyNote, CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using NpgsqlTransaction tx = await connection.BeginTransactionAsync(cancellationToken);

        long projectId;
        string title;
        string? description;

        // Đặt Converted trước và chỉ khi Idea còn Draft/Backlog: hai người cùng chuyển một Idea thì chỉ một người thành công.
        await using (var mark = new NpgsqlCommand("""
            UPDATE ideas SET status = 'CONVERTED'::idea_status
            WHERE idea_id = @id AND status IN ('DRAFT'::idea_status, 'BACKLOG'::idea_status)
            RETURNING project_id, title, description
            """, connection, tx))
        {
            mark.Parameters.AddWithValue("id", NpgsqlDbType.Bigint, ideaId);
            await using var reader = await mark.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return null;
            projectId = reader.GetInt64(0);
            title = reader.GetString(1);
            description = reader.IsDBNull(2) ? null : reader.GetString(2);
        }

        long contentId;
        await using (var insert = new NpgsqlCommand("""
            INSERT INTO contents (project_id, source_idea_id, title, description, content_type, priority, status, created_by)
            VALUES (@project_id, @idea_id, @title, @description, @content_type, 'MEDIUM'::content_priority, 'IDEA'::content_status, @created_by)
            RETURNING content_id
            """, connection, tx))
        {
            insert.Parameters.AddWithValue("project_id", NpgsqlDbType.Bigint, projectId);
            insert.Parameters.AddWithValue("idea_id", NpgsqlDbType.Bigint, ideaId);
            insert.Parameters.AddWithValue("title", NpgsqlDbType.Varchar, title);
            insert.Parameters.Add("description", NpgsqlDbType.Text).Value = (object?)description ?? DBNull.Value;
            insert.Parameters.AddWithValue("content_type", NpgsqlDbType.Varchar, contentType);
            insert.Parameters.AddWithValue("created_by", NpgsqlDbType.Bigint, convertedByUserId);
            contentId = (long)(await insert.ExecuteScalarAsync(cancellationToken))!;
        }

        // Tag của Idea và Content dùng chung bảng tags của Project nên chỉ cần nối thêm content_tags.
        await using (var tags = new NpgsqlCommand("""
            INSERT INTO content_tags (content_id, tag_id)
            SELECT @content_id, it.tag_id FROM idea_tags it WHERE it.idea_id = @idea_id
            ON CONFLICT (content_id, tag_id) DO NOTHING
            """, connection, tx))
        {
            tags.Parameters.AddWithValue("content_id", NpgsqlDbType.Bigint, contentId);
            tags.Parameters.AddWithValue("idea_id", NpgsqlDbType.Bigint, ideaId);
            await tags.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var history = new NpgsqlCommand("""
            INSERT INTO content_status_history (content_id, from_status, to_status, changed_by, note)
            VALUES (@content_id, 'IDEA'::content_status, 'IDEA'::content_status, @changed_by, @note)
            """, connection, tx))
        {
            history.Parameters.AddWithValue("content_id", NpgsqlDbType.Bigint, contentId);
            history.Parameters.AddWithValue("changed_by", NpgsqlDbType.Bigint, convertedByUserId);
            history.Parameters.AddWithValue("note", NpgsqlDbType.Text, historyNote);
            await history.ExecuteNonQueryAsync(cancellationToken);
        }

        await tx.CommitAsync(cancellationToken);
        return contentId;
    }

    public async Task DeleteAsync(long ideaId, CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand("DELETE FROM ideas WHERE idea_id = @id", connection);
        command.Parameters.AddWithValue("id", NpgsqlDbType.Bigint, ideaId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>Đảm bảo mọi tag có trong bảng tags của Project chứa Idea, rồi thêm/bớt idea_tags cho khớp danh sách.</summary>
    private static async Task SyncTagsAsync(NpgsqlConnection connection, NpgsqlTransaction tx, long ideaId,
        IReadOnlyList<string> tagNames, CancellationToken cancellationToken)
    {
        string[] names = tagNames.ToArray();

        await using (var ensure = new NpgsqlCommand("""
            INSERT INTO tags (project_id, name)
            SELECT i.project_id, n FROM ideas i, unnest(@names) AS n
            WHERE i.idea_id = @idea_id
            ON CONFLICT (project_id, name) DO NOTHING
            """, connection, tx))
        {
            ensure.Parameters.AddWithValue("idea_id", NpgsqlDbType.Bigint, ideaId);
            ensure.Parameters.Add("names", NpgsqlDbType.Array | NpgsqlDbType.Varchar).Value = names;
            await ensure.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var add = new NpgsqlCommand("""
            INSERT INTO idea_tags (idea_id, tag_id)
            SELECT i.idea_id, t.tag_id FROM ideas i
            JOIN tags t ON t.project_id = i.project_id AND t.name = ANY(@names)
            WHERE i.idea_id = @idea_id
            ON CONFLICT (idea_id, tag_id) DO NOTHING
            """, connection, tx))
        {
            add.Parameters.AddWithValue("idea_id", NpgsqlDbType.Bigint, ideaId);
            add.Parameters.Add("names", NpgsqlDbType.Array | NpgsqlDbType.Varchar).Value = names;
            await add.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var remove = new NpgsqlCommand("""
            DELETE FROM idea_tags it USING tags t
            WHERE it.idea_id = @idea_id AND t.tag_id = it.tag_id AND t.name <> ALL(@names)
            """, connection, tx))
        {
            remove.Parameters.AddWithValue("idea_id", NpgsqlDbType.Bigint, ideaId);
            remove.Parameters.Add("names", NpgsqlDbType.Array | NpgsqlDbType.Varchar).Value = names;
            await remove.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static void AddEditable(NpgsqlCommand command, IdeaDraft draft)
    {
        command.Parameters.AddWithValue("title", NpgsqlDbType.Varchar, draft.Title);
        command.Parameters.Add("description", NpgsqlDbType.Text).Value = ToDbText(draft.Description);
        command.Parameters.Add("note", NpgsqlDbType.Text).Value = ToDbText(draft.Note);
        command.Parameters.Add("status", NpgsqlDbType.Text).Value = ToDb(draft.Status);
    }

    private static async Task<IReadOnlyList<Idea>> ReadAsync(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        var ideas = new List<Idea>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            ideas.Add(new Idea(
                IdeaId: reader.GetInt64(0),
                ProjectId: reader.GetInt64(1),
                Title: reader.GetString(2),
                Description: reader.GetString(3),
                Note: reader.GetString(4),
                Status: Enum.Parse<IdeaStatus>(reader.GetString(5), ignoreCase: true),
                Tags: reader.GetFieldValue<string[]>(10),
                CreatedByUserId: reader.GetInt64(6),
                CreatedByName: reader.GetString(7),
                CreatedAt: reader.GetDateTime(8),
                UpdatedAt: reader.GetDateTime(9),
                ConvertedContentId: reader.IsDBNull(11) ? null : reader.GetInt64(11)));
        }
        return ideas;
    }

    /// <summary>IdeaStatus.Backlog → "BACKLOG" (các giá trị idea_status đều là một từ).</summary>
    private static string ToDb(IdeaStatus status) => status.ToString().ToUpperInvariant();

    private static string EscapeLike(string text) => text.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    private static object ToDbText(string? text) => string.IsNullOrEmpty(text) ? DBNull.Value : text;
}
