using CreatorFlow.Api.Models.Admin;
using CreatorFlow.Api.Models.Auth;

namespace CreatorFlow.Api.Repositories.Admin;

/// <summary>Kết quả một lần đổi trạng thái tài khoản trong transaction.</summary>
public sealed record AdminStatusChange(
    bool Found,
    bool Changed,
    bool BlockedLastAdmin,
    AccountStatus PreviousStatus,
    bool TargetIsSystemAdmin,
    string Email);

/// <summary>Truy vấn dữ liệu người dùng phục vụ quản trị hệ thống.</summary>
public interface IAdminUserRepository
{
    /// <summary>Tìm người dùng theo từ khóa và trạng thái, có phân trang.</summary>
    Task<IReadOnlyList<AdminUserRecord>> SearchAsync(string? search, string? status, int limit, int offset,
        CancellationToken cancellationToken = default);

    /// <summary>Đếm số người dùng khớp bộ lọc (dùng chung điều kiện với SearchAsync).</summary>
    Task<int> CountAsync(string? search, string? status,
        CancellationToken cancellationToken = default);

    /// <summary>Đổi trạng thái trong transaction kèm ghi audit; null khi target không tồn tại.</summary>
    Task<AdminStatusChange?> UpdateStatusAsync(long targetUserId, AccountStatus status, long actorUserId,
        string traceId, CancellationToken cancellationToken = default);
}
