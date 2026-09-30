using Microsoft.Extensions.Configuration;

namespace CreatorFlow.Data;

public static class DatabaseConfiguration
{
    private const string ConnectionStringName = "CreatorFlow";

    public static string GetConnectionString()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        string? connectionString = configuration.GetConnectionString(ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Database connection string 'CreatorFlow' is missing. " +
                "Set ConnectionStrings__CreatorFlow in the local environment.");
        }

        return connectionString;
    }
}
