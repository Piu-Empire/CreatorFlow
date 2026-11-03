using CreatorFlow.Models;

namespace CreatorFlow.Services.AI;

/// <summary>
/// Cổng gọi AI phân tích hiệu suất (SCRUM-60). Cài đặt thật bọc lõi AI client;
/// UI chỉ biết interface này, không tự gọi dịch vụ AI.
/// </summary>
public interface IPerformanceAiService
{
    bool IsConfigured { get; }

    Task<PerformanceAiResponse> AnalyzeAsync(
        PerformanceAiRequest request, CancellationToken cancellationToken = default);
}
