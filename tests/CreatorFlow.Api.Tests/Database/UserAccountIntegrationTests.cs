using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Models.Auth;
using CreatorFlow.Api.Repositories.Auth;
using CreatorFlow.Api.Services.Auth;
using CreatorFlow.Contracts.Auth;
using CreatorFlow.Contracts.Users;
using CreatorFlow.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;
using NpgsqlTypes;

namespace CreatorFlow.Api.Tests.Database;

[TestClass]
[TestCategory("AuthApiDatabase")]
public sealed class UserAccountIntegrationTests
{
    [TestMethod]
    public async Task RegisterProfilePassword_PersistAndPreserveProtectedFields()
    {
        await using var fixture = new Fixture();
        var repository = new UserRepository(fixture.Factory); var auth = DatabaseAuthFactory.Create(repository);
        var registered = await auth.RegisterAsync(new RegisterRequest("Person", fixture.Email, "Original1!", "Original1!"));
        Assert.IsTrue(registered.Succeeded); Assert.IsTrue(registered.Value!.RequiresEmailVerification);
        User user = (await repository.FindByEmailAsync(fixture.Email))!;
        Assert.IsNull(user.EmailVerifiedAt); Assert.IsFalse(user.IsSystemAdmin); Assert.AreEqual(AccountStatus.Active, user.AccountStatus);
        Assert.IsTrue(BCrypt.Net.BCrypt.Verify("Original1!", user.PasswordHash));
        var beforeVerify = await auth.LoginAsync(new LoginRequest(fixture.Email, "Original1!"));
        Assert.AreEqual("email_verification_required", beforeVerify.ErrorCode); Assert.IsNull(beforeVerify.Value);
        await using (var connection = await fixture.Factory.OpenConnectionAsync())
        await using (var verify = new NpgsqlCommand("UPDATE users SET email_verified_at=clock_timestamp() WHERE user_id=@id;", connection))
        { verify.Parameters.AddWithValue("id", NpgsqlDbType.Bigint, user.UserId); await verify.ExecuteNonQueryAsync(); }
        var loggedIn = await auth.LoginAsync(new LoginRequest(fixture.Email, "Original1!"));
        Assert.IsTrue(loggedIn.Succeeded); Assert.AreEqual(user.UserId, loggedIn.Value!.CurrentUser.UserId);
        user = (await repository.FindByIdAsync(user.UserId))!;
        var context = new CurrentAuthenticatedUser { User = user };
        var profile = new UserService(repository, context, new AvatarImageProcessor());
        Assert.AreEqual(user.UserId, (await profile.GetProfileAsync()).Value!.UserId);
        Assert.IsTrue((await profile.SaveProfileAsync("Saved Person", "https://example.test/avatar.png", AvatarAction.Url, null)).Succeeded);
        var saved = (await repository.FindByIdAsync(user.UserId))!;
        Assert.AreEqual("Saved Person", saved.DisplayName); Assert.AreEqual(user.Email, saved.Email);
        Assert.AreEqual(user.TokenVersion, saved.TokenVersion); Assert.AreEqual(user.PasswordHash, saved.PasswordHash);
        Assert.AreEqual(user.AccountStatus, saved.AccountStatus); Assert.AreEqual(user.IsSystemAdmin, saved.IsSystemAdmin);
        Assert.IsTrue((await profile.ChangePasswordAsync(new ChangePasswordRequest("Original1!", "Newpass2!", "Newpass2!"))).Succeeded);
        var changed = (await repository.FindByIdAsync(user.UserId))!;
        Assert.AreEqual(user.TokenVersion + 1, changed.TokenVersion);
        Assert.IsTrue(BCrypt.Net.BCrypt.Verify("Newpass2!", changed.PasswordHash));
        Assert.AreEqual(401, (await profile.GetProfileAsync()).Status);
        Assert.IsFalse((await auth.LoginAsync(new LoginRequest(fixture.Email, "Original1!"))).Succeeded);
        Assert.IsTrue((await auth.LoginAsync(new LoginRequest(fixture.Email, "Newpass2!"))).Succeeded);
    }

    [TestMethod]
    public async Task ConcurrentRegister_UniqueEmailAllowsOneAccount()
    {
        await using var fixture = new Fixture(); var repository = new UserRepository(fixture.Factory);
        var results = await Task.WhenAll(
            DatabaseAuthFactory.Create(repository).RegisterAsync(new RegisterRequest("First", fixture.Email, "Original1!", "Original1!")),
            DatabaseAuthFactory.Create(repository).RegisterAsync(new RegisterRequest("Second", fixture.Email.ToUpperInvariant(), "Original1!", "Original1!")));
        Assert.AreEqual(1, results.Count(result => result.Succeeded)); Assert.AreEqual("Email", results.Single(result => !result.Succeeded).ErrorField);
        await using var connection = await fixture.Factory.OpenConnectionAsync();
        await using var count = new NpgsqlCommand("SELECT count(*) FROM users WHERE LOWER(email)=LOWER(@email);", connection);
        count.Parameters.AddWithValue("email", NpgsqlDbType.Varchar, fixture.Email); Assert.AreEqual(1L, (long)(await count.ExecuteScalarAsync())!);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        public NpgsqlConnectionFactory Factory { get; }
        public string Email { get; } = $"auth-api-profile-{Guid.NewGuid():N}@example.test";
        public Fixture()
        {
            string? value = Environment.GetEnvironmentVariable("CREATORFLOW_TEST_CONNECTION_STRING");
            if (string.IsNullOrWhiteSpace(value) || Environment.GetEnvironmentVariable("CREATORFLOW_TEST_ALLOW_WRITES") != "true")
                Assert.Inconclusive("Requires approved disposable database, write opt-in and migrations 04/05/06; no config fallback.");
            Factory = new NpgsqlConnectionFactory(value!);
        }
        public async ValueTask DisposeAsync()
        {
            await using var connection = await Factory.OpenConnectionAsync(); await using var delete = new NpgsqlCommand("DELETE FROM users WHERE LOWER(email)=LOWER(@email);", connection);
            delete.Parameters.AddWithValue("email", NpgsqlDbType.Varchar, Email); await delete.ExecuteNonQueryAsync();
        }
    }
}
