namespace CreatorFlow.Contracts.Users;

public sealed record ProfileResponse(long UserId, string Email, string DisplayName,
    string AccountStatus, bool IsSystemAdmin, string? AvatarUrl);

public enum AvatarAction { Keep, Url, Upload, Remove }
