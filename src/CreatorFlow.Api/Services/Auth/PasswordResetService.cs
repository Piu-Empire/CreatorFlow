using CreatorFlow.Api.Models.Auth;
using System.Data.Common;

namespace CreatorFlow.Api.Services.Auth;

public sealed class PasswordResetService
{
    private readonly CreatorFlow.Api.Repositories.Auth.IUserRepository userRepository;
    private readonly CreatorFlow.Api.Authentication.CurrentAuthenticatedUser session;
    private readonly EmailOtpCodeProtector? _resetProtector;
    private readonly IEmailSender? _emailSender;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<PasswordResetService> _logger;
    public PasswordResetService(CreatorFlow.Api.Repositories.Auth.IUserRepository repository, CreatorFlow.Api.Authentication.CurrentAuthenticatedUser userSession,
        EmailOtpCodeProtector? protector, IEmailSender? sender, TimeProvider? clock = null, ILogger<PasswordResetService>? logger = null)
    {
        userRepository = repository; session = userSession; _resetProtector = protector;
        _emailSender = sender; _timeProvider = clock ?? TimeProvider.System;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<PasswordResetService>.Instance;
    }

    public const int PasswordResetResendSeconds = 60;
    private const string ResetUnavailable = "Khôi phục mật khẩu chưa sẵn sàng. Vui lòng thử lại sau.";
    private const string ResetRequestMessage = "Nếu tài khoản đủ điều kiện, bạn sẽ nhận được mã xác nhận qua email. Mã có hiệu lực 10 phút.";
    private const string InvalidResetCode = "Mã xác nhận không hợp lệ, đã hết hạn hoặc không còn sử dụng được. Vui lòng yêu cầu mã mới.";
    private const string ResetDatabaseFailure = "Không thể xử lý lúc này. Vui lòng kiểm tra kết nối và thử lại.";

    public async Task<bool> IsPasswordResetAvailableAsync(CancellationToken cancellationToken = default)
    {
        if (_resetProtector is null || _emailSender is not { IsConfigured: true } || session.IsAuthenticated) return false;
        try { return await userRepository.IsPasswordResetSchemaReadyAsync(cancellationToken); }
        catch (Exception exception) when (AuthRules.IsDatabaseError(exception, cancellationToken))
        {
            _logger.LogWarning("Auth database readiness failed. FailureType: {FailureType}", exception.GetType().Name);
            return false;
        }
    }

    public Task<PasswordResetResult> ResendPasswordResetAsync(string? email, CancellationToken cancellationToken = default) =>
        RequestPasswordResetAsync(email, cancellationToken);

    public async Task<PasswordResetResult> RequestPasswordResetAsync(string? email, CancellationToken cancellationToken = default)
    {
        if (session.IsAuthenticated) return PasswordResetResult.Failure("Vui lòng đăng xuất hoặc dùng Đổi mật khẩu trong Hồ sơ.");
        string? error = AuthRules.ValidateEmail(email);
        if (error is not null) return PasswordResetResult.Failure(error, "Email");
        if (!await IsPasswordResetAvailableAsync(cancellationToken)) return PasswordResetResult.Failure(ResetUnavailable);
        long started = _timeProvider.GetTimestamp();
        Guid requestId = Guid.NewGuid();
        bool reserved = false;
        try
        {
            string canonicalEmail = await userRepository.NormalizeResetEmailAsync(email!.Trim(), cancellationToken);
            PasswordResetRequest? previous = await userRepository.FindLatestPasswordResetAsync(canonicalEmail, cancellationToken);
            User? target = await userRepository.FindByEmailAsync(canonicalEmail, cancellationToken);
            long? targetId = target?.AccountStatus == CreatorFlow.Api.Models.Auth.AccountStatus.Active ? target.UserId : null;
            string code;
            do { code = EmailOtpCodeProtector.GenerateCode(); }
            while (previous is { VerifierVersion: 2 } && _resetProtector!.Verify(previous.RequestId, previous.UserId, previous.EmailNormalized, EmailOtpCodeProtector.ResetPurpose, code, previous.OtpVerifier));
            byte[] verifier = _resetProtector!.Protect(requestId, targetId, canonicalEmail, EmailOtpCodeProtector.ResetPurpose, code);
            reserved = await userRepository.TryCreatePasswordResetAsync(requestId, canonicalEmail, verifier,
                previous?.RequestId, targetId, cancellationToken);
            if (reserved)
            {
                PasswordResetRequest? request = await userRepository.FindPasswordResetAsync(requestId, cancellationToken);
                if (request is { IsActiveAccount: true, UserId: not null, Email: not null })
                {
                    using var delivery = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    delivery.CancelAfter(TimeSpan.FromSeconds(10));
                    try
                    {
                        await _emailSender!.SendPasswordResetCodeAsync(request.Email, code, delivery.Token);
                        if (!await userRepository.MarkPasswordResetDeliveredAsync(requestId, cancellationToken))
                            await InvalidateUndeliveredResetAsync(requestId);
                    }
                    catch (EmailDeliveryException)
                    {
                        _logger.LogWarning("Email provider delivery failed. Operation: PasswordReset");
                        await InvalidateUndeliveredResetAsync(requestId);
                    }
                    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogWarning("Email provider delivery timed out. Operation: PasswordReset");
                        await InvalidateUndeliveredResetAsync(requestId);
                    }
                }
            }
            TimeSpan remaining = TimeSpan.FromSeconds(10) - _timeProvider.GetElapsedTime(started);
            if (remaining > TimeSpan.Zero) await Task.Delay(remaining, _timeProvider, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return new PasswordResetResult(true, ResetRequestMessage, requestId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            if (reserved) await InvalidateUndeliveredResetAsync(requestId);
            throw;
        }
        catch (Exception exception) when (AuthRules.IsDatabaseError(exception, cancellationToken))
        {
            if (reserved) await InvalidateUndeliveredResetAsync(requestId);
            return PasswordResetResult.Failure(ResetDatabaseFailure);
        }
    }

    private async Task InvalidateUndeliveredResetAsync(Guid requestId)
    {
        // Cleanup has its own bounded token: navigation cancellation must not leave a usable code.
        using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try { await userRepository.InvalidatePasswordResetAsync(requestId, cleanup.Token); }
        catch (Exception exception) when (exception is DbException or TimeoutException or ArgumentException or OperationCanceledException)
        {
            // An unmarked delivery cannot reset; no plaintext code or DB exception is logged.
        }
    }

    public async Task<PasswordResetResult> ResetPasswordAsync(Guid requestId, string? code,
        string? newPassword, string? confirmPassword, CancellationToken cancellationToken = default)
    {
        if (session.IsAuthenticated) return PasswordResetResult.Failure("Vui lòng đăng xuất hoặc dùng Đổi mật khẩu trong Hồ sơ.");
        if (string.IsNullOrWhiteSpace(code)) return PasswordResetResult.Failure("Vui lòng nhập mã xác nhận.", "Code");
        string? error = PasswordPolicy.Validate(newPassword);
        if (error is not null) return PasswordResetResult.Failure(error, "NewPassword");
        if (newPassword != confirmPassword) return PasswordResetResult.Failure("Mật khẩu xác nhận không khớp.", "ConfirmPassword");
        if (!await IsPasswordResetAvailableAsync(cancellationToken)) return PasswordResetResult.Failure(ResetUnavailable);
        try
        {
            PasswordResetRequest? request = await userRepository.FindPasswordResetAsync(requestId, cancellationToken);
            if (request is null || request.VerifierVersion != 2 || request.Purpose != EmailOtpCodeProtector.ResetPurpose || !request.IsActiveAccount || request.UserId is null || request.PasswordHash is null ||
                request.DeliveredAt is null || request.InvalidatedAt is not null || request.ConsumedAt is not null ||
                request.FailedAttempts >= 5 || request.ExpiresAt <= request.DatabaseNow)
                return PasswordResetResult.Failure(InvalidResetCode, "Code");
            if (!EmailOtpCodeProtector.IsCodeFormatValid(code) || !_resetProtector!.Verify(requestId, request.UserId, request.EmailNormalized, EmailOtpCodeProtector.ResetPurpose, code, request.OtpVerifier))
            {
                await userRepository.RecordPasswordResetFailureAsync(requestId, request.OtpVerifier, cancellationToken);
                return PasswordResetResult.Failure(InvalidResetCode, "Code");
            }
            if (await Task.Run(() => AuthRules.VerifyPassword(newPassword!, request.PasswordHash), cancellationToken))
                return PasswordResetResult.Failure("Mật khẩu mới phải khác mật khẩu hiện tại.", "NewPassword");
            string hash = await Task.Run(() => BCrypt.Net.BCrypt.HashPassword(newPassword!), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            bool saved = await userRepository.CompletePasswordResetAsync(requestId, request.OtpVerifier,
                request.PasswordHash, hash, cancellationToken);
            // Do not turn a successfully committed reset into a cancellation/failure response.
            return saved ? new PasswordResetResult(true, "Đã đặt lại mật khẩu. Vui lòng đăng nhập bằng mật khẩu mới.")
                : PasswordResetResult.Failure(InvalidResetCode, "Code");
        }
        catch (Exception exception) when (AuthRules.IsDatabaseError(exception, cancellationToken))
        { return PasswordResetResult.Failure(ResetDatabaseFailure); }
    }
}
