using System.Security.Cryptography;
using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Models.Auth;
using CreatorFlow.Api.Services.Auth;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.Api.Tests.Services;

[TestClass]
[TestCategory("AuthApiUnit")]
public sealed class EmailFlowTests
{
    [TestMethod]
    public async Task Verification_WrongAttemptsExpiryAndResend_RespectRequestState()
    {
        var repository = Repository(); var sender = new TestEmailSender(); var service = Verification(repository, sender);
        var first = await service.RequestEmailVerificationAsync(repository.User!.Email);
        Guid id = first.RequestId!.Value;
        string oldCode = sender.VerificationCode!;
        Assert.IsFalse((await service.RequestEmailVerificationAsync(repository.User.Email)).Succeeded);
        for (int attempt = 0; attempt < 5; attempt++)
            Assert.IsFalse((await service.VerifyEmailAsync(id, oldCode == "000000" ? "000001" : "000000")).Succeeded);
        Assert.AreEqual(5, repository.VerificationRequests[id].FailedAttempts);
        Assert.IsFalse((await service.VerifyEmailAsync(id, oldCode)).Succeeded);
        repository.Now = repository.Now.AddSeconds(61);
        var next = await service.RequestEmailVerificationAsync(repository.User.Email);
        Assert.IsTrue(next.Succeeded);
        Assert.IsFalse((await service.VerifyEmailAsync(id, oldCode)).Succeeded);
        repository.Now = repository.Now.AddMinutes(11);
        Assert.IsFalse((await service.VerifyEmailAsync(next.RequestId!.Value, sender.VerificationCode)).Succeeded);
        Assert.IsNull(repository.User.EmailVerifiedAt);
    }

    [TestMethod]
    public async Task VerificationDeliveryAndCommitFailure_DoNotVerifyUser()
    {
        var repository = Repository(); var sender = new TestEmailSender { Fail = true }; var service = Verification(repository, sender);
        var failed = await service.RequestEmailVerificationAsync(repository.User!.Email);
        Assert.IsFalse(failed.Succeeded); Assert.IsNull(repository.User.EmailVerifiedAt);
        Assert.IsNull(repository.VerificationRequests[failed.RequestId!.Value].DeliveredAt);
        sender.Fail = false; repository.Now = repository.Now.AddSeconds(61);
        var next = await service.RequestEmailVerificationAsync(repository.User.Email);
        repository.FailVerificationCommit = true;
        Assert.IsFalse((await service.VerifyEmailAsync(next.RequestId!.Value, sender.VerificationCode)).Succeeded);
        Assert.IsNull(repository.User.EmailVerifiedAt);
    }

    [TestMethod]
    public async Task Reset_UnverifiedActiveCanRecoverButCannotBecomeVerified_AndConsumesOnce()
    {
        var repository = Repository(); var sender = new TestEmailSender(); var reset = Reset(repository, sender);
        var requested = await reset.RequestPasswordResetAsync(repository.User!.Email);
        var saved = await reset.ResetPasswordAsync(requested.RequestId!.Value, sender.ResetCode, "Newpass2!", "Newpass2!");
        Assert.IsTrue(saved.Succeeded); Assert.IsNull(repository.User.EmailVerifiedAt);
        Assert.IsFalse(BCrypt.Net.BCrypt.Verify("Original1!", repository.User.PasswordHash));
        Assert.IsTrue(BCrypt.Net.BCrypt.Verify("Newpass2!", repository.User.PasswordHash));
        Assert.AreEqual(1L, repository.User.TokenVersion);
        Assert.IsFalse((await reset.ResetPasswordAsync(requested.RequestId.Value, sender.ResetCode, "Otherpass3!", "Otherpass3!")).Succeeded);
    }

    [TestMethod]
    [DataRow(AccountStatus.Locked)]
    [DataRow(AccountStatus.Disabled)]
    public async Task Reset_UnknownAndInactiveResponsesAreGenericWithoutDelivery(AccountStatus status)
    {
        var repository = Repository(status); var sender = new TestEmailSender(); var reset = Reset(repository, sender);
        var inactive = await reset.RequestPasswordResetAsync(repository.User!.Email);
        var unknown = await reset.RequestPasswordResetAsync("unknown@example.test");
        Assert.IsTrue(inactive.Succeeded); Assert.IsTrue(unknown.Succeeded); Assert.AreEqual(inactive.Message, unknown.Message);
        Assert.IsNull(sender.ResetCode);
        Assert.IsFalse((await reset.ResetPasswordAsync(inactive.RequestId!.Value, "000000", "Newpass2!", "Newpass2!")).Succeeded);
    }

    [TestMethod]
    public async Task Reset_WrongAttemptsExpiredCodeResendAndQuota_AreEnforced()
    {
        var repository = Repository(); var sender = new TestEmailSender(); var service = Reset(repository, sender);
        var first = await service.RequestPasswordResetAsync(repository.User!.Email);
        Guid id = first.RequestId!.Value; string original = sender.ResetCode!;
        for (int i = 0; i < 5; i++) Assert.IsFalse((await service.ResetPasswordAsync(id, original == "000000" ? "000001" : "000000", "Newpass2!", "Newpass2!")).Succeeded);
        Assert.AreEqual(5, repository.ResetRequests[id].FailedAttempts);
        Assert.IsFalse((await service.ResetPasswordAsync(id, original, "Newpass2!", "Newpass2!")).Succeeded);
        for (int i = 1; i < 5; i++) { repository.Now = repository.Now.AddSeconds(61); await service.RequestPasswordResetAsync(repository.User.Email); }
        int rows = repository.ResetRequests.Count;
        repository.Now = repository.Now.AddSeconds(61);
        await service.RequestPasswordResetAsync(repository.User.Email);
        Assert.AreEqual(rows, repository.ResetRequests.Count);
        var latest = (await repository.FindLatestPasswordResetAsync(repository.User.Email))!;
        repository.Now = repository.Now.AddMinutes(11);
        Assert.IsFalse((await service.ResetPasswordAsync(latest.RequestId, sender.ResetCode, "Newpass2!", "Newpass2!")).Succeeded);
    }

    private static UserAccountFake Repository(AccountStatus status = AccountStatus.Active) => new()
    { User = new User { UserId = 42, DisplayName = "Fixture", Email = "fixture@example.test", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Original1!"), AccountStatus = status } };
    private static EmailVerificationService Verification(UserAccountFake repository, TestEmailSender sender) =>
        new(repository, new CurrentAuthenticatedUser(), new EmailOtpCodeProtector(RandomNumberGenerator.GetBytes(32)), sender, new ImmediateClock());
    private static PasswordResetService Reset(UserAccountFake repository, TestEmailSender sender) =>
        new(repository, new CurrentAuthenticatedUser(), new EmailOtpCodeProtector(RandomNumberGenerator.GetBytes(32)), sender, new ImmediateClock());
}
