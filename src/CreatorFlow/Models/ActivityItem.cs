namespace CreatorFlow.Models;

/// <summary>
/// 1 dòng trong Activity timeline (mục 6 UX spec).
/// Gộp từ ContentStatusHistory (đổi trạng thái) và Reviews (submit/approve/reject/feedback)
/// thành 1 danh sách chung, sắp theo thời gian, để hiển thị trong ActivityTimelineControl.
/// </summary>
public class ActivityItem
{
    public DateTime Timestamp { get; set; }
    public string ActorName { get; set; } = string.Empty;

    /// <summary>VD: "SCRIPT → PRODUCTION", "submitted Review #1", "rejected Review #1".</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Feedback đi kèm (nếu có, VD khi Reject) - hiển thị dòng phụ bên dưới Description.</summary>
    public string? Feedback { get; set; }
}