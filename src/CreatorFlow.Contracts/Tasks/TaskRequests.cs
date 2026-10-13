namespace CreatorFlow.Contracts.Tasks;

/// <summary>Creator cập nhật tiến độ công việc của chính mình (0–100).</summary>
public sealed record UpdateProgressRequest(int ProgressPercent);

/// <summary>Owner/Manager đổi deadline riêng của một Creator. null = bỏ deadline riêng (rơi về deadline chung của Content).</summary>
public sealed record ChangeDeadlineRequest(DateOnly? Deadline);

/// <summary>Một Creator được giao kèm deadline riêng. Deadline = null nghĩa là giữ nguyên deadline hiện có.</summary>
public sealed record AssigneeDeadlineRequest(long UserId, DateOnly? Deadline);

/// <summary>
/// Danh sách Creator được giao Content SAU KHI giao (người mới → tạo assignment; người cũ → giữ tiến độ;
/// người không còn trong danh sách → huỷ assignment, trừ người đã hoàn thành).
/// </summary>
public sealed record AssignContentRequest(IReadOnlyList<AssigneeDeadlineRequest> Assignments);
