using CreatorFlow.Models;

namespace CreatorFlow.Services.AI;

/// <summary>
/// Tách output AI phân tích hiệu suất thành Nhận xét / Gợi ý (SCRUM-60).
/// Chịu lỗi: thiếu mốc thì đưa toàn bộ text vào Nhận xét và đánh dấu IsStructured=false.
/// Hàm thuần, kiểm thử offline.
/// </summary>
public static class PerformanceAiResponseParser
{
    /// <summary>Số gợi ý tối đa giữ lại; phần dư bị bỏ để panel không quá dài.</summary>
    public const int MaxSuggestions = 8;

    private static readonly char[] BulletPrefixes = { '-', '*', '•' };

    public static PerformanceParsedContent Parse(string? raw)
    {
        string text = (raw ?? string.Empty).Replace("\r\n", "\n").Trim();
        if (text.Length == 0)
            return new PerformanceParsedContent(string.Empty, Array.Empty<string>(), false);

        int remarksIndex = text.IndexOf(PerformanceAiPromptBuilder.RemarksMarker, StringComparison.OrdinalIgnoreCase);
        int suggestionsIndex = text.IndexOf(PerformanceAiPromptBuilder.SuggestionsMarker, StringComparison.OrdinalIgnoreCase);

        // Thiếu mốc hoặc mốc sai thứ tự: coi toàn bộ là nhận xét, UI sẽ cảnh báo định dạng.
        if (remarksIndex < 0 || suggestionsIndex < 0 || suggestionsIndex < remarksIndex)
            return new PerformanceParsedContent(text, Array.Empty<string>(), false);

        int remarksStart = remarksIndex + PerformanceAiPromptBuilder.RemarksMarker.Length;
        string remarks = text[remarksStart..suggestionsIndex].Trim();
        string suggestionsBlock = text[(suggestionsIndex + PerformanceAiPromptBuilder.SuggestionsMarker.Length)..];

        return new PerformanceParsedContent(remarks, ParseSuggestions(suggestionsBlock), true);
    }

    private static IReadOnlyList<string> ParseSuggestions(string block)
    {
        var suggestions = new List<string>();
        var plainLines = new List<string>();

        foreach (string line in block.Replace("\r\n", "\n").Split('\n'))
        {
            string trimmed = line.Trim();
            if (trimmed.Length == 0) continue;

            string item = TrimBullet(trimmed);
            if (item.Length == 0)
            {
                plainLines.Add(trimmed);
                continue;
            }

            suggestions.Add(item);
            if (suggestions.Count == MaxSuggestions) return suggestions;
        }

        // AI trả về đoạn văn không có gạch đầu dòng: vẫn giữ nội dung thay vì bỏ trắng.
        if (suggestions.Count == 0)
            return plainLines.Take(MaxSuggestions).ToList();

        return suggestions;
    }

    private static string TrimBullet(string line)
    {
        string value = line.Trim();
        if (value.Length == 0) return string.Empty;

        // "- ", "* ", "• " hoặc danh sách đánh số "1. ", "2) ".
        if (Array.IndexOf(BulletPrefixes, value[0]) >= 0)
            return value[1..].Trim();

        int dot = value.IndexOfAny(new[] { '.', ')' });
        if (dot > 0 && dot <= 3 && value[..dot].All(char.IsDigit))
            return value[(dot + 1)..].Trim();

        return string.Empty;
    }
}
