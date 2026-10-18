using CreatorFlow.Api.Models.Tasks;

namespace CreatorFlow.Api.Services.Tasks;

/// <summary>
/// Quy tắc nghiệp vụ giao việc / progress / deadline thực thi Ở BACKEND (client chỉ dùng để bật/tắt nút cho gọn).
/// Hàm kiểm tra trả về thông báo lỗi (null = hợp lệ) để TaskService đóng gói thành kết quả HTTP.
/// </summary>
public static class TaskRules
{
    public const int MinProgress = 0;
    public const int MaxProgress = 100;

    /// <summary>Chỉ Owner/Manager được giao Content cho người khác.</summary>
    public static bool CanAssign(ProjectRole? role) => role is ProjectRole.Owner or ProjectRole.Manager;

    /// <summary>Chỉ thành viên có vai trò Creator mới được nhận Content.</summary>
    public static bool CanBeAssignee(ProjectRole? role) => role is ProjectRole.Creator;

    /// <summary>Chỉ Owner/Manager được đổi deadline.</summary>
    public static bool CanChangeDeadline(ProjectRole? role) => role is ProjectRole.Owner or ProjectRole.Manager;

    public static string? ValidateProgress(int percent) =>
        percent is < MinProgress or > MaxProgress ? $"Tiến độ phải từ {MinProgress} đến {MaxProgress}%." : null;

    /// <summary>0% → Assigned, 1–99% → InProgress, 100% → Completed.</summary>
    public static AssignmentStatus StatusForProgress(int percent) =>
        percent <= MinProgress ? AssignmentStatus.Assigned
        : percent >= MaxProgress ? AssignmentStatus.Completed
        : AssignmentStatus.InProgress;

    /// <summary>
    /// Deadline mới không được ở quá khứ. Giữ nguyên deadline cũ (kể cả đã quá hạn) hoặc bỏ deadline (null) thì hợp lệ.
    /// </summary>
    public static string? ValidateNewDeadline(DateOnly? newDeadline, DateOnly? currentDeadline, DateOnly today)
    {
        if (!newDeadline.HasValue || newDeadline == currentDeadline) return null;
        return newDeadline.Value < today ? "Deadline mới không được ở trong quá khứ." : null;
    }

    /// <summary>Content hoàn thành khi có ít nhất một Creator được giao và TẤT CẢ đều Completed.</summary>
    public static bool IsContentCompleted(IEnumerable<AssignmentStatus> activeAssignmentStatuses)
    {
        bool any = false;
        foreach (AssignmentStatus status in activeAssignmentStatuses)
        {
            any = true;
            if (status != AssignmentStatus.Completed) return false;
        }
        return any;
    }
}
