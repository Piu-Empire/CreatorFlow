using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CreatorFlow.Data;

public static class DatabaseConfiguration
{
    private const string ConnectionStringName = "CreatorFlow";

    public static string GetConnectionString(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        string? connectionString = configuration.GetConnectionString(ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Database connection string 'CreatorFlow' is missing. " +
                "Set backend User Secrets or ConnectionStrings__CreatorFlow in the environment.");
        }

        try
        {
            var settings = new NpgsqlConnectionStringBuilder(connectionString);
            if (string.IsNullOrWhiteSpace(settings.Host) || string.IsNullOrWhiteSpace(settings.Database))
            {
                throw new InvalidOperationException(
                    "Backend database configuration requires Host and Database.");
            }
        }
        catch (ArgumentException)
        {
            throw new InvalidOperationException(
                "Backend database connection string 'CreatorFlow' is invalid.");
        }

        return connectionString;
    }
}
