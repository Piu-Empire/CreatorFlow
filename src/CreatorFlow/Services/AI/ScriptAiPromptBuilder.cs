using System.Text;
using CreatorFlow.Models;
using CreatorFlow.Models.Enums;

namespace CreatorFlow.Services.AI;

/// <summary>
/// Dựng prompt gửi AI từ thông tin Content đã chuẩn bị trong <see cref="AiScriptRequest"/> (SCRUM-58).
/// Hàm thuần, không gọi mạng nên kiểm thử được offline.
///   - Outline: đề xuất dàn ý (Hook / Body / Call To Action) từ thông tin Content.
///   - Script : viết kịch bản đầy đủ; nếu đã có script thì bám theo/cải thiện.
/// </summary>
public static class ScriptAiPromptBuilder
{
    /// <summary>Mốc đoạn mà editor dùng, để kết quả AI dán vào là tách đoạn được ngay.</summary>
    public const string SectionGuide = "[Hook 0-3s], [Body], [Call To Action]";

    public static string Build(AiScriptRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var prompt = new StringBuilder();
        prompt.AppendLine("Bạn là trợ lý biên kịch cho CreatorFlow.");
        prompt.AppendLine(request.RequestType == AiRequestType.Outline
            ? "Nhiệm vụ: đề xuất DÀN Ý (outline) cho nội dung dưới đây."
            : "Nhiệm vụ: viết KỊCH BẢN cho nội dung dưới đây.");
        prompt.AppendLine();
        prompt.AppendLine("== Thông tin Content ==");
        prompt.AppendLine($"- Tiêu đề: {request.Title}");
        if (!string.IsNullOrWhiteSpace(request.Description))
            prompt.AppendLine($"- Mô tả: {request.Description}");
        if (!string.IsNullOrWhiteSpace(request.ContentType))
            prompt.AppendLine($"- Loại nội dung: {request.ContentType}");
        prompt.AppendLine($"- Ưu tiên: {request.Priority}");
        prompt.AppendLine($"- Nền tảng: {(request.Platforms.Count == 0 ? "(không chỉ định)" : string.Join(", ", request.Platforms))}");

        if (!string.IsNullOrWhiteSpace(request.Script))
        {
            prompt.AppendLine();
            prompt.AppendLine(request.RequestType == AiRequestType.Outline
                ? "== Kịch bản hiện có (tham khảo để lên dàn ý) =="
                : "== Kịch bản hiện có (bám theo và cải thiện) ==");
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
        if (request.RequestType == AiRequestType.Outline)
        {
            prompt.AppendLine($"Trình bày dàn ý theo các mốc: {SectionGuide}.");
            prompt.AppendLine("Mỗi mốc là một dòng mở đầu bằng dấu mốc [ ... ], kèm 1-3 gạch đầu dòng ngắn cho mốc đó.");
        }
        else
        {
            prompt.AppendLine($"Chia kịch bản theo các mốc ở đầu dòng: {SectionGuide}.");
            prompt.AppendLine("Viết nội dung nói/ thoại cụ thể cho từng mốc.");
        }

        prompt.AppendLine("Không giải thích, không thêm lời dẫn, không dùng markdown code fence.");
        return prompt.ToString();
    }
}
