namespace CreatorFlow.Models;

/// <summary>
/// Một dòng trong yêu cầu giao việc: giao cho <see cref="UserId"/> với deadline riêng.
/// <see cref="Deadline"/> = null nghĩa là giữ nguyên deadline hiện có của người đó (hoặc chưa đặt nếu là người mới).
/// </summary>
public sealed record AssignmentRequest(long UserId, DateTime? Deadline = null);