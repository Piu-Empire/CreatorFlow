namespace CreatorFlow.Api.Services.Auth;

public sealed record PasswordResetResult(bool Succeeded, string Message,
    Guid? RequestId = null, string? ErrorField = null)
{
    public static PasswordResetResult Failure(string message, string? field = null) =>
        new(false, message, ErrorField: field);
}
