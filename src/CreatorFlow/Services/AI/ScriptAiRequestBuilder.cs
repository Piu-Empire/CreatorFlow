using CreatorFlow.Models;
using CreatorFlow.Models.Enums;

namespace CreatorFlow.Services.AI;

/// <summary>Chuẩn bị dữ liệu gửi AI từ Content + script đã lưu: chuẩn hóa, tách đoạn, thống kê, cắt gọn.</summary>
public static class ScriptAiRequestBuilder
{
    /// <summary>Giới hạn ký tự script gửi AI (script dài hơn bị cắt và đánh dấu IsScriptTruncated).</summary>
    public const int MaxScriptCharacters = 30000;

    public static AiScriptRequest Build(
        Content detail,
        IReadOnlyList<string> platforms,
        string? script,
        AiRequestType requestType,
        string? instruction,
        long requestedByUserId)
    {
        ArgumentNullException.ThrowIfNull(detail);
        ArgumentNullException.ThrowIfNull(platforms);

        string normalized = ScriptTextAnalyzer.Normalize(script);
        string sent = ScriptTextAnalyzer.Truncate(normalized, MaxScriptCharacters, out bool truncated);

        return new AiScriptRequest
        {
            ContentId = detail.ContentId,
            ProjectId = detail.ProjectId,
            RequestedByUserId = requestedByUserId,
            RequestType = requestType,
            Title = detail.Title,
            Description = detail.Description,
            ContentType = detail.ContentType,
            Priority = detail.Priority,
            Platforms = platforms.ToList(),
            Script = sent,
            IsScriptTruncated = truncated,
            Sections = ScriptTextAnalyzer.SplitSections(sent),
            Statistics = ScriptTextAnalyzer.Analyze(normalized),
            Instruction = string.IsNullOrWhiteSpace(instruction) ? null : instruction.Trim(),
        };
    }
}
