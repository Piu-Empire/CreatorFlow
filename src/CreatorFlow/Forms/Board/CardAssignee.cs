using CreatorFlow.Models.Enums;

namespace CreatorFlow.Models;

/// <summary>Một Creator được giao Content, dùng để hiển thị trên Board / drawer.</summary>
public sealed record CardAssignee(long UserId, string Name, AssignmentStatus Status, int ProgressPercent, DateTime? Deadline);