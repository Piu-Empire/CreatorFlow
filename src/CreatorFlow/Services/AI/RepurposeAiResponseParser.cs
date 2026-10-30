using CreatorFlow.Models;

namespace CreatorFlow.Services.AI;

/// <summary>
/// Tách output AI Repurpose thành Tiêu đề / Mô tả / Kịch bản (SCRUM-38).
/// Chịu lỗi: thiếu mốc thì đưa toàn bộ text vào Kịch bản và đánh dấu IsStructured=false.
/// Hàm thuần, kiểm thử offline.
/// </summary>
public static class RepurposeAiResponseParser
{
    private static readonly string[] TitleMarkers = { "== TIÊU ĐỀ ==", "== TIEU DE ==" };
    private static readonly string[] DescriptionMarkers = { "== MÔ TẢ ==", "== MO TA ==" };
    private static readonly string[] ScriptMarkers = { "== KỊCH BẢN ==", "== KICH BAN ==" };

    public static RepurposeParsedContent Parse(string? raw)
    {
        string text = (raw ?? string.Empty).Replace("\r\n", "\n").Trim();
        if (text.Length == 0)
            return new RepurposeParsedContent(null, null, null, false);

        int titleIndex = FindMarker(text, TitleMarkers, 0);
        int descriptionIndex = FindMarker(text, DescriptionMarkers, titleIndex < 0 ? 0 : titleIndex);
        int scriptIndex = FindMarker(text, ScriptMarkers, descriptionIndex < 0 ? 0 : descriptionIndex);

        if (titleIndex < 0 || descriptionIndex < 0 || scriptIndex < 0 ||
            titleIndex >= descriptionIndex || descriptionIndex >= scriptIndex)
        {
            return new RepurposeParsedContent(null, null, text, false);
        }

        string title = Slice(text, titleIndex, TitleMarkers, descriptionIndex).Trim();
        string description = Slice(text, descriptionIndex, DescriptionMarkers, scriptIndex).Trim();
        string script = SliceToEnd(text, scriptIndex, ScriptMarkers).Trim();

        return new RepurposeParsedContent(
            title.Length == 0 ? null : title,
            description.Length == 0 ? null : description,
            script.Length == 0 ? null : script,
            true);
    }

    private static int FindMarker(string text, string[] markers, int startIndex)
    {
        int found = -1;
        foreach (string marker in markers)
        {
            int index = text.IndexOf(marker, startIndex, StringComparison.OrdinalIgnoreCase);
            if (index >= 0 && (found < 0 || index < found)) found = index;
        }

        return found;
    }

    private static string Slice(string text, int markerIndex, string[] markers, int nextIndex)
    {
        int start = MarkerEnd(text, markerIndex, markers);
        return text[start..nextIndex];
    }

    private static string SliceToEnd(string text, int markerIndex, string[] markers)
    {
        int start = MarkerEnd(text, markerIndex, markers);
        return text[start..];
    }

    private static int MarkerEnd(string text, int markerIndex, string[] markers)
    {
        foreach (string marker in markers)
        {
            if (text.AsSpan(markerIndex).StartsWith(marker, StringComparison.OrdinalIgnoreCase))
                return markerIndex + marker.Length;
        }

        return markerIndex;
    }
}
