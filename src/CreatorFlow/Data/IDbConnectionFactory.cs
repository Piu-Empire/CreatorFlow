using Npgsql;

namespace CreatorFlow.Data;

public interface IDbConnectionFactory
{
    Task<NpgsqlConnection> OpenConnectionAsync(
        CancellationToken cancellationToken = default);
}
