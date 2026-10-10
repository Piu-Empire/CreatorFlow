using CreatorFlow.Models.Enums;

namespace CreatorFlow.Models;

/// <summary>Một Idea trong Idea Bank (bảng ideas + tag từ idea_tags). Luôn thuộc đúng một Project.</summary>
public sealed class Idea
{
    public long IdeaId { get; set; }
    public long ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;

    /// <summary>Mô tả ý tưởng (chủ đề, concept).</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Ghi chú nội bộ của nhóm (ví dụ nguồn tham khảo, việc cần làm tiếp).</summary>
    public string Note { get; set; } = string.Empty;

    public IdeaStatus Status { get; set; } = IdeaStatus.Draft;
    public List<string> Tags { get; set; } = new();
    public long CreatedByUserId { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>Mã hiển thị, ví dụ IDEA-001.</summary>
    public string Code => $"IDEA-{IdeaId:000}";
}
