namespace CreatorFlow.Api.Services.Auth;

internal sealed class UnavailableEmailSender : IEmailSender
{
    public bool IsConfigured => false;
    public Task SendEmailVerificationCodeAsync(string email, string code, CancellationToken cancellationToken = default) => throw new EmailDeliveryException();
    public Task SendPasswordResetCodeAsync(string email, string code, CancellationToken cancellationToken = default) => throw new EmailDeliveryException();
    public Task SendProjectInvitationAsync(string email, long projectId, string role, string inviterName,
        DateTime expiresAtUtc, CancellationToken cancellationToken = default) => throw new EmailDeliveryException();
    public Task SendNotificationAsync(string email, string title, string message,
        CancellationToken cancellationToken = default) => throw new EmailDeliveryException();
}
