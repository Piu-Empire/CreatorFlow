using CreatorFlow.Api.Models.ContentDetail;

namespace CreatorFlow.Api.Repositories.ContentDetail;

/// <summary>Truy cập chi tiết / script / activity của Content bằng Npgsql (chỉ Backend được chạm database).</summary>
public interface IContentDetailRepository
{
    Task<ContentDetailRecord?> GetDetailAsync(long contentId, CancellationToken cancellationToken = default);

    /// <summary>Tên các nền tảng đang bật (platforms.is_active).</summary>
    Task<IReadOnlyList<string>> GetActivePlatformNamesAsync(CancellationToken cancellationToken = default);

    /// <summary>Tên các nền tảng của Content đã PUBLISHED (không được bỏ chọn).</summary>
    Task<IReadOnlyList<string>> GetPublishedPlatformNamesAsync(long contentId, CancellationToken cancellationToken = default);

    /// <summary>Tạo Content, đồng bộ content_platforms và ghi dòng lịch sử đầu tiên trong MỘT transaction. Trả về content_id mới.</summary>
    Task<long> CreateAsync(NewContent content, CancellationToken cancellationToken = default);

    /// <summary>Ghi thông tin lập kế hoạch và đồng bộ content_platforms trong MỘT transaction.</summary>
    Task UpdateAsync(long contentId, ContentUpdate update, CancellationToken cancellationToken = default);

    Task<ScriptRecord?> GetScriptAsync(long contentId, CancellationToken cancellationToken = default);

    /// <summary>Compare-and-set: chỉ ghi nếu script trong DB vẫn bằng <paramref name="expectedRaw"/>. false = đã bị người khác sửa.</summary>
    Task<bool> TryUpdateScriptAsync(long contentId, string? expectedRaw, string? newScript, CancellationToken cancellationToken = default);

    /// <summary>Activity timeline (lịch sử trạng thái + Review), tăng dần theo thời gian.</summary>
    Task<IReadOnlyList<ActivityRecord>> GetActivityAsync(long contentId, CancellationToken cancellationToken = default);
}
