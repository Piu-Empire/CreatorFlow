namespace CreatorFlow.Api.Services.Auth;

public interface IEmailSender
{
    bool IsConfigured { get; }
    Task SendEmailVerificationCodeAsync(string email, string code, CancellationToken cancellationToken = default);
    Task SendPasswordResetCodeAsync(string email, string code, CancellationToken cancellationToken = default);
    Task SendProjectInvitationAsync(string email, long projectId, string role, string inviterName,
        DateTime expiresAtUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gửi nội dung một thông báo nghiệp vụ (SCRUM-50). Chỉ gọi khi <see cref="IsConfigured"/> là true;
    /// đây là hiệu ứng phụ nên nơi gọi phải chịu được lỗi gửi mail.
    /// </summary>
    Task SendNotificationAsync(string email, string title, string message,
        CancellationToken cancellationToken = default);
}

public sealed class EmailDeliveryException() : Exception("Không thể gửi email xác nhận lúc này.") { }
