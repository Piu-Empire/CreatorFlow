using CreatorFlow.Models;

namespace CreatorFlow.Services.Backends;

/// <summary>
/// Nơi THỰC THI nghiệp vụ My Tasks / progress / deadline khi chạy theo mô hình Client–Server:
/// quyền và validation nằm ở CreatorFlow.Api, client chỉ gọi qua REST. Người thao tác luôn là người đang đăng nhập.
/// </summary>
public interface IMyTaskBackend
{
    List<MyTaskItem> GetMyTasks(long projectId);

    /// <summary>Creator cập nhật tiến độ công việc của chính mình.</summary>
    MyTaskItem UpdateProgress(long contentId, int percent);

    /// <summary>Owner/Manager đổi (hoặc bỏ, khi null) deadline riêng của một Creator.</summary>
    MyTaskItem ChangeDeadline(long contentId, long assigneeUserId, DateTime? deadline);

    /// <summary>Người đang đăng nhập có quyền đổi deadline trong Project (chỉ dùng để bật/tắt nút; Backend vẫn kiểm tra lại).</summary>
    bool CanChangeDeadline(long projectId);
}
