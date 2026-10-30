using CreatorFlow.Models;

namespace CreatorFlow.Repositories;

public interface IPlanRepository : IRepository<Plan, long>
{
    /// <summary>Toàn bộ gói, kể cả gói đã tắt (SCRUM-23).</summary>
    Task<IReadOnlyList<Plan>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Tìm theo mã gói, không phân biệt hoa/thường; null khi chưa có.</summary>
    Task<Plan?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>Cập nhật giá/giới hạn/trạng thái; null khi gói không tồn tại.</summary>
    Task<Plan?> UpdateAsync(Plan entity, CancellationToken cancellationToken = default);
}
