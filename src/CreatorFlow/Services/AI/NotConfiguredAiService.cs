using CreatorFlow.Models;

namespace CreatorFlow.Services.AI;

/// <summary>Cài đặt tạm của IAiService cho tới khi có AIService thật: luôn báo chưa cấu hình, không gửi dữ liệu đi đâu.</summary>
public sealed class NotConfiguredAiService : IAiService
{
    public const string Message = "Dịch vụ AI chưa được cấu hình.";

    public bool IsConfigured => false;

    public Task<AiScriptResponse> RequestScriptAssistAsync(AiScriptRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(AiScriptResponse.Failure(Message));
}
