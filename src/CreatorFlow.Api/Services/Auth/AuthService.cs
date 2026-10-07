using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Models.Auth;
using CreatorFlow.Api.Repositories.Auth;
using CreatorFlow.Contracts.Auth;
using Npgsql;

namespace CreatorFlow.Api.Services.Auth;

public sealed class AuthService(IUserRepository repository, EmailVerificationService verification, JwtTokenIssuer issuer)
{
    public async Task<AuthOperationResult<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken token = default)
    {
        string? error = AuthRules.ValidateDisplayName(request.DisplayName);
        if (error is not null) return AuthOperationResult<RegisterResponse>.Fail("validation", error, "DisplayName");
        error = AuthRules.ValidateEmail(request.Email);
        if (error is not null) return AuthOperationResult<RegisterResponse>.Fail("validation", error, "Email");
        error = PasswordPolicy.Validate(request.Password);
        if (error is not null) return AuthOperationResult<RegisterResponse>.Fail("validation", error, "Password");
        if (request.Password != request.ConfirmPassword)
            return AuthOperationResult<RegisterResponse>.Fail("validation", "Mật khẩu xác nhận không khớp.", "ConfirmPassword");
        string email = request.Email!.Trim();
        try
        {
            if (await repository.FindByEmailAsync(email, token) is not null)
                return AuthOperationResult<RegisterResponse>.Fail("email_in_use", "Email đã được sử dụng.", "Email", 409);
            if (!await verification.IsEmailVerificationAvailableAsync(token))
                return AuthOperationResult<RegisterResponse>.Fail("email_unavailable", "Xác minh email chưa sẵn sàng.", status: 503);
            string hash = await Task.Run(() => BCrypt.Net.BCrypt.HashPassword(request.Password!), token);
            token.ThrowIfCancellationRequested();
            await repository.CreateAsync(request.DisplayName!.Trim(), email, hash, token);
            return new(new RegisterResponse(email, true), Status: 201);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation && exception.ConstraintName == "uq_users_email_lower")
        { return AuthOperationResult<RegisterResponse>.Fail("email_in_use", "Email đã được sử dụng.", "Email", 409); }
        catch (Exception exception) when (AuthRules.IsDatabaseError(exception, token))
        { return AuthOperationResult<RegisterResponse>.Fail("service_unavailable", "Không thể tạo tài khoản lúc này.", status: 503); }
    }

    public async Task<AuthOperationResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email)) return AuthOperationResult<LoginResponse>.Fail("validation", "Vui lòng nhập email.", "Email");
        if (string.IsNullOrWhiteSpace(request.Password)) return AuthOperationResult<LoginResponse>.Fail("validation", "Vui lòng nhập mật khẩu.", "Password");
        try
        {
            User? user = await repository.FindByEmailAsync(request.Email.Trim(), token);
            if (user is null || !await Task.Run(() => AuthRules.VerifyPassword(request.Password, user.PasswordHash), token))
                return AuthOperationResult<LoginResponse>.Fail("invalid_credentials", "Email hoặc mật khẩu không đúng", status: 401);
            token.ThrowIfCancellationRequested();
            if (user.AccountStatus != AccountStatus.Active)
                return AuthOperationResult<LoginResponse>.Fail("account_ineligible", "Tài khoản đã bị khóa hoặc vô hiệu hóa.", status: 403);
            if (user.EmailVerifiedAt is null)
                return AuthOperationResult<LoginResponse>.Fail("email_verification_required", "Vui lòng xác minh email trước khi đăng nhập.", status: 403);
            User? snapshot = await repository.RecordSuccessfulLoginAsync(user.UserId, user.PasswordHash, user.TokenVersion, token);
            if (snapshot is null)
                return AuthOperationResult<LoginResponse>.Fail("invalid_credentials", "Email hoặc mật khẩu không đúng", status: 401);
            token.ThrowIfCancellationRequested();
            return new(issuer.Issue(snapshot));
        }
        catch (Exception exception) when (AuthRules.IsDatabaseError(exception, token))
        { return AuthOperationResult<LoginResponse>.Fail("service_unavailable", "Không thể đăng nhập lúc này.", status: 503); }
    }
}
