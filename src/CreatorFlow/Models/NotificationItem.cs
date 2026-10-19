namespace CreatorFlow.Models;

/// <summary>Một thông báo nghiệp vụ của người đang đăng nhập, lấy từ CreatorFlow.Api (SCRUM-50).</summary>
public class NotificationItem
{
    public long NotificationId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public long? ProjectId { get; set; }
    public long? ContentId { get; set; }
    public string? ContentCode { get; set; }

    /// <summary>Nhãn hiển thị ngắn theo loại thông báo (tiếng Anh cho player-facing text).</summary>
    public string TypeLabel => Type switch
    {
        "ASSIGNMENT" => "Assignment",
        "REVIEW_SUBMITTED" => "Review submitted",
        "REVIEW_APPROVED" => "Approved",
        "REVIEW_REJECTED" => "Rejected",
        "DEADLINE_NEAR" => "Due soon",
        "OVERDUE" => "Overdue",
        _ => "Notification",
    };
}
