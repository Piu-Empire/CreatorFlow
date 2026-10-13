namespace CreatorFlow.Api.Authentication;

public sealed class AuthBackendUnavailableException : Exception
{
    public AuthBackendUnavailableException() : base("Authentication backend is unavailable.") { }
}
