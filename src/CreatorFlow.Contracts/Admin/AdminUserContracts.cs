namespace CreatorFlow.Contracts.Admin;

/// <summary>Tóm tắt một tài khoản cho màn hình quản trị (không bao giờ chứa hash mật khẩu).</summary>
public sealed record AdminUserSummaryResponse(
    long UserId,
    string Email,
    string DisplayName,
    string AccountStatus,
    bool IsSystemAdmin,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt);

/// <summary>Danh sách người dùng kèm tổng số dòng khớp bộ lọc.</summary>
public sealed record AdminUsersListResponse(
    IReadOnlyList<AdminUserSummaryResponse> Items,
    int TotalCount);

/// <summary>Yêu cầu đổi trạng thái tài khoản (chỉ nhận ACTIVE hoặc LOCKED).</summary>
public sealed record UpdateUserStatusRequest(string? Status);

/// <summary>Kết quả đổi trạng thái tài khoản.</summary>
public sealed record UpdateUserStatusResponse(
    long UserId,
    string Status,
    bool TargetIsSystemAdmin,
    string Message);
