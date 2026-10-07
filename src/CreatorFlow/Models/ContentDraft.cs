using CreatorFlow.Models.Enums;
namespace CreatorFlow.Models;

/// <summary>
/// Dữ liệu người dùng nhập khi tạo mới / sửa một Content (chưa gồm Status vì Status chỉ đổi qua WorkflowService).
/// </summary>
public class ContentDraft
{
    public string Title { get; set; } = string.Empty;

    /// <summary>Concept hook / mô tả ngắn.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Kịch bản chi tiết (Hook / Body / CTA...). Để trống = chưa có kịch bản.</summary>
    public string Script { get; set; } = string.Empty;

    /// <summary>Loại nội dung, phải nằm trong ContentService.AvailableContentTypes. Để trống = loại mặc định.</summary>
    public string ContentType { get; set; } = string.Empty;

    public Priority Priority { get; set; } = Priority.Medium;
    public List<string> Platforms { get; set; } = new();
    public string Sprint { get; set; } = "Sprint 25";
    public DateTime? Deadline { get; set; }

    /// <summary>Ngày dự kiến đăng (chỉ lấy phần ngày). null = chưa lên lịch đăng.</summary>
    public DateTime? PlannedPublishAt { get; set; }
    public string EstimatedDuration { get; set; } = string.Empty;
    public long? AssigneeUserId { get; set; }
}