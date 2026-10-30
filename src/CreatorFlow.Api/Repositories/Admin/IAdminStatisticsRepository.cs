using CreatorFlow.Api.Models.Admin;

namespace CreatorFlow.Api.Repositories.Admin;

/// <summary>Đọc số liệu thống kê cơ bản toàn hệ thống (SCRUM-23), chỉ đọc.</summary>
public interface IAdminStatisticsRepository
{
    Task<AdminStatisticsRecord> GetAsync(CancellationToken cancellationToken = default);
}
