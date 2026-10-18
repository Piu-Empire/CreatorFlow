namespace CreatorFlow.Api.Models.Board;

/// <summary>Một dòng contents gộp nền tảng, tag và cờ Review đang chờ để hiện thẻ trên Board. Status/Priority là PascalCase.</summary>
public sealed record BoardCard(
    long ContentId,
    long ProjectId,
    string Code,
    string Title,
    string Description,
    string Status,
    string Priority,
    DateOnly? Deadline,
    DateTime? PlannedPublishAt,
    IReadOnlyList<string> Platforms,
    IReadOnlyList<string> Tags,
    bool HasPendingReview,
    string Sprint = CreatorFlow.Contracts.ContentDetail.SprintCatalog.DefaultSprint);
