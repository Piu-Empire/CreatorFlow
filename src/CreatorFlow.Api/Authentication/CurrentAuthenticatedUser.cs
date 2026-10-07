using CreatorFlow.Api.Models.Auth;

namespace CreatorFlow.Api.Authentication;

// Scoped to one HTTP request, populated only after JWT and database validation.
public sealed class CurrentAuthenticatedUser
{
    public User? User { get; internal set; }
    public bool IsAuthenticated => User is not null;
}
