

namespace CreatorFlow.Api.Models.Auth;

public sealed record UserProfile
{
    public long UserId { get; init; }
    public required string Email { get; init; }
    public required string DisplayName { get; init; }
    public string? AvatarUrl { get; init; }
    public AccountStatus AccountStatus { get; init; }
    public bool IsSystemAdmin { get; init; }
}
