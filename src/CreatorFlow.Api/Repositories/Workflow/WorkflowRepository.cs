using CreatorFlow.Api.Models.Tasks;
using CreatorFlow.Api.Models.Workflow;
using CreatorFlow.Data;
using Npgsql;
using NpgsqlTypes;

namespace CreatorFlow.Api.Repositories.Workflow;

/// <summary>
/// Cài đặt Npgsql cho IWorkflowRepository, viết theo database/01_schema.sql
/// (cột snake_case, enum UPPER_SNAKE_CASE). Mọi thao tác nhiều bước chạy trong
/// 1 transaction; đổi trạng thái có guard `status = @from` để không ghi đè khi bị race.
/// </summary>
public sealed class WorkflowRepository(IDbConnectionFactory connectionFactory) : IWorkflowRepository
{
    private const string ContentSelect = """
        SELECT content_id, project_id, status::text, created_by, updated_at
        FROM contents
        WHERE content_id = @content_id;
        """;

    public async Task<WorkflowContent?> GetContentAsync(long contentId, CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(ContentSelect, connection);
        command.Parameters.AddWithValue("content_id", NpgsqlDbType.Bigint, contentId);

        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new WorkflowContent(
            reader.GetInt64(0),
            reader.GetInt64(1),
            ParseEnum<WorkflowContentStatus>(ToPascal(reader.GetString(2))),
            reader.GetInt64(3),
            reader.GetDateTime(4));
    }

