namespace CreatorFlow.Models;

public sealed record CurrentUser
{
    public long UserId { get; init; }

    public required string Email { get; init; }

    public required string DisplayName { get; init; }

    public bool IsSystemAdmin { get; init; }
}
