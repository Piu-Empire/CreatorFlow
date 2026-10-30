using CreatorFlow.AI.Models;
using CreatorFlow.Models;
using AiClient = CreatorFlow.AI.IAIService;

namespace CreatorFlow.Services.AI;

/// <summary>
/// Cài đặt thật của <see cref="IPerformanceAiService"/> (SCRUM-60): adapter nối cổng phân tích hiệu suất
/// với lõi AI client (tái dùng SCRUM-57/58/59). Lỗi mạng/timeout trả Failure thay vì ném ra UI.
/// </summary>
public sealed class PerformanceAiService : IPerformanceAiService
{
    private readonly AiClient? _ai;

    public PerformanceAiService(AiClient? ai) => _ai = ai;

    public bool IsConfigured => _ai is not null;

    public async Task<PerformanceAiResponse> AnalyzeAsync(
        PerformanceAiRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_ai is null)
            return PerformanceAiResponse.Failure(NotConfiguredAiService.Message);

        AiResult result;
        try
        {
            result = await _ai.AskAsync(PerformanceAiPromptBuilder.Build(request), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Để UI phân biệt hủy theo yêu cầu người dùng và ném lại.
            throw;
        }
        catch (Exception)
        {
            // Không lộ chi tiết dịch vụ ngoài (có thể chứa URL/khóa) ra giao diện.
            return PerformanceAiResponse.Failure("Không thể kết nối dịch vụ AI lúc này. Vui lòng thử lại sau.");
        }

        if (!result.Success)
            return PerformanceAiResponse.Failure(result.ErrorMessage ?? "Không nhận được kết quả từ AI.");

        string text = (result.Content ?? string.Empty).Trim();
        if (text.Length == 0)
            return PerformanceAiResponse.Failure("AI không trả về nội dung.");

        PerformanceParsedContent parsed = PerformanceAiResponseParser.Parse(text);
        if (parsed.Remarks.Length == 0 && parsed.Suggestions.Count == 0)
            return PerformanceAiResponse.Failure("AI không trả về nội dung.");

        return PerformanceAiResponse.Success(parsed.Remarks, parsed.Suggestions, parsed.IsStructured);
    }
}
