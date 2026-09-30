using System;

namespace CreatorFlow.Models;

/// <summary>
/// Lưu mọi lần chuyển trạng thái Content (mục 5.4, bảng 18).
/// Đây là dữ liệu lịch sử; Contents.Status vẫn là trạng thái hiện tại.
/// </summary>
public class ContentStatusHistory
{
    public long Id { get; set; }
    public long ContentId { get; set; }
    public ContentStatus FromStatus { get; set; }
    public ContentStatus ToStatus { get; set; }
    public long ChangedByUserId { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? Note { get; set; }
}