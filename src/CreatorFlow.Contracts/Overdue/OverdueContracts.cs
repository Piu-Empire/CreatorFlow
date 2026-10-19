namespace CreatorFlow.Contracts.Overdue;

/// <summary>Một Creator đang được giao Content quá hạn (để Dashboard/Notification biết gửi cho ai).</summary>
public sealed record OverdueAssigneeResponse(long UserId, string DisplayName);

/// <summary>
/// Một Content quá deadline nhưng chưa hoàn thành. DaysLate là số ngày trễ so với mốc AsOfDate của response.
/// </summary>
public sealed record OverdueContentResponse(
    long ContentId,
    long ProjectId,
    string ContentCode,
    string ContentTitle,
    string Status,
    DateOnly Deadline,
    int DaysLate,
    IReadOnlyList<OverdueAssigneeResponse> Assignees,
    IReadOnlyList<string> Platforms);

/// <summary>
/// Danh sách Content quá hạn của một Project, kèm mốc ngày đã dùng để xét (UTC) và tổng số dòng.
/// Dữ liệu cho Dashboard/Notification dùng lại, không tự tính riêng.
/// </summary>
public sealed record OverdueResponse(
    long ProjectId,
    DateOnly AsOfDate,
    int Total,
    IReadOnlyList<OverdueContentResponse> Items);
