using CreatorFlow.Models;

namespace CreatorFlow.Services;

public sealed record UserOperationResult(
    bool Succeeded, string? ErrorMessage, string? ErrorField = null, UserProfile? Profile = null)
{
    public static UserOperationResult Success(UserProfile? profile = null) => new(true, null, Profile: profile);

    public static UserOperationResult Failure(string message, string? field = null) => new(false, message, field);
}
