using CreatorFlow.Models.Enums;
using CreatorFlow.Models;

namespace CreatorFlow.Repositories.Interfaces;

/// <summary>Ghi thông tin chi tiết của Content (tạo mới / cập nhật). Status chỉ đổi qua IContentRepository.UpdateStatus.</summary>
public interface IContentDetailsRepository
{
    /// <summary>Tạo Content mới ở trạng thái <paramref name="status"/>, trả về Id mới.</summary>
    long Create(long projectId, ContentStatus status, ContentDraft draft, long createdByUserId);

    /// <summary>Cập nhật thông tin chi tiết (không đổi Status).</summary>
    void Update(long contentId, ContentDraft draft);
}
