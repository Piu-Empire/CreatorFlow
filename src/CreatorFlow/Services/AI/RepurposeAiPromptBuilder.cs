using System.Text;
using CreatorFlow.Models;

namespace CreatorFlow.Services.AI;

/// <summary>
/// Dựng prompt Repurpose (SCRUM-59) từ Content gốc + platform đích.
/// Hàm thuần, không gọi mạng nên kiểm thử được offline.
/// </summary>
public static class RepurposeAiPromptBuilder
{
    public const string SectionGuide = "[Hook 0-3s], [Body], [Call To Action]";

    public static string Build(RepurposeAiRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var prompt = new StringBuilder();
        prompt.AppendLine("Bạn là trợ lý biên kịch cho CreatorFlow.");
        prompt.AppendLine($"Nhiệm vụ: viết lại nội dung dưới đây cho nền tảng {request.TargetPlatform}.");
        prompt.AppendLine();
        prompt.AppendLine("== Content gốc ==");
        prompt.AppendLine($"- Tiêu đề: {request.Title}");
        if (!string.IsNullOrWhiteSpace(request.Description))
            prompt.AppendLine($"- Mô tả: {request.Description}");
        if (!string.IsNullOrWhiteSpace(request.ContentType))
            prompt.AppendLine($"- Loại nội dung: {request.ContentType}");
        prompt.AppendLine($"- Mức ưu tiên: {request.Priority}");
        prompt.AppendLine($"- Nền tảng gốc: {(request.SourcePlatforms.Count == 0 ? "(không chỉ định)" : string.Join(", ", request.SourcePlatforms))}");
        prompt.AppendLine($"- Nền tảng đích: {request.TargetPlatform}");

        if (!string.IsNullOrWhiteSpace(request.Script))
        {
            prompt.AppendLine();
            prompt.AppendLine("== Kịch bản gốc (tham khảo) ==");
            prompt.AppendLine(request.Script);
            if (request.IsScriptTruncated)
                prompt.AppendLine("(Kịch bản đã được rút gọn để gửi đi.)");
        }

        if (!string.IsNullOrWhiteSpace(request.Instruction))
        {
            prompt.AppendLine();
            prompt.AppendLine($"== Yêu cầu thêm của người dùng == {request.Instruction}");
        }

        prompt.AppendLine();
        prompt.AppendLine("== Định dạng trả về ==");
        prompt.AppendLine("Trả về đúng 3 phần, mỗi phần bắt đầu bằng dòng mốc sau:");
        prompt.AppendLine("== TIÊU ĐỀ ==");
        prompt.AppendLine("(một dòng tiêu đề mới, phù hợp nền tảng đích)");
        prompt.AppendLine("== MÔ TẢ ==");
        prompt.AppendLine("(1-3 câu mô tả ngắn)");
        prompt.AppendLine("== KỊCH BẢN ==");
        prompt.AppendLine($"(kịch bản chia theo các mục {SectionGuide}, viết tự nhiên cho nền tảng đích)");
        prompt.AppendLine("Không giải thích, không thêm lời dẫn, không dùng markdown code fence.");
        return prompt.ToString();
    }
}
