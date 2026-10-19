namespace CreatorFlow.Models;

/// <summary>
/// Một gợi ý Idea/Title do AI trả về (SCRUM-57). Người dùng xem và chỉnh trước khi lưu.
/// </summary>
public sealed class IdeaSuggestion
{
    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;
}
