using System.Net;
using System.Net.Http;
using CreatorFlow.ApiClients;
using CreatorFlow.Contracts.Auth;
using CreatorFlow.Contracts.Users;
using CreatorFlow.Models;

namespace CreatorFlow.Services;

// UI adapter only: backend owns validation, hashing, OTP and persistence.
public sealed class AuthApiFacade(ApiClient api, UserSession session)
{
    public UserSession Session => session;

    public async Task<bool> IsEmailVerificationAvailableAsync(CancellationToken token = default) =>
        (await CapabilitiesAsync(token))?.EmailVerificationAvailable == true;
    public async Task<bool> IsPasswordResetAvailableAsync(CancellationToken token = default) =>
        (await CapabilitiesAsync(token))?.PasswordResetAvailable == true;
    private async Task<AuthCapabilitiesResponse?> CapabilitiesAsync(CancellationToken token)
    {
        try { return await api.SendJsonAsync<AuthCapabilitiesResponse>(HttpMethod.Get, "api/auth/capabilities", null, token: token); }
        catch (Exception exception) when (exception is ApiException or ApiBusinessException) { return null; }
    }

    public async Task<LoginResult> LoginAsync(string? email, string? password, CancellationToken token = default)
    {
        long generation = session.Generation;
        try
        {
            var response = await api.SendJsonAsync<LoginResponse>(HttpMethod.Post, "api/auth/login", new LoginRequest(email, password), token: token);
            token.ThrowIfCancellationRequested();
            return session.TrySetLogin(response, generation) ? LoginResult.Success() : LoginResult.Failure("Đăng nhập đã bị hủy.");
        }
        catch (ApiBusinessException exception)
        {
            if (exception.Code == "email_verification_required") return LoginResult.NeedsVerification(email?.Trim() ?? string.Empty);
            return LoginResult.Failure(exception.Message);
        }
        catch (ApiException exception) { return LoginResult.Failure(exception.Message); }
    }

    public async Task<UserOperationResult> RegisterAsync(string? name, string? email, string? password, string? confirm, CancellationToken token = default)
    {
        try
        {
            await api.SendJsonAsync<RegisterResponse>(HttpMethod.Post, "api/auth/register", new RegisterRequest(name, email, password, confirm), token: token);
            return UserOperationResult.Success();
        }
        catch (ApiBusinessException exception) { return UserOperationResult.Failure(exception.Message, exception.ErrorField); }
        catch (ApiException exception) { return UserOperationResult.Failure(exception.Message); }
    }

    public async Task<PasswordResetResult> RequestEmailVerificationAsync(string? email, CancellationToken token = default)
    {
        try
        {
            var response = await api.SendJsonAsync<VerificationRequestResponse>(HttpMethod.Post, "api/auth/email-verification/requests", new EmailCodeRequest(email), token: token);
            return new(true, response.Message, response.RequestId);
        }
        catch (ApiBusinessException exception) { return new(false, exception.Message, exception.RequestId, exception.ErrorField); }
        catch (ApiException exception) { return PasswordResetResult.Failure(exception.Message); }
    }

    public async Task<PasswordResetResult> RequestPasswordResetAsync(string? email, CancellationToken token = default)
    {
        try
        {
            var response = await api.SendJsonAsync<ResetRequestResponse>(HttpMethod.Post, "api/auth/password-reset/requests", new EmailCodeRequest(email), token: token);
            return new(true, response.Message, response.RequestId);
        }
        catch (ApiBusinessException exception) { return PasswordResetResult.Failure(exception.Message, exception.ErrorField); }
        catch (ApiException exception) { return PasswordResetResult.Failure(exception.Message); }
    }

    public Task<PasswordResetResult> ResendPasswordResetAsync(string? email, CancellationToken token = default) => RequestPasswordResetAsync(email, token);

