using CreatorFlow.Api.Models.Invitations;
using CreatorFlow.Api.Models.Tasks;
using CreatorFlow.Data;
using Npgsql;
using NpgsqlTypes;

namespace CreatorFlow.Api.Repositories.Invitations;

/// <summary>
/// Cài đặt Npgsql cho IInvitationRepository, viết theo database/01_schema.sql (cột snake_case, enum UPPER_SNAKE_CASE).
/// Chống mời trùng nhờ unique index uq_pending_invitation (project_id, LOWER(invited_email)) WHERE status = 'PENDING';
/// lời mời Pending đã hết hạn được chuyển sang EXPIRED trước khi tạo lời mời mới để không chặn oan.
/// </summary>
public sealed class InvitationRepository(IDbConnectionFactory connectionFactory) : IInvitationRepository
{
    private const string PendingUniqueIndex = "uq_pending_invitation";

    private const string InvitationSelect = """
        SELECT i.invitation_id, i.project_id, i.invited_email, i.role::text, i.status::text,
               u.display_name, i.created_at, i.expires_at, i.responded_at
        FROM project_invitations i
        JOIN users u ON u.user_id = i.invited_by
        """;

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

    public async Task<bool> IsActiveMemberByEmailAsync(long projectId, string email, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM project_members pm
                JOIN users u ON u.user_id = pm.user_id
                WHERE pm.project_id = @project_id AND pm.is_active AND LOWER(u.email) = LOWER(@email));
            """;

        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("project_id", NpgsqlDbType.Bigint, projectId);
        command.Parameters.AddWithValue("email", NpgsqlDbType.Varchar, email);

        return (bool)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    public async Task<ProjectInvitation?> CreatePendingAsync(
        long projectId, string email, ProjectRole role, long invitedBy, string token,
        DateTime nowUtc, DateTime expiresAtUtc, CancellationToken cancellationToken = default)
    {
        long invitationId;

        await using (NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken))
        await using (NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken))
        {
            await using (var expire = new NpgsqlCommand("""
                UPDATE project_invitations
                SET status = 'EXPIRED'
                WHERE project_id = @project_id
                  AND LOWER(invited_email) = LOWER(@email)
                  AND status = 'PENDING'
                  AND expires_at <= @now;
                """, connection, transaction))
            {
                expire.Parameters.AddWithValue("project_id", NpgsqlDbType.Bigint, projectId);
                expire.Parameters.AddWithValue("email", NpgsqlDbType.Varchar, email);
                expire.Parameters.AddWithValue("now", NpgsqlDbType.TimestampTz, nowUtc);
                await expire.ExecuteNonQueryAsync(cancellationToken);
            }

            try
            {
                await using var insert = new NpgsqlCommand("""
                    INSERT INTO project_invitations
                        (project_id, invited_email, invited_user_id, invited_by, role, token, status, expires_at, created_at)
                    VALUES
                        (@project_id, @email, (SELECT user_id FROM users WHERE LOWER(email) = LOWER(@email)),
                         @invited_by, @role::project_role, @token, 'PENDING', @expires_at, @now)
                    RETURNING invitation_id;
                    """, connection, transaction);
                insert.Parameters.AddWithValue("project_id", NpgsqlDbType.Bigint, projectId);
                insert.Parameters.AddWithValue("email", NpgsqlDbType.Varchar, email);
                insert.Parameters.AddWithValue("invited_by", NpgsqlDbType.Bigint, invitedBy);
                insert.Parameters.AddWithValue("role", NpgsqlDbType.Text, ToDbEnum(role));
                insert.Parameters.AddWithValue("token", NpgsqlDbType.Varchar, token);
                insert.Parameters.AddWithValue("expires_at", NpgsqlDbType.TimestampTz, expiresAtUtc);
                insert.Parameters.AddWithValue("now", NpgsqlDbType.TimestampTz, nowUtc);
                invitationId = (long)(await insert.ExecuteScalarAsync(cancellationToken))!;
            }
            catch (PostgresException exception)
                when (exception.SqlState == PostgresErrorCodes.UniqueViolation && exception.ConstraintName == PendingUniqueIndex)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null; // Đã có lời mời Pending còn hạn cho email này trong Project.
            }

            await transaction.CommitAsync(cancellationToken);
        }

        return await GetByIdAsync(invitationId, cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectInvitation>> GetByProjectAsync(long projectId, CancellationToken cancellationToken = default)
    {
        string sql = InvitationSelect + """

            WHERE i.project_id = @project_id
            ORDER BY i.created_at DESC, i.invitation_id DESC;
            """;

        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("project_id", NpgsqlDbType.Bigint, projectId);
        return await ReadAllAsync(command, cancellationToken);
    }

    // ------------------------------------------------------------------ helpers

    private async Task<ProjectInvitation?> GetByIdAsync(long invitationId, CancellationToken cancellationToken)
    {
        string sql = InvitationSelect + """

            WHERE i.invitation_id = @invitation_id;
            """;

        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("invitation_id", NpgsqlDbType.Bigint, invitationId);
        var rows = await ReadAllAsync(command, cancellationToken);
        return rows.Count == 0 ? null : rows[0];
    }

    private static async Task<IReadOnlyList<ProjectInvitation>> ReadAllAsync(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        var rows = new List<ProjectInvitation>();
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new ProjectInvitation(
                InvitationId: reader.GetInt64(0),
                ProjectId: reader.GetInt64(1),
                InvitedEmail: reader.GetString(2),
                Role: ParseEnum<ProjectRole>(reader.GetString(3)),
                Status: ParseEnum<InvitationStatus>(reader.GetString(4)),
                InvitedByName: reader.GetString(5),
                CreatedAt: reader.GetDateTime(6),
                ExpiresAt: reader.GetDateTime(7),
                RespondedAt: reader.IsDBNull(8) ? null : reader.GetDateTime(8)));
        }
        return rows;
    }

    /// <summary>"IN_PROGRESS" → "InProgress".</summary>
    private static string ToPascal(string databaseValue) =>
        string.Concat(databaseValue.Split('_', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant()));

    /// <summary>ProjectRole.Manager → "MANAGER".</summary>
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

    private static TEnum ParseEnum<TEnum>(string databaseValue) where TEnum : struct, Enum =>
        Enum.Parse<TEnum>(ToPascal(databaseValue), ignoreCase: true);
}
