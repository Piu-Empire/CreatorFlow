using CreatorFlow.Api.Models.Admin;
using CreatorFlow.Data;

using Npgsql;
using NpgsqlTypes;

namespace CreatorFlow.Api.Repositories.Admin;

/// <summary>Truy vấn SQL parameterized cho báo cáo vi phạm.</summary>
public sealed class AdminReportRepository : IAdminReportRepository
{
    private readonly IDbConnectionFactory connectionFactory;

    public AdminReportRepository(IDbConnectionFactory connectionFactory) => this.connectionFactory = connectionFactory;

    /// <summary>Tìm báo cáo theo email hai bên và trạng thái.</summary>
    public async Task<IReadOnlyList<AdminReportRecord>> SearchAsync(string? search, string? status, int limit, int offset,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT r.report_id, r.reporter_user_id, reporter.email AS reporter_email,
                   r.reported_user_id, reported.email AS reported_email,
                   r.reason, r.details, r.status::text AS status,
                   r.handled_by, r.handled_note, r.created_at, r.handled_at
            FROM reports r
            JOIN users reporter ON reporter.user_id = r.reporter_user_id
            JOIN users reported ON reported.user_id = r.reported_user_id
            WHERE (@status IS NULL OR r.status = @status::report_status)
              AND (@like IS NULL OR reporter.email ILIKE @like ESCAPE '\' OR reported.email ILIKE @like ESCAPE '\')
            ORDER BY r.report_id DESC
            LIMIT @limit OFFSET @offset;
            """;

        await using NpgsqlConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        AddFilterParameters(command, search, status);
        command.Parameters.AddWithValue("limit", NpgsqlDbType.Integer, limit);
        command.Parameters.AddWithValue("offset", NpgsqlDbType.Integer, offset);
        await using NpgsqlDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken);

        var items = new List<AdminReportRecord>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(MapRecord(reader));
        }

        return items;
    }

    /// <summary>Đếm số báo cáo khớp cùng điều kiện lọc với SearchAsync.</summary>
    public async Task<int> CountAsync(string? search, string? status,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM reports r
            JOIN users reporter ON reporter.user_id = r.reporter_user_id
            JOIN users reported ON reported.user_id = r.reported_user_id
            WHERE (@status IS NULL OR r.status = @status::report_status)
              AND (@like IS NULL OR reporter.email ILIKE @like ESCAPE '\' OR reported.email ILIKE @like ESCAPE '\');
            """;

        await using NpgsqlConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        AddFilterParameters(command, search, status);
        object? value = await command.ExecuteScalarAsync(cancellationToken);
        return value is long count ? checked((int)count) : 0;
    }

    /// <summary>Tạo báo cáo: reported phải tồn tại, trùng cặp đang mở thì báo duplicate.</summary>
    public async Task<AdminReportCreateResult?> CreateAsync(long reporterUserId, long reportedUserId,
        string reason, string? details, CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using NpgsqlTransaction transaction =
            await connection.BeginTransactionAsync(cancellationToken);

        await using (var exists = new NpgsqlCommand("SELECT 1 FROM users WHERE user_id = @id;",
            connection, transaction))
        {
            exists.Parameters.AddWithValue("id", NpgsqlDbType.Bigint, reportedUserId);
            if (await exists.ExecuteScalarAsync(cancellationToken) is null)
            {
                return null;
            }
        }

        await using (var dup = new NpgsqlCommand(
            "SELECT 1 FROM reports WHERE reporter_user_id = @from AND reported_user_id = @to AND status IN ('OPEN','IN_REVIEW') LIMIT 1;",
            connection, transaction))
        {
            dup.Parameters.AddWithValue("from", NpgsqlDbType.Bigint, reporterUserId);
            dup.Parameters.AddWithValue("to", NpgsqlDbType.Bigint, reportedUserId);
            if (await dup.ExecuteScalarAsync(cancellationToken) is not null)
            {
                return new AdminReportCreateResult(Duplicate: true, ReportId: 0);
            }
        }

        await using (var insert = new NpgsqlCommand(
            "INSERT INTO reports (reporter_user_id, reported_user_id, reason, details) VALUES (@from, @to, @reason, @details) RETURNING report_id;",
            connection, transaction))
        {
            insert.Parameters.AddWithValue("from", NpgsqlDbType.Bigint, reporterUserId);
            insert.Parameters.AddWithValue("to", NpgsqlDbType.Bigint, reportedUserId);
            insert.Parameters.AddWithValue("reason", NpgsqlDbType.Text, reason);
            insert.Parameters.AddWithValue("details", NpgsqlDbType.Text, (object?)details ?? DBNull.Value);
            long id = (long)(await insert.ExecuteScalarAsync(cancellationToken))!;
            await transaction.CommitAsync(cancellationToken);
            return new AdminReportCreateResult(Duplicate: false, ReportId: id);
        }
    }

    /// <summary>Đổi trạng thái xử lý kèm audit trong một transaction.</summary>
    public async Task<AdminReportHandleResult?> HandleAsync(long reportId, ReportStatus status, string? note, long actorUserId,
        string traceId, CancellationToken cancellationToken = default)
    {
        string statusText = ToDbText(status);
        string action = status switch
        {
            ReportStatus.InReview => "REPORT_REVIEW",
            ReportStatus.Resolved => "REPORT_RESOLVE",
            _ => "REPORT_REJECT",
        };

        await using NpgsqlConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using NpgsqlTransaction transaction =
            await connection.BeginTransactionAsync(cancellationToken);

        // Khóa dòng report trước rồi mới đọc, request đồng thời xếp hàng tại đây.
        string? reporterEmail;
        string? reportedEmail;
        ReportStatus previous;
        await using var lockReport = new NpgsqlCommand(
            "SELECT r.status::text AS status, reporter.email AS reporter_email, reported.email AS reported_email FROM reports r JOIN users reporter ON reporter.user_id = r.reporter_user_id JOIN users reported ON reported.user_id = r.reported_user_id WHERE r.report_id = @id FOR UPDATE OF r;",
            connection, transaction);
        lockReport.Parameters.AddWithValue("id", NpgsqlDbType.Bigint, reportId);
        await using (var reader = await lockReport.ExecuteReaderAsync(cancellationToken))
        {
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            previous = ParseStatus(reader.GetString(reader.GetOrdinal("status")));
            reporterEmail = reader.GetString(reader.GetOrdinal("reporter_email"));
            reportedEmail = reader.GetString(reader.GetOrdinal("reported_email"));
        }

        await using (var update = new NpgsqlCommand(
            "UPDATE reports SET status = @status::report_status, handled_by = @actor, handled_note = @note, handled_at = CURRENT_TIMESTAMP WHERE report_id = @id;",
            connection, transaction))
        {
            update.Parameters.AddWithValue("id", NpgsqlDbType.Bigint, reportId);
            update.Parameters.AddWithValue("status", NpgsqlDbType.Varchar, statusText);
            update.Parameters.AddWithValue("actor", NpgsqlDbType.Bigint, actorUserId);
            update.Parameters.AddWithValue("note", NpgsqlDbType.Text, (object?)note ?? DBNull.Value);
            if (await update.ExecuteNonQueryAsync(cancellationToken) != 1)
            {
                return null;
            }
        }

        await using (var audit = new NpgsqlCommand(
            "INSERT INTO audit_logs (actor_user_id, action, entity_type, entity_id, description) VALUES (@actor, @action, 'reports', @id, @detail);",
            connection, transaction))
        {
            audit.Parameters.AddWithValue("actor", NpgsqlDbType.Bigint, actorUserId);
            audit.Parameters.AddWithValue("action", NpgsqlDbType.Varchar, action);
            audit.Parameters.AddWithValue("id", NpgsqlDbType.Bigint, reportId);
            audit.Parameters.AddWithValue("detail", NpgsqlDbType.Text,
                $"{action} report {reportId} ({reporterEmail} -> {reportedEmail}) {ToDbText(previous)}->{statusText} trace={traceId}");
            await audit.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return new AdminReportHandleResult(previous, reporterEmail, reportedEmail);
    }

    private static void AddFilterParameters(NpgsqlCommand command, string? search, string? status)
    {
        string? pattern = string.IsNullOrWhiteSpace(search) ? null : $"%{EscapeLike(search.Trim())}%";
        command.Parameters.AddWithValue("status", NpgsqlDbType.Varchar, (object?)status ?? DBNull.Value);
        command.Parameters.AddWithValue("like", NpgsqlDbType.Varchar, (object?)pattern ?? DBNull.Value);
    }

    /// <summary>Escape các ký tự đại diện của LIKE để từ khóa chỉ match nghĩa đen.</summary>
    private static string EscapeLike(string value) => value
        .Replace(@"\", @"\\", StringComparison.Ordinal)
        .Replace("%", @"\%", StringComparison.Ordinal)
        .Replace("_", @"\_", StringComparison.Ordinal);

    /// <summary>Map enum C# sang đúng chữ DB (IN_REVIEW có gạch dưới).</summary>
    internal static string ToDbText(ReportStatus status) => status switch
    {
        ReportStatus.Open => "OPEN",
        ReportStatus.InReview => "IN_REVIEW",
        ReportStatus.Resolved => "RESOLVED",
        _ => "REJECTED",
    };

    private static ReportStatus ParseStatus(string value) => value.ToUpperInvariant() switch
    {
        "OPEN" => ReportStatus.Open,
        "IN_REVIEW" => ReportStatus.InReview,
        "RESOLVED" => ReportStatus.Resolved,
        _ => ReportStatus.Rejected,
    };

    private static AdminReportRecord MapRecord(NpgsqlDataReader reader)
    {
        int handledAt = reader.GetOrdinal("handled_at");
        int details = reader.GetOrdinal("details");
        int handledNote = reader.GetOrdinal("handled_note");
        int createdAt = reader.GetOrdinal("created_at");
        return new AdminReportRecord
        {
            ReportId = reader.GetInt64(reader.GetOrdinal("report_id")),
            ReporterUserId = reader.GetInt64(reader.GetOrdinal("reporter_user_id")),
            ReporterEmail = reader.GetString(reader.GetOrdinal("reporter_email")),
            ReportedUserId = reader.GetInt64(reader.GetOrdinal("reported_user_id")),
            ReportedEmail = reader.GetString(reader.GetOrdinal("reported_email")),
            Reason = reader.GetString(reader.GetOrdinal("reason")),
            Details = reader.IsDBNull(details) ? null : reader.GetString(details),
            Status = ParseStatus(reader.GetString(reader.GetOrdinal("status"))),
            HandledBy = reader.IsDBNull(reader.GetOrdinal("handled_by")) ? null : reader.GetInt64(reader.GetOrdinal("handled_by")),
            HandledNote = reader.IsDBNull(handledNote) ? null : reader.GetString(handledNote),
            CreatedAt = new DateTimeOffset(reader.GetDateTime(createdAt)),
            HandledAt = reader.IsDBNull(handledAt) ? null : new DateTimeOffset(reader.GetDateTime(handledAt)),
        };
    }
}
