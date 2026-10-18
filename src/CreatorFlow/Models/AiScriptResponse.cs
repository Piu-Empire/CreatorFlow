namespace CreatorFlow.Models;

/// <summary>Kết quả AIService trả về. Lỗi kết nối/chưa cấu hình được báo bằng Failure, không ném exception ra UI.</summary>
public sealed record AiScriptResponse(bool Succeeded, string? Text, string? ErrorMessage)
{
    public static AiScriptResponse Success(string text) => new(true, text, null);

    public static AiScriptResponse Failure(string message) => new(false, null, message);
}
