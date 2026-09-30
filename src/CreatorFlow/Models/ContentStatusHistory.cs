using CreatorFlow.Models.Enums;

namespace CreatorFlow.Models;

public sealed record ContentStatusHistory
{
    public long HistoryId { get; init; }

    public long ContentId { get; init; }

    public ContentStatus? FromStatus { get; init; }

    public ContentStatus ToStatus { get; init; }

    public long? ChangedBy { get; init; }

    public string? Note { get; init; }

    public DateTime ChangedAt { get; init; }
}
