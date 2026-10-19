using CreatorFlow.Contracts.ContentDetail;

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

    /// <summary>Feedback đi kèm (nếu có, VD khi Reject hoặc ghi chú khi gửi duyệt).</summary>
    public string? Feedback { get; set; }

    /// <summary>Loại sự kiện: <see cref="ActivityKinds"/>.</summary>
    public string Kind { get; set; } = ActivityKinds.StatusChange;

    /// <summary>Trạng thái trước/sau (PascalCase, VD "Editing"); null khi không áp dụng.</summary>
    public string? FromStatus { get; set; }
    public string? ToStatus { get; set; }

    /// <summary>Số thứ tự Review (chỉ có ở sự kiện Review).</summary>
    public int? ReviewNo { get; set; }

    /// <summary>"Pending" / "Approved" / "Rejected" (chỉ có ở sự kiện Review).</summary>
    public string? ReviewStatus { get; set; }
}
