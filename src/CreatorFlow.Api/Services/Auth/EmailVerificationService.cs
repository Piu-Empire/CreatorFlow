using CreatorFlow.Api.Models.Auth;
using System.Data.Common;

namespace CreatorFlow.Api.Services.Auth;

public sealed class EmailVerificationService
{
    private readonly CreatorFlow.Api.Repositories.Auth.IUserRepository userRepository;
    private readonly CreatorFlow.Api.Authentication.CurrentAuthenticatedUser session;
    private readonly EmailOtpCodeProtector? _resetProtector;
    private readonly IEmailSender? _emailSender;
    private readonly TimeProvider _timeProvider;
    public EmailVerificationService(CreatorFlow.Api.Repositories.Auth.IUserRepository repository, CreatorFlow.Api.Authentication.CurrentAuthenticatedUser userSession,
        EmailOtpCodeProtector? protector, IEmailSender? sender, TimeProvider? clock = null)
    {
        userRepository = repository; session = userSession; _resetProtector = protector;
        _emailSender = sender; _timeProvider = clock ?? TimeProvider.System;
    }

    public const int EmailVerificationResendSeconds = 60;
    private const string VerificationUnavailable = "Xác minh email chưa sẵn sàng. Vui lòng thử lại sau.";
    private const string InvalidVerificationCode = "Mã xác nhận không hợp lệ, đã hết hạn hoặc không còn sử dụng được. Vui lòng yêu cầu mã mới.";
    private const string VerificationDatabaseFailure = "Không thể xử lý lúc này. Vui lòng kiểm tra kết nối và thử lại.";

    public async Task<bool> IsEmailVerificationAvailableAsync(CancellationToken cancellationToken = default)
    {
        if (_resetProtector is null || _emailSender is not { IsConfigured: true } || session.IsAuthenticated) return false;
        try { return await userRepository.IsEmailVerificationSchemaReadyAsync(cancellationToken); }
        catch (Exception exception) when (AuthRules.IsDatabaseError(exception, cancellationToken)) { return false; }
    }

    public Task<PasswordResetResult> ResendEmailVerificationAsync(string? email, CancellationToken cancellationToken = default) =>
        RequestEmailVerificationAsync(email, cancellationToken);

    public async Task<PasswordResetResult> RequestEmailVerificationAsync(string? email, CancellationToken cancellationToken = default)
    {
        if (session.IsAuthenticated) return PasswordResetResult.Failure("Vui lòng đăng xuất hoặc dùng Đổi mật khẩu trong Hồ sơ.");
        string? error = AuthRules.ValidateEmail(email);
        if (error is not null) return PasswordResetResult.Failure(error, "Email");
        if (!await IsEmailVerificationAvailableAsync(cancellationToken)) return PasswordResetResult.Failure(VerificationUnavailable);
        long started = _timeProvider.GetTimestamp();
        Guid requestId = Guid.NewGuid();
        bool reserved = false;
        bool delivered = false;
        try
        {
            string canonicalEmail = await userRepository.NormalizeVerificationEmailAsync(email!.Trim(), cancellationToken);
            EmailVerificationRequest? previous = await userRepository.FindLatestEmailVerificationAsync(canonicalEmail, cancellationToken);
            User? target = await userRepository.FindByEmailAsync(canonicalEmail, cancellationToken);
            if (target is null || target.AccountStatus != CreatorFlow.Api.Models.Auth.AccountStatus.Active || target.EmailVerifiedAt is not null)
                return PasswordResetResult.Failure("Không thể gửi mã xác minh cho yêu cầu này.");
            long? targetId = target?.AccountStatus == CreatorFlow.Api.Models.Auth.AccountStatus.Active ? target.UserId : null;
            string code;
            do { code = EmailOtpCodeProtector.GenerateCode(); }
            while (previous is { VerifierVersion: 2 } && _resetProtector!.Verify(previous.RequestId, previous.UserId, previous.EmailNormalized, EmailOtpCodeProtector.VerificationPurpose, code, previous.OtpVerifier));
            byte[] verifier = _resetProtector!.Protect(requestId, targetId, canonicalEmail, EmailOtpCodeProtector.VerificationPurpose, code);
            reserved = await userRepository.TryCreateEmailVerificationAsync(requestId, canonicalEmail, verifier,
                previous?.RequestId, targetId, cancellationToken);
            if (!reserved) return PasswordResetResult.Failure("Chưa thể gửi lại mã. Chờ ít nhất 60 giây; tối đa 5 yêu cầu trong 1 giờ.");
            if (reserved)
            {
                EmailVerificationRequest? request = await userRepository.FindEmailVerificationAsync(requestId, cancellationToken);
                if (request is { IsActiveAccount: true, UserId: not null, Email: not null })
                {
                    using var delivery = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    delivery.CancelAfter(TimeSpan.FromSeconds(10));
                    try
                    {
                        await _emailSender!.SendEmailVerificationCodeAsync(request.Email, code, delivery.Token);
                        delivered = await userRepository.MarkEmailVerificationDeliveredAsync(requestId, cancellationToken);
                        if (!delivered) await InvalidateUndeliveredVerificationAsync(requestId);
                    }
                    catch (EmailDeliveryException) { await InvalidateUndeliveredVerificationAsync(requestId); }
                    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                    { await InvalidateUndeliveredVerificationAsync(requestId); }
                }
            }
            TimeSpan remaining = TimeSpan.FromSeconds(10) - _timeProvider.GetElapsedTime(started);
            if (remaining > TimeSpan.Zero) await Task.Delay(remaining, _timeProvider, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return new PasswordResetResult(delivered, delivered ? "Mã đã được gửi. Vui lòng kiểm tra email." :
                "Chưa gửi được email xác minh. Bạn có thể thử lại sau 60 giây.", requestId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            if (reserved) await InvalidateUndeliveredVerificationAsync(requestId);
            throw;
        }
        catch (Exception exception) when (AuthRules.IsDatabaseError(exception, cancellationToken))
        {
            if (reserved) await InvalidateUndeliveredVerificationAsync(requestId);
            return PasswordResetResult.Failure(VerificationDatabaseFailure);
        }
    }

    private async Task InvalidateUndeliveredVerificationAsync(Guid requestId)
    {
        // Cleanup has its own bounded token: navigation cancellation must not leave a usable code.
        using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try { await userRepository.InvalidateEmailVerificationAsync(requestId, cleanup.Token); }
        catch (Exception exception) when (exception is DbException or TimeoutException or ArgumentException or OperationCanceledException)
        {
            // An unmarked delivery cannot reset; no plaintext code or DB exception is logged.
        }
    }

    public async Task<PasswordResetResult> VerifyEmailAsync(Guid requestId, string? code,
        CancellationToken cancellationToken = default)
    {
        if (session.IsAuthenticated) return PasswordResetResult.Failure("Vui lòng đăng xuất trước.");
        if (string.IsNullOrWhiteSpace(code)) return PasswordResetResult.Failure("Vui lòng nhập mã xác nhận.", "Code");
        if (!await IsEmailVerificationAvailableAsync(cancellationToken)) return PasswordResetResult.Failure(VerificationUnavailable);
        try
        {
            EmailVerificationRequest? request = await userRepository.FindEmailVerificationAsync(requestId, cancellationToken);
            if (request is null || request.VerifierVersion != 2 || request.Purpose != EmailOtpCodeProtector.VerificationPurpose ||
                !request.IsActiveAccount || request.UserId is null || request.DeliveredAt is null ||
                request.InvalidatedAt is not null || request.ConsumedAt is not null || request.FailedAttempts >= 5 ||
                request.ExpiresAt <= request.DatabaseNow)
                return PasswordResetResult.Failure(InvalidVerificationCode, "Code");
            if (!EmailOtpCodeProtector.IsCodeFormatValid(code) ||
                !_resetProtector!.Verify(requestId, request.UserId, request.EmailNormalized,
                    EmailOtpCodeProtector.VerificationPurpose, code, request.OtpVerifier))
            {
                await userRepository.RecordEmailVerificationFailureAsync(requestId, request.OtpVerifier, cancellationToken);
                return PasswordResetResult.Failure(InvalidVerificationCode, "Code");
            }
            bool saved = await userRepository.CompleteEmailVerificationAsync(requestId, request.OtpVerifier, cancellationToken);
            return saved ? new PasswordResetResult(true, "Email đã được xác minh. Vui lòng đăng nhập.")
                : PasswordResetResult.Failure(InvalidVerificationCode, "Code");
        }
        catch (Exception exception) when (AuthRules.IsDatabaseError(exception, cancellationToken))
        { return PasswordResetResult.Failure(VerificationDatabaseFailure); }
    }
}
