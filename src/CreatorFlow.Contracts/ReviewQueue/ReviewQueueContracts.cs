namespace CreatorFlow.Contracts.ReviewQueue;

/// <summary>
/// Một dòng Review Queue: Content đang có Review PENDING (dành cho Owner/Manager).
/// Người gửi duyệt và thời điểm lấy từ Review, không lấy từ client.
/// </summary>
public sealed record PendingReviewResponse(
    long ContentId,
    long ProjectId,
    string ContentCode,
    string ContentTitle,
    string SubmittedByName,
    DateTime SubmittedAt,
    int ReviewNo);

/// <summary>Danh sách Content đang chờ duyệt của một Project, cũ nhất trước.</summary>
public sealed record ReviewQueueResponse(long ProjectId, IReadOnlyList<PendingReviewResponse> Items);
