using CreatorFlow.Models;
using CreatorFlow.Models.Enums;

namespace CreatorFlow.Services.AI;

/// <summary>
/// Chuyển một gợi ý AI (đã được người dùng chỉnh) thành <see cref="IdeaDraft"/> để lưu (SCRUM-57).
/// Hàm thuần, kiểm thử được offline.
/// </summary>
public static class IdeaSuggestionDraftMapper
{
    public static IdeaDraft ToDraft(
        IdeaSuggestRequest request,
        string? title,
        string? description,
        IEnumerable<string>? tags = null)
    {
        return new IdeaDraft
        {
            Title = title?.Trim() ?? string.Empty,
            Description = description?.Trim() ?? string.Empty,
            Note = BuildNote(request),
            Status = IdeaStatus.Backlog,
            Tags = (tags ?? request.Platforms)
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Trim())
                .Distinct()
                .ToList(),
        };
    }

    private static string BuildNote(IdeaSuggestRequest request)
    {
        var parts = new List<string>
        {
            $"Chủ đề: {request.Topic}",
            $"Mục tiêu: {request.Goal}",
        };

        if (request.Platforms.Count > 0)
        {
            parts.Add($"Nền tảng: {string.Join(", ", request.Platforms)}");
        }

        parts.Add("Nguồn: Trợ lý AI");
        return string.Join(Environment.NewLine, parts);
    }
}
