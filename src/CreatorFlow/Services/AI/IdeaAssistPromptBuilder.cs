using System.Text;
using CreatorFlow.Models;

namespace CreatorFlow.Services.AI;

/// <summary>
/// Dựng prompt gửi cho AI từ chủ đề / nền tảng / mục tiêu người dùng nhập (SCRUM-57).
/// Hàm thuần, không gọi mạng nên kiểm thử được offline.
/// </summary>
public static class IdeaAssistPromptBuilder
{
    /// <summary>Số gợi ý yêu cầu AI trả về mỗi lần.</summary>
    public const int SuggestionCount = 5;

    public static string Build(IdeaSuggestRequest request)
    {
        string platforms = request.Platforms.Count == 0
            ? "(không chỉ định)"
            : string.Join(", ", request.Platforms);

        var prompt = new StringBuilder();
        prompt.AppendLine("Bạn là trợ lý sáng tạo nội dung cho CreatorFlow.");
        prompt.AppendLine($"Hãy đề xuất đúng {SuggestionCount} ý tưởng nội dung mới dựa trên:");
        prompt.AppendLine($"- Chủ đề: {request.Topic}");
        prompt.AppendLine($"- Nền tảng: {platforms}");
        prompt.AppendLine($"- Mục tiêu: {request.Goal}");
        prompt.AppendLine();
        prompt.AppendLine("Định dạng trả về: mỗi ý tưởng trên MỘT dòng, theo mẫu:");
        prompt.AppendLine("Tiêu đề ngắn gọn | Mô tả ngắn 1-2 câu");
        prompt.AppendLine("Không đánh số, không markdown, không thêm dòng trống giữa các ý tưởng.");
        return prompt.ToString();
    }
}
