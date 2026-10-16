using CreatorFlow.Api.Models.Projects;
using CreatorFlow.Data;
using Npgsql;
using NpgsqlTypes;

namespace CreatorFlow.Api.Repositories.Projects;

public sealed class ProjectRepository(IDbConnectionFactory factory) : IProjectRepository
{
    private const string SelectSql = """
        SELECT p.project_id, p.project_name, p.description, p.owner_id, u.display_name,
               m.role::text, p.status::text, p.created_at
        FROM projects p JOIN project_members m ON m.project_id = p.project_id
        JOIN users u ON u.user_id = p.owner_id
        WHERE m.user_id = @user AND m.is_active
        """;

    public async Task<IReadOnlyList<ProjectRecord>> ListAsync(long userId, CancellationToken token)
    {
        await using var connection = await factory.OpenConnectionAsync(token);
        await using var command = new NpgsqlCommand(SelectSql + " ORDER BY p.project_name, p.project_id", connection);
        command.Parameters.AddWithValue("user", userId);
        await using var reader = await command.ExecuteReaderAsync(token);
        var items = new List<ProjectRecord>();
        while (await reader.ReadAsync(token)) items.Add(Map(reader));
        return items;
    }

    public async Task<ProjectRecord?> FindAsync(long projectId, long userId, CancellationToken token)
    {
        await using var connection = await factory.OpenConnectionAsync(token);
        return await ReadAsync(connection, null, projectId, userId, false, token);
    }

    public async Task<ProjectMembersRecord?> ListMembersAsync(long projectId, long actorUserId, CancellationToken token)
    {
        await using var connection = await factory.OpenConnectionAsync(token);
        await using var command = new NpgsqlCommand("""
            SELECT actor.role::text, member.project_id, member.user_id, u.display_name, u.email,
                   member.role::text, member.joined_at
            FROM project_members actor
            JOIN project_members member ON member.project_id = actor.project_id AND member.is_active
            JOIN users u ON u.user_id = member.user_id
            WHERE actor.project_id = @project AND actor.user_id = @actor AND actor.is_active
            ORDER BY CASE member.role WHEN 'OWNER' THEN 0 WHEN 'MANAGER' THEN 1 ELSE 2 END,
                     LOWER(u.display_name), member.user_id
            """, connection);
        command.Parameters.AddWithValue("project", projectId);
        command.Parameters.AddWithValue("actor", actorUserId);
        await using var reader = await command.ExecuteReaderAsync(token);
        string? actorRole = null;
        var members = new List<ProjectMemberRecord>();
        while (await reader.ReadAsync(token))
        {
            actorRole ??= reader.GetString(0);
            members.Add(new ProjectMemberRecord(reader.GetInt64(1), reader.GetInt64(2), reader.GetString(3),
                reader.GetString(4), reader.GetString(5), new DateTimeOffset(reader.GetDateTime(6))));
        }
        return actorRole is null ? null : new ProjectMembersRecord(actorRole, members);
    }

    public async Task<T> InTransactionAsync<T>(Func<IProjectTransaction, CancellationToken, Task<T>> action, CancellationToken token)
    {
        await using var connection = await factory.OpenConnectionAsync(token);
        await using var transaction = await connection.BeginTransactionAsync(token);
        // Permission checks and writes share the locked transaction; exceptions dispose/rollback it.
        T result = await action(new Transaction(connection, transaction), token);
        await transaction.CommitAsync(token);
        return result;
    }

    private static async Task<ProjectRecord?> ReadAsync(NpgsqlConnection connection, NpgsqlTransaction? transaction,
        long projectId, long userId, bool locked, CancellationToken token)
    {
        // Acquire Project then membership in the same order for every mutation.
        if (locked)
        {
            await using var projectLock = new NpgsqlCommand("SELECT project_id FROM projects WHERE project_id = @id FOR UPDATE", connection, transaction);
            projectLock.Parameters.AddWithValue("id", projectId);
            if (await projectLock.ExecuteScalarAsync(token) is null) return null;
        }
        await using var command = new NpgsqlCommand(SelectSql + " AND p.project_id = @id" +
            (locked ? " FOR UPDATE OF m" : string.Empty), connection, transaction);
        command.Parameters.AddWithValue("id", projectId);
        command.Parameters.AddWithValue("user", userId);
        await using var reader = await command.ExecuteReaderAsync(token);
        return await reader.ReadAsync(token) ? Map(reader) : null;
    }

    private static ProjectRecord Map(NpgsqlDataReader reader) => new(reader.GetInt64(0), reader.GetString(1),
        reader.IsDBNull(2) ? null : reader.GetString(2), reader.GetInt64(3), reader.GetString(4), reader.GetString(5),
        reader.GetString(6), new DateTimeOffset(reader.GetDateTime(7)));

    private sealed class Transaction(NpgsqlConnection connection, NpgsqlTransaction transaction) : IProjectTransaction
    {
        public Task<ProjectRecord?> LockAsync(long projectId, long userId, CancellationToken token) =>
            ReadAsync(connection, transaction, projectId, userId, true, token);

        public async Task<ProjectMemberRecord?> LockMemberAsync(long projectId, long memberUserId, CancellationToken token)
        {
            await using var command = new NpgsqlCommand("""
                SELECT pm.project_id, pm.user_id, u.display_name, u.email, pm.role::text, pm.joined_at, pm.is_active
                FROM project_members pm JOIN users u ON u.user_id = pm.user_id
                WHERE pm.project_id = @project AND pm.user_id = @user AND pm.is_active
                FOR UPDATE OF pm
                """, connection, transaction);
            command.Parameters.AddWithValue("project", projectId);
            command.Parameters.AddWithValue("user", memberUserId);
            await using var reader = await command.ExecuteReaderAsync(token);
            return await reader.ReadAsync(token)
                ? new ProjectMemberRecord(reader.GetInt64(0), reader.GetInt64(1), reader.GetString(2), reader.GetString(3),
                    reader.GetString(4), new DateTimeOffset(reader.GetDateTime(5)), reader.GetBoolean(6))
                : null;
        }

