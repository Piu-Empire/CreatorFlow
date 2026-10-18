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
    IReadOnlyList<BoardAssigneeResponse> Assignees,
    string Sprint = CreatorFlow.Contracts.ContentDetail.SprintCatalog.DefaultSprint);

/// <summary>Các thẻ của một Project (không gồm Content đã Archived), theo thứ tự ContentId.</summary>
public sealed record BoardResponse(IReadOnlyList<BoardCardResponse> Cards);

/// <summary>
/// Query của Kho Content. Các tiêu chí được kết hợp bằng AND; null/rỗng nghĩa là bỏ qua tiêu chí đó.
/// CreatorId là UserId của Creator được giao. DeadlineFrom/DeadlineTo là hai đầu bao gồm.
/// SortBy hợp lệ: title, platform, status, creator, priority, deadline, tag.
/// </summary>
public sealed class ContentLibraryQuery
{
    public string? Title { get; set; }
    public string? Platform { get; set; }
    public string? Status { get; set; }
    public long? CreatorId { get; set; }
    public string? Priority { get; set; }
    public bool? HasDeadline { get; set; }
    public DateOnly? DeadlineFrom { get; set; }
    public DateOnly? DeadlineTo { get; set; }
    public string? Tag { get; set; }
    public string? SortBy { get; set; }
    public bool? SortDescending { get; set; }
}
