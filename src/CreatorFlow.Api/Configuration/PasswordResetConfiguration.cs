using Microsoft.Extensions.Configuration;
using System.Net.Mail;

namespace CreatorFlow.Api.Configuration;

public sealed class PasswordResetConfiguration
{
    public required byte[] VerifierKey { get; init; }
    public required string Host { get; init; }
    public int Port { get; init; }
    public required string From { get; init; }
    public required string Username { get; init; }
    public required string Password { get; init; }
    public required string TlsMode { get; init; }

    public static PasswordResetConfiguration? Load(IConfiguration configuration)
    {
        string? keyValue = configuration["PasswordReset:VerifierKey"];
        string? host = configuration["PasswordReset:Smtp:Host"];
        string? from = configuration["PasswordReset:Smtp:From"];
        string? username = configuration["PasswordReset:Smtp:Username"];
        string? password = configuration["PasswordReset:Smtp:Password"];
        string? tls = configuration["PasswordReset:Smtp:TlsMode"];
        if (string.IsNullOrWhiteSpace(keyValue) || string.IsNullOrWhiteSpace(host) ||
            !MailAddress.TryCreate(from, out _) || string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrEmpty(password) || tls is not ("StartTls" or "SslOnConnect") ||
            !int.TryParse(configuration["PasswordReset:Smtp:Port"], out int port) || port is < 1 or > 65535)
            return null;
        byte[] key;
        try { key = Convert.FromBase64String(keyValue); }
        catch (FormatException) { return null; }
        if (key.Length != 32) return null;
        return new PasswordResetConfiguration { VerifierKey = key, Host = host.Trim(), Port = port,
            From = from!, Username = username, Password = password, TlsMode = tls };
    }
}
