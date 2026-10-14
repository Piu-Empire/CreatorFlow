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
