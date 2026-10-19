using CreatorFlow.Contracts.ContentDetail;

namespace CreatorFlow.Api.Models.ContentDetail;

/// <summary>Một dòng contents gộp nền tảng. Status/Priority là PascalCase.</summary>
public sealed record ContentDetailRecord(
    long ContentId,
    long ProjectId,
    string Title,
    string Description,
    string ContentType,
    string Status,
    string Priority,
    DateOnly? Deadline,
    DateOnly? PlannedPublishAt,
    long CreatedBy,
    IReadOnlyList<string> Platforms,
    string Sprint = CreatorFlow.Contracts.ContentDetail.SprintCatalog.DefaultSprint);

/// <summary>Dữ liệu đã kiểm tra, sẵn sàng ghi. Priority là giá trị enum DB (UPPER).</summary>
public sealed record ContentUpdate(
    string Title,
    string Description,
    string ContentType,
    string PriorityDb,
    DateOnly? Deadline,
    DateOnly? PlannedPublishAt,
    IReadOnlyList<string> Platforms,
    string Sprint = CreatorFlow.Contracts.ContentDetail.SprintCatalog.DefaultSprint);

/// <summary>Content mới đã kiểm tra, sẵn sàng ghi. Status/Priority là giá trị enum DB (UPPER).</summary>
public sealed record NewContent(
    long ProjectId,
    string StatusDb,
    ContentUpdate Details,
    long CreatedBy);

/// <summary>Script thô đang lưu (null = chưa có) cùng thông tin để tính quyền.</summary>
public sealed record ScriptRecord(long ContentId, long ProjectId, string Status, long CreatedBy, string? Script);

public sealed record ActivityRecord(
    DateTime Timestamp,
    string ActorName,
    string Description,
    string? Feedback,
    string Kind = ActivityKinds.StatusChange,
    string? FromStatus = null,
    string? ToStatus = null,
    int? ReviewNo = null,
    string? ReviewStatus = null);
