namespace CreatorFlow.Models;

/// <summary>
/// View-model cho 1 card trên Board (mục 4 - CONTENT CARD trong UX spec).
/// Không phải Entity DB, chỉ gộp dữ liệu cần hiển thị từ Content + ContentPlatforms + ContentAssignments.
/// </summary>
public class ContentBoardCard
{
    public long ContentId { get; set; }
    public long ProjectId { get; set; }

    /// <summary>VD: "CNT-021".</summary>
    public string Code { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ContentStatus Status { get; set; }
    public Priority Priority { get; set; }
    public List<string> Platforms { get; set; } = new();
    public string? AssigneeName { get; set; }
    public long? AssigneeUserId { get; set; }

    /// <summary>Sprint chứa nội dung này (VD "Sprint 25").</summary>
    public string Sprint { get; set; } = "Sprint 25";

    /// <summary>Thời lượng dự kiến dạng chữ tự do (VD "24 min", "60 sec").</summary>
    public string EstimatedDuration { get; set; } = string.Empty;
    public DateTime? Deadline { get; set; }
    public List<string> Tags { get; set; } = new();

    /// <summary>Có Review đang PENDING hay không (hiện icon Review trên card).</summary>
    public bool HasPendingReview { get; set; }

    public bool IsOverdue =>
        Deadline.HasValue && Deadline.Value.Date < DateTime.Today && Status != ContentStatus.Published;
}