using CreatorFlow.Models;

namespace CreatorFlow.Services.Backends;

/// <summary>Nơi thực thi nghiệp vụ giao Content cho một hoặc nhiều Creator (xem <see cref="IMyTaskBackend"/>).</summary>
public interface IAssignmentBackend
{
    /// <summary>Người có thể chọn làm người nhận việc: Owner/Manager thấy mọi Creator, Creator chỉ thấy chính mình.</summary>
    List<ProjectMemberInfo> GetAssignableMembers(long projectId);

    /// <summary>Mọi Creator đang được giao Content (assignment chưa huỷ).</summary>
    List<MyTaskItem> GetAssignments(long contentId);

    /// <summary>Đặt danh sách Creator được giao Content (kèm deadline riêng), xem <c>ContentService.AssignContent</c>.</summary>
    void AssignContent(long contentId, IReadOnlyList<AssignmentRequest> assignments);
}
