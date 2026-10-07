using CreatorFlow.Models;
using CreatorFlow.Contracts.Auth;

namespace CreatorFlow.Services;

public sealed class UserSession
{
    public CurrentUser? CurrentUser { get; private set; }
    public string? AccessToken { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public long Generation { get; private set; }
    public bool IsAuthenticated => CurrentUser is not null && AccessToken is not null && ExpiresAtUtc > DateTimeOffset.UtcNow;
    public event EventHandler? Cleared;

    public bool TrySetLogin(LoginResponse response, long expectedGeneration)
    {
        if (Generation != expectedGeneration || CurrentUser is not null || response.CurrentUser is null ||
            response.CurrentUser.UserId <= 0 || string.IsNullOrWhiteSpace(response.CurrentUser.Email) ||
            string.IsNullOrWhiteSpace(response.CurrentUser.DisplayName) || response.ExpiresAtUtc <= DateTimeOffset.UtcNow ||
            string.IsNullOrWhiteSpace(response.AccessToken)) return false;
        AccessToken = response.AccessToken;
        ExpiresAtUtc = response.ExpiresAtUtc;
        CurrentUser = new() { UserId = response.CurrentUser.UserId, Email = response.CurrentUser.Email,
            DisplayName = response.CurrentUser.DisplayName, IsSystemAdmin = response.CurrentUser.IsSystemAdmin };
        Generation++;
        return true;
    }

    public void UpdateDisplayName(string name, long expectedGeneration)
    {
        if (Generation == expectedGeneration && CurrentUser is not null) CurrentUser = CurrentUser with { DisplayName = name };
    }

    public void Clear()
    {
        Generation++;
        CurrentUser = null;
        AccessToken = null;
        ExpiresAtUtc = default;
        Cleared?.Invoke(this, EventArgs.Empty);
    }
}
