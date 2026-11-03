namespace CreatorFlow.Models;

/// <summary>
/// Kết quả AI phân tích hiệu suất (SCRUM-60). Suggestions luôn khác null (rỗng khi thất bại)
/// để UI không phải kiểm tra null. IsStructured=false khi AI trả về sai định dạng.
/// </summary>
public sealed record PerformanceAiResponse(
    bool Succeeded,
    string? Remarks,
    IReadOnlyList<string> Suggestions,
    string? ErrorMessage,
    bool IsStructured = true)
{
    public static PerformanceAiResponse Success(
        string remarks, IReadOnlyList<string> suggestions, bool isStructured = true) =>
        new(true, remarks, suggestions, null, isStructured);

    public static PerformanceAiResponse Failure(string message) =>
        new(false, null, Array.Empty<string>(), message);
}

/// <summary>Kết quả tách output AI thành 2 phần; IsStructured=false khi thiếu mốc.</summary>
public sealed record PerformanceParsedContent(
    string Remarks,
    IReadOnlyList<string> Suggestions,
    bool IsStructured);
