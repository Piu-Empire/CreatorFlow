using CreatorFlow.Models.Enums;

namespace CreatorFlow.Models;

/// <summary>Điều kiện tìm kiếm / lọc Idea trong một Project. Thuộc tính để trống/null nghĩa là không lọc theo tiêu chí đó.</summary>
public sealed class IdeaFilter
{
    /// <summary>Từ khóa tìm trong tiêu đề, mô tả, ghi chú và tên tag (không phân biệt hoa/thường).</summary>
    public string SearchText { get; set; } = string.Empty;

    public IdeaStatus? Status { get; set; }

    /// <summary>Chỉ lấy Idea có đúng tag này (không phân biệt hoa/thường).</summary>
    public string? Tag { get; set; }
}
