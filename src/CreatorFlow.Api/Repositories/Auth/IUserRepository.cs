using CreatorFlow.Api.Models.Auth;

namespace CreatorFlow.Api.Repositories.Auth;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<User?> RecordSuccessfulLoginAsync(long userId, string expectedPasswordHash, long expectedTokenVersion,
        CancellationToken cancellationToken = default);

    Task<User?> FindByIdAsync(long userId, CancellationToken cancellationToken = default);

    Task<long> CreateAsync(string displayName, string email, string passwordHash,
        CancellationToken cancellationToken = default);

    Task<bool> UpdatePasswordAsync(long userId, string currentPasswordHash, string newPasswordHash, long expectedTokenVersion,
        CancellationToken cancellationToken = default);

    Task<bool> IsPasswordResetSchemaReadyAsync(CancellationToken cancellationToken = default);
    Task<string> NormalizeResetEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<PasswordResetRequest?> FindLatestPasswordResetAsync(string emailNormalized, CancellationToken cancellationToken = default);
    Task<PasswordResetRequest?> FindPasswordResetAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<bool> TryCreatePasswordResetAsync(Guid requestId, string emailNormalized, byte[] verifier,
        Guid? expectedPreviousId, long? expectedUserId, CancellationToken cancellationToken = default);
    Task<bool> MarkPasswordResetDeliveredAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task InvalidatePasswordResetAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task RecordPasswordResetFailureAsync(Guid requestId, byte[] expectedVerifier, CancellationToken cancellationToken = default);
    Task<bool> CompletePasswordResetAsync(Guid requestId, byte[] expectedVerifier, string expectedPasswordHash,
        string newPasswordHash, CancellationToken cancellationToken = default);
    Task<bool> IsEmailVerificationSchemaReadyAsync(CancellationToken cancellationToken = default);
    Task<string> NormalizeVerificationEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<EmailVerificationRequest?> FindLatestEmailVerificationAsync(string email, CancellationToken cancellationToken = default);
    Task<EmailVerificationRequest?> FindEmailVerificationAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<bool> TryCreateEmailVerificationAsync(Guid requestId, string email, byte[] verifier, Guid? previousId, long? expectedUserId, CancellationToken cancellationToken = default);
    Task<bool> MarkEmailVerificationDeliveredAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task InvalidateEmailVerificationAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task RecordEmailVerificationFailureAsync(Guid requestId, byte[] expectedVerifier, CancellationToken cancellationToken = default);
    Task<bool> CompleteEmailVerificationAsync(Guid requestId, byte[] expectedVerifier, CancellationToken cancellationToken = default);
    Task<UserAvatar?> GetAvatarAsync(long userId, CancellationToken cancellationToken = default);
    Task<UserProfile?> SaveProfileAvatarAsync(long userId, string displayName, string? url, AvatarChange change,
        UserAvatar? avatar, long expectedTokenVersion, CancellationToken cancellationToken = default);
}
