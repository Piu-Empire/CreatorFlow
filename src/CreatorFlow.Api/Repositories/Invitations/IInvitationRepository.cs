using CreatorFlow.Api.Models.Invitations;
using CreatorFlow.Api.Models.Tasks;

namespace CreatorFlow.Api.Repositories.Invitations;

/// <summary>Truy cập project_invitations bằng Npgsql (chỉ Backend được chạm database).</summary>
public interface IInvitationRepository
{
    /// <summary>Vai trò đang hoạt động của User trong Project, hoặc null nếu không phải thành viên.</summary>
    Task<ProjectRole?> GetRoleAsync(long projectId, long userId, CancellationToken cancellationToken = default);

    /// <summary>true nếu email (không phân biệt hoa/thường) thuộc một thành viên đang hoạt động của Project.</summary>
    Task<bool> IsActiveMemberByEmailAsync(long projectId, string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Trong MỘT transaction: chuyển các lời mời Pending đã hết hạn của email này (trong Project) sang Expired,
    /// rồi tạo lời mời Pending mới. Trả về null nếu email vẫn còn một lời mời Pending còn hạn (unique index chặn).
    /// </summary>
    Task<ProjectInvitation?> CreatePendingAsync(
        long projectId, string email, ProjectRole role, long invitedBy, string token,
        DateTime nowUtc, DateTime expiresAtUtc, CancellationToken cancellationToken = default);

    /// <summary>Mọi lời mời của Project, mới nhất trước.</summary>
    Task<IReadOnlyList<ProjectInvitation>> GetByProjectAsync(long projectId, CancellationToken cancellationToken = default);
}
