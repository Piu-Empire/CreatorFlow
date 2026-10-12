using CreatorFlow.Api.Models.Invitations;
using CreatorFlow.Api.Models.Tasks;
using CreatorFlow.Data;
using Npgsql;
using NpgsqlTypes;

namespace CreatorFlow.Api.Repositories.Invitations;

/// <summary>
/// Cài đặt Npgsql cho IMyInvitationRepository, viết theo database/01_schema.sql (cột snake_case, enum UPPER_SNAKE_CASE).
/// Mọi giá trị enum liên quan (role, invitation_status) là MỘT từ nên chuyển bằng ToString().ToUpperInvariant() / Enum.Parse.
/// Membership chỉ được tạo đúng một lần nhờ: khoá dòng lời mời (FOR UPDATE) + UNIQUE(project_id, user_id) + ON CONFLICT.
/// </summary>
public sealed class MyInvitationRepository(IDbConnectionFactory connectionFactory) : IMyInvitationRepository
{
    private const string MyInvitationSelect = """
        SELECT i.invitation_id, i.project_id, p.project_name, i.role::text, i.status::text,
               u.display_name, i.created_at, i.expires_at, i.responded_at
        FROM project_invitations i
        JOIN projects p ON p.project_id = i.project_id
        JOIN users u ON u.user_id = i.invited_by
        """;

