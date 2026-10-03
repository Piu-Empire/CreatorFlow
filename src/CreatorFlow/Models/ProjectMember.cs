using CreatorFlow.Models.Enums;

namespace CreatorFlow.Models;

public sealed record ProjectMember
{
    public long ProjectMemberId { get; init; }

    public long ProjectId { get; init; }

    public long UserId { get; init; }

    public ProjectRole Role { get; init; }

    public DateTime JoinedAt { get; init; }

    public bool IsActive { get; init; } = true;
}
