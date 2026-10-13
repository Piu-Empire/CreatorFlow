namespace CreatorFlow.Models;

/// <summary>Số liệu nhanh của một script (hiện ở thanh trạng thái editor và gửi kèm cho AI).</summary>
/// <param name="Characters">Tổng ký tự sau khi chuẩn hóa (tính cả dấu xuống dòng).</param>
/// <param name="Words">Số từ tách theo khoảng trắng (tiếng Việt: mỗi âm tiết một từ).</param>
/// <param name="Lines">Số dòng.</param>
/// <param name="EstimatedSpeakingSeconds">Thời lượng đọc ước tính, theo <c>ScriptTextAnalyzer.WordsPerMinute</c>.</param>
public sealed record ScriptStatistics(int Characters, int Words, int Lines, int EstimatedSpeakingSeconds)
{
    public static readonly ScriptStatistics Empty = new(0, 0, 0, 0);
}
