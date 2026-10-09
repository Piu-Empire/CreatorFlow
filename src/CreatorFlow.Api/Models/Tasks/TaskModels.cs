namespace CreatorFlow.Api.Models.Tasks;

public enum ProjectRole { Owner, Manager, Creator }

public enum AssignmentStatus { Assigned, InProgress, Completed, Cancelled }

/// <summary>Thông tin tối thiểu của Content cần để kiểm tra quyền giao việc.</summary>
public sealed record ContentInfo(long ContentId, long ProjectId, string Status, long CreatedBy, DateOnly? Deadline);

public sealed record ProjectMember(long UserId, string DisplayName, ProjectRole Role);

/// <summary>Một dòng content_assignments gộp thông tin Content và số liệu của nhóm Creator.</summary>
public sealed record TaskAssignment(
    long AssignmentId,
    long ContentId,
    long ProjectId,
    long AssigneeUserId,
    string ContentCode,
    string ContentTitle,
    string ContentStage,
    string ContentPriority,
    AssignmentStatus Status,
    int ProgressPercent,
    DateOnly? Deadline,
    string? AssignedByName,
    IReadOnlyList<string> Platforms,
    int TeamTotal,
    int TeamDone);

public sealed record BoardAssignee(long ContentId, long UserId, string DisplayName, AssignmentStatus Status, int ProgressPercent, DateOnly? Deadline);

public sealed record NewAssignment(long UserId, DateOnly? Deadline);

public sealed record DeadlineChange(long AssignmentId, DateOnly? Deadline);
