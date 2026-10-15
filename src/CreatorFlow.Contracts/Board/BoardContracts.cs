using CreatorFlow.Contracts.Tasks;

namespace CreatorFlow.Contracts.Board;

/// <summary>
/// Một thẻ Content trên Board. Status / Priority là tên enum PascalCase ("Idea", "Script", "Medium"...).
/// Code dạng "CNT-021". Deadline là NGÀY (UTC). Assignees là các Creator đang được giao (assignment chưa Cancelled).
/// </summary>
public sealed record BoardCardResponse(
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
    IReadOnlyList<BoardAssigneeResponse> Assignees);

/// <summary>Các thẻ của một Project (không gồm Content đã Archived), theo thứ tự ContentId.</summary>
public sealed record BoardResponse(IReadOnlyList<BoardCardResponse> Cards);
