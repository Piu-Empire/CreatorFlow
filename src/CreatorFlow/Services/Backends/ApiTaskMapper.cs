using CreatorFlow.Contracts.Tasks;
using CreatorFlow.Models;
using CreatorFlow.Models.Enums;

namespace CreatorFlow.Services.Backends;

/// <summary>Đổi DTO của API sang model hiển thị của WinForms.</summary>
internal static class ApiTaskMapper
{
    public static MyTaskItem ToItem(MyTaskResponse r) => new()
    {
        AssignmentId = r.AssignmentId,
        ContentId = r.ContentId,
        ProjectId = r.ProjectId,
        AssigneeUserId = r.AssigneeUserId,
        ContentCode = r.ContentCode,
        ContentTitle = r.ContentTitle,
        ContentStage = Enum.Parse<ContentStatus>(r.ContentStage, ignoreCase: true),
        Status = Enum.Parse<AssignmentStatus>(r.Status, ignoreCase: true),
        Priority = Enum.Parse<Priority>(r.Priority, ignoreCase: true),
        Deadline = ToDateTime(r.Deadline),
        ProgressPercent = r.ProgressPercent,
        AssignedByName = r.AssignedByName,
        Platforms = r.Platforms.ToList(),
        TeamTotal = r.TeamTotal,
        TeamDone = r.TeamDone,
    };

    public static ProjectMemberInfo ToMember(AssignableMemberResponse r) => new()
    {
        UserId = r.UserId,
        Name = r.DisplayName,
        Role = Enum.Parse<ProjectRole>(r.Role, ignoreCase: true),
    };

    public static CardAssignee ToCardAssignee(BoardAssigneeResponse r) => new(
        r.UserId, r.DisplayName, Enum.Parse<AssignmentStatus>(r.Status, ignoreCase: true), r.ProgressPercent, ToDateTime(r.Deadline));

    public static DateTime? ToDateTime(DateOnly? date) => date?.ToDateTime(TimeOnly.MinValue);

    public static DateOnly? ToDateOnly(DateTime? date) => date.HasValue ? DateOnly.FromDateTime(date.Value) : null;
}
