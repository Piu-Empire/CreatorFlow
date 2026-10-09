using CreatorFlow.Models;

namespace CreatorFlow.Services.AI;

/// <summary>
/// Cổng duy nhất để gửi dữ liệu script sang AI. Phần gọi LLM thật (qua backend, không để API key ở WinForms)
/// sẽ là một class cài đặt interface này; ScriptService chỉ biết interface.
/// </summary>
public interface IAiService
{
    /// <summary>false nếu chưa có cài đặt thật (chưa cấu hình/chưa có backend).</summary>
    bool IsConfigured { get; }

    Task<AiScriptResponse> RequestScriptAssistAsync(AiScriptRequest request, CancellationToken cancellationToken = default);
}