    public Task<PasswordResetResult> VerifyEmailAsync(Guid id, string? code, CancellationToken token = default) =>
        ConfirmAsync("api/auth/email-verification/confirm", new VerifyEmailRequest(id, code), "Email đã xác minh. Vui lòng đăng nhập.", token);
    public Task<PasswordResetResult> ResetPasswordAsync(Guid id, string? code, string? password, string? confirm, CancellationToken token = default) =>
        ConfirmAsync("api/auth/password-reset/confirm", new ResetPasswordRequest(id, code, password, confirm), "Mật khẩu đã cập nhật. Vui lòng đăng nhập.", token);
    private async Task<PasswordResetResult> ConfirmAsync(string path, object request, string message, CancellationToken token)
    {
        try { await api.SendJsonAsync<ReloginResponse>(HttpMethod.Post, path, request, token: token); return new(true, message); }
        catch (ApiBusinessException exception) { return PasswordResetResult.Failure(exception.Message, exception.ErrorField); }
        catch (ApiException exception) { return PasswordResetResult.Failure(exception.Message); }
    }

    private (string Token, long Generation)? Credentials()
    {
        if (session.IsAuthenticated) return (session.AccessToken!, session.Generation);
        session.Clear();
        return null;
    }
    private void HandleUnauthorized(ApiBusinessException exception, long generation)
    {
        if (exception.StatusCode == HttpStatusCode.Unauthorized && generation == session.Generation) session.Clear();
    }

    public async Task<UserOperationResult> GetCurrentProfileAsync(CancellationToken token = default)
    {
        var credentials = Credentials();
        if (credentials is null) return UserOperationResult.Failure("Vui lòng đăng nhập lại.");
        try
        {
            var profile = await api.SendJsonAsync<ProfileResponse>(HttpMethod.Get, "api/users/me", null, credentials.Value.Token, token);
            return credentials.Value.Generation == session.Generation ? UserOperationResult.Success(ToProfile(profile)) : UserOperationResult.Failure("Phiên đã kết thúc.");
        }
        catch (ApiBusinessException exception) { HandleUnauthorized(exception, credentials.Value.Generation); return UserOperationResult.Failure(exception.Message, exception.ErrorField); }
        catch (ApiException exception) { return UserOperationResult.Failure(exception.Message); }
    }

    public async Task<UserAvatar?> GetCurrentAvatarAsync(CancellationToken token = default)
    {
        var credentials = Credentials();
        if (credentials is null) return null;
        try
        {
            byte[]? bytes = await api.GetAvatarAsync(credentials.Value.Token, token);
            if (bytes is null || credentials.Value.Generation != session.Generation) return null;
            using var image = CreatorFlow.Helpers.AvatarImageLoader.Decode(bytes);
            return image is null ? null : new(bytes, image.Width, image.Height);
        }
        catch (ApiBusinessException exception) { HandleUnauthorized(exception, credentials.Value.Generation); return null; }
        catch (ApiException) { return null; }
    }

    public Task<AvatarPreviewResult> PreviewAvatarAsync(byte[] bytes, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        try
        {
            using var stream = new MemoryStream(bytes, writable: false);
            using Image image = Image.FromStream(stream, false, true);
            // Local preview is rendering only. Original bytes are validated/normalized by the backend on Save.
            return Task.FromResult(new AvatarPreviewResult(new UserAvatar(bytes, image.Width, image.Height), null));
        }
        catch (Exception exception) when (exception is ArgumentException or System.Runtime.InteropServices.ExternalException or OutOfMemoryException)
        { return Task.FromResult(new AvatarPreviewResult(null, "Không thể xem trước ảnh này.")); }
    }

