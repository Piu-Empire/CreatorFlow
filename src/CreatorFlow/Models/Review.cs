using System;

namespace CreatorFlow.Models;

/// <summary>
/// Mỗi lần Submit Review tạo 1 dòng mới (mục 5.4, bảng 19).
/// KHÔNG ghi đè Review cũ. UNIQUE(ContentId, ReviewNo) (mục 5.6).
/// </summary>
public class Review
{
    public long Id { get; set; }
    public long ContentId { get; set; }
    public int ReviewNo { get; set; }
    public long SubmittedByUserId { get; set; }
    public long? ReviewerUserId { get; set; }
    public ReviewStatus Status { get; set; }
    public string? Feedback { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? DecidedAt { get; set; }
}