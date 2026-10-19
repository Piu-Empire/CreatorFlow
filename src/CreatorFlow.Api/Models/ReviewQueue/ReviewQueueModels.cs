namespace CreatorFlow.Api.Models.ReviewQueue;

/// <summary>
/// Một Review đang PENDING kèm thông tin Content để hiển thị Review Queue.
/// </summary>
public sealed record PendingReviewRecord(
    long ContentId,
    long ProjectId,
    string ContentTitle,
    string SubmittedByName,
    DateTime SubmittedAt,
    int ReviewNo);
