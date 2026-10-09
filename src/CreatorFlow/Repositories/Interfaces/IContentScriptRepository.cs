using CreatorFlow.Models;

namespace CreatorFlow.Repositories.Interfaces;

/// <summary>Đọc/ghi riêng cột script của Content (script có thể dài, không nên đi chung với thẻ Board).</summary>
public interface IContentScriptRepository
{
    /// <summary>Script thô + thông tin kiểm tra quyền của Content. null nếu Content không tồn tại.</summary>
    ContentScript? GetByContentId(long contentId);

    /// <summary>
    /// Ghi script mới CHỈ KHI script đang lưu vẫn đúng bằng <paramref name="expectedScript"/> (compare-and-set).
    /// Trả về false nếu người khác đã đổi script trong lúc đó (hoặc Content không còn). null = chưa có script.
    /// Chạy trong transaction hiện hành của Unit of Work.
    /// </summary>
    bool TryUpdateScript(long contentId, string? expectedScript, string? newScript);
}
