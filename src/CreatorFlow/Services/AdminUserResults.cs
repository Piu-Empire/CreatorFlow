using CreatorFlow.Contracts.Admin;

namespace CreatorFlow.Services;

/// <summary>Kết quả tải danh sách người dùng cho màn hình admin.</summary>
public sealed record AdminUsersResult(
    bool Succeeded, string? ErrorMessage, AdminUsersListResponse? Response = null)
{
    public static AdminUsersResult Success(AdminUsersListResponse response) => new(true, null, response);

    public static AdminUsersResult Failure(string message) => new(false, message);
}

/// <summary>Kết quả khóa/mở khóa tài khoản từ màn hình admin.</summary>
public sealed record AdminStatusResult(
    bool Succeeded, string? ErrorMessage, UpdateUserStatusResponse? Response = null)
{
    public static AdminStatusResult Success(UpdateUserStatusResponse response) => new(true, null, response);

    public static AdminStatusResult Failure(string message) => new(false, message);
}
