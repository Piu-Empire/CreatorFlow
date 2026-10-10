namespace CreatorFlow.Api.Models.Workflow;

public enum WorkflowContentStatus { Idea, Script, Production, Editing, Review, Ready, Published, Archived }

/// <summary>Thông tin tối thiểu của Content cần để kiểm tra quyền chuyển trạng thái.</summary>
public sealed record WorkflowContent(
    long ContentId,
    long ProjectId,
    WorkflowContentStatus Status,
    long CreatedBy,
    DateTime UpdatedAt);

/// <summary>Một dòng lịch sử đổi trạng thái.</summary>
public sealed record StatusHistoryEntry(
    long HistoryId,
    string? FromStatus,
    string ToStatus,
    long? ChangedBy,
    string? ChangedByName,
    string? Note,
    DateTime ChangedAt);
