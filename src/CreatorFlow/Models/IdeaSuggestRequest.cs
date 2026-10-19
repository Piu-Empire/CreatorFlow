namespace CreatorFlow.Models;

/// <summary>
/// Dữ liệu người dùng nhập ở màn Trợ lý AI (SCRUM-57): chủ đề, nền tảng và mục tiêu.
/// </summary>
public sealed class IdeaSuggestRequest
{
    public string Topic { get; init; } = string.Empty;

    public List<string> Platforms { get; init; } = new();

    public string Goal { get; init; } = string.Empty;
}