        public async Task<int> CountActiveOwnersAsync(long projectId, CancellationToken token)
        {
            await using var command = new NpgsqlCommand("""
                SELECT COUNT(*) FROM project_members
                WHERE project_id = @project AND is_active AND role = 'OWNER'
                """, connection, transaction);
            command.Parameters.AddWithValue("project", projectId);
            return Convert.ToInt32(await command.ExecuteScalarAsync(token));
        }

        public async Task<long?> FindActiveOwnerAsync(long projectId, long exceptUserId, CancellationToken token)
        {
            await using var command = new NpgsqlCommand("""
                SELECT user_id FROM project_members
                WHERE project_id = @project AND user_id <> @except AND is_active AND role = 'OWNER'
                ORDER BY joined_at, user_id LIMIT 1
                """, connection, transaction);
            command.Parameters.AddWithValue("project", projectId);
            command.Parameters.AddWithValue("except", exceptUserId);
            object? value = await command.ExecuteScalarAsync(token);
            return value is null or DBNull ? null : (long)value;
        }

        public async Task SetProjectOwnerAsync(long projectId, long userId, CancellationToken token)
        {
            await using var command = new NpgsqlCommand("UPDATE projects SET owner_id = @user WHERE project_id = @project", connection, transaction);
            command.Parameters.AddWithValue("project", projectId);
            command.Parameters.AddWithValue("user", userId);
            await command.ExecuteNonQueryAsync(token);
        }

        public async Task UpdateMemberRoleAsync(long projectId, long memberUserId, string role, CancellationToken token)
        {
            await using var command = new NpgsqlCommand("""
                UPDATE project_members SET role = @role::project_role
                WHERE project_id = @project AND user_id = @user AND is_active
                """, connection, transaction);
            command.Parameters.AddWithValue("project", projectId);
            command.Parameters.AddWithValue("user", memberUserId);
            command.Parameters.AddWithValue("role", role);
            await command.ExecuteNonQueryAsync(token);
        }

        public async Task RemoveMemberAsync(long projectId, long memberUserId, CancellationToken token)
        {
            await using var command = new NpgsqlCommand("""
                UPDATE project_members SET is_active = FALSE
                WHERE project_id = @project AND user_id = @user AND is_active
                """, connection, transaction);
            command.Parameters.AddWithValue("project", projectId);
            command.Parameters.AddWithValue("user", memberUserId);
            await command.ExecuteNonQueryAsync(token);
        }

        public async Task<ProjectRecord> CreateAsync(long userId, string name, string? description, CancellationToken token)
        {
            await using var create = new NpgsqlCommand("""
                INSERT INTO projects(project_name, description, owner_id, status)
                VALUES (@name, @description, @user, 'ACTIVE') RETURNING project_id
                """, connection, transaction);
            AddInput(create, userId, name, description);
            long id = (long)(await create.ExecuteScalarAsync(token))!;
            await using var member = new NpgsqlCommand("""
                INSERT INTO project_members(project_id, user_id, role, is_active)
                VALUES (@id, @user, 'OWNER', TRUE)
                """, connection, transaction);
            member.Parameters.AddWithValue("id", id);
            member.Parameters.AddWithValue("user", userId);
            await member.ExecuteNonQueryAsync(token);
            return (await ReadAsync(connection, transaction, id, userId, false, token))!;
        }

        public async Task<ProjectRecord> UpdateAsync(long projectId, long userId, string name, string? description, CancellationToken token)
        {
            await using var command = new NpgsqlCommand("UPDATE projects SET project_name = @name, description = @description WHERE project_id = @id", connection, transaction);
            AddInput(command, userId, name, description);
            command.Parameters.AddWithValue("id", projectId);
            await command.ExecuteNonQueryAsync(token);
            return (await ReadAsync(connection, transaction, projectId, userId, false, token))!;
        }

        public async Task<ProjectRecord> ArchiveAsync(long projectId, long userId, CancellationToken token)
        {
            await using var command = new NpgsqlCommand("UPDATE projects SET status = 'ARCHIVED' WHERE project_id = @id AND status <> 'ARCHIVED'", connection, transaction);
            command.Parameters.AddWithValue("id", projectId);
            await command.ExecuteNonQueryAsync(token);
            return (await ReadAsync(connection, transaction, projectId, userId, false, token))!;
        }

        public async Task<ProjectRecord> RestoreAsync(long projectId, long userId, CancellationToken token)
        {
            await using var command = new NpgsqlCommand("UPDATE projects SET status = 'ACTIVE' WHERE project_id = @id AND status = 'ARCHIVED'", connection, transaction);
            command.Parameters.AddWithValue("id", projectId);
            await command.ExecuteNonQueryAsync(token);
            return (await ReadAsync(connection, transaction, projectId, userId, false, token))!;
        }

        public async Task DeleteAsync(long projectId, CancellationToken token)
        {
            await using var command = new NpgsqlCommand("DELETE FROM projects WHERE project_id = @id", connection, transaction);
            command.Parameters.AddWithValue("id", projectId);
            await command.ExecuteNonQueryAsync(token);
        }

        private static void AddInput(NpgsqlCommand command, long userId, string name, string? description)
        {
            command.Parameters.AddWithValue("user", userId);
            command.Parameters.AddWithValue("name", NpgsqlDbType.Varchar, name);
            command.Parameters.AddWithValue("description", NpgsqlDbType.Text, (object?)description ?? DBNull.Value);
        }
    }
}
