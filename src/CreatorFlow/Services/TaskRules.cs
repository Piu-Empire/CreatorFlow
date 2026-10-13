using CreatorFlow.Models.Enums;
using CreatorFlow.Services.Exceptions;

namespace CreatorFlow.Services;

/// <summary>
/// Quy tắc nghiệp vụ dùng chung cho Progress / Deadline / Overdue.
/// Board, My Tasks và các Service đều gọi vào đây để tránh mỗi nơi tự định nghĩa "quá hạn" một kiểu.
/// </summary>
public static class TaskRules
{
    public const int MinProgress = 0;
    public const int MaxProgress = 100;

    // ---------- Phân quyền deadline ----------

    /// <summary>Chỉ Owner/Manager được đổi deadline. Creator và người ngoài Project thì không.</summary>
    public static bool CanChangeDeadline(ProjectRole? role) =>
        role is ProjectRole.Owner or ProjectRole.Manager;

    // ---------- Phân quyền giao việc ----------

    /// <summary>Chỉ Owner/Manager được giao Content cho người khác.</summary>
    public static bool CanAssign(ProjectRole? role) =>
        role is ProjectRole.Owner or ProjectRole.Manager;

    /// <summary>Chỉ thành viên có vai trò Creator mới được nhận Content.</summary>
    public static bool CanBeAssignee(ProjectRole? role) =>
        role is ProjectRole.Creator;

    // ---------- Validation progress ----------
    // ---------- Validation progress ----------

    /// <summary>Progress phải là số nguyên từ 0 đến 100.</summary>
    public static void ValidateProgress(int percent)
    {
        if (percent < MinProgress || percent > MaxProgress)
            throw new ContentValidationException($"Tiến độ phải từ {MinProgress} đến {MaxProgress}%.");
    }

    /// <summary>0% → Assigned, 1–99% → InProgress, 100% → Completed.</summary>
    public static AssignmentStatus StatusForProgress(int percent) =>
        percent <= MinProgress ? AssignmentStatus.Assigned
        : percent >= MaxProgress ? AssignmentStatus.Completed
        : AssignmentStatus.InProgress;

    // ---------- Validation deadline ----------

    /// <summary>
    /// Deadline mới không được ở quá khứ. Giữ nguyên deadline cũ (kể cả đã quá hạn) thì hợp lệ,
    /// và đặt về null (bỏ deadline) cũng hợp lệ.
    /// </summary>
    public static void ValidateNewDeadline(DateTime? newDeadline, DateTime? currentDeadline, DateTime today)
    {
        if (!newDeadline.HasValue) return;
        if (currentDeadline.HasValue && newDeadline.Value.Date == currentDeadline.Value.Date) return;

        if (newDeadline.Value.Date < today.Date)
            throw new ContentValidationException("Deadline mới không được ở trong quá khứ.");
    }

    // ---------- Hoàn thành Content (nhiều Creator) ----------

    /// <summary>
    /// Content hoàn thành khi có ít nhất một người được giao và TẤT CẢ đều Completed
    /// (assignment đã Cancelled không được truyền vào danh sách này).
    /// </summary>
    public static bool IsContentCompleted(IEnumerable<AssignmentStatus> activeAssignmentStatuses)
    {
        bool any = false;
        foreach (var status in activeAssignmentStatuses)
        {
            any = true;
            if (status != AssignmentStatus.Completed) return false;
        }
        return any;
    }

    // ---------- Phát hiện overdue ----------
    // ---------- Phát hiện overdue ----------

    /// <summary>Quá hạn = có deadline, deadline (theo ngày) đã qua so với hôm nay, và công việc chưa hoàn tất.</summary>
    public static bool IsOverdue(DateTime? deadline, bool isFinished, DateTime today) =>
        !isFinished && deadline.HasValue && deadline.Value.Date < today.Date;

    /// <summary>Số ngày trễ (0 nếu chưa quá hạn hoặc không có deadline).</summary>
    public static int DaysLate(DateTime? deadline, DateTime today) =>
        deadline.HasValue && deadline.Value.Date < today.Date ? (today.Date - deadline.Value.Date).Days : 0;
}