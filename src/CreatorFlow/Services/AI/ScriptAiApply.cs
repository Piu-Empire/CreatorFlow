namespace CreatorFlow.Services.AI;

/// <summary>
/// Áp kết quả AI vào kịch bản (SCRUM-58). Hàm thuần, kiểm thử offline.
/// Nguyên tắc AC1: không tự ghi đè — control chỉ gọi các hàm này khi người dùng bấm nút áp;
/// "thay thế toàn bộ" luôn kèm hộp xác nhận ở UI.
/// </summary>
public static class ScriptAiApply
{
    /// <summary>
    /// Chèn <paramref name="addition"/> vào cuối <paramref name="current"/>, cách nhau một dòng trống.
    /// Kịch bản trống thì kết quả chính là phần thêm vào.
    /// </summary>
    public static string Append(string? current, string? addition)
    {
        string left = ScriptTextAnalyzer.Normalize(current);
        string right = ScriptTextAnalyzer.Normalize(addition);

        if (right.Length == 0)
            return left;
        if (left.Length == 0)
            return right;

        return left.TrimEnd() + "\n\n" + right;
    }

    /// <summary>Nội dung thay thế: chính là phần AI trả về, đã chuẩn hóa.</summary>
    public static string Replace(string? addition) => ScriptTextAnalyzer.Normalize(addition);
}
