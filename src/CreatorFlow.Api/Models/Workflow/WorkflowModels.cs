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

/// <summary>
/// Review đang PENDING mới nhất của một Content kèm nhãn Content.
/// Dùng để dựng thông báo gửi duyệt / kết quả duyệt (SCRUM-50) mà không phải đọc thêm bảng contents.
/// </summary>
public sealed record PendingReviewInfo(
    long ContentId,
    long ProjectId,
    string ContentCode,
    string ContentTitle,
    int ReviewNo,
    long SubmittedBy);
