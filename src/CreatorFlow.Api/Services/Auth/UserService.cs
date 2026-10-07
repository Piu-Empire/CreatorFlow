using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Models.Auth;
using CreatorFlow.Api.Repositories.Auth;
using CreatorFlow.Contracts.Auth;
using CreatorFlow.Contracts.Users;

namespace CreatorFlow.Api.Services.Auth;

public sealed class UserService(IUserRepository repository, CurrentAuthenticatedUser current,
    IAvatarImageProcessor? images = null)
{
    private async Task<User?> EligibleUserAsync(CancellationToken token)
    {
        if (current.User is not { } authenticated) return null;
        User? user = await repository.FindByIdAsync(authenticated.UserId, token);
        return user is { AccountStatus: AccountStatus.Active, EmailVerifiedAt: not null } &&
            user.TokenVersion == authenticated.TokenVersion ? user : null;
    }

    public async Task<AuthOperationResult<ProfileResponse>> GetProfileAsync(CancellationToken token = default)
    {
        User? user = await EligibleUserAsync(token);
        return user is null ? AuthOperationResult<ProfileResponse>.Fail("unauthorized", "Vui lòng đăng nhập lại.", status: 401) : new(ToProfile(user));
    }

    public async Task<AuthOperationResult<UserAvatar>> GetAvatarAsync(CancellationToken token = default)
    {
        User? user = await EligibleUserAsync(token);
        if (user is null) return AuthOperationResult<UserAvatar>.Fail("unauthorized", "Vui lòng đăng nhập lại.", status: 401);
        UserAvatar? avatar = await repository.GetAvatarAsync(user.UserId, token);
        return avatar is null ? AuthOperationResult<UserAvatar>.Fail("avatar_not_found", "Chưa có ảnh đại diện.", status: 404) : new(avatar);
    }

    public async Task<AuthOperationResult<ProfileResponse>> SaveProfileAsync(string? name, string? url,
        AvatarAction action, byte[]? image, CancellationToken token = default)
    {
        User? user = await EligibleUserAsync(token);
        if (user is null) return AuthOperationResult<ProfileResponse>.Fail("unauthorized", "Vui lòng đăng nhập lại.", status: 401);
        string? error = AuthRules.ValidateDisplayName(name);
        if (error is not null) return AuthOperationResult<ProfileResponse>.Fail("validation", error, "DisplayName");
        if (!Enum.IsDefined(action)) return AuthOperationResult<ProfileResponse>.Fail("validation", "Nguồn ảnh không hợp lệ.", "AvatarUrl");
        string? normalizedUrl = string.IsNullOrWhiteSpace(url) ? null : url.Trim();
        if (action == AvatarAction.Url && normalizedUrl is not null &&
            (!Uri.TryCreate(normalizedUrl, UriKind.Absolute, out Uri? uri) || uri.Scheme is not ("http" or "https")))
            return AuthOperationResult<ProfileResponse>.Fail("validation", "URL ảnh phải dùng HTTP hoặc HTTPS.", "AvatarUrl");
        UserAvatar? avatar = null;
        if (action == AvatarAction.Upload)
        {
            if (images is null) return AuthOperationResult<ProfileResponse>.Fail("avatar_unavailable", "Upload ảnh chưa sẵn sàng.", status: 503);
            if (image is null || image.Length is 0 or > 2097152)
                return AuthOperationResult<ProfileResponse>.Fail("validation", "Ảnh phải là JPEG/PNG tối đa 2 MiB.", "AvatarUrl");
            try { avatar = await Task.Run(() => images.Normalize(image), token); }
            catch (ArgumentException) { return AuthOperationResult<ProfileResponse>.Fail("validation", "Ảnh không hợp lệ hoặc vượt giới hạn.", "AvatarUrl"); }
        }
        UserProfile? saved = await repository.SaveProfileAvatarAsync(user.UserId, name!.Trim(), normalizedUrl,
            (AvatarChange)(int)action, avatar, user.TokenVersion, token);
        return saved is null ? AuthOperationResult<ProfileResponse>.Fail("unauthorized", "Vui lòng đăng nhập lại.", status: 401) :
            new(new ProfileResponse(saved.UserId, saved.Email, saved.DisplayName, saved.AccountStatus.ToString(), saved.IsSystemAdmin, saved.AvatarUrl));
    }

    public async Task<AuthOperationResult<ReloginResponse>> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken token = default)
    {
        User? user = await EligibleUserAsync(token);
        if (user is null) return AuthOperationResult<ReloginResponse>.Fail("unauthorized", "Vui lòng đăng nhập lại.", status: 401);
        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
            return AuthOperationResult<ReloginResponse>.Fail("validation", "Vui lòng nhập mật khẩu hiện tại.", "CurrentPassword");
        string? error = PasswordPolicy.Validate(request.NewPassword);
        if (error is not null) return AuthOperationResult<ReloginResponse>.Fail("validation", error, "NewPassword");
        if (request.NewPassword != request.ConfirmPassword)
            return AuthOperationResult<ReloginResponse>.Fail("validation", "Mật khẩu xác nhận không khớp.", "ConfirmPassword");
        if (!await Task.Run(() => AuthRules.VerifyPassword(request.CurrentPassword, user.PasswordHash), token))
            return AuthOperationResult<ReloginResponse>.Fail("validation", "Mật khẩu hiện tại không đúng.", "CurrentPassword");
        string hash = await Task.Run(() => BCrypt.Net.BCrypt.HashPassword(request.NewPassword!), token);
        token.ThrowIfCancellationRequested();
        if (!await repository.UpdatePasswordAsync(user.UserId, user.PasswordHash, hash, user.TokenVersion, token))
            return AuthOperationResult<ReloginResponse>.Fail("unauthorized", "Tài khoản đã thay đổi. Vui lòng đăng nhập lại.", status: 401);
        return new(new ReloginResponse(true));
    }

    private static ProfileResponse ToProfile(User user) =>
        new(user.UserId, user.Email, user.DisplayName, user.AccountStatus.ToString(), user.IsSystemAdmin, user.AvatarUrl);
}
