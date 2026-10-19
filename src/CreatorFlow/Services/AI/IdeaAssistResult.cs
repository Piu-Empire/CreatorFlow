using CreatorFlow.Models;

namespace CreatorFlow.Services.AI;

/// <summary>Kết quả một lần gọi AI gợi ý Idea/Title (SCRUM-57). Không ném lỗi ra UI.</summary>
public sealed class IdeaAssistResult
{
    public bool Succeeded { get; init; }

    public IReadOnlyList<IdeaSuggestion> Suggestions { get; init; } = Array.Empty<IdeaSuggestion>();

    public string? ErrorMessage { get; init; }

    public static IdeaAssistResult Success(IReadOnlyList<IdeaSuggestion> suggestions) =>
        new() { Succeeded = true, Suggestions = suggestions };

    public static IdeaAssistResult Failure(string errorMessage) =>
        new() { Succeeded = false, ErrorMessage = errorMessage };
}
