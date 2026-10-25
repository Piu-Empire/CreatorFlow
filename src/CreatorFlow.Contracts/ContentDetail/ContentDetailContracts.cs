namespace CreatorFlow.Contracts.ContentDetail;

/// <summary>
/// Chi tiết Content cho drawer Content Detail. Status/Priority là tên enum PascalCase ("Editing", "High"...).
/// Deadline / PlannedPublishAt là NGÀY thuần. Role là vai trò của người đang đăng nhập trong Project.
/// </summary>
public sealed record ContentDetailResponse(
    long ContentId,
    long ProjectId,
    string Code,
    string Title,
    string Description,
    string ContentType,
    string Status,
    string Priority,
    DateOnly? Deadline,
    DateOnly? PlannedPublishAt,
    long CreatedBy,
    IReadOnlyList<string> Platforms,
    string Role,
    string Sprint = SprintCatalog.DefaultSprint,
    long? SourceContentId = null);

/// <summary>Sửa thông tin lập kế hoạch của Content (Status chỉ đổi qua Workflow). Người sửa lấy từ JWT.</summary>
public sealed record UpdateContentRequest(
    string Title,
    string? Description,
    string? ContentType,
    string Priority,
    DateOnly? Deadline,
    DateOnly? PlannedPublishAt,
    IReadOnlyList<string>? Platforms,
    string? Sprint = null);

/// <summary>
/// Tạo Content mới trong Project. Status là giai đoạn ban đầu ("Idea", "Script"...; không tạo thẳng ở Review/Archived).
/// AssigneeUserId (tuỳ chọn) là Creator được giao ngay khi tạo. Người tạo lấy từ JWT.
/// </summary>
public sealed record CreateContentRequest(
    string Title,
    string? Description,
    string? ContentType,
    string Priority,
    string? Status,
    DateOnly? Deadline,
    DateOnly? PlannedPublishAt,
    IReadOnlyList<string>? Platforms,
    long? AssigneeUserId,
    string? Sprint = null,
    long? SourceContentId = null);

/// <summary>Tên các nền tảng đang bật — nguồn cho menu chọn platform.</summary>
public sealed record PlatformOptionsResponse(IReadOnlyList<string> Platforms);

/// <summary>Script của Content kèm quyền sửa. Version là dấu vân tay của bản đang lưu (client coi như chuỗi mờ).</summary>
public sealed record ScriptResponse(
    long ContentId,
    long ProjectId,
    string Text,
    string Version,
    bool CanEdit,
    bool IsContentLocked,
    string? ReadOnlyReason);

/// <summary>Lưu script: ExpectedVersion là Version lúc người dùng mở/lưu lần cuối (phát hiện xung đột).</summary>
public sealed record SaveScriptRequest(string? Text, string ExpectedVersion);

/// <summary>Loại sự kiện trên Activity timeline.</summary>
public static class ActivityKinds
{
    public const string StatusChange = "StatusChange";
    public const string ReviewSubmitted = "ReviewSubmitted";
    public const string ReviewDecided = "ReviewDecided";
}

/// <summary>
/// Một dòng Activity timeline (gộp lịch sử đổi trạng thái + Review), tăng dần theo thời gian.
/// FromStatus/ToStatus là PascalCase ("Editing"...); ReviewStatus là "Pending"/"Approved"/"Rejected".
/// Description giữ format cũ cho nơi hiển thị cũ; UI mới dùng các field cấu trúc.
/// </summary>
public sealed record ActivityItemResponse(
    DateTime Timestamp,
    string ActorName,
    string Description,
    string? Feedback,
    string Kind = ActivityKinds.StatusChange,
    string? FromStatus = null,
    string? ToStatus = null,
    int? ReviewNo = null,
    string? ReviewStatus = null);

public sealed record ActivityResponse(long ContentId, IReadOnlyList<ActivityItemResponse> Items);

/// <summary>Một liên kết tham khảo gắn với Content. URL không được server truy cập để kiểm tra.</summary>
public sealed record ContentReferenceResponse(
    long ReferenceId,
    long ContentId,
    string DisplayName,
    string ResourceUrl,
    long? AddedBy,
    DateTime CreatedAt);

public sealed record ContentReferencesResponse(
    long ContentId,
    bool CanManage,
    IReadOnlyList<ContentReferenceResponse> References);

public sealed record CreateContentReferenceRequest(string DisplayName, string ResourceUrl);

public sealed record DeleteContentReferenceResponse(long ContentId, long ReferenceId);

public sealed record SaveContentReferenceChangesRequest(
    IReadOnlyList<CreateContentReferenceRequest>? Additions,
    IReadOnlyList<long>? DeletedReferenceIds);
