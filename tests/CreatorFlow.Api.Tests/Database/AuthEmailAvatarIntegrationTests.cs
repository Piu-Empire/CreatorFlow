using CreatorFlow.Data;
using CreatorFlow.Api.Models.Auth;
using CreatorFlow.Api.Repositories.Auth;
using CreatorFlow.Api.Services.Auth;
using CreatorFlow.Api.Tests.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;
using NpgsqlTypes;
using System.Security.Cryptography;

namespace CreatorFlow.Api.Tests.Database;
[TestClass]
[TestCategory("AuthApiDatabase")]
public sealed class AuthEmailAvatarIntegrationTests
{
    [TestMethod]
    public async Task VerifyConcurrent_ConsumesOnce_ThenLoginUpdatesTimestamp()
    {
        await using var f = await Fixture.CreateAsync();
        var sender = new TestEmailSender();
        var verification = new EmailVerificationService(f.Repository, new CreatorFlow.Api.Authentication.CurrentAuthenticatedUser(), new EmailOtpCodeProtector(RandomNumberGenerator.GetBytes(32)), sender, new ImmediateClock());
        var service = DatabaseAuthFactory.Create(f.Repository);
        Assert.AreEqual("email_verification_required", (await service.LoginAsync(new CreatorFlow.Contracts.Auth.LoginRequest(f.Email, "Original1!"))).ErrorCode);
        Assert.IsNull(await f.ScalarAsync("SELECT last_login_at FROM users WHERE user_id=@id;"));
        var requested = await verification.RequestEmailVerificationAsync(f.Email);
        Guid id = requested.RequestId!.Value;
        PasswordResetResult[] results = await Task.WhenAll(verification.VerifyEmailAsync(id, sender.VerificationCode), verification.VerifyEmailAsync(id, sender.VerificationCode));
        Assert.AreEqual(1, results.Count(r => r.Succeeded));
        Assert.IsNotNull((await f.Repository.FindByIdAsync(f.UserId))!.EmailVerifiedAt);
        Assert.IsNotNull((await f.Repository.FindEmailVerificationAsync(id))!.ConsumedAt);
        Assert.IsTrue((await service.LoginAsync(new CreatorFlow.Contracts.Auth.LoginRequest(f.Email, "Original1!"))).Succeeded);
        Assert.IsNotNull(await f.ScalarAsync("SELECT last_login_at FROM users WHERE user_id=@id;"));
    }

    [TestMethod]
    public async Task VerifyExpiryDuringConsume_RollsBackVerifiedTimestamp()
    {
        await using var f = await Fixture.CreateAsync();
        Guid id = Guid.NewGuid();
        byte[] verifier = RandomNumberGenerator.GetBytes(32);
        Assert.IsTrue(await f.Repository.TryCreateEmailVerificationAsync(id, f.Email, verifier, null, f.UserId));
        Assert.IsTrue(await f.Repository.MarkEmailVerificationDeliveredAsync(id));
        await using (var connection = await f.Factory.OpenConnectionAsync())
        await using (var nearExpiry = new NpgsqlCommand("""
            WITH stamp AS (SELECT clock_timestamp() AS now)
            UPDATE email_verification_requests SET created_at=stamp.now-INTERVAL '9 minutes 58 seconds',
                expires_at=stamp.now+INTERVAL '2 seconds', delivered_at=stamp.now-INTERVAL '9 minutes 58 seconds'
            FROM stamp WHERE request_id=@request;
            """, connection))
        { nearExpiry.Parameters.AddWithValue("request", NpgsqlDbType.Uuid, id); await nearExpiry.ExecuteNonQueryAsync(); }
        await using var blocker = await f.Factory.OpenConnectionAsync();
        await using var transaction = await blocker.BeginTransactionAsync();
        await using (var command = new NpgsqlCommand("SELECT user_id FROM users WHERE user_id=@id FOR UPDATE;", blocker, transaction))
        { command.Parameters.AddWithValue("id", NpgsqlDbType.Bigint, f.UserId); await command.ExecuteScalarAsync(); }
        Task<bool> verify = f.Repository.CompleteEmailVerificationAsync(id, verifier);
        await Task.Delay(TimeSpan.FromSeconds(3));
        await transaction.RollbackAsync();
        Assert.IsFalse(await verify);
        Assert.IsNull((await f.Repository.FindByIdAsync(f.UserId))!.EmailVerifiedAt);
        Assert.IsNull((await f.Repository.FindEmailVerificationAsync(id))!.ConsumedAt);
    }

