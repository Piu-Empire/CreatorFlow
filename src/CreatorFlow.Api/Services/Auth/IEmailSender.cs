namespace CreatorFlow.Api.Services.Auth;

public interface IEmailSender
{
    bool IsConfigured { get; }
    Task SendEmailVerificationCodeAsync(string email, string code, CancellationToken cancellationToken = default);
    Task SendPasswordResetCodeAsync(string email, string code, CancellationToken cancellationToken = default);
}

public sealed class EmailDeliveryException() : Exception("Không thể gửi email xác nhận lúc này.") { }
