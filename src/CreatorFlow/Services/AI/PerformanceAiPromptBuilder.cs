using System.Globalization;
using System.Text;
using CreatorFlow.Models;

namespace CreatorFlow.Services.AI;

/// <summary>
/// Dựng prompt AI phân tích hiệu suất (SCRUM-60) từ Metrics/Statistics đã lưu trong hệ thống.
/// Hàm thuần, không gọi mạng, không đọc thời gian hệ thống nên kiểm thử được offline.
/// </summary>
public static class PerformanceAiPromptBuilder
{
    public const string RemarksMarker = "== NHẬN XÉT ==";
    public const string SuggestionsMarker = "== GỢI Ý CẢI THIỆN ==";

    /// <summary>Số dòng tối đa mỗi chiều để prompt không phình to với project lớn.</summary>
    public const int MaxRows = 20;

    /// <summary>Độ dài tối đa của tên hiển thị để tránh phá cấu trúc prompt.</summary>
    public const int MaxNameLength = 100;

    public static string Build(PerformanceAiRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var prompt = new StringBuilder();
        prompt.AppendLine("Bạn là chuyên gia phân tích hiệu suất nội dung cho CreatorFlow.");
        prompt.AppendLine("Nhiệm vụ: nhận xét hiệu suất và gợi ý cải thiện dựa trên số liệu dưới đây.");
        prompt.AppendLine();

        prompt.AppendLine("== Dữ liệu ==");
        prompt.AppendLine($"- Project: {Clean(request.ProjectName)}");
        prompt.AppendLine($"- Phạm vi: {Clean(request.ScopeLabel)}");
        if (request.Scope == PerformanceScope.Metrics && !string.IsNullOrWhiteSpace(request.GroupBy))
            prompt.AppendLine($"- Chiều tổng hợp: {Clean(request.GroupBy)}");
        prompt.AppendLine($"- Số liệu được tải từ hệ thống lúc: {request.LoadedAt.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)}");
        prompt.AppendLine("- Nguồn: Metrics do hệ thống CreatorFlow tổng hợp từ dữ liệu đã lưu.");
        prompt.AppendLine("  Bạn KHÔNG có quyền truy cập TikTok/Facebook/YouTube hay bất kỳ nền tảng mạng xã hội nào.");
        prompt.AppendLine();

        prompt.AppendLine("== Số liệu ==");
        AppendRows(prompt, request);
        prompt.AppendLine();

        prompt.AppendLine("== Ràng buộc ==");
        prompt.AppendLine("- Chỉ dùng đúng các số liệu ở trên; không bịa thêm số, nền tảng hay xu hướng không có trong dữ liệu.");
        prompt.AppendLine("- Không mô tả dữ liệu là realtime hay do bạn tự thu thập từ nền tảng.");
        prompt.AppendLine("- Views = 0 nghĩa là chưa có dữ liệu tương tác, không phải hiệu suất kém.");
        prompt.AppendLine("- Nếu thiếu dữ liệu để kết luận, nói rõ là thiếu dữ liệu thay vì suy đoán.");
        prompt.AppendLine("- Viết bằng tiếng Việt, ngắn gọn, tập trung vào việc người dùng có thể làm tiếp.");
        prompt.AppendLine();

        prompt.AppendLine("== Định dạng trả về ==");
        prompt.AppendLine("Trả về đúng 2 phần, mỗi phần bắt đầu bằng dòng mốc sau:");
        prompt.AppendLine(RemarksMarker);
        prompt.AppendLine("(3-6 câu nhận xét dựa trên số liệu ở trên)");
        prompt.AppendLine(SuggestionsMarker);
        prompt.AppendLine("(3-5 dòng gợi ý, mỗi dòng bắt đầu bằng \"- \")");
        prompt.AppendLine("Không giải thích ngoài lề, không dùng markdown code fence.");

        return prompt.ToString();
    }

    private static void AppendRows(StringBuilder prompt, PerformanceAiRequest request)
    {
        if (request.Scope == PerformanceScope.Creator)
        {
            if (request.ProjectTotals is null && request.Creators.Count == 0)
            {
                prompt.AppendLine("- Chưa có dữ liệu Metrics nào cho phạm vi này.");
                return;
            }

            if (request.ProjectTotals is { } totals)
                prompt.AppendLine($"- {Clean(totals.Name)} (tổng Project) | {CreatorCells(totals)}");

            IReadOnlyList<PerformanceCreatorLine> creators = request.Creators
                .OrderByDescending(c => c.Views)
                .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (PerformanceCreatorLine creator in creators.Take(MaxRows))
                prompt.AppendLine($"- {Clean(creator.Name)} | {CreatorCells(creator)}");

            if (request.IsTruncated || creators.Count > MaxRows)
                prompt.AppendLine($"- … (đã rút gọn, còn {Math.Max(0, creators.Count - MaxRows)} dòng)");

            return;
        }

        if (request.Metrics.Count == 0)
        {
            prompt.AppendLine("- Chưa có dữ liệu Metrics nào cho phạm vi này.");
            return;
        }

        IReadOnlyList<PerformanceMetricLine> metrics = request.Metrics
            .OrderByDescending(m => m.Views)
            .ThenBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (PerformanceMetricLine line in metrics.Take(MaxRows))
        {
            prompt.AppendLine($"- {Clean(line.Name)} | Views {Number(line.Views)} | Likes {Number(line.Likes)}" +
                              $" | Comments {Number(line.Comments)} | Shares {Number(line.Shares)}" +
                              $" | Engagement {Engagement(line.EngagementPercent)}");
        }

        if (request.IsTruncated || metrics.Count > MaxRows)
            prompt.AppendLine($"- … (đã rút gọn, còn {Math.Max(0, metrics.Count - MaxRows)} dòng)");
    }

    private static string CreatorCells(PerformanceCreatorLine line) =>
        $"Total {Number(line.Total)} | In Progress {Number(line.InProgress)} | Published {Number(line.Published)}" +
        $" | Overdue {Number(line.Overdue)} | Views {Number(line.Views)} | Likes {Number(line.Likes)}" +
        $" | Comments {Number(line.Comments)} | Shares {Number(line.Shares)}" +
        $" | Engagement {Engagement(line.EngagementPercent)}";

    private static string Number(long value) => value.ToString("N0", CultureInfo.InvariantCulture);

    private static string Engagement(double percent) =>
        percent.ToString("0.##", CultureInfo.InvariantCulture) + "%";

    /// <summary>Bỏ xuống dòng/tab và cắt độ dài để tên hiển thị không phá cấu trúc prompt.</summary>
    private static string Clean(string? value)
    {
        string text = (value ?? string.Empty)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Replace("\t", " ", StringComparison.Ordinal)
            .Trim();

        if (text.Length <= MaxNameLength) return text;
        return text[..MaxNameLength] + "…";
    }
}
