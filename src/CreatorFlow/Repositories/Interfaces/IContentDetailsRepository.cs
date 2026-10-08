using CreatorFlow.Models.Enums;
using CreatorFlow.Models;

namespace CreatorFlow.Repositories.Interfaces;

/// <summary>Ghi và đọc thông tin chi tiết của Content (tạo mới / cập nhật / xem chi tiết). Status chỉ đổi qua IContentRepository.UpdateStatus.</summary>
public interface IContentDetailsRepository
{
    /// <summary>Tạo Content mới ở trạng thái <paramref name="status"/>, trả về Id mới.</summary>
    long Create(long projectId, ContentStatus status, ContentDraft draft, long createdByUserId);

    /// <summary>Cập nhật thông tin chi tiết (không đổi Status).</summary>
    void Update(long contentId, ContentDraft draft);

    /// <summary>Đọc đầy đủ thông tin lập kế hoạch (mô tả, script, loại, ưu tiên, deadline, ngày dự kiến đăng...) của Content. null nếu không tồn tại.</summary>
    Content? GetDetail(long contentId);
}