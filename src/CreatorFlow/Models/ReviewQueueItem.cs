namespace CreatorFlow.Models;

/// <summary>
/// View-model 1 dòng trong Review Queue (mục 2 UX spec - dành cho Owner/Manager).
/// Luôn ứng với Review đang ở trạng thái PENDING.
/// </summary>
public class ReviewQueueItem
{
    public long ContentId { get; set; }
    public long ProjectId { get; set; }
    public string ContentCode { get; set; } = string.Empty;
    public string ContentTitle { get; set; } = string.Empty;
    public string SubmittedByName { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public int ReviewNo { get; set; }
}