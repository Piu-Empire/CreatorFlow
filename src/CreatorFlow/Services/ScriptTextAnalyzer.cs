using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using CreatorFlow.Models;

namespace CreatorFlow.Services;

/// <summary>
/// Xử lý thuần văn bản cho script (không đụng DB/UI nên test được trực tiếp):
/// chuẩn hóa, thống kê, tách đoạn [Hook]/[Body]/[CTA], cắt gọn để gửi AI, và tính Version để phát hiện xung đột.
/// </summary>
public static class ScriptTextAnalyzer
{
    /// <summary>Tốc độ đọc dùng để ước tính thời lượng (từ/phút).</summary>
    public const int WordsPerMinute = 150;

    public const string DefaultSectionName = "General";

    // Dấu mốc đoạn phải nằm ở ĐẦU dòng: "[Hook 0-3s]: ..." / "[Body]" / "[Call To Action] ...".
    private static readonly Regex SectionMarker = new(
        @"^\s*\[(?<name>[^\]\n]{1,60})\]\s*:?\s*(?<rest>.*)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Chuẩn hóa để lưu: xuống dòng về "\n", bỏ ký tự NUL (PostgreSQL TEXT không nhận) và BOM, cắt khoảng trắng hai đầu.
    /// </summary>
    public static string Normalize(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Replace("\0", string.Empty)
            .Replace("\uFEFF", string.Empty)
            .Trim();
    }

    public static ScriptStatistics Analyze(string? text)
    {
        string normalized = Normalize(text);
        if (normalized.Length == 0)
            return ScriptStatistics.Empty;

        int words = CountWords(normalized);
        int lines = 1;
        foreach (char c in normalized)
        {
            if (c == '\n') lines++;
        }

        int seconds = (int)Math.Ceiling(words * 60d / WordsPerMinute);
        return new ScriptStatistics(normalized.Length, words, lines, seconds);
    }

    public static List<ScriptSection> SplitSections(string? text)
    {
        var sections = new List<ScriptSection>();
        string normalized = Normalize(text);
        if (normalized.Length == 0)
            return sections;

        string name = DefaultSectionName;
        bool explicitMarker = false;
        var buffer = new StringBuilder();

        void AddSection()
        {
            string body = buffer.ToString().Trim();
            // Đoạn "General" rỗng (script bắt đầu ngay bằng dấu mốc) thì bỏ; đoạn có dấu mốc thì giữ dù chưa có chữ.
            if (body.Length > 0 || explicitMarker)
                sections.Add(new ScriptSection(name, body));
        }

        foreach (string line in normalized.Split('\n'))
        {
            Match match = SectionMarker.Match(line);
            if (match.Success)
            {
                AddSection();
                name = match.Groups["name"].Value.Trim();
                explicitMarker = true;
                buffer.Clear();
                buffer.Append(match.Groups["rest"].Value);
            }
            else
            {
                if (buffer.Length > 0)
                    buffer.Append('\n');
                buffer.Append(line);
            }
        }

        AddSection();
        return sections;
    }

    /// <summary>Cắt <paramref name="text"/> còn tối đa <paramref name="maxCharacters"/> ký tự, ưu tiên cắt ở cuối dòng.</summary>
    public static string Truncate(string text, int maxCharacters, out bool truncated)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxCharacters, 1);

        if (text.Length <= maxCharacters)
        {
            truncated = false;
            return text;
        }

        truncated = true;
        int cut = text.LastIndexOf('\n', maxCharacters - 1);
        if (cut < maxCharacters / 2)
            cut = maxCharacters; // không có điểm xuống dòng hợp lý → cắt cứng

        if (cut > 0 && char.IsHighSurrogate(text[cut - 1]))
            cut--; // không cắt đôi một cặp surrogate (emoji)

        return text[..cut].TrimEnd();
    }

    /// <summary>
    /// Dấu vân tay (SHA-256, hex) của script đang lưu. Hai bản null và rỗng cho cùng Version
    /// vì cả hai đều nghĩa là "chưa có script".
    /// </summary>
    public static string ComputeVersion(string? rawScript)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawScript ?? string.Empty));
        return Convert.ToHexString(hash);
    }

    private static int CountWords(string text)
    {
        int count = 0;
        bool inWord = false;
        foreach (char c in text)
        {
            if (char.IsWhiteSpace(c))
            {
                inWord = false;
            }
            else if (!inWord)
            {
                inWord = true;
                count++;
            }
        }
        return count;
    }
}
