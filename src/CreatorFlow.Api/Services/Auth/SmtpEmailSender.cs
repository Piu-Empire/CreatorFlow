using CreatorFlow.Api.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace CreatorFlow.Api.Services.Auth;

public sealed class SmtpEmailSender(PasswordResetConfiguration configuration) : IEmailSender
{
    public bool IsConfigured => true;

    public Task SendPasswordResetCodeAsync(string email, string code, CancellationToken cancellationToken = default) =>
        SendCodeAsync(email, code, false, cancellationToken);
    public Task SendEmailVerificationCodeAsync(string email, string code, CancellationToken cancellationToken = default) =>
        SendCodeAsync(email, code, true, cancellationToken);

    public Task SendProjectInvitationAsync(string email, long projectId, string role, string inviterName,
        DateTime expiresAtUtc, CancellationToken cancellationToken = default)
    {
        var message = CreateMessage(email, $"CreatorFlow · Lời mời tham gia Project #{projectId}",
            $"Xin chào,\n\n{inviterName} đã mời bạn tham gia Project #{projectId} trong CreatorFlow với vai trò {role}.\n" +
            $"Lời mời có hiệu lực đến {expiresAtUtc:yyyy-MM-dd HH:mm} UTC.\n\n" +
            "Để phản hồi, hãy mở ứng dụng CreatorFlow, đăng nhập bằng địa chỉ email này và chọn mục Lời mời.\n\n" +
            "Nếu bạn không mong đợi lời mời này, bạn có thể bỏ qua email.");
        return SendMessageAsync(message, cancellationToken);
    }

    private async Task SendCodeAsync(string email, string code, bool verification, CancellationToken cancellationToken)
    {
        var message = CreateMessage(email,
            verification ? "CreatorFlow · Xác minh email" : "CreatorFlow · Mã đặt lại mật khẩu",
            $"Mã xác nhận của bạn: {code}\nMã hết hạn sau 10 phút. Không chia sẻ mã này.\n" +
            "Nếu bạn không yêu cầu mã này, hãy bỏ qua email này.");
        await SendMessageAsync(message, cancellationToken);
    }

    private MimeMessage CreateMessage(string email, string subject, string body)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(configuration.From));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };
        return message;
    }

    private async Task SendMessageAsync(MimeMessage message, CancellationToken cancellationToken)
    {
        // No protocol logger: SMTP transcripts can contain the code and credentials.
        using var client = new SmtpClient { Timeout = 10_000 };
        try
        {
            SecureSocketOptions mode = configuration.TlsMode == "StartTls"
                ? SecureSocketOptions.StartTls : SecureSocketOptions.SslOnConnect;
            await client.ConnectAsync(configuration.Host, configuration.Port, mode, cancellationToken);
            client.AuthenticationMechanisms.Remove("XOAUTH2");
            client.AuthenticationMechanisms.Remove("OAUTHBEARER");
            await client.AuthenticateAsync(configuration.Username, configuration.Password, cancellationToken);
            await client.SendAsync(message, cancellationToken);
        }
        catch (Exception exception) when (exception is MailKit.CommandException or MailKit.ProtocolException or
            IOException or System.Net.Sockets.SocketException or MailKit.Security.AuthenticationException or
            System.Security.Authentication.AuthenticationException or MailKit.ServiceNotAuthenticatedException or
            MailKit.ServiceNotConnectedException or NotSupportedException)
        {
            throw new EmailDeliveryException();
        }
    }
}
