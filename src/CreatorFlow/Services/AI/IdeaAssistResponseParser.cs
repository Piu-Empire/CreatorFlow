using System.Text.Json;
using System.Text.RegularExpressions;
using CreatorFlow.Models;

namespace CreatorFlow.Services.AI;

/// <summary>
/// Phân tích nội dung AI trả về thành danh sách gợi ý Idea/Title (SCRUM-57).
/// Chịu được nhiều định dạng: mảng JSON, dòng "Tiêu đề | Mô tả", danh sách đánh số, text thô.
/// Hàm thuần, kiểm thử được offline.
/// </summary>
public static class IdeaAssistResponseParser
{
    public static List<IdeaSuggestion> Parse(string? text, int max)
    {
        var suggestions = new List<IdeaSuggestion>();
        if (string.IsNullOrWhiteSpace(text) || max <= 0)
        {
            return suggestions;
        }

        string cleaned = StripCodeFences(text);

        if (cleaned.TrimStart().StartsWith('[') && TryParseJsonArray(cleaned, suggestions, max))
        {
            return suggestions;
        }

        foreach (string rawLine in cleaned.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            if (suggestions.Count >= max)
            {
                break;
            }

            string line = StripListMarker(rawLine.Trim());
            if (line.Length == 0)
            {
                continue;
            }

            int separator = line.IndexOf('|');
            if (separator >= 0)
            {
                string title = line[..separator].Trim().Trim('*').Trim();
                string description = line[(separator + 1)..].Trim().Trim('*').Trim();
                if (title.Length > 0 || description.Length > 0)
                {
                    suggestions.Add(new IdeaSuggestion { Title = title, Description = description });
                }
            }
            else
            {
                suggestions.Add(new IdeaSuggestion { Title = line, Description = string.Empty });
            }
        }

        return suggestions;
    }

    private static bool TryParseJsonArray(string json, List<IdeaSuggestion> output, int max)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (JsonElement element in document.RootElement.EnumerateArray())
            {
                if (output.Count >= max)
                {
                    break;
                }

                if (element.ValueKind == JsonValueKind.String)
                {
                    string title = element.GetString()?.Trim() ?? string.Empty;
                    if (title.Length > 0)
                    {
                        output.Add(new IdeaSuggestion { Title = title });
                    }

                    continue;
                }

                if (element.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                string itemTitle = ReadProperty(element, "title") ?? ReadProperty(element, "Title") ?? string.Empty;
                string itemDescription =
                    ReadProperty(element, "description") ?? ReadProperty(element, "Description") ?? string.Empty;

                if (itemTitle.Trim().Length > 0 || itemDescription.Trim().Length > 0)
                {
                    output.Add(new IdeaSuggestion
                    {
                        Title = itemTitle.Trim(),
                        Description = itemDescription.Trim(),
                    });
                }
            }

            return output.Count > 0;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string? ReadProperty(JsonElement element, string name) =>
        element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string StripCodeFences(string text)
    {
        string trimmed = text.Trim();
        if (!trimmed.StartsWith("```"))
        {
            return trimmed;
        }

        int firstNewline = trimmed.IndexOf('\n');
        string body = firstNewline >= 0 ? trimmed[(firstNewline + 1)..] : string.Empty;
        int closing = body.LastIndexOf("```", StringComparison.Ordinal);
        return (closing >= 0 ? body[..closing] : body).Trim();
    }

    /// <summary>Bỏ gạch đầu dòng / số thứ tự / in đậm markdown ở đầu dòng.</summary>
    private static string StripListMarker(string line) =>
        ListMarkerPattern.Replace(line, string.Empty).Trim().Trim('*').Trim();

    private static readonly Regex ListMarkerPattern =
        new(@"^\s*(?:[-*•]|\d+[.)])\s*", RegexOptions.Compiled);
}
