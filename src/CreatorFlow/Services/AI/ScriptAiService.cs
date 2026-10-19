using CreatorFlow.AI.Models;
using CreatorFlow.Models;
using AiClient = CreatorFlow.AI.IAIService;

namespace CreatorFlow.Services.AI;

/// <summary>
/// Cài đặt thật của cổng <see cref="IAiService"/> cho script (SCRUM-58): adapter nối cổng script với
/// lõi AI client <see cref="AiClient"/> (đã có từ SCRUM-57, đọc cấu hình từ .env).
///   - Dựng prompt bằng <see cref="ScriptAiPromptBuilder"/> (không để lộ prompt ở tầng service khác).
///   - Map <see cref="AiResult"/> sang <see cref="AiScriptResponse"/>; lỗi mạng/timeout trả Failure thay vì ném.
/// Lưu ý kiến trúc: hiện là POC gọi AI phía client — cần chuyển sang Backend sau.
/// </summary>
public sealed class ScriptAiService : IAiService
{
    private readonly AiClient? _ai;

    public ScriptAiService(AiClient? ai) => _ai = ai;

    public bool IsConfigured => _ai is not null;

    public async Task<AiScriptResponse> RequestScriptAssistAsync(
        AiScriptRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_ai is null)
            return AiScriptResponse.Failure(NotConfiguredAiService.Message);

        AiResult result;
        try
        {
            result = await _ai.AskAsync(ScriptAiPromptBuilder.Build(request), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Để ScriptService phân biệt hủy theo yêu cầu người dùng và ném lại.
            throw;
        }
        catch (Exception)
        {
            // Không lộ chi tiết dịch vụ ngoài (có thể chứa URL/khóa) ra giao diện.
            return AiScriptResponse.Failure("Không thể kết nối dịch vụ AI lúc này. Vui lòng thử lại sau.");
        }

        if (!result.Success)
            return AiScriptResponse.Failure(result.ErrorMessage ?? "Không nhận được kết quả từ AI.");

        string text = (result.Content ?? string.Empty).Trim();
        return text.Length == 0
            ? AiScriptResponse.Failure("AI không trả về nội dung.")
            : AiScriptResponse.Success(text);
    }
}