    public async Task<ProjectRole?> GetRoleAsync(long projectId, long userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT role::text
            FROM project_members
            WHERE project_id = @project_id AND user_id = @user_id AND is_active;
            """;

        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("project_id", NpgsqlDbType.Bigint, projectId);
        command.Parameters.AddWithValue("user_id", NpgsqlDbType.Bigint, userId);

        object? value = await command.ExecuteScalarAsync(cancellationToken);
        return value is string text ? ParseEnum<ProjectRole>(text) : null;
    }

    public async Task<WorkflowContent?> ChangeStatusAsync(
        long contentId, WorkflowContentStatus from, WorkflowContentStatus to,
        long changedBy, string? note, CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var update = new NpgsqlCommand("""
            UPDATE contents
            SET status = @to::content_status, updated_at = CURRENT_TIMESTAMP
            WHERE content_id = @content_id AND status = @from::content_status;
            """, connection, transaction))
        {
            update.Parameters.AddWithValue("to", NpgsqlDbType.Text, ToDbEnum(to));
            update.Parameters.AddWithValue("from", NpgsqlDbType.Text, ToDbEnum(from));
            update.Parameters.AddWithValue("content_id", NpgsqlDbType.Bigint, contentId);
            int affected = await update.ExecuteNonQueryAsync(cancellationToken);
            if (affected == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null; // Content không còn ở trạng thái gốc (bị đổi đồng thời).
            }
        }

        await InsertHistoryAsync(connection, transaction, contentId, from, to, changedBy, note, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetContentAsync(contentId, cancellationToken);
    }

    public async Task<WorkflowContent?> SubmitReviewAsync(
        long contentId, long submittedBy, string? note, CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        // Review không ghi đè lần duyệt cũ: mỗi lần gửi là 1 dòng mới, review_no tăng dần.
        await using (var review = new NpgsqlCommand("""
            INSERT INTO reviews (content_id, review_no, submitted_by)
            VALUES (@content_id,
                    COALESCE((SELECT MAX(review_no) FROM reviews WHERE content_id = @content_id), 0) + 1,
                    @submitted_by);
            """, connection, transaction))
        {
            review.Parameters.AddWithValue("content_id", NpgsqlDbType.Bigint, contentId);
            review.Parameters.AddWithValue("submitted_by", NpgsqlDbType.Bigint, submittedBy);
            await review.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var update = new NpgsqlCommand("""
            UPDATE contents
            SET status = 'REVIEW'::content_status, updated_at = CURRENT_TIMESTAMP
            WHERE content_id = @content_id AND status = 'EDITING'::content_status;
            """, connection, transaction))
        {
            update.Parameters.AddWithValue("content_id", NpgsqlDbType.Bigint, contentId);
            int affected = await update.ExecuteNonQueryAsync(cancellationToken);
            if (affected == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }
        }

        await InsertHistoryAsync(connection, transaction, contentId,
            WorkflowContentStatus.Editing, WorkflowContentStatus.Review, submittedBy, note, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetContentAsync(contentId, cancellationToken);
    }

    public async Task<WorkflowContent?> DecideReviewAsync(
        long contentId, long reviewerId, bool approve, string? feedback,
        CancellationToken cancellationToken = default)
    {
        var newStatus = approve ? WorkflowContentStatus.Ready : WorkflowContentStatus.Editing;

        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        // Quyết định Review PENDING mới nhất; không có dòng nào → rollback (409 ở service).
        await using (var decide = new NpgsqlCommand("""
            UPDATE reviews
            SET status = @status::review_status,
                reviewer_id = @reviewer_id,
                feedback = @feedback,
                reviewed_at = CURRENT_TIMESTAMP
            WHERE review_id = (
                SELECT review_id
                FROM reviews
                WHERE content_id = @content_id AND status = 'PENDING'
                ORDER BY review_no DESC
                LIMIT 1
            );
            """, connection, transaction))
        {
            decide.Parameters.AddWithValue("status", NpgsqlDbType.Text, approve ? "APPROVED" : "REJECTED");
            decide.Parameters.AddWithValue("reviewer_id", NpgsqlDbType.Bigint, reviewerId);
            decide.Parameters.AddWithValue("feedback", NpgsqlDbType.Text, (object?)feedback ?? DBNull.Value);
            decide.Parameters.AddWithValue("content_id", NpgsqlDbType.Bigint, contentId);
            int affected = await decide.ExecuteNonQueryAsync(cancellationToken);
            if (affected == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }
        }

        await using (var update = new NpgsqlCommand("""
            UPDATE contents
            SET status = @to::content_status, updated_at = CURRENT_TIMESTAMP
            WHERE content_id = @content_id AND status = 'REVIEW'::content_status;
            """, connection, transaction))
        {
            update.Parameters.AddWithValue("to", NpgsqlDbType.Text, ToDbEnum(newStatus));
            update.Parameters.AddWithValue("content_id", NpgsqlDbType.Bigint, contentId);
            int affected = await update.ExecuteNonQueryAsync(cancellationToken);
            if (affected == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }
        }

        await InsertHistoryAsync(connection, transaction, contentId,
            WorkflowContentStatus.Review, newStatus, reviewerId, feedback, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetContentAsync(contentId, cancellationToken);
    }

    public async Task<IReadOnlyList<StatusHistoryEntry>> GetStatusHistoryAsync(long contentId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT h.history_id, h.from_status::text, h.to_status::text, h.changed_by,
                   u.display_name, h.note, h.changed_at
            FROM content_status_history h
            LEFT JOIN users u ON u.user_id = h.changed_by
            WHERE h.content_id = @content_id
            ORDER BY h.changed_at DESC, h.history_id DESC;
            """;

        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("content_id", NpgsqlDbType.Bigint, contentId);

        var rows = new List<StatusHistoryEntry>();
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new StatusHistoryEntry(
                reader.GetInt64(0),
                reader.IsDBNull(1) ? null : ToPascal(reader.GetString(1)),
                ToPascal(reader.GetString(2)),
                reader.IsDBNull(3) ? null : reader.GetInt64(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.GetDateTime(6)));
        }
        return rows;
    }

    // ------------------------------------------------------------------ helpers

    private static async Task InsertHistoryAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction,
        long contentId, WorkflowContentStatus? from, WorkflowContentStatus to,
        long changedBy, string? note, CancellationToken cancellationToken)
    {
        await using var history = new NpgsqlCommand("""
            INSERT INTO content_status_history (content_id, from_status, to_status, changed_by, note)
            VALUES (@content_id, @from::content_status, @to::content_status, @changed_by, @note);
            """, connection, transaction);
        history.Parameters.AddWithValue("content_id", NpgsqlDbType.Bigint, contentId);
        history.Parameters.AddWithValue("from", NpgsqlDbType.Text, from is null ? DBNull.Value : ToDbEnum(from.Value));
        history.Parameters.AddWithValue("to", NpgsqlDbType.Text, ToDbEnum(to));
        history.Parameters.AddWithValue("changed_by", NpgsqlDbType.Bigint, changedBy);
        history.Parameters.AddWithValue("note", NpgsqlDbType.Text, (object?)note ?? DBNull.Value);
        await history.ExecuteNonQueryAsync(cancellationToken);
    }

    private static TEnum ParseEnum<TEnum>(string databaseValue) where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(ToPascal(databaseValue), ignoreCase: true, out TEnum value) ? value : default;

    /// <summary>"IN_PROGRESS" → "InProgress".</summary>
    private static string ToPascal(string databaseValue) =>
        string.Concat(databaseValue.Split('_', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant()));

    /// <summary>WorkflowContentStatus.InProgress → "IN_PROGRESS".</summary>
    private static string ToDbEnum<TEnum>(TEnum value) where TEnum : struct, Enum
    {
        string name = value.ToString();
        var builder = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i])) builder.Append('_');
            builder.Append(char.ToUpperInvariant(name[i]));
        }
        return builder.ToString();
    }
}
