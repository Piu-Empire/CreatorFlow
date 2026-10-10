using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Models.Auth;
using CreatorFlow.Api.Repositories.Admin;
using CreatorFlow.Api.Services.Auth;
using CreatorFlow.Contracts.Admin;

namespace CreatorFlow.Api.Services.Admin;

/// <summary>Nghiệp vụ quản trị người dùng: lớp phòng thủ thứ hai sau policy.</summary>
public sealed class AdminUserService(
    IAdminUserRepository repository,
    CurrentAuthenticatedUser currentUser,
    ILogger<AdminUserService> logger)
{
    private const int DefaultLimit = 20;
    private const int MaxLimit = 100;
    private const int MaxOffset = 10_000;
    private const int MaxSearchLength = 100;

    /// <summary>Tìm người dùng theo từ khóa và trạng thái, chỉ dành cho System Admin.</summary>
    public async Task<AuthOperationResult<AdminUsersListResponse>> GetUsersAsync(string? search, string? status,
        int? limit, int? offset, CancellationToken cancellationToken = default)
    {
        if (currentUser.User is not { IsSystemAdmin: true })
            return AuthOperationResult<AdminUsersListResponse>.Fail("forbidden", "Bạn không có quyền quản trị hệ thống.", status: 403);
        if (search is not null && search.Trim().Length > MaxSearchLength)
            return AuthOperationResult<AdminUsersListResponse>.Fail("validation", "Từ khóa tìm kiếm không được vượt quá 100 ký tự.", "Search");
        string? normalized = string.IsNullOrWhiteSpace(status) || string.Equals(status!.Trim(), "ALL", StringComparison.OrdinalIgnoreCase)
            ? null : status.Trim().ToUpperInvariant();
        if (normalized is not (null or "ACTIVE" or "LOCKED" or "DISABLED"))
            return AuthOperationResult<AdminUsersListResponse>.Fail("validation", "Trạng thái lọc không hợp lệ.", "Status");
        int take = limit ?? DefaultLimit;
        if (limit is < 1)
            return AuthOperationResult<AdminUsersListResponse>.Fail("validation", "Limit phải lớn hơn 0.", "Limit");
        if (take > MaxLimit)
            take = MaxLimit;
        int skip = offset ?? 0;
        if (skip is < 0 or > MaxOffset)
            return AuthOperationResult<AdminUsersListResponse>.Fail("validation", "Offset không hợp lệ.", "Offset");

        try
        {
            IReadOnlyList<Models.Admin.AdminUserRecord> items =
                await repository.SearchAsync(string.IsNullOrWhiteSpace(search) ? null : search.Trim(), normalized, take, skip, cancellationToken);
            int total = await repository.CountAsync(string.IsNullOrWhiteSpace(search) ? null : search.Trim(), normalized, cancellationToken);
            return new(new AdminUsersListResponse([.. items.Select(ToSummary)], total));
        }
        catch (Exception exception) when (AuthRules.IsDatabaseError(exception, cancellationToken))
        {
            return AuthOperationResult<AdminUsersListResponse>.Fail("service_unavailable", "Không thể tải danh sách người dùng lúc này.", status: 503);
        }
    }

    /// <summary>Khóa hoặc mở khóa tài khoản, kèm ghi audit trong cùng transaction.</summary>
    public async Task<AuthOperationResult<UpdateUserStatusResponse>> UpdateUserStatusAsync(long userId,
        UpdateUserStatusRequest request, string traceId, CancellationToken cancellationToken = default)
    {
        if (currentUser.User is not { IsSystemAdmin: true })
            return AuthOperationResult<UpdateUserStatusResponse>.Fail("forbidden", "Bạn không có quyền quản trị hệ thống.", status: 403);
        long adminId = currentUser.User.UserId;
        if (userId == adminId)
            return AuthOperationResult<UpdateUserStatusResponse>.Fail("self_lock", "Không thể tự khóa tài khoản của chính mình.", status: 400);
        AccountStatus? newStatus = request.Status?.Trim().ToUpperInvariant() switch
        {
            "ACTIVE" => AccountStatus.Active,
            "LOCKED" => AccountStatus.Locked,
            _ => null,
        };
        if (newStatus is null)
            return AuthOperationResult<UpdateUserStatusResponse>.Fail("validation", "Trạng thái không hợp lệ. Chỉ nhận ACTIVE hoặc LOCKED.", "Status");

        try
        {
            string statusText = newStatus == AccountStatus.Locked ? "LOCKED" : "ACTIVE";
            AdminStatusChange? change = await repository.UpdateStatusAsync(userId, newStatus.Value, adminId,
                traceId, cancellationToken);
            if (change is null)
                return AuthOperationResult<UpdateUserStatusResponse>.Fail("not_found", "Không tìm thấy tài khoản.", status: 404);
            if (change.BlockedLastAdmin)
            {
                logger.LogWarning("Admin lock rejected to protect last active admin. TraceId: {TraceId}; TargetUserId: {TargetUserId}",
                    traceId, userId);
                return AuthOperationResult<UpdateUserStatusResponse>.Fail("last_active_admin", "Không thể khóa admin cuối cùng còn hoạt động.", status: 400);
            }

            string message = !change.Changed
                ? "Trạng thái tài khoản không thay đổi."
                : newStatus == AccountStatus.Locked ? "Đã khóa tài khoản." : "Đã mở khóa tài khoản.";
            return new(new UpdateUserStatusResponse(userId, statusText, change.TargetIsSystemAdmin, message));
        }
        catch (Exception exception) when (AuthRules.IsDatabaseError(exception, cancellationToken))
        {
            return AuthOperationResult<UpdateUserStatusResponse>.Fail("service_unavailable", "Không thể cập nhật tài khoản lúc này.", status: 503);
        }
    }

    private static AdminUserSummaryResponse ToSummary(Models.Admin.AdminUserRecord record) => new(
        record.UserId,
        record.Email,
        record.DisplayName,
        record.AccountStatus.ToString().ToUpperInvariant(),
        record.IsSystemAdmin,
        record.CreatedAt,
        record.LastLoginAt);
}
