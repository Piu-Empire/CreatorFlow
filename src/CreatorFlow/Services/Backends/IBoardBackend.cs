using CreatorFlow.Models;
using CreatorFlow.Contracts.Board;

namespace CreatorFlow.Services.Backends;

/// <summary>
/// Nguồn thẻ Content của Board khi chạy theo mô hình Client–Server (quyền xem do CreatorFlow.Api quyết định).
/// Lỗi được đổi sang ContentValidationException (mất mạng, dữ liệu sai...) / UnauthorizedWorkflowActionException
/// (không phải thành viên, hết phiên) / InvalidOperationException (API trả 404).
/// </summary>
public interface IBoardBackend
{
    /// <summary>Các thẻ Content của Project (không gồm Content đã Archived). Chạy bất đồng bộ để không treo giao diện.</summary>
    Task<List<ContentBoardCard>> GetBoardCardsAsync(long projectId, CancellationToken token = default);

    /// <summary>Kho toàn bộ Content của Project, gồm cả Content đã Archived. Mặc định tương thích backend chỉ có Board.</summary>
    Task<List<ContentBoardCard>> GetProjectContentsAsync(long projectId, CancellationToken token = default) =>
        GetBoardCardsAsync(projectId, token);

    /// <summary>Kho Content với filter/sort server-side. Backend cũ có thể fallback về danh sách đầy đủ.</summary>
    Task<List<ContentBoardCard>> GetProjectContentsAsync(long projectId, ContentLibraryQuery query, CancellationToken token = default) =>
        GetProjectContentsAsync(projectId, token);
}
