using CreatorFlow.Api.Repositories.Auth;
using CreatorFlow.Data;
using Npgsql;
using NpgsqlTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.Api.Tests.Database;

[TestClass]
[TestCategory("AuthApiDatabase")]
public sealed class TokenVersionIntegrationTests
{
    [TestMethod]
    public async Task SecurityTriggerAndConditionalLogin_PreserveVersionAndRejectStaleSnapshot()
    {
        await using var fixture = await Fixture.CreateAsync();
        var user = (await fixture.Repository.FindByIdAsync(fixture.Id))!;
        Assert.AreEqual(0L, user.TokenVersion);
        await fixture.ExecuteAsync("UPDATE users SET email_verified_at=clock_timestamp() WHERE user_id=@id;");
        user = (await fixture.Repository.FindByIdAsync(fixture.Id))!;
        long verifiedVersion = user.TokenVersion;
        Assert.AreEqual(1L, verifiedVersion);
        Assert.IsNotNull(await fixture.Repository.RecordSuccessfulLoginAsync(user.UserId, user.PasswordHash, verifiedVersion));
        Assert.AreEqual(verifiedVersion, (await fixture.Repository.FindByIdAsync(fixture.Id))!.TokenVersion);
        await fixture.ExecuteAsync("UPDATE users SET display_name='Saved',avatar_url='https://example.test/image.png' WHERE user_id=@id;");
        Assert.AreEqual(verifiedVersion, (await fixture.Repository.FindByIdAsync(fixture.Id))!.TokenVersion);
        await fixture.ExecuteAsync("UPDATE users SET account_status='LOCKED' WHERE user_id=@id;");
        await fixture.ExecuteAsync("UPDATE users SET account_status='ACTIVE' WHERE user_id=@id;");
        Assert.AreEqual(verifiedVersion + 2, (await fixture.Repository.FindByIdAsync(fixture.Id))!.TokenVersion);
        Assert.IsNull(await fixture.Repository.RecordSuccessfulLoginAsync(user.UserId, user.PasswordHash, verifiedVersion));
        Assert.IsFalse(await fixture.Repository.UpdatePasswordAsync(user.UserId, user.PasswordHash, BCrypt.Net.BCrypt.HashPassword("Newpass2!"), verifiedVersion));
    }

    [TestMethod]
    public async Task ResetConsumeFailure_RollsBackPasswordAndVersion()
    {
        await using var fixture = await Fixture.CreateAsync();
        var user = (await fixture.Repository.FindByIdAsync(fixture.Id))!;
        byte[] verifier = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        Guid id = Guid.NewGuid();
        Assert.IsTrue(await fixture.Repository.TryCreatePasswordResetAsync(id, fixture.Email, verifier, null, fixture.Id));
        Assert.IsTrue(await fixture.Repository.MarkPasswordResetDeliveredAsync(id));
        await using (var connection = await fixture.Factory.OpenConnectionAsync())
        await using (var age = new NpgsqlCommand("""
            WITH stamp AS (SELECT clock_timestamp() AS now)
            UPDATE password_reset_requests SET created_at=stamp.now-INTERVAL '9 minutes 58 seconds',
                expires_at=stamp.now+INTERVAL '2 seconds',delivered_at=stamp.now-INTERVAL '9 minutes 58 seconds'
            FROM stamp WHERE request_id=@request;
            """, connection))
        { age.Parameters.AddWithValue("request", NpgsqlDbType.Uuid, id); await age.ExecuteNonQueryAsync(); }
        await using var blocker = await fixture.Factory.OpenConnectionAsync();
        await using var transaction = await blocker.BeginTransactionAsync();
        await using (var row = new NpgsqlCommand("SELECT user_id FROM users WHERE user_id=@id FOR UPDATE;", blocker, transaction))
        { row.Parameters.AddWithValue("id", NpgsqlDbType.Bigint, fixture.Id); await row.ExecuteScalarAsync(); }
        Task<bool> reset = fixture.Repository.CompletePasswordResetAsync(id, verifier, user.PasswordHash, BCrypt.Net.BCrypt.HashPassword("Newpass2!"));
        await Task.Delay(TimeSpan.FromSeconds(3)); await transaction.RollbackAsync();
        Assert.IsFalse(await reset);
        var unchanged = (await fixture.Repository.FindByIdAsync(fixture.Id))!;
        Assert.AreEqual(user.TokenVersion, unchanged.TokenVersion);
        Assert.IsTrue(BCrypt.Net.BCrypt.Verify("Original1!", unchanged.PasswordHash));
        Assert.IsNull((await fixture.Repository.FindPasswordResetAsync(id))!.ConsumedAt);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        public required NpgsqlConnectionFactory Factory { get; init; }
        public required UserRepository Repository { get; init; }
        public required string Email { get; init; }
        public long Id { get; init; }
        public static async Task<Fixture> CreateAsync()
        {
            string? value = Environment.GetEnvironmentVariable("CREATORFLOW_TEST_CONNECTION_STRING");
            if (string.IsNullOrWhiteSpace(value) || Environment.GetEnvironmentVariable("CREATORFLOW_TEST_ALLOW_WRITES") != "true")
                Assert.Inconclusive("Requires explicitly approved disposable database, migrations 04/05/06 and write opt-in. No config fallback.");
            var factory = new NpgsqlConnectionFactory(value!);
            var repository = new UserRepository(factory);
            string email = $"auth-api-{Guid.NewGuid():N}@example.test";
            long id = await repository.CreateAsync("Fixture", email, BCrypt.Net.BCrypt.HashPassword("Original1!"));
            return new() { Factory = factory, Repository = repository, Email = email, Id = id };
        }
        public async Task ExecuteAsync(string sql)
        {
            await using var connection = await Factory.OpenConnectionAsync(); await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("id", NpgsqlDbType.Bigint, Id); await command.ExecuteNonQueryAsync();
        }
        public async ValueTask DisposeAsync() => await ExecuteAsync("""
            DELETE FROM password_reset_requests WHERE user_id=@id;
            DELETE FROM email_verification_requests WHERE user_id=@id;
            DELETE FROM user_avatars WHERE user_id=@id;
            DELETE FROM users WHERE user_id=@id;
            """);
    }
}
