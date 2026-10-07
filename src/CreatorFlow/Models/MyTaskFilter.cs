using CreatorFlow.Models.Enums;

namespace CreatorFlow.Models;

public enum MyTaskDeadlineFilter
{
    All,
    Overdue,
    Today,
    Next7Days,
    NoDeadline
}

/// <summary>Bộ lọc của màn My Tasks: trạng thái công việc + khoảng deadline.</summary>
public sealed record MyTaskFilter
{
    /// <summary>null = tất cả trạng thái.</summary>
    public AssignmentStatus? Status { get; init; }

    public MyTaskDeadlineFilter Deadline { get; init; } = MyTaskDeadlineFilter.All;
}