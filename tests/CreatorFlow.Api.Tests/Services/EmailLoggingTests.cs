using System.Security.Cryptography;
using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Models.Auth;
using CreatorFlow.Api.Services.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.Api.Tests.Services;

[TestClass]
[TestCategory("AuthApiUnit")]
public sealed class EmailLoggingTests
{
    [TestMethod]
    public async Task FailedVerification_LogsCategoryWithoutEmailOrOtp_AndKeepsRequestUnusable()
    {
        var repository = Repository();
        var logger = new RecordingLogger<EmailVerificationService>();
        var service = new EmailVerificationService(repository, new CurrentAuthenticatedUser(),
            new EmailOtpCodeProtector(RandomNumberGenerator.GetBytes(32)), new TestEmailSender { Fail = true }, new ImmediateClock(), logger);
        var result = await service.RequestEmailVerificationAsync(repository.User!.Email);
        Assert.IsFalse(result.Succeeded);
        Assert.IsNotNull(repository.VerificationRequests[result.RequestId!.Value].InvalidatedAt);
        CollectionAssert.AreEqual(new[] { "Email provider delivery failed. Operation: EmailVerification" }, logger.Messages);
        Assert.IsFalse(logger.HasException);
    }

    [TestMethod]
    public async Task FailedReset_LogsCategoryButKeepsGenericResponseAndNoDeliveredCode()
    {
        var repository = Repository();
        var logger = new RecordingLogger<PasswordResetService>();
        var service = new PasswordResetService(repository, new CurrentAuthenticatedUser(),
            new EmailOtpCodeProtector(RandomNumberGenerator.GetBytes(32)), new TestEmailSender { Fail = true }, new ImmediateClock(), logger);
        var result = await service.RequestPasswordResetAsync(repository.User!.Email);
        Assert.IsTrue(result.Succeeded);
        var request = repository.ResetRequests[result.RequestId!.Value];
        Assert.IsNull(request.DeliveredAt);
        Assert.IsNotNull(request.InvalidatedAt);
        CollectionAssert.AreEqual(new[] { "Email provider delivery failed. Operation: PasswordReset" }, logger.Messages);
        Assert.IsFalse(logger.HasException);
    }

    private static UserAccountFake Repository() => new()
    {
        User = new User { UserId = 42, DisplayName = "Fixture", Email = "private-mail@example.test",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Original1!"), AccountStatus = AccountStatus.Active }
    };

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];
        public bool HasException { get; private set; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
            HasException |= exception is not null;
        }
    }
}
