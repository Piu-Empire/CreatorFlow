using CreatorFlow.Models.Enums;

namespace CreatorFlow.Models;

/// <summary>
/// Dữ liệu gửi AI cho Repurpose (SCRUM-38): thông tin Content gốc + platform đích + yêu cầu thêm.
/// AI tự ghép thành prompt; request không chứa prompt dựng sẵn.
/// </summary>
public sealed record RepurposeAiRequest
{
    public long SourceContentId { get; init; }

    public long ProjectId { get; init; }

    public long RequestedByUserId { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    /// <summary>Kịch bản gốc đã chuẩn hóa, đã cắt nếu vượt giới hạn gửi AI.</summary>
    public string? Script { get; init; }

    /// <summary>true nếu <see cref="Script"/> bị cắt bớt.</summary>
    public bool IsScriptTruncated { get; init; }

    public string? ContentType { get; init; }

    public Priority Priority { get; init; } = Priority.Medium;

    public IReadOnlyList<string> SourcePlatforms { get; init; } = Array.Empty<string>();

    public required string TargetPlatform { get; init; }

    /// <summary>Yêu cầu thêm của người dùng (tone, độ dài, CTA...). null = không có.</summary>
    public string? Instruction { get; init; }
}
