

namespace CreatorFlow.Api.Models.Auth;

public sealed class User
{
    public long TokenVersion { get; init; }
    public long UserId { get; init; }

    public required string Email { get; init; }

    public required string DisplayName { get; init; }

    public DateTimeOffset? EmailVerifiedAt { get; init; }

    public string? AvatarUrl { get; init; }

    public required string PasswordHash { get; init; }

    public AccountStatus AccountStatus { get; init; }

    public bool IsSystemAdmin { get; init; }
}
