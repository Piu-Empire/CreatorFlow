using CreatorFlow.Models;

namespace CreatorFlow.Services.AI;

/// <summary>
/// Cổng gọi AI cho Repurpose (SCRUM-38). Cài đặt thật bọc lõi AI client; UI chỉ biết interface này.
/// </summary>
public interface IRepurposeAiService
{
    bool IsConfigured { get; }

    Task<RepurposeAiResponse> RequestRepurposeAsync(
        RepurposeAiRequest request, CancellationToken cancellationToken = default);
}
