namespace CreatorFlow.Api.Models.Auth;

// Repository snapshot for Service only; never return this object to a Form.
public sealed record EmailVerificationRequest
{
    public int VerifierVersion { get; init; }
    public required string Purpose { get; init; }
    public Guid RequestId { get; init; }
    public required string EmailNormalized { get; init; }
    public long? UserId { get; init; }
    public required byte[] OtpVerifier { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public DateTimeOffset DatabaseNow { get; init; }
    public int FailedAttempts { get; init; }
    public DateTimeOffset? DeliveredAt { get; init; }
    public DateTimeOffset? ConsumedAt { get; init; }
    public DateTimeOffset? InvalidatedAt { get; init; }
    public bool IsActiveAccount { get; init; }
    public string? Email { get; init; }
    public string? PasswordHash { get; init; }
}
