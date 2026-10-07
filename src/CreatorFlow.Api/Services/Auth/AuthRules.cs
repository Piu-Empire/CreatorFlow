using System.Data.Common;
using System.Net.Mail;

namespace CreatorFlow.Api.Services.Auth;

internal static class AuthRules
{
    public static bool IsDatabaseError(Exception exception, CancellationToken token) =>
        exception is DbException or TimeoutException or ArgumentException ||
        exception is OperationCanceledException && !token.IsCancellationRequested;

    public static string? ValidateDisplayName(string? name) =>
        string.IsNullOrWhiteSpace(name) ? "Vui lòng nhập tên hiển thị." :
        name.Trim().Length > 150 ? "Tên hiển thị không được vượt quá 150 ký tự." : null;

    public static string? ValidateEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return "Vui lòng nhập email.";
        string value = email.Trim();
        return value.Length > 255 || !MailAddress.TryCreate(value, out MailAddress? address) ||
            address.DisplayName.Length != 0 || !string.Equals(address.Address, value, StringComparison.OrdinalIgnoreCase)
            ? "Email không hợp lệ hoặc vượt quá 255 ký tự." : null;
    }

    public static bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(hash) || hash.StartsWith("DEV_HASH_", StringComparison.Ordinal)) return false;
        try { return BCrypt.Net.BCrypt.Verify(password, hash); }
        catch (Exception exception) when (exception is BCrypt.Net.SaltParseException or ArgumentException or
            FormatException or OverflowException or IndexOutOfRangeException) { return false; }
    }
}
