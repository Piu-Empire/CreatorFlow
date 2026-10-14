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
    string Role);

/// <summary>Sửa thông tin lập kế hoạch của Content (Status chỉ đổi qua Workflow). Người sửa lấy từ JWT.</summary>
public sealed record UpdateContentRequest(
    string Title,
    string? Description,
    string? ContentType,
    string Priority,
    DateOnly? Deadline,
    DateOnly? PlannedPublishAt,
    IReadOnlyList<string>? Platforms);

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
    long? AssigneeUserId);

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

/// <summary>Một dòng Activity timeline (gộp lịch sử đổi trạng thái + Review), tăng dần theo thời gian.</summary>
public sealed record ActivityItemResponse(DateTime Timestamp, string ActorName, string Description, string? Feedback);

public sealed record ActivityResponse(long ContentId, IReadOnlyList<ActivityItemResponse> Items);
