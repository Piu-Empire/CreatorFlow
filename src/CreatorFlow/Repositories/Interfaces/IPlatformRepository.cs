using CreatorFlow.Models;

namespace CreatorFlow.Repositories.Interfaces;

/// <summary>Đọc danh sách nền tảng (bảng platforms) và các nền tảng đã gắn vào một Content (bảng content_platforms).</summary>
public interface IPlatformRepository
{
    /// <summary>Các nền tảng đang bật (is_active) — nguồn cho checkbox chọn platform và cho việc validate.</summary>
    List<Platform> GetActive();

    /// <summary>Các nền tảng đã gắn vào Content, kèm publication_status của từng nền tảng.</summary>
    List<ContentPlatform> GetByContentId(long contentId);
}