    public async Task<IReadOnlyList<MyInvitation>> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        string sql = MyInvitationSelect + """

            WHERE LOWER(i.invited_email) = LOWER(@email)
            ORDER BY i.created_at DESC, i.invitation_id DESC;
            """;

        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("email", NpgsqlDbType.Varchar, email);
        return await ReadAllAsync(command, cancellationToken);
    }

    public async Task<InvitationDecisionResult> DecideAsync(
        long invitationId, long userId, string email, InvitationStatus decision, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        if (decision is not (InvitationStatus.Accepted or InvitationStatus.Rejected))
            throw new ArgumentOutOfRangeException(nameof(decision), decision, "Quyết định phải là Accepted hoặc Rejected.");

        // Mọi return sớm bên dưới KHÔNG commit: transaction bị huỷ khi Dispose nên không có thay đổi nào được ghi.
        await using (NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken))
        await using (NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken))
        {
            // 1) Khoá dòng lời mời của CHÍNH email này. Lần chấp nhận thứ hai (đồng thời) sẽ chờ ở đây,
            //    rồi thấy status đã là ACCEPTED nên dừng ở NotPending.
            bool found;
            long projectId = 0;
            ProjectRole role = default;
            InvitationStatus status = default;
            DateTime expiresAt = default;

            await using (var lockRow = new NpgsqlCommand("""
                SELECT project_id, role::text, status::text, expires_at
                FROM project_invitations
                WHERE invitation_id = @invitation_id AND LOWER(invited_email) = LOWER(@email)
                FOR UPDATE;
                """, connection, transaction))
            {
                lockRow.Parameters.AddWithValue("invitation_id", NpgsqlDbType.Bigint, invitationId);
                lockRow.Parameters.AddWithValue("email", NpgsqlDbType.Varchar, email);

                await using NpgsqlDataReader reader = await lockRow.ExecuteReaderAsync(cancellationToken);
                found = await reader.ReadAsync(cancellationToken);
                if (found)
                {
                    projectId = reader.GetInt64(0);
                    role = ParseEnum<ProjectRole>(reader.GetString(1));
                    status = ParseEnum<InvitationStatus>(reader.GetString(2));
                    expiresAt = reader.GetDateTime(3);
                }
            }

            if (!found) return new InvitationDecisionResult(InvitationDecisionOutcome.NotFound);
            if (status != InvitationStatus.Pending)
                return new InvitationDecisionResult(InvitationDecisionOutcome.NotPending, CurrentStatus: status);

            // 2) Pending nhưng đã quá hạn: ghi nhận Expired (không tạo membership) rồi báo cho người dùng.
            if (expiresAt <= nowUtc)
            {
                await using (var expire = new NpgsqlCommand(
                    "UPDATE project_invitations SET status = 'EXPIRED' WHERE invitation_id = @invitation_id;", connection, transaction))
                {
                    expire.Parameters.AddWithValue("invitation_id", NpgsqlDbType.Bigint, invitationId);
                    await expire.ExecuteNonQueryAsync(cancellationToken);
                }
                await transaction.CommitAsync(cancellationToken);
                return new InvitationDecisionResult(InvitationDecisionOutcome.Expired);
            }

            // 3) Chấp nhận → tạo membership. Đã là thành viên đang hoạt động thì giữ nguyên (không đổi role, không tạo dòng thứ hai);
            //    từng bị gỡ (is_active = false) thì kích hoạt lại với vai trò được mời.
            if (decision == InvitationStatus.Accepted)
            {
                await using var member = new NpgsqlCommand("""
                    INSERT INTO project_members (project_id, user_id, role)
                    VALUES (@project_id, @user_id, @role::project_role)
                    ON CONFLICT (project_id, user_id) DO UPDATE
                        SET is_active = TRUE, role = EXCLUDED.role, joined_at = CURRENT_TIMESTAMP
                        WHERE project_members.is_active = FALSE;
                    """, connection, transaction);
                member.Parameters.AddWithValue("project_id", NpgsqlDbType.Bigint, projectId);
                member.Parameters.AddWithValue("user_id", NpgsqlDbType.Bigint, userId);
                member.Parameters.AddWithValue("role", NpgsqlDbType.Text, role.ToString().ToUpperInvariant());
                await member.ExecuteNonQueryAsync(cancellationToken);
            }

            // 4) Cập nhật trạng thái lời mời.
            await using (var update = new NpgsqlCommand("""
                UPDATE project_invitations
                SET status = @status::invitation_status, responded_at = @now, invited_user_id = @user_id
                WHERE invitation_id = @invitation_id;
                """, connection, transaction))
            {
                update.Parameters.AddWithValue("status", NpgsqlDbType.Text, decision.ToString().ToUpperInvariant());
                update.Parameters.AddWithValue("now", NpgsqlDbType.TimestampTz, nowUtc);
                update.Parameters.AddWithValue("user_id", NpgsqlDbType.Bigint, userId);
                update.Parameters.AddWithValue("invitation_id", NpgsqlDbType.Bigint, invitationId);
                await update.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }

        MyInvitation? decided = await GetByIdAsync(invitationId, cancellationToken);
        return new InvitationDecisionResult(InvitationDecisionOutcome.Decided, decided);
    }

    // ------------------------------------------------------------------ helpers

    private async Task<MyInvitation?> GetByIdAsync(long invitationId, CancellationToken cancellationToken)
    {
        string sql = MyInvitationSelect + """

            WHERE i.invitation_id = @invitation_id;
            """;

        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("invitation_id", NpgsqlDbType.Bigint, invitationId);
        var rows = await ReadAllAsync(command, cancellationToken);
        return rows.Count == 0 ? null : rows[0];
    }

    private static async Task<IReadOnlyList<MyInvitation>> ReadAllAsync(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        var rows = new List<MyInvitation>();
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new MyInvitation(
                InvitationId: reader.GetInt64(0),
                ProjectId: reader.GetInt64(1),
                ProjectName: reader.GetString(2),
                Role: ParseEnum<ProjectRole>(reader.GetString(3)),
                Status: ParseEnum<InvitationStatus>(reader.GetString(4)),
                InvitedByName: reader.GetString(5),
                CreatedAt: reader.GetDateTime(6),
                ExpiresAt: reader.GetDateTime(7),
                RespondedAt: reader.IsDBNull(8) ? null : reader.GetDateTime(8)));
        }
        return rows;
    }

    /// <summary>"OWNER" → ProjectRole.Owner, "ACCEPTED" → InvitationStatus.Accepted (mọi giá trị đều là một từ).</summary>
    private static TEnum ParseEnum<TEnum>(string databaseValue) where TEnum : struct, Enum =>
        Enum.Parse<TEnum>(databaseValue, ignoreCase: true);
}
