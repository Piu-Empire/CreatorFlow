using CreatorFlow.Models.Enums;

namespace CreatorFlow.Models;

public sealed record ContentStatusHistory
{
    public long HistoryId { get; set; }

    /// <summary>Alias của HistoryId.</summary>
    public long Id
    {
        get => HistoryId;
        set => HistoryId = value;
    }

    public long ContentId { get; init; }

    public ContentStatus? FromStatus { get; init; }

    public ContentStatus ToStatus { get; init; }

    public long? ChangedBy { get; set; }

    /// <summary>Alias của ChangedBy.</summary>
    public long? ChangedByUserId
    {
        get => ChangedBy;
        set => ChangedBy = value;
    }

    public string? Note { get; init; }

    public DateTime ChangedAt { get; init; }
}