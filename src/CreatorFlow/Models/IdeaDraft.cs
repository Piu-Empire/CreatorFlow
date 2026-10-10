using CreatorFlow.Models.Enums;

namespace CreatorFlow.Models;

/// <summary>Dữ liệu người dùng nhập khi thêm / sửa một Idea.</summary>
public class IdeaDraft
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public IdeaStatus Status { get; set; } = IdeaStatus.Draft;
    public List<string> Tags { get; set; } = new();
}
