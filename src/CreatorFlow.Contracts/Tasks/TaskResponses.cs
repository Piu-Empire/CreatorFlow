namespace CreatorFlow.Contracts.Tasks;

/// <summary>
/// Một công việc được giao (một dòng content_assignments gộp thông tin Content).
/// ContentStage / Status / Priority là tên enum PascalCase ("Script", "InProgress", "High"...).
/// Deadline là NGÀY (UTC) — deadline riêng của người này, rơi về deadline chung của Content nếu chưa đặt riêng.
/// </summary>
public sealed record MyTaskResponse(
    long AssignmentId,
    long ContentId,
    long ProjectId,
    long AssigneeUserId,
    string ContentCode,
    string ContentTitle,
    string ContentStage,
    string Status,
    string Priority,
    DateOnly? Deadline,
    int ProgressPercent,
    string? AssignedByName,
    IReadOnlyList<string> Platforms,
    int TeamTotal,
    int TeamDone);

public sealed record MyTasksResponse(IReadOnlyList<MyTaskResponse> Tasks);

/// <summary>Mọi Creator đang được giao một Content (assignment chưa Cancelled).</summary>
public sealed record AssignmentsResponse(IReadOnlyList<MyTaskResponse> Assignments);

/// <summary>Vai trò của người đang đăng nhập trong Project ("Owner", "Manager", "Creator") — client dùng để bật/tắt nút.</summary>
public sealed record ProjectRoleResponse(string Role);

public sealed record AssignableMemberResponse(long UserId, string DisplayName, string Role);

/// <summary>Thành viên có thể chọn làm người nhận việc: Owner/Manager thấy mọi Creator, Creator chỉ thấy chính mình.</summary>
public sealed record AssignableMembersResponse(IReadOnlyList<AssignableMemberResponse> Members);

/// <summary>Creator được giao trên thẻ Board (một dòng cho mỗi Creator của mỗi Content).</summary>
public sealed record BoardAssigneeResponse(
    long ContentId,
    long UserId,
    string DisplayName,
    string Status,
    int ProgressPercent,
    DateOnly? Deadline);

public sealed record BoardAssigneesResponse(IReadOnlyList<BoardAssigneeResponse> Assignees);
