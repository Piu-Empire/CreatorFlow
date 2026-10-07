using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Models.Auth;
using CreatorFlow.Api.Services.Auth;
using CreatorFlow.Contracts.Auth;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.Cryptography;

namespace CreatorFlow.Api.Tests.Services;

[TestClass]
[TestCategory("AuthApiUnit")]
public sealed class AuthServiceTests
{
    [TestMethod]
    [DataRow("", "Original1!", "Email")]
    [DataRow("fixture@example.test", "", "Password")]
    public async Task Login_RequiresInput(string email, string password, string field)
    {
        var result = await Create(new UserAccountFake()).LoginAsync(new LoginRequest(email, password));
        Assert.IsFalse(result.Succeeded); Assert.AreEqual(field, result.ErrorField);
    }

    [TestMethod]
    [DataRow(AccountStatus.Active, true, 200)]
    [DataRow(AccountStatus.Active, false, 403)]
    [DataRow(AccountStatus.Locked, true, 403)]
    [DataRow(AccountStatus.Disabled, true, 403)]
    public async Task Login_ChecksEligibilityAfterPassword(AccountStatus status, bool verified, int expected)
    {
        var repository = new UserAccountFake { User = new User { UserId = 42, Email = "fixture@example.test", DisplayName = "Fixture",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Original1!"), AccountStatus = status,
            EmailVerifiedAt = verified ? DateTimeOffset.UtcNow : null } };
        var service = Create(repository);
        var result = await service.LoginAsync(new LoginRequest("FIXTURE@example.test", "Original1!"));
        Assert.AreEqual(expected, result.Status);
        if (expected == 200) Assert.IsNotNull(result.Value!.AccessToken);
        else Assert.IsNull(result.Value);
        var wrong = await service.LoginAsync(new LoginRequest("fixture@example.test", "Wrong1!"));
        Assert.AreEqual("invalid_credentials", wrong.ErrorCode);
    }

    [TestMethod]
    [DataRow("DEV_HASH_FIXTURE")]
    [DataRow("broken-hash")]
    public async Task InvalidSeedAndMalformedHash_AreSafelyRejected(string hash)
    {
        var repository = new UserAccountFake { User = new User { UserId = 42, Email = "fixture@example.test", DisplayName = "Fixture", PasswordHash = hash } };
        var result = await Create(repository).LoginAsync(new LoginRequest("fixture@example.test", "Original1!"));
        Assert.AreEqual("invalid_credentials", result.ErrorCode);
    }

    [TestMethod]
    public async Task Register_CreatesSafeUnverifiedAccountAndDuplicateIsRejected()
    {
        var repository = new UserAccountFake();
        var service = Create(repository);
        var request = new RegisterRequest("Fixture", "fixture@example.test", "Original1!", "Original1!");
        var result = await service.RegisterAsync(request);
        Assert.IsTrue(result.Succeeded); Assert.AreEqual(201, result.Status);
        Assert.IsNull(repository.User!.EmailVerifiedAt); Assert.IsFalse(repository.User.IsSystemAdmin);
        Assert.IsTrue(BCrypt.Net.BCrypt.Verify("Original1!", repository.User.PasswordHash));
        Assert.AreEqual("Email", (await service.RegisterAsync(request with { Email = "FIXTURE@example.test" })).ErrorField);
    }

    private static AuthService Create(UserAccountFake repository)
    {
        var verifier = new EmailVerificationService(repository, new CurrentAuthenticatedUser(),
            new EmailOtpCodeProtector(RandomNumberGenerator.GetBytes(32)), new TestEmailSender(), new ImmediateClock());
        return new(repository, verifier, new JwtTokenIssuer(JwtTests.Configuration(), TimeProvider.System));
    }
}
