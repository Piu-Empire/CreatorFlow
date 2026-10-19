using CreatorFlow.Api.Models.Auth;
using CreatorFlow.Api.Repositories.Auth;

namespace CreatorFlow.Api.Tests.Notifications;

/// <summary>
/// IUserRepository giả chỉ cần FindByIdAsync (để gửi email thông báo).
/// Các hàm còn lại không được gọi nên ném NotSupportedException để lộ ngay nếu service phụ thuộc thêm.
/// </summary>
internal sealed class NotificationUserRepository : IUserRepository
{
    public Dictionary<long, string> Emails { get; } = new(); // userId -> email

    public Task<User?> FindByIdAsync(long userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<User?>(Emails.TryGetValue(userId, out string? email)
            ? new User { UserId = userId, Email = email, DisplayName = $"User {userId}", PasswordHash = "x" }
            : null);

    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<User?> RecordSuccessfulLoginAsync(long userId, string expectedPasswordHash, long expectedTokenVersion,
        CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<long> CreateAsync(string displayName, string email, string passwordHash,
        CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<bool> UpdatePasswordAsync(long userId, string currentPasswordHash, string newPasswordHash, long expectedTokenVersion,
        CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<bool> IsPasswordResetSchemaReadyAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<string> NormalizeResetEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<PasswordResetRequest?> FindLatestPasswordResetAsync(string emailNormalized, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<PasswordResetRequest?> FindPasswordResetAsync(Guid requestId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<bool> TryCreatePasswordResetAsync(Guid requestId, string emailNormalized, byte[] verifier,
        Guid? expectedPreviousId, long? expectedUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<bool> MarkPasswordResetDeliveredAsync(Guid requestId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task InvalidatePasswordResetAsync(Guid requestId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task RecordPasswordResetFailureAsync(Guid requestId, byte[] expectedVerifier, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<bool> CompletePasswordResetAsync(Guid requestId, byte[] expectedVerifier, string expectedPasswordHash,
        string newPasswordHash, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<bool> IsEmailVerificationSchemaReadyAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<string> NormalizeVerificationEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<EmailVerificationRequest?> FindLatestEmailVerificationAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<EmailVerificationRequest?> FindEmailVerificationAsync(Guid requestId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<bool> TryCreateEmailVerificationAsync(Guid requestId, string email, byte[] verifier, Guid? previousId, long? expectedUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<bool> MarkEmailVerificationDeliveredAsync(Guid requestId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task InvalidateEmailVerificationAsync(Guid requestId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task RecordEmailVerificationFailureAsync(Guid requestId, byte[] expectedVerifier, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<bool> CompleteEmailVerificationAsync(Guid requestId, byte[] expectedVerifier, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<UserAvatar?> GetAvatarAsync(long userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<UserProfile?> SaveProfileAvatarAsync(long userId, string displayName, string? url, AvatarChange change,
        UserAvatar? avatar, long expectedTokenVersion, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}
