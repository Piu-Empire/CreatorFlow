namespace CreatorFlow.Api.Services.Auth;

internal sealed class UnavailableEmailSender : IEmailSender
{
    public bool IsConfigured => false;
    public Task SendEmailVerificationCodeAsync(string email, string code, CancellationToken cancellationToken = default) => throw new EmailDeliveryException();
    public Task SendPasswordResetCodeAsync(string email, string code, CancellationToken cancellationToken = default) => throw new EmailDeliveryException();
}
