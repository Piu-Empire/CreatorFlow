namespace CreatorFlow.Api.Services.Auth;

public sealed record AuthOperationResult<T>(T? Value, string? ErrorCode = null, string? Message = null,
    string? ErrorField = null, int Status = 200)
{
    public bool Succeeded => ErrorCode is null;
    public static AuthOperationResult<T> Fail(string code, string message, string? field = null, int status = 400) =>
        new(default, code, message, field, status);
}
