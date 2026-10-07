using CreatorFlow.Api.Models.Auth;
using CreatorFlow.Api.Repositories.Auth;
namespace CreatorFlow.Api.Tests.Services;
internal sealed partial class UserAccountFake : IUserRepository
{
    public User? User { get; set; }
    public Exception? CreateException { get; set; }
    public Exception? ReadException { get; set; }
    public bool PasswordWriteResult { get; set; } = true;
    public Action? BeforeProfileWrite { get; set; }
    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        Task.FromResult(User is { } user && string.Equals(email, user.Email, StringComparison.OrdinalIgnoreCase) ? user : null);
    public Task<User?> FindByIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        if (ReadException is not null) throw ReadException;
        return Task.FromResult(User?.UserId == userId ? User : null);
    }
    public Task<long> CreateAsync(string displayName, string email, string passwordHash, CancellationToken cancellationToken = default)
    {
        if (CreateException is not null) throw CreateException;
        User = new User { UserId = 42, Email = email, DisplayName = displayName, PasswordHash = passwordHash,
            AccountStatus = AccountStatus.Active, IsSystemAdmin = false };
        return Task.FromResult(User.UserId);
    }
    public Task<User?> RecordSuccessfulLoginAsync(long userId, string expectedPasswordHash, long expectedTokenVersion, CancellationToken cancellationToken = default) =>
        Task.FromResult(User?.UserId == userId && User.PasswordHash == expectedPasswordHash && User.TokenVersion == expectedTokenVersion && User.AccountStatus == AccountStatus.Active && User.EmailVerifiedAt is not null ? User : null);
    public Task<UserProfile?> UpdateProfileAsync(long userId, string displayName, string? avatarUrl, CancellationToken cancellationToken = default)
    {
        BeforeProfileWrite?.Invoke();
        if (User is not { } user || user.UserId != userId || user.AccountStatus != AccountStatus.Active) return Task.FromResult<UserProfile?>(null);
        User = new User { EmailVerifiedAt = user.EmailVerifiedAt, UserId = user.UserId, Email = user.Email, DisplayName = displayName,
            TokenVersion = user.TokenVersion, AvatarUrl = avatarUrl, PasswordHash = user.PasswordHash, AccountStatus = user.AccountStatus, IsSystemAdmin = user.IsSystemAdmin };
        return Task.FromResult<UserProfile?>(new UserProfile { UserId = userId, Email = user.Email, DisplayName = displayName,
            AvatarUrl = avatarUrl, AccountStatus = user.AccountStatus, IsSystemAdmin = user.IsSystemAdmin });
    }
    public Task<bool> UpdatePasswordAsync(long userId, string currentPasswordHash, string newPasswordHash, long expectedTokenVersion, CancellationToken cancellationToken = default)
    {
        if (!PasswordWriteResult || User is not { } user || user.UserId != userId || user.PasswordHash != currentPasswordHash || user.TokenVersion != expectedTokenVersion ||
            user.AccountStatus != AccountStatus.Active) return Task.FromResult(false);
        User = new User { EmailVerifiedAt = user.EmailVerifiedAt, UserId = user.UserId, Email = user.Email, DisplayName = user.DisplayName,
            TokenVersion = user.TokenVersion + 1, AvatarUrl = user.AvatarUrl, PasswordHash = newPasswordHash, AccountStatus = user.AccountStatus, IsSystemAdmin = user.IsSystemAdmin };
        return Task.FromResult(true);
    }
}