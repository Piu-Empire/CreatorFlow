namespace CreatorFlow.Services;

public sealed record LoginResult(bool Succeeded, string? ErrorMessage)
{
    public string? VerificationEmail { get; init; }
    public static LoginResult NeedsVerification(string email) => new(false, "Vui lòng xác minh email.") { VerificationEmail = email };

    public static LoginResult Success() => new(true, null);

    public static LoginResult Failure(string message) => new(false, message);
}
