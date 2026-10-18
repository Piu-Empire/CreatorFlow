using CreatorFlow.Models.Enums;

namespace CreatorFlow.Models;

/// <summary>
/// Script thô của một Content (cột contents.script) kèm đúng các thông tin cần để kiểm tra quyền sửa.
/// Không gộp vào <see cref="Content"/> để Board/Workflow không phải kéo theo một chuỗi có thể dài hàng chục nghìn ký tự.
/// </summary>
public sealed record ContentScript
{
    public long ContentId { get; init; }

    public long ProjectId { get; init; }

    public ContentStatus Status { get; init; }

    public long CreatedByUserId { get; init; }

    /// <summary>Script đúng như đang lưu trong DB (chưa chuẩn hóa xuống dòng). null = chưa có script.</summary>
    public string? Script { get; init; }
}
