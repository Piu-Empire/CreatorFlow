using CreatorFlow.Data;
using CreatorFlow.Api.Models.Auth;

using CreatorFlow.Api.Repositories.Auth;
using CreatorFlow.Api.Services.Auth;
using CreatorFlow.Contracts.Auth;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;
using NpgsqlTypes;

namespace CreatorFlow.Api.Tests.Database;

[TestClass]
[TestCategory("AuthApiDatabase")]
public sealed class UserRepositoryIntegrationTests
{
    [TestMethod]
    public async Task Login_ActiveUser_UpdatesTimestampAndCreatesSession()
    {
        await using AuthFixture fixture = await AuthFixture.CreateAsync(AccountStatus.Active);
        var repository = new UserRepository(fixture.ConnectionFactory);
        var service = DatabaseAuthFactory.Create(repository);
        (DateTime? timestampBefore, DateTime clockBefore) = await fixture.ReadLoginStateAsync();
        Assert.IsNull(timestampBefore);

        AuthOperationResult<LoginResponse> result = await service.LoginAsync(new LoginRequest(fixture.Email.ToUpperInvariant(), fixture.Password));

        Assert.IsTrue(result.Succeeded, result.Message);
        (DateTime? timestampAfter, DateTime clockAfter) = await fixture.ReadLoginStateAsync();
        Assert.IsTrue(timestampAfter.HasValue);
        Assert.IsTrue(timestampAfter.Value >= clockBefore && timestampAfter.Value <= clockAfter);
        Assert.IsNotNull(result.Value!.CurrentUser);
        Assert.AreEqual(fixture.UserId, result.Value!.CurrentUser.UserId);
        Assert.AreEqual(fixture.Email, result.Value!.CurrentUser.Email);
        Assert.AreEqual("SCRUM-20 integration fixture", result.Value!.CurrentUser.DisplayName);
        Assert.IsFalse(result.Value!.CurrentUser.IsSystemAdmin);

        Assert.IsNotNull(result.Value!.AccessToken);
    }

    [TestMethod]
    [DataRow(AccountStatus.Active, false, false)]
    [DataRow(AccountStatus.Locked, true, false)]
    [DataRow(AccountStatus.Disabled, true, false)]
    [DataRow(AccountStatus.Active, true, true)]
    public async Task Login_RejectedUser_DoesNotUpdateTimestamp(
        AccountStatus status, bool correctPassword, bool placeholderHash)
    {
        await using AuthFixture fixture = await AuthFixture.CreateAsync(status, placeholderHash);
        var service = DatabaseAuthFactory.Create(new UserRepository(fixture.ConnectionFactory));

        AuthOperationResult<LoginResponse> result = await service.LoginAsync(
            new LoginRequest(fixture.Email, correctPassword ? fixture.Password : Guid.NewGuid().ToString("N")));

        Assert.IsFalse(result.Succeeded);
        Assert.IsNull(result.Value);
        Assert.IsNull((await fixture.ReadLoginStateAsync()).LastLoginAt);
    }

    [TestMethod]
    [DataRow(AccountStatus.Locked)]
    [DataRow(AccountStatus.Disabled)]
    public async Task UpdateLastLogin_InactiveUser_RejectsWrite(AccountStatus status)
    {
        await using AuthFixture fixture = await AuthFixture.CreateAsync(status);
        var repository = new UserRepository(fixture.ConnectionFactory);

        User user = (await repository.FindByIdAsync(fixture.UserId))!;
        Assert.IsNull(await repository.RecordSuccessfulLoginAsync(fixture.UserId, user.PasswordHash, user.TokenVersion));
        Assert.IsNull((await fixture.ReadLoginStateAsync()).LastLoginAt);
    }

    [TestMethod]
    public async Task FindByEmail_UsesCaseInsensitiveLookupAndTreatsInputAsData()
    {
        await using AuthFixture fixture = await AuthFixture.CreateAsync(AccountStatus.Active);
        var repository = new UserRepository(fixture.ConnectionFactory);

        User? user = await repository.FindByEmailAsync(fixture.Email.ToUpperInvariant());
        Assert.IsNotNull(user);
        Assert.AreEqual(fixture.UserId, user.UserId);
        Assert.IsNull(await repository.FindByEmailAsync(fixture.Email + "' OR '1'='1"));
    }

    private sealed class AuthFixture : IAsyncDisposable
    {
        public required NpgsqlConnectionFactory ConnectionFactory { get; init; }
        public long UserId { get; init; }
        public required string Email { get; init; }
        public required string Password { get; init; }

        public static async Task<AuthFixture> CreateAsync(AccountStatus status, bool placeholderHash = false)
        {
            string? connectionString = Environment.GetEnvironmentVariable("CREATORFLOW_TEST_CONNECTION_STRING");
            if (string.IsNullOrWhiteSpace(connectionString) || Environment.GetEnvironmentVariable("CREATORFLOW_TEST_ALLOW_WRITES") != "true")
            {
                Assert.Inconclusive(
                    "Auth database tests require an explicitly approved test database via " +
                    "CREATORFLOW_TEST_CONNECTION_STRING. Application configuration is never used.");
            }

            var factory = new NpgsqlConnectionFactory(connectionString);
            string email = $"scrum20-{Guid.NewGuid():N}@example.test";
            string password = Guid.NewGuid().ToString("N");
            string passwordHash = placeholderHash ? "DEV_HASH_OWNER" : BCrypt.Net.BCrypt.HashPassword(password);
            const string sql = """
                INSERT INTO users (email, password_hash, display_name, account_status, is_system_admin, email_verified_at)
                VALUES (@email, @password_hash, @display_name, CAST(@status AS account_status), FALSE, clock_timestamp())
                RETURNING user_id;
                """;
            await using NpgsqlConnection connection = await factory.OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("email", NpgsqlDbType.Varchar, email);
            command.Parameters.AddWithValue("password_hash", NpgsqlDbType.Text, passwordHash);
            command.Parameters.AddWithValue("display_name", NpgsqlDbType.Varchar, "SCRUM-20 integration fixture");
            command.Parameters.AddWithValue("status", NpgsqlDbType.Text, status.ToString().ToUpperInvariant());
            long userId = (long)(await command.ExecuteScalarAsync())!;
            return new AuthFixture
            {
                ConnectionFactory = factory, UserId = userId, Email = email, Password = password
            };
        }

        public async Task<(DateTime? LastLoginAt, DateTime DatabaseNow)> ReadLoginStateAsync()
        {
            const string sql = """
                SELECT last_login_at, clock_timestamp() AS database_now
                FROM users WHERE user_id = @user_id;
                """;
            await using NpgsqlConnection connection = await ConnectionFactory.OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("user_id", NpgsqlDbType.Bigint, UserId);
            await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
            Assert.IsTrue(await reader.ReadAsync());
            return (reader.IsDBNull(0) ? null : reader.GetDateTime(0), reader.GetDateTime(1));
        }

        public async ValueTask DisposeAsync()
        {
            const string sql = "DELETE FROM users WHERE user_id = @user_id AND email = @email;";
            await using NpgsqlConnection connection = await ConnectionFactory.OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("user_id", NpgsqlDbType.Bigint, UserId);
            command.Parameters.AddWithValue("email", NpgsqlDbType.Varchar, Email);
            Assert.AreEqual(1, await command.ExecuteNonQueryAsync(), "The auth test fixture was not removed.");
        }
    }
}
