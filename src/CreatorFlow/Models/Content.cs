using CreatorFlow.Models.Enums;

namespace CreatorFlow.Models;

public sealed record Content
{
    public long ContentId { get; set; }

    /// <summary>Alias của ContentId (code Repository/Service/Form đang dùng tên Id).</summary>
    public long Id
    {
        get => ContentId;
        set => ContentId = value;
    }

    public long ProjectId { get; init; }

    public long? SourceIdeaId { get; set; }

    /// <summary>Alias của SourceIdeaId.</summary>
    public long? IdeaId
    {
        get => SourceIdeaId;
        set => SourceIdeaId = value;
    }

    public required string Title { get; init; }

    public string? Description { get; init; }

    public string? Script { get; init; }

    public string? ContentType { get; init; }

    public Priority Priority { get; init; } = Priority.Medium;

    // set (không còn init) để WorkflowService gán được sau khi load từ DB
    public ContentStatus Status { get; set; } = ContentStatus.Idea;

    public DateTime? Deadline { get; init; }

    public DateTime? PlannedPublishAt { get; init; }

    public long CreatedBy { get; set; }

    /// <summary>Alias của CreatedBy.</summary>
    public long CreatedByUserId
    {
        get => CreatedBy;
        set => CreatedBy = value;
    }

    public DateTime CreatedAt { get; init; }

    // set (không còn init) vì WorkflowService cập nhật khi đổi trạng thái
    public DateTime UpdatedAt { get; set; }
}