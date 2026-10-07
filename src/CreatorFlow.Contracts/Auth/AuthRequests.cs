namespace CreatorFlow.Contracts.Auth;

public sealed record RegisterRequest(string? DisplayName, string? Email, string? Password, string? ConfirmPassword);
public sealed record LoginRequest(string? Email, string? Password);
public sealed record EmailCodeRequest(string? Email);
public sealed record VerifyEmailRequest(Guid RequestId, string? Code);
public sealed record ResetPasswordRequest(Guid RequestId, string? Code, string? NewPassword, string? ConfirmPassword);
public sealed record ChangePasswordRequest(string? CurrentPassword, string? NewPassword, string? ConfirmPassword);
