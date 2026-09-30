using CreatorFlow.Models.Enums;

namespace CreatorFlow.Models;

public sealed record Content
{
    public long ContentId { get; init; }

    public long ProjectId { get; init; }

    public long? SourceIdeaId { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    public string? Script { get; init; }

    public string? ContentType { get; init; }

    public Priority Priority { get; init; } = Priority.Medium;

    public ContentStatus Status { get; init; } = ContentStatus.Idea;

    public DateTime? Deadline { get; init; }

    public DateTime? PlannedPublishAt { get; init; }

    public long CreatedBy { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }
}
