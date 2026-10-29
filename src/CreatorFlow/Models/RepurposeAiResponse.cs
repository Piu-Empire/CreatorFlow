namespace CreatorFlow.Models;

/// <summary>Kết quả AI Repurpose (SCRUM-38). Title/Description có thể null khi AI trả sai định dạng.</summary>
public sealed record RepurposeAiResponse(
    bool Succeeded,
    string? Title,
    string? Description,
    string? Script,
    string? ErrorMessage,
    bool IsStructured = true)
{
    public static RepurposeAiResponse Success(string? title, string? description, string? script, bool isStructured = true) =>
        new(true, title, description, script, null, isStructured);

    public static RepurposeAiResponse Failure(string message) =>
        new(false, null, null, null, message);
}

/// <summary>Kết quả tách output AI thành 3 phần; IsStructured=false khi thiếu mốc.</summary>
public sealed record RepurposeParsedContent(
    string? Title,
    string? Description,
    string? Script,
    bool IsStructured);
