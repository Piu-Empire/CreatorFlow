using CreatorFlow.Api.Models.Auth;

namespace CreatorFlow.Api.Models.Admin;

/// <summary>Một dòng người dùng phục vụ màn hình quản trị (nội bộ API).</summary>
public sealed class AdminUserRecord
{
    public long UserId { get; init; }

    public required string Email { get; init; }

    public required string DisplayName { get; init; }

    public AccountStatus AccountStatus { get; init; }

    public bool IsSystemAdmin { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? LastLoginAt { get; init; }
}
