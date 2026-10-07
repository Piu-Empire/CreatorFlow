using CreatorFlow.Data;
using CreatorFlow.Api.Models.Auth;

using CreatorFlow.Api.Repositories.Auth;
using CreatorFlow.Api.Services.Auth;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;
using NpgsqlTypes;
using System.Security.Cryptography;

namespace CreatorFlow.Api.Tests.Database;

[TestClass]
[TestCategory("AuthApiDatabase")]
public sealed class PasswordResetIntegrationTests
{
    [TestMethod]
    public async Task CompleteReset_ChangesPasswordAndConsumesAtomically_OnlyOnce()
    {
        await using var fixture = await ResetFixture.CreateAsync();
        PasswordResetRequest request = await fixture.CreateRequestAsync();
        Assert.IsTrue(await fixture.Repository.MarkPasswordResetDeliveredAsync(request.RequestId));
        string newHash = BCrypt.Net.BCrypt.HashPassword("Newpass2!");
        bool[] results = await Task.WhenAll(
            fixture.Repository.CompletePasswordResetAsync(request.RequestId, request.OtpVerifier, fixture.OriginalHash, newHash),
            fixture.Repository.CompletePasswordResetAsync(request.RequestId, request.OtpVerifier, fixture.OriginalHash, newHash));
        Assert.AreEqual(1, results.Count(value => value));
        Assert.IsNotNull((await fixture.Repository.FindPasswordResetAsync(request.RequestId))!.ConsumedAt);
        User user = (await fixture.Repository.FindByIdAsync(fixture.UserId))!;
        Assert.AreEqual(newHash, user.PasswordHash);
        var service = DatabaseAuthFactory.Create(fixture.Repository);
        Assert.IsFalse((await service.LoginAsync(new CreatorFlow.Contracts.Auth.LoginRequest(fixture.Email, "Original1!"))).Succeeded);
        Assert.IsTrue((await service.LoginAsync(new CreatorFlow.Contracts.Auth.LoginRequest(fixture.Email, "Newpass2!"))).Succeeded);
    }

    [TestMethod]
    public async Task Resend_EnforcesCooldownInvalidatesPrevious_AndConcurrentQuota()
    {
        await using var fixture = await ResetFixture.CreateAsync();
        PasswordResetRequest first = await fixture.CreateRequestAsync();
        Assert.IsFalse(await fixture.Repository.TryCreatePasswordResetAsync(Guid.NewGuid(), fixture.CanonicalEmail,
            RandomNumberGenerator.GetBytes(32), first.RequestId, fixture.UserId));
        await fixture.AgeRequestAsync(first.RequestId, 61);
        Guid next = Guid.NewGuid();
        bool[] results = await Task.WhenAll(
            fixture.Repository.TryCreatePasswordResetAsync(next, fixture.CanonicalEmail, RandomNumberGenerator.GetBytes(32), first.RequestId, fixture.UserId),
            fixture.Repository.TryCreatePasswordResetAsync(Guid.NewGuid(), fixture.CanonicalEmail, RandomNumberGenerator.GetBytes(32), first.RequestId, fixture.UserId));
        Assert.AreEqual(1, results.Count(value => value));
        Assert.IsNotNull((await fixture.Repository.FindPasswordResetAsync(first.RequestId))!.InvalidatedAt);
        PasswordResetRequest latest = (await fixture.Repository.FindLatestPasswordResetAsync(fixture.CanonicalEmail))!;
        for (int count = 2; count < 5; count++)
        {
            await fixture.AgeRequestAsync(latest.RequestId, 61);
            latest = await fixture.CreateRequestAsync();
        }
        await fixture.AgeRequestAsync(latest.RequestId, 61);
        Assert.IsFalse(await fixture.Repository.TryCreatePasswordResetAsync(Guid.NewGuid(), fixture.CanonicalEmail,
            RandomNumberGenerator.GetBytes(32), latest.RequestId, fixture.UserId));
    }

    [TestMethod]
    public async Task FiveWrongAttempts_AreSerializedAndNeverExceedFive()
    {
        await using var fixture = await ResetFixture.CreateAsync();
        PasswordResetRequest request = await fixture.CreateRequestAsync();
        await fixture.Repository.MarkPasswordResetDeliveredAsync(request.RequestId);
        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ =>
            fixture.Repository.RecordPasswordResetFailureAsync(request.RequestId, request.OtpVerifier)));
        PasswordResetRequest saved = (await fixture.Repository.FindPasswordResetAsync(request.RequestId))!;
        Assert.AreEqual(5, saved.FailedAttempts);
        Assert.IsNotNull(saved.InvalidatedAt);
        Assert.IsFalse(await fixture.Repository.CompletePasswordResetAsync(request.RequestId, request.OtpVerifier,
            fixture.OriginalHash, BCrypt.Net.BCrypt.HashPassword("Newpass2!")));
        Assert.AreEqual(fixture.OriginalHash, (await fixture.Repository.FindByIdAsync(fixture.UserId))!.PasswordHash);
    }

    [TestMethod]
    public async Task ExpiredUndeliveredAndStaleHash_CannotReset()
    {
        await using var fixture = await ResetFixture.CreateAsync();
        PasswordResetRequest request = await fixture.CreateRequestAsync();
        string newHash = BCrypt.Net.BCrypt.HashPassword("Newpass2!");
        Assert.IsFalse(await fixture.Repository.CompletePasswordResetAsync(request.RequestId, request.OtpVerifier, fixture.OriginalHash, newHash));
        await fixture.Repository.MarkPasswordResetDeliveredAsync(request.RequestId);
        Assert.IsFalse(await fixture.Repository.CompletePasswordResetAsync(request.RequestId, RandomNumberGenerator.GetBytes(32), fixture.OriginalHash, newHash));
        await fixture.AgeRequestAsync(request.RequestId, 601);
        Assert.IsFalse(await fixture.Repository.CompletePasswordResetAsync(request.RequestId, request.OtpVerifier, fixture.OriginalHash, newHash));
        PasswordResetRequest next = await fixture.CreateRequestAsync();
        await fixture.Repository.MarkPasswordResetDeliveredAsync(next.RequestId);
        Assert.IsFalse(await fixture.Repository.CompletePasswordResetAsync(next.RequestId, next.OtpVerifier, "stale-hash", newHash));
        Assert.IsNotNull((await fixture.Repository.FindPasswordResetAsync(next.RequestId))!.InvalidatedAt);
        Assert.AreEqual(fixture.OriginalHash, (await fixture.Repository.FindByIdAsync(fixture.UserId))!.PasswordHash);
    }

    [TestMethod]
    [DataRow(AccountStatus.Locked)]
    [DataRow(AccountStatus.Disabled)]
    public async Task InactiveAccountAndUnknownEmail_CreateNeutralRequestsWithoutDelivery(AccountStatus status)
    {
        await using var fixture = await ResetFixture.CreateAsync();
        await fixture.SetStatusAsync(status);
        PasswordResetRequest request = await fixture.CreateRequestAsync();
        Assert.IsNull(request.UserId);
        Assert.IsFalse(await fixture.Repository.MarkPasswordResetDeliveredAsync(request.RequestId));
        // Unknown case uses a separate UUID address that belongs to this fixture for cleanup.
        string unknown = await fixture.Repository.NormalizeResetEmailAsync(fixture.UnknownEmail);
        Guid requestId = Guid.NewGuid();
        Assert.IsTrue(await fixture.Repository.TryCreatePasswordResetAsync(requestId, unknown, RandomNumberGenerator.GetBytes(32), null, null));
        Assert.IsNull((await fixture.Repository.FindPasswordResetAsync(requestId))!.UserId);
        Assert.IsFalse(await fixture.Repository.MarkPasswordResetDeliveredAsync(requestId));
    }

    [TestMethod]
    public async Task ConsumeFailure_RollsBackPasswordWrite()
    {
        await using var fixture = await ResetFixture.CreateAsync();
        PasswordResetRequest request = await fixture.CreateRequestAsync();
        await fixture.Repository.MarkPasswordResetDeliveredAsync(request.RequestId);
        string newHash = BCrypt.Net.BCrypt.HashPassword("Newpass2!");
        // Hold only this fixture's user row, causing UPDATE users to wait until the OTP expires.
        await fixture.SetNearExpiryAsync(request.RequestId);
        await using NpgsqlConnection blocker = await fixture.Factory.OpenConnectionAsync();
        await using NpgsqlTransaction blockingTransaction = await blocker.BeginTransactionAsync();
        await using (var lockUser = new NpgsqlCommand("SELECT user_id FROM users WHERE user_id = @id FOR UPDATE;", blocker, blockingTransaction))
        {
            lockUser.Parameters.AddWithValue("id", NpgsqlDbType.Bigint, fixture.UserId);
            await lockUser.ExecuteScalarAsync();
        }
        Task<bool> reset = fixture.Repository.CompletePasswordResetAsync(request.RequestId, request.OtpVerifier, fixture.OriginalHash, newHash);
        await Task.Delay(TimeSpan.FromSeconds(3));
        await blockingTransaction.RollbackAsync();
        Assert.IsFalse(await reset);
        Assert.AreEqual(fixture.OriginalHash, (await fixture.Repository.FindByIdAsync(fixture.UserId))!.PasswordHash);
        Assert.IsNull((await fixture.Repository.FindPasswordResetAsync(request.RequestId))!.ConsumedAt);
    }

    private sealed class ResetFixture : IAsyncDisposable
    {
        public required NpgsqlConnectionFactory Factory { get; init; }
        public required UserRepository Repository { get; init; }
        public required string Email { get; init; }
        public required string UnknownEmail { get; init; }
        public required string CanonicalEmail { get; init; }
        public required string OriginalHash { get; init; }
        public long UserId { get; init; }

        public static async Task<ResetFixture> CreateAsync()
        {
            string? value = Environment.GetEnvironmentVariable("CREATORFLOW_TEST_CONNECTION_STRING");
            if (string.IsNullOrWhiteSpace(value) || Environment.GetEnvironmentVariable("CREATORFLOW_TEST_ALLOW_WRITES") != "true") Assert.Inconclusive("Requires an approved test database via CREATORFLOW_TEST_CONNECTION_STRING; no app config fallback.");
            var factory = new NpgsqlConnectionFactory(value!);
            var repository = new UserRepository(factory);
            Assert.IsTrue(await repository.IsPasswordResetSchemaReadyAsync(), "Approved reset migration must already be applied by the operator.");
            string email = $"reset-{Guid.NewGuid():N}@example.test";
            string hash = BCrypt.Net.BCrypt.HashPassword("Original1!");
            long userId = await repository.CreateAsync("Reset fixture", email, hash);
            await using (var connection = await factory.OpenConnectionAsync())
            await using (var verify = new NpgsqlCommand("UPDATE users SET email_verified_at=clock_timestamp() WHERE user_id=@id;", connection))
            { verify.Parameters.AddWithValue("id", NpgsqlDbType.Bigint, userId); await verify.ExecuteNonQueryAsync(); }
            return new ResetFixture { Factory = factory, Repository = repository, Email = email,
                UnknownEmail = $"reset-unknown-{Guid.NewGuid():N}@example.test", CanonicalEmail = email,
                OriginalHash = hash, UserId = userId };
        }

        public async Task<PasswordResetRequest> CreateRequestAsync()
        {
            PasswordResetRequest? latest = await Repository.FindLatestPasswordResetAsync(CanonicalEmail);
            Guid requestId = Guid.NewGuid();
            Assert.IsTrue(await Repository.TryCreatePasswordResetAsync(requestId, CanonicalEmail, RandomNumberGenerator.GetBytes(32), latest?.RequestId, UserId));
            return (await Repository.FindPasswordResetAsync(requestId))!;
        }

        public async Task AgeRequestAsync(Guid requestId, int seconds)
        {
            const string sql = """
                UPDATE password_reset_requests SET
                    created_at = created_at - make_interval(secs => @seconds),
                    expires_at = expires_at - make_interval(secs => @seconds),
                    delivered_at = delivered_at - make_interval(secs => @seconds),
                    consumed_at = consumed_at - make_interval(secs => @seconds),
                    invalidated_at = invalidated_at - make_interval(secs => @seconds)
                WHERE request_id = @request_id AND email_normalized = @email;
                """;
            await ExecuteAsync(sql, command =>
            {
                command.Parameters.AddWithValue("seconds", NpgsqlDbType.Double, (double)seconds);
                command.Parameters.AddWithValue("request_id", NpgsqlDbType.Uuid, requestId);
                command.Parameters.AddWithValue("email", NpgsqlDbType.Varchar, CanonicalEmail);
            });
        }

        public async Task SetNearExpiryAsync(Guid requestId) => await ExecuteAsync("""
            WITH stamp AS (SELECT clock_timestamp() AS now)
            UPDATE password_reset_requests SET
                created_at = stamp.now - INTERVAL '9 minutes 58 seconds',
                expires_at = stamp.now + INTERVAL '2 seconds',
                delivered_at = stamp.now - INTERVAL '9 minutes 58 seconds'
            FROM stamp
            WHERE request_id = @request_id AND email_normalized = @email;
            """, command =>
            {
                command.Parameters.AddWithValue("request_id", NpgsqlDbType.Uuid, requestId);
                command.Parameters.AddWithValue("email", NpgsqlDbType.Varchar, CanonicalEmail);
            });

        public async Task SetStatusAsync(AccountStatus status) => await ExecuteAsync("""
            UPDATE users SET account_status = CAST(@status AS account_status) WHERE user_id = @id AND email = @email;
            """, command =>
            {
                command.Parameters.AddWithValue("status", NpgsqlDbType.Text, status.ToString().ToUpperInvariant());
                command.Parameters.AddWithValue("id", NpgsqlDbType.Bigint, UserId);
                command.Parameters.AddWithValue("email", NpgsqlDbType.Varchar, Email);
            });

        private async Task ExecuteAsync(string sql, Action<NpgsqlCommand> parameters)
        {
            await using NpgsqlConnection connection = await Factory.OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            parameters(command);
            await command.ExecuteNonQueryAsync();
        }

        public async ValueTask DisposeAsync() => await ExecuteAsync("""
            DELETE FROM password_reset_requests WHERE email_normalized = @email OR email_normalized = @unknown;
            DELETE FROM users WHERE user_id = @id AND email = @email;
            """, command =>
            {
                command.Parameters.AddWithValue("email", NpgsqlDbType.Varchar, CanonicalEmail);
                command.Parameters.AddWithValue("unknown", NpgsqlDbType.Varchar, UnknownEmail);
                command.Parameters.AddWithValue("id", NpgsqlDbType.Bigint, UserId);
            });
    }
}