    [TestMethod]
    public async Task VerificationAttemptsResendAndPurposeQuota_AreSerialized()
    {
        await using var f = await Fixture.CreateAsync();
        Guid id = Guid.NewGuid();
        byte[] verifier = RandomNumberGenerator.GetBytes(32);
        Assert.IsTrue(await f.Repository.TryCreateEmailVerificationAsync(id, f.Email, verifier, null, f.UserId));
        await f.Repository.MarkEmailVerificationDeliveredAsync(id);
        Assert.IsFalse(await f.Repository.TryCreateEmailVerificationAsync(Guid.NewGuid(), f.Email, verifier, id, f.UserId));
        Assert.IsTrue(await f.Repository.TryCreatePasswordResetAsync(Guid.NewGuid(), f.Email, verifier, null, f.UserId));
        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => f.Repository.RecordEmailVerificationFailureAsync(id, verifier)));
        var row = (await f.Repository.FindEmailVerificationAsync(id))!;
        Assert.AreEqual(5, row.FailedAttempts);
        Assert.IsNotNull(row.InvalidatedAt);
        Assert.IsFalse(await f.Repository.CompleteEmailVerificationAsync(id, verifier));
        await using var connection = await f.Factory.OpenConnectionAsync();
        await using var age = new NpgsqlCommand("""
            UPDATE email_verification_requests SET created_at=created_at-INTERVAL '61 seconds',
                expires_at=expires_at-INTERVAL '61 seconds', delivered_at=delivered_at-INTERVAL '61 seconds',
                invalidated_at=invalidated_at-INTERVAL '61 seconds' WHERE request_id=@request;
            """, connection);
        age.Parameters.AddWithValue("request", NpgsqlDbType.Uuid, id);
        await age.ExecuteNonQueryAsync();
        bool[] results = await Task.WhenAll(
            f.Repository.TryCreateEmailVerificationAsync(Guid.NewGuid(), f.Email, verifier, id, f.UserId),
            f.Repository.TryCreateEmailVerificationAsync(Guid.NewGuid(), f.Email, verifier, id, f.UserId));
        Assert.AreEqual(1, results.Count(r => r));
    }

    [TestMethod]
    public async Task AvatarPersistsAcrossRepositoryInstances_AndFailureRollsBackNameAndSource()
    {
        await using var f = await Fixture.CreateAsync();
        await f.ScalarAsync("UPDATE users SET email_verified_at=clock_timestamp() WHERE user_id=@id RETURNING user_id;");
        long version = (await f.Repository.FindByIdAsync(f.UserId))!.TokenVersion;
        UserAvatar avatar = new AvatarImageProcessor().Normalize(AvatarFixtures.Encode(16, 16));
        await f.Repository.SaveProfileAvatarAsync(f.UserId, "Saved", null, AvatarChange.Upload, avatar, version);
        var reopened = new UserRepository(f.Factory);
        CollectionAssert.AreEqual(avatar.ImageData, (await reopened.GetAvatarAsync(f.UserId))!.ImageData);
        Assert.IsNull((await reopened.FindByIdAsync(f.UserId))!.AvatarUrl);
        await Assert.ThrowsExactlyAsync<PostgresException>(() => reopened.SaveProfileAvatarAsync(f.UserId, "Must rollback", null,
            AvatarChange.Upload, avatar with { Width = 0 }, version));
        Assert.AreEqual("Saved", (await reopened.FindByIdAsync(f.UserId))!.DisplayName);
        CollectionAssert.AreEqual(avatar.ImageData, (await reopened.GetAvatarAsync(f.UserId))!.ImageData);
        await reopened.SaveProfileAvatarAsync(f.UserId, "Saved", "https://example.test/avatar.png", AvatarChange.Url, null, version);
        Assert.IsNull(await reopened.GetAvatarAsync(f.UserId));
        Assert.IsNotNull((await reopened.FindByIdAsync(f.UserId))!.AvatarUrl);
        await reopened.SaveProfileAvatarAsync(f.UserId, "Saved", null, AvatarChange.Remove, null, version);
        Assert.IsNull(await reopened.GetAvatarAsync(f.UserId));
        Assert.IsNull((await reopened.FindByIdAsync(f.UserId))!.AvatarUrl);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        public required NpgsqlConnectionFactory Factory { get; init; }
        public required UserRepository Repository { get; init; }
        public required string Email { get; init; }
        public long UserId { get; init; }
        public static async Task<Fixture> CreateAsync()
        {
            string? value = Environment.GetEnvironmentVariable("CREATORFLOW_TEST_CONNECTION_STRING");
            if (string.IsNullOrWhiteSpace(value) || Environment.GetEnvironmentVariable("CREATORFLOW_TEST_ALLOW_WRITES") != "true") Assert.Inconclusive("Requires explicitly approved test environment and migration 05; no config fallback.");
            var factory = new NpgsqlConnectionFactory(value!);
            var repository = new UserRepository(factory);
            Assert.IsTrue(await repository.IsEmailVerificationSchemaReadyAsync());
            string email = $"auth-email-avatar-{Guid.NewGuid():N}@example.test";
            long id = await repository.CreateAsync("Fixture", email, BCrypt.Net.BCrypt.HashPassword("Original1!"));
            return new() { Factory=factory, Repository=repository, Email=email, UserId=id };
        }
        public async Task<object?> ScalarAsync(string sql)
        {
            await using var connection = await Factory.OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("id", NpgsqlDbType.Bigint, UserId);
            object? value = await command.ExecuteScalarAsync();
            return value is DBNull ? null : value;
        }
        public async ValueTask DisposeAsync()
        {
            await using var connection = await Factory.OpenConnectionAsync();
            await using var command = new NpgsqlCommand("""
                DELETE FROM email_verification_requests WHERE user_id=@id;
                DELETE FROM password_reset_requests WHERE user_id=@id;
                DELETE FROM user_avatars WHERE user_id=@id;
                DELETE FROM users WHERE user_id=@id AND email=@email;
                """, connection);
            command.Parameters.AddWithValue("id", NpgsqlDbType.Bigint, UserId);
            command.Parameters.AddWithValue("email", NpgsqlDbType.Varchar, Email);
            await command.ExecuteNonQueryAsync();
        }
    }
}
