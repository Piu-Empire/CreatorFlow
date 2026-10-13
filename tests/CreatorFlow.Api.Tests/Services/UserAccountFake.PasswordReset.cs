using CreatorFlow.Api.Models.Auth;
using CreatorFlow.Api.Services.Auth;

using System.Security.Cryptography;

namespace CreatorFlow.Api.Tests.Services;

internal sealed partial class UserAccountFake
{
    public DateTimeOffset Now { get; set; } = new(2026, 10, 4, 0, 0, 0, TimeSpan.Zero);
    public bool ResetSchemaReady { get; set; } = true;
    public bool FailResetCommit { get; set; }
    public Action? BeforeResetCommit { get; set; }
    public Dictionary<Guid, PasswordResetRequest> ResetRequests { get; } = new();
    private readonly object _resetGate = new();

    public Task<bool> IsPasswordResetSchemaReadyAsync(CancellationToken cancellationToken = default) => Task.FromResult(ResetSchemaReady);
    public Task<string> NormalizeResetEmailAsync(string email, CancellationToken cancellationToken = default) => Task.FromResult(email.ToLowerInvariant());
    public Task<PasswordResetRequest?> FindLatestPasswordResetAsync(string emailNormalized, CancellationToken cancellationToken = default)
    {
        lock (_resetGate)
        {
            PasswordResetRequest? request = ResetRequests.Values.Where(row => row.EmailNormalized == emailNormalized)
                .OrderByDescending(row => row.CreatedAt).FirstOrDefault();
            return Task.FromResult(request is null ? null : Refresh(request));
        }
    }

    public Task<PasswordResetRequest?> FindPasswordResetAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        lock (_resetGate) return Task.FromResult(ResetRequests.TryGetValue(requestId, out var request) ? Refresh(request) : null);
    }

    private PasswordResetRequest Refresh(PasswordResetRequest request) => request with
    {
        DatabaseNow = Now, IsActiveAccount = User?.UserId == request.UserId && User?.AccountStatus == AccountStatus.Active,
        Email = User is { } emailUser && emailUser.UserId == request.UserId ? emailUser.Email : null,
        PasswordHash = User is { } hashUser && hashUser.UserId == request.UserId ? hashUser.PasswordHash : null
    };

    public Task<bool> TryCreatePasswordResetAsync(Guid requestId, string emailNormalized, byte[] verifier,
        Guid? expectedPreviousId, long? expectedUserId, CancellationToken cancellationToken = default)
    {
        lock (_resetGate)
        {
            PasswordResetRequest? previous = ResetRequests.Values.Where(row => row.EmailNormalized == emailNormalized)
                .OrderByDescending(row => row.CreatedAt).FirstOrDefault();
            if (previous?.RequestId != expectedPreviousId || previous?.CreatedAt > Now.AddSeconds(-60) ||
                ResetRequests.Values.Count(row => row.EmailNormalized == emailNormalized && row.CreatedAt > Now.AddHours(-1)) >= 5)
                return Task.FromResult(false);
            foreach (var row in ResetRequests.Values.Where(row => row.EmailNormalized == emailNormalized &&
                row.ConsumedAt is null && row.InvalidatedAt is null).ToArray())
                ResetRequests[row.RequestId] = row with { InvalidatedAt = Now };
            bool eligible = User is { AccountStatus: AccountStatus.Active } user &&
                user.UserId == expectedUserId && string.Equals(user.Email, emailNormalized, StringComparison.OrdinalIgnoreCase);
            ResetRequests[requestId] = new PasswordResetRequest { VerifierVersion = 2, Purpose = EmailOtpCodeProtector.ResetPurpose, RequestId = requestId,
                EmailNormalized = emailNormalized, UserId = eligible ? User!.UserId : null,
                OtpVerifier = verifier, CreatedAt = Now, ExpiresAt = Now.AddMinutes(10), DatabaseNow = Now };
            return Task.FromResult(true);
        }
    }

    private bool IsUsable(PasswordResetRequest request) => request.UserId is not null && Refresh(request).IsActiveAccount &&
        request.InvalidatedAt is null && request.ConsumedAt is null && request.DeliveredAt is not null &&
        request.ExpiresAt > Now && request.FailedAttempts < 5;

    public Task<bool> MarkPasswordResetDeliveredAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        lock (_resetGate)
        {
            if (!ResetRequests.TryGetValue(requestId, out var request) || request.InvalidatedAt is not null ||
                request.ConsumedAt is not null || request.ExpiresAt <= Now || !Refresh(request).IsActiveAccount)
                return Task.FromResult(false);
            ResetRequests[requestId] = request with { DeliveredAt = Now };
            return Task.FromResult(true);
        }
    }

    public Task InvalidatePasswordResetAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        lock (_resetGate)
        {
            if (ResetRequests.TryGetValue(requestId, out var request) && request.ConsumedAt is null)
                ResetRequests[requestId] = request with { InvalidatedAt = Now };
        }
        return Task.CompletedTask;
    }

    public Task RecordPasswordResetFailureAsync(Guid requestId, byte[] expectedVerifier, CancellationToken cancellationToken = default)
    {
        lock (_resetGate)
        {
            if (ResetRequests.TryGetValue(requestId, out var request) && IsUsable(request) &&
                CryptographicOperations.FixedTimeEquals(request.OtpVerifier, expectedVerifier))
            {
                int attempts = request.FailedAttempts + 1;
                ResetRequests[requestId] = request with { FailedAttempts = attempts, InvalidatedAt = attempts >= 5 ? Now : null };
            }
        }
        return Task.CompletedTask;
    }

    public Task<bool> CompletePasswordResetAsync(Guid requestId, byte[] expectedVerifier, string expectedPasswordHash,
        string newPasswordHash, CancellationToken cancellationToken = default)
    {
        lock (_resetGate)
        {
            BeforeResetCommit?.Invoke();
            if (!ResetRequests.TryGetValue(requestId, out var request) || !IsUsable(request) ||
                !CryptographicOperations.FixedTimeEquals(request.OtpVerifier, expectedVerifier)) return Task.FromResult(false);
            if (User!.PasswordHash != expectedPasswordHash)
            {
                ResetRequests[requestId] = request with { InvalidatedAt = Now };
                return Task.FromResult(false);
            }
            if (FailResetCommit) throw new TimeoutException("Private database detail");
            User user = User;
            User = new User { EmailVerifiedAt = user.EmailVerifiedAt, UserId = user.UserId, Email = user.Email, DisplayName = user.DisplayName,
                AvatarUrl = user.AvatarUrl, AccountStatus = user.AccountStatus, IsSystemAdmin = user.IsSystemAdmin,
                TokenVersion = user.TokenVersion + 1, PasswordHash = newPasswordHash };
            ResetRequests[requestId] = request with { ConsumedAt = Now };
            return Task.FromResult(true);
        }
    }
}
