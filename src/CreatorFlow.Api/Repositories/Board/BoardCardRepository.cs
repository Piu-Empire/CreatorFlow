using CreatorFlow.Api.Models.Board;
using CreatorFlow.Api.Repositories.Tasks;
using CreatorFlow.Data;
using Npgsql;
using NpgsqlTypes;

namespace CreatorFlow.Api.Repositories.Board;

/// <summary>
/// Cài đặt Npgsql cho IBoardCardRepository, viết theo database/01_schema.sql (cột snake_case, enum UPPER_SNAKE_CASE).
/// Một câu SELECT gộp contents + nền tảng + tag + cờ Review PENDING (cùng cách BoardRepository của WinForms trước đây);
/// Creator được giao do BoardService ghép thêm từ ITaskRepository.GetBoardAssigneesAsync.
/// </summary>
public sealed class BoardCardRepository(IDbConnectionFactory connectionFactory) : IBoardCardRepository
{
    private const string Sql = """
        SELECT
            c.content_id,
            c.project_id,
            c.title,
            COALESCE(c.description, ''),
            c.status::text,
            c.priority::text,
            c.deadline,
            c.planned_publish_at,
            ARRAY(SELECT p.name FROM content_platforms cp JOIN platforms p ON p.platform_id = cp.platform_id
                  WHERE cp.content_id = c.content_id ORDER BY p.name)::text[] AS platforms,
            ARRAY(SELECT t.name FROM content_tags ct JOIN tags t ON t.tag_id = ct.tag_id
                  WHERE ct.content_id = c.content_id ORDER BY t.name)::text[] AS tags,
            EXISTS (SELECT 1 FROM reviews r WHERE r.content_id = c.content_id AND r.status = 'PENDING') AS has_pending_review
        FROM contents c
        WHERE c.project_id = @project_id
          AND (@include_archived OR c.status <> 'ARCHIVED')
        ORDER BY c.content_id;
        """;

    public Task<IReadOnlyList<BoardCard>> GetCardsAsync(long projectId, CancellationToken cancellationToken = default) =>
        QueryCardsAsync(projectId, includeArchived: false, cancellationToken);

    public Task<IReadOnlyList<BoardCard>> GetProjectContentsAsync(long projectId, CancellationToken cancellationToken = default) =>
        QueryCardsAsync(projectId, includeArchived: true, cancellationToken);

    private async Task<IReadOnlyList<BoardCard>> QueryCardsAsync(long projectId, bool includeArchived, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(Sql, connection);
        command.Parameters.AddWithValue("project_id", NpgsqlDbType.Bigint, projectId);
        command.Parameters.AddWithValue("include_archived", NpgsqlDbType.Boolean, includeArchived);

        var rows = new List<BoardCard>();
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            long contentId = reader.GetInt64(0);
            rows.Add(new BoardCard(
                ContentId: contentId,
                ProjectId: reader.GetInt64(1),
                Code: $"CNT-{contentId:000}",
                Title: reader.GetString(2),
                Description: reader.GetString(3),
                Status: TaskRepository.ToPascal(reader.GetString(4)),
                Priority: TaskRepository.ToPascal(reader.GetString(5)),
                Deadline: reader.IsDBNull(6) ? null : DateOnly.FromDateTime(reader.GetDateTime(6).ToUniversalTime()),
                PlannedPublishAt: reader.IsDBNull(7) ? null : DateTime.SpecifyKind(reader.GetDateTime(7), DateTimeKind.Utc),
                Platforms: reader.GetFieldValue<string[]>(8),
                Tags: reader.GetFieldValue<string[]>(9),
                HasPendingReview: reader.GetBoolean(10)));
        }
        return rows;
    }
}
