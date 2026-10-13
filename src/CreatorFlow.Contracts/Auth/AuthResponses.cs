namespace CreatorFlow.Contracts.Auth;

public sealed record CurrentUserResponse(long UserId, string Email, string DisplayName, bool IsSystemAdmin);
public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAtUtc, CurrentUserResponse CurrentUser);
public sealed record RegisterResponse(string Email, bool RequiresEmailVerification);
public sealed record VerificationRequestResponse(Guid RequestId, string Message, int ResendAfterSeconds);
public sealed record ResetRequestResponse(Guid RequestId, string Message);
public sealed record AuthCapabilitiesResponse(bool EmailVerificationAvailable, bool PasswordResetAvailable);
public sealed record ReloginResponse(bool RequiresLogin);
