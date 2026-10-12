using CreatorFlow.Api.Models.Metrics;
using CreatorFlow.Api.Models.Tasks;
using CreatorFlow.Contracts.Metrics;

namespace CreatorFlow.Api.Services.Metrics;

/// <summary>Quy tắc nghiệp vụ Metrics, thực thi Ở BACKEND (client chỉ dùng để bật/tắt nút).</summary>
public static class MetricRules
{
    /// <summary>Quyền ghi: Owner/Manager. (Câu hỏi mở: có cho Creator được giao ghi không — đổi tại đây.)</summary>
    public static bool CanRecord(ProjectRole? role) => role is ProjectRole.Owner or ProjectRole.Manager;

    /// <summary>Chỉ nhập sau Published: platform PUBLISHED hoặc Content PUBLISHED. Đổi điều kiện tại đây nếu team chốt khác.</summary>
    public static bool IsEligible(PlatformTarget target) =>
        target.PublicationStatus == "PUBLISHED" || target.ContentStatus == "PUBLISHED";

    public static string? Validate(RecordMetricRequest request)
    {
        if (request.Views is null && request.Likes is null && request.Comments is null && request.Shares is null)
            return "Cần nhập ít nhất một chỉ số.";
        if (request.Views < 0 || request.Likes < 0 || request.Comments < 0 || request.Shares < 0)
            return "Chỉ số không được âm.";
        return null;
    }
}
