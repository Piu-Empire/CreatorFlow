using System;

namespace CreatorFlow.Models;

/// <summary>
/// Bảng nghiệp vụ chính (mục 5.4, bảng 10 - Contents).
/// Chỉ giữ các cột liên quan trực tiếp tới Workflow; các cột khác
/// (script, deadline, priority...) bổ sung dần khi làm các module khác.
/// </summary>
public class Content
{
    public long Id { get; set; }
    public long ProjectId { get; set; }
    public long? IdeaId { get; set; }
    public string Title { get; set; } = string.Empty;
    public ContentStatus Status { get; set; }
    public long CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
}