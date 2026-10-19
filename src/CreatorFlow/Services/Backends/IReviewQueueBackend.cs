using CreatorFlow.Models;

namespace CreatorFlow.Services.Backends;

/// <summary>
/// Nguồn dữ liệu Review Queue khi chạy qua CreatorFlow.Api:
/// quyền Owner/Manager và danh sách Review PENDING do Backend quyết định,
/// client chỉ đổi DTO sang model hiển thị và gửi quyết định duyệt/từ chối.
/// </summary>
public interface IReviewQueueBackend
{
    /// <summary>Content đang chờ duyệt của Project, cũ nhất trước.</summary>
    List<ReviewQueueItem> GetPending(long projectId);

    /// <summary>Duyệt hoặc từ chối Review đang PENDING của Content.</summary>
    void Decide(long contentId, bool approve, string? feedback);
}
