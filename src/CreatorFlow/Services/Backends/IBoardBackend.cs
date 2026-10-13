using CreatorFlow.Models;

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
}
