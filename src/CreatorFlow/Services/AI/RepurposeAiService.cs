using CreatorFlow.AI.Models;
using CreatorFlow.Models;
using AiClient = CreatorFlow.AI.IAIService;

namespace CreatorFlow.Services.AI;

/// <summary>
/// Cài đặt thật của <see cref="IRepurposeAiService"/> (SCRUM-59): adapter nối cổng Repurpose với
/// lõi AI client <see cref="AiClient"/> (tái dùng SCRUM-57/58, key ở .env — POC client).
/// Lỗi mạng/timeout trả Failure thay vì ném ra UI.
/// </summary>
public sealed class RepurposeAiService : IRepurposeAiService
{
    private readonly AiClient? _ai;

    public RepurposeAiService(AiClient? ai) => _ai = ai;

    public bool IsConfigured => _ai is not null;

    public async Task<RepurposeAiResponse> RequestRepurposeAsync(
        RepurposeAiRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_ai is null)
            return RepurposeAiResponse.Failure(NotConfiguredAiService.Message);

        AiResult result;
        try
        {
            result = await _ai.AskAsync(RepurposeAiPromptBuilder.Build(request), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Để dialog phân biệt hủy theo yêu cầu người dùng và ném lại.
            throw;
        }
        catch (Exception)
        {
            // Không lộ chi tiết dịch vụ ngoài (có thể chứa URL/khóa) ra giao diện.
            return RepurposeAiResponse.Failure("Không thể kết nối dịch vụ AI lúc này. Vui lòng thử lại sau.");
        }

        if (!result.Success)
            return RepurposeAiResponse.Failure(result.ErrorMessage ?? "Không nhận được kết quả từ AI.");

        string text = (result.Content ?? string.Empty).Trim();
        if (text.Length == 0)
            return RepurposeAiResponse.Failure("AI không trả về nội dung.");

        RepurposeParsedContent parsed = RepurposeAiResponseParser.Parse(text);
        if (!parsed.IsStructured)
            return RepurposeAiResponse.Success(null, null, parsed.Script, isStructured: false);

        if (string.IsNullOrWhiteSpace(parsed.Title) && string.IsNullOrWhiteSpace(parsed.Description) &&
            string.IsNullOrWhiteSpace(parsed.Script))
            return RepurposeAiResponse.Failure("AI không trả về nội dung.");

        return RepurposeAiResponse.Success(parsed.Title, parsed.Description, parsed.Script);
    }
}
