using CreatorFlow.Api.Models.Auth;
using CreatorFlow.Api.Services.Auth;

using System.Security.Cryptography;

namespace CreatorFlow.Api.Tests.Services;

internal sealed partial class UserAccountFake
{
    public bool VerificationSchemaReady { get; set; } = true;
    public bool FailVerificationCommit { get; set; }
    public Action? BeforeVerificationCommit { get; set; }
    public Dictionary<Guid, EmailVerificationRequest> VerificationRequests { get; } = new();
    private readonly object _verificationGate = new();

    public Task<bool> IsEmailVerificationSchemaReadyAsync(CancellationToken cancellationToken = default) => Task.FromResult(VerificationSchemaReady);
    public Task<string> NormalizeVerificationEmailAsync(string email, CancellationToken cancellationToken = default) => Task.FromResult(email.ToLowerInvariant());
    public Task<EmailVerificationRequest?> FindLatestEmailVerificationAsync(string emailNormalized, CancellationToken cancellationToken = default)
    {
        lock (_verificationGate)
        {
            EmailVerificationRequest? request = VerificationRequests.Values.Where(row => row.EmailNormalized == emailNormalized)
                .OrderByDescending(row => row.CreatedAt).FirstOrDefault();
            return Task.FromResult(request is null ? null : RefreshVerification(request));
        }
    }

    public Task<EmailVerificationRequest?> FindEmailVerificationAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        lock (_verificationGate) return Task.FromResult(VerificationRequests.TryGetValue(requestId, out var request) ? RefreshVerification(request) : null);
    }

    private EmailVerificationRequest RefreshVerification(EmailVerificationRequest request) => request with
    {
        DatabaseNow = Now, IsActiveAccount = User?.UserId == request.UserId && User?.AccountStatus == AccountStatus.Active,
        Email = User is { } emailUser && emailUser.UserId == request.UserId ? emailUser.Email : null,
        PasswordHash = User is { } hashUser && hashUser.UserId == request.UserId ? hashUser.PasswordHash : null
    };

    public Task<bool> TryCreateEmailVerificationAsync(Guid requestId, string emailNormalized, byte[] verifier,
        Guid? expectedPreviousId, long? expectedUserId, CancellationToken cancellationToken = default)
    {
        lock (_verificationGate)
        {
            EmailVerificationRequest? previous = VerificationRequests.Values.Where(row => row.EmailNormalized == emailNormalized)
                .OrderByDescending(row => row.CreatedAt).FirstOrDefault();
            if (previous?.RequestId != expectedPreviousId || previous?.CreatedAt > Now.AddSeconds(-60) ||
                VerificationRequests.Values.Count(row => row.EmailNormalized == emailNormalized && row.CreatedAt > Now.AddHours(-1)) >= 5)
                return Task.FromResult(false);
            foreach (var row in VerificationRequests.Values.Where(row => row.EmailNormalized == emailNormalized &&
                row.ConsumedAt is null && row.InvalidatedAt is null).ToArray())
                VerificationRequests[row.RequestId] = row with { InvalidatedAt = Now };
            bool eligible = User is { AccountStatus: AccountStatus.Active } user &&
                user.UserId == expectedUserId && string.Equals(user.Email, emailNormalized, StringComparison.OrdinalIgnoreCase);
            VerificationRequests[requestId] = new EmailVerificationRequest { VerifierVersion = 2, Purpose = EmailOtpCodeProtector.VerificationPurpose, RequestId = requestId,
                EmailNormalized = emailNormalized, UserId = eligible ? User!.UserId : null,
                OtpVerifier = verifier, CreatedAt = Now, ExpiresAt = Now.AddMinutes(10), DatabaseNow = Now };
            return Task.FromResult(true);
        }
    }

    private bool IsUsableVerification(EmailVerificationRequest request) => request.UserId is not null && RefreshVerification(request).IsActiveAccount &&
        request.InvalidatedAt is null && request.ConsumedAt is null && request.DeliveredAt is not null &&
        request.ExpiresAt > Now && request.FailedAttempts < 5;

    public Task<bool> MarkEmailVerificationDeliveredAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        lock (_verificationGate)
        {
            if (!VerificationRequests.TryGetValue(requestId, out var request) || request.InvalidatedAt is not null ||
                request.ConsumedAt is not null || request.ExpiresAt <= Now || !RefreshVerification(request).IsActiveAccount)
                return Task.FromResult(false);
            VerificationRequests[requestId] = request with { DeliveredAt = Now };
            return Task.FromResult(true);
        }
    }

    public Task InvalidateEmailVerificationAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        lock (_verificationGate)
        {
            if (VerificationRequests.TryGetValue(requestId, out var request) && request.ConsumedAt is null)
                VerificationRequests[requestId] = request with { InvalidatedAt = Now };
        }
        return Task.CompletedTask;
    }

    public Task RecordEmailVerificationFailureAsync(Guid requestId, byte[] expectedVerifier, CancellationToken cancellationToken = default)
    {
        lock (_verificationGate)
        {
            if (VerificationRequests.TryGetValue(requestId, out var request) && IsUsableVerification(request) &&
                CryptographicOperations.FixedTimeEquals(request.OtpVerifier, expectedVerifier))
            {
                int attempts = request.FailedAttempts + 1;
                VerificationRequests[requestId] = request with { FailedAttempts = attempts, InvalidatedAt = attempts >= 5 ? Now : null };
            }
        }
        return Task.CompletedTask;
    }

    public Task<bool> CompleteEmailVerificationAsync(Guid requestId, byte[] expectedVerifier, CancellationToken cancellationToken = default)
    {
        lock (_verificationGate)
        {
            BeforeVerificationCommit?.Invoke();
            if (!VerificationRequests.TryGetValue(requestId, out var request) || !IsUsableVerification(request) ||
                !CryptographicOperations.FixedTimeEquals(request.OtpVerifier, expectedVerifier) || User!.EmailVerifiedAt is not null)
                return Task.FromResult(false);
            if (FailVerificationCommit) throw new TimeoutException("Private database detail");
            User user = User;
            User = new User { UserId=user.UserId, Email=user.Email, DisplayName=user.DisplayName, PasswordHash=user.PasswordHash,
                AvatarUrl=user.AvatarUrl, AccountStatus=user.AccountStatus, IsSystemAdmin=user.IsSystemAdmin, TokenVersion=user.TokenVersion+1, EmailVerifiedAt=Now };
            VerificationRequests[requestId] = request with { ConsumedAt = Now };
            return Task.FromResult(true);
        }
    }
    public UserAvatar? Avatar { get; private set; }
    public Task<UserAvatar?> GetAvatarAsync(long userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(User?.UserId == userId ? Avatar : null);
    public async Task<UserProfile?> SaveProfileAvatarAsync(long userId, string displayName, string? url, AvatarChange change,
        UserAvatar? avatar, long expectedTokenVersion, CancellationToken cancellationToken = default)
    {
        if (User?.TokenVersion != expectedTokenVersion) return null;
        UserProfile? saved = await UpdateProfileAsync(userId, displayName, change == AvatarChange.Keep ? User?.AvatarUrl :
            change == AvatarChange.Url ? url : null, cancellationToken);
        if (saved is not null && change != AvatarChange.Keep) Avatar = change == AvatarChange.Upload ? avatar : null;
        return saved;
    }
}