    public async Task<UserOperationResult> SaveCurrentProfileAvatarAsync(string? name, string? url, AvatarChange action, byte[]? bytes, CancellationToken token = default)
    {
        var credentials = Credentials();
        if (credentials is null) return UserOperationResult.Failure("Vui lòng đăng nhập lại.");
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(name ?? string.Empty), "displayName");
        content.Add(new StringContent(url ?? string.Empty), "avatarUrl");
        content.Add(new StringContent(action.ToString()), "avatarAction");
        if (action == AvatarChange.Upload && bytes is not null) content.Add(new ByteArrayContent(bytes), "image", "avatar");
        try
        {
            var response = await api.SendMultipartAsync<ProfileResponse>("api/users/me/profile", content, credentials.Value.Token, token);
            if (credentials.Value.Generation != session.Generation) return UserOperationResult.Failure("Phiên đã kết thúc.");
            session.UpdateDisplayName(response.DisplayName, credentials.Value.Generation);
            return UserOperationResult.Success(ToProfile(response));
        }
        catch (ApiBusinessException exception) { HandleUnauthorized(exception, credentials.Value.Generation); return UserOperationResult.Failure(exception.Message, exception.ErrorField); }
        catch (ApiException exception) { return UserOperationResult.Failure(exception.Message); }
    }

    public async Task<UserOperationResult> ChangePasswordAsync(string? current, string? password, string? confirm, CancellationToken token = default)
    {
        var credentials = Credentials();
        if (credentials is null) return UserOperationResult.Failure("Vui lòng đăng nhập lại.");
        try
        {
            await api.SendJsonAsync<ReloginResponse>(HttpMethod.Post, "api/users/me/password", new ChangePasswordRequest(current, password, confirm), credentials.Value.Token, token);
            if (credentials.Value.Generation == session.Generation) session.Clear();
            return UserOperationResult.Success();
        }
        catch (ApiBusinessException exception) { HandleUnauthorized(exception, credentials.Value.Generation); return UserOperationResult.Failure(exception.Message, exception.ErrorField); }
        catch (ApiException exception) { return UserOperationResult.Failure(exception.Message); }
    }
    public void Logout() => session.Clear();

    /// <summary>Tải danh sách người dùng cho System Admin (401 tự clear phiên, 403 giữ phiên).</summary>
    public async Task<AdminUsersResult> GetAdminUsersAsync(string? search, string? status, int limit, int offset,
        CancellationToken token = default)
    {
        var credentials = Credentials();
        if (credentials is null) return AdminUsersResult.Failure("Vui lòng đăng nhập lại.");
        try
        {
            var response = await api.GetAdminUsersAsync(search, status, limit, offset, credentials.Value.Token, token);
            if (credentials.Value.Generation != session.Generation) return AdminUsersResult.Failure("Phiên đã kết thúc.");
            return AdminUsersResult.Success(response);
        }
        catch (ApiBusinessException exception) { HandleUnauthorized(exception, credentials.Value.Generation); return AdminUsersResult.Failure(exception.Message); }
        catch (ApiException exception) { return AdminUsersResult.Failure(exception.Message); }
    }

    /// <summary>Khóa hoặc mở khóa một tài khoản (401 tự clear phiên, 403 giữ phiên).</summary>
    public async Task<AdminStatusResult> UpdateUserStatusAsync(long userId, string status,
        CancellationToken token = default)
    {
        var credentials = Credentials();
        if (credentials is null) return AdminStatusResult.Failure("Vui lòng đăng nhập lại.");
        try
        {
            var response = await api.UpdateUserStatusAsync(userId, status, credentials.Value.Token, token);
            if (credentials.Value.Generation != session.Generation) return AdminStatusResult.Failure("Phiên đã kết thúc.");
            return AdminStatusResult.Success(response);
        }
        catch (ApiBusinessException exception) { HandleUnauthorized(exception, credentials.Value.Generation); return AdminStatusResult.Failure(exception.Message); }
        catch (ApiException exception) { return AdminStatusResult.Failure(exception.Message); }
    }
    private static UserProfile ToProfile(ProfileResponse profile)
    {
        if (!Enum.TryParse(profile.AccountStatus, true, out CreatorFlow.Models.Enums.AccountStatus status) || !Enum.IsDefined(status))
            throw new ApiException(ApiErrorKind.InvalidResponse, "API trả trạng thái hồ sơ không hợp lệ.");
        return new() { UserId = profile.UserId, Email = profile.Email, DisplayName = profile.DisplayName, AvatarUrl = profile.AvatarUrl,
            IsSystemAdmin = profile.IsSystemAdmin, AccountStatus = status };
    }
}
