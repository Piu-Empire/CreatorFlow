using CreatorFlow.Models;
using CreatorFlow.Models.Enums;

namespace CreatorFlow.Repositories.Interfaces;

/// <summary>
/// Truy cập assignment (công việc được giao). Một Content có thể giao cho NHIỀU Creator;
/// mỗi người có assignment riêng với trạng thái, tiến độ và deadline riêng.
/// </summary>
public interface IMyTaskRepository
{
    /// <summary>
    /// Công việc được giao cho <paramref name="assigneeUserId"/> trong đúng <paramref name="projectId"/>.
    /// Bỏ qua assignment đã Cancelled và Content đã Archived. Lọc/sắp xếp theo UI làm ở MyTaskService.
    /// </summary>
    List<MyTaskItem> GetMyTasks(long projectId, long assigneeUserId);

    /// <summary>Assignment đang hiệu lực (không Cancelled) của một Creator trên một Content, hoặc null.</summary>
    MyTaskItem? GetAssignment(long contentId, long assigneeUserId);

    /// <summary>Mọi assignment đang hiệu lực (không Cancelled) của Content, sắp theo thứ tự được giao.</summary>
    List<MyTaskItem> GetAssignments(long contentId);

    /// <summary>Ghi progress + trạng thái của assignment (chạy trong transaction hiện hành của Unit of Work).</summary>
    void UpdateProgress(long assignmentId, int percent, AssignmentStatus status);

    /// <summary>Đặt deadline riêng cho một assignment. null = bỏ deadline riêng (rơi về deadline chung của Content).</summary>
    void UpdateAssignmentDeadline(long assignmentId, DateTime? deadline);

    /// <summary>Tạo assignment mới (Assigned, 0%) cho <paramref name="assigneeUserId"/>.</summary>
    void AddAssignment(long contentId, long assigneeUserId, long assignedByUserId, DateTime? deadline);

    /// <summary>Huỷ assignment (đặt Cancelled). Lịch sử vẫn còn trong DB.</summary>
    void CancelAssignment(long assignmentId);
}