using CreatorFlow.AI;
using CreatorFlow.AI.Models;
using CreatorFlow.Models;

namespace CreatorFlow.Services.AI;

/// <summary>
/// Nghiệp vụ màn Trợ lý AI (SCRUM-57): dựng prompt từ chủ đề/nền tảng/mục tiêu, gọi cổng AI
/// (<see cref="IAIService"/>) rồi phân tích kết quả thành danh sách gợi ý Idea/Title.
/// Lưu ý kiến trúc: hiện dùng AIService phía client (đọc .env) — đây là POC, sẽ chuyển sang Backend sau
/// (xem docs/tasks/scrum-57-ai-goi-y-idea-va-title/plan.md).
/// </summary>
public sealed class IdeaAssistService
{
    private readonly IAIService? _ai;

    public IdeaAssistService(IAIService? ai) => _ai = ai;

    public bool IsConfigured => _ai is not null;

    public async Task<IdeaAssistResult> SuggestAsync(
        IdeaSuggestRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Topic))
        {
            return IdeaAssistResult.Failure("Vui lòng nhập chủ đề.");
        }

        if (string.IsNullOrWhiteSpace(request.Goal))
        {
            return IdeaAssistResult.Failure("Vui lòng nhập mục tiêu.");
        }

        if (_ai is null)
        {
            return IdeaAssistResult.Failure("Dịch vụ AI chưa được cấu hình.");
        }

        AiResult reply;
        try
        {
            reply = await _ai.AskAsync(IdeaAssistPromptBuilder.Build(request), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return IdeaAssistResult.Failure("Đã hủy yêu cầu gợi ý.");
        }

        if (!reply.Success)
        {
            return IdeaAssistResult.Failure(reply.ErrorMessage ?? "Không nhận được gợi ý từ AI.");
        }

        var suggestions = IdeaAssistResponseParser.Parse(reply.Content, IdeaAssistPromptBuilder.SuggestionCount);
        if (suggestions.Count == 0)
        {
            return IdeaAssistResult.Failure("AI không trả về gợi ý hợp lệ. Vui lòng thử lại.");
        }

        return IdeaAssistResult.Success(suggestions);
    }
}
