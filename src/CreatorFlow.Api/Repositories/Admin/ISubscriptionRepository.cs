using CreatorFlow.Api.Models.Admin;

namespace CreatorFlow.Api.Repositories.Admin;

/// <summary>Truy vấn đăng ký gói dịch vụ (SCRUM-23).</summary>
public interface ISubscriptionRepository
{
    /// <summary>Tìm đăng ký gói theo trạng thái/gói/từ khoá email, có phân trang.</summary>
    Task<IReadOnlyList<AdminSubscriptionRecord>> SearchAsync(string? status, long? planId, string? search,
        int limit, int offset, CancellationToken cancellationToken = default);

    /// <summary>Đếm số dòng khớp cùng điều kiện với <see cref="SearchAsync"/>.</summary>
    Task<int> CountAsync(string? status, long? planId, string? search,
        CancellationToken cancellationToken = default);

    /// <summary>Một đăng ký theo id; null khi không tồn tại.</summary>
    Task<AdminSubscriptionRecord?> GetByIdAsync(long subscriptionId,
        CancellationToken cancellationToken = default);

    /// <summary>Đăng ký còn hiệu lực của một người dùng; null khi chưa có.</summary>
    Task<AdminSubscriptionRecord?> GetActiveByUserAsync(long userId,
        CancellationToken cancellationToken = default);

    /// <summary>Kiểm tra người dùng tồn tại trước khi gán gói.</summary>
    Task<bool> UserExistsAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>Tạo đăng ký mới; null khi người dùng hoặc gói không tồn tại.</summary>
    Task<AdminSubscriptionRecord?> CreateAsync(long userId, long planId, DateTimeOffset? expiresAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Đổi trạng thái/gia hạn trong transaction; null khi đăng ký không tồn tại.
    /// <paramref name="expiresAt"/> null = giữ nguyên hạn cũ.
    /// </summary>
    Task<AdminSubscriptionRecord?> UpdateAsync(long subscriptionId, string status, DateTimeOffset? expiresAt,
        CancellationToken cancellationToken = default);
}
