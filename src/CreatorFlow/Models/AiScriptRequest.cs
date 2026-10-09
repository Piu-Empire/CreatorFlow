using CreatorFlow.Models.Enums;

namespace CreatorFlow.Models;

/// <summary>
/// Dữ liệu đã chuẩn bị sẵn để gửi script sang AIService: thông tin ngữ cảnh của Content + script đã chuẩn hóa/cắt
/// + các đoạn đã tách + số liệu. AIService tự ghép thành prompt; request không chứa prompt dựng sẵn.
/// </summary>
public sealed record AiScriptRequest
{
    public long ContentId { get; init; }

    public long ProjectId { get; init; }

    public long RequestedByUserId { get; init; }

    /// <summary>Map thẳng sang enum ai_request_type của bảng ai_requests (Script/Title/Outline/Repurpose/Other).</summary>
    public AiRequestType RequestType { get; init; } = AiRequestType.Script;

    public required string Title { get; init; }

    public string? Description { get; init; }

    public string? ContentType { get; init; }

    public Priority Priority { get; init; } = Priority.Medium;

    public IReadOnlyList<string> Platforms { get; init; } = Array.Empty<string>();

    /// <summary>Script đã chuẩn hóa, đã cắt nếu vượt giới hạn gửi AI.</summary>
    public string Script { get; init; } = string.Empty;

    /// <summary>true nếu <see cref="Script"/> bị cắt bớt (script gốc dài hơn giới hạn gửi AI).</summary>
    public bool IsScriptTruncated { get; init; }

    public IReadOnlyList<ScriptSection> Sections { get; init; } = Array.Empty<ScriptSection>();

    /// <summary>Thống kê của script GỐC (trước khi cắt).</summary>
    public ScriptStatistics Statistics { get; init; } = ScriptStatistics.Empty;

    /// <summary>Yêu cầu thêm của người dùng (VD "rút gọn còn 30 giây"). null = không có.</summary>
    public string? Instruction { get; init; }
}
