using CreatorFlow.Models;

namespace CreatorFlow.Services.Backends;

/// <summary>
/// Nơi thực thi các bước Workflow liên quan Review khi chạy theo mô hình Client–Server:
/// quyền, trạng thái hợp lệ và transaction nằm ở CreatorFlow.Api, client chỉ gọi qua REST.
/// Người thao tác luôn là người đang đăng nhập (lấy từ JWT).
/// </summary>
public interface IWorkflowBackend
{
    /// <summary>EDITING → REVIEW. Backend tạo Review mới, ghi người gửi/thời gian và lịch sử trạng thái.</summary>
    void SubmitReview(long contentId, string? note);

    /// <summary>
    /// Duyệt (REVIEW → READY) hoặc từ chối (REVIEW → EDITING). Từ chối bắt buộc có feedback.
    /// Backend ghi reviewer + thời gian từ JWT, chỉ Owner/Manager được gọi.
    /// </summary>
    void DecideReview(long contentId, bool approve, string? feedback);

    /// <summary>Activity timeline của Content (cũ → mới), dựng từ lịch sử trạng thái ở Backend.</summary>
    List<ActivityItem> GetActivity(long contentId);
}
