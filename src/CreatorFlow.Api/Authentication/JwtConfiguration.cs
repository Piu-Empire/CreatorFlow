using Microsoft.IdentityModel.Tokens;

namespace CreatorFlow.Api.Authentication;

public sealed class JwtConfiguration
{
    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public required SymmetricSecurityKey SigningKey { get; init; }
    public TimeSpan Lifetime => TimeSpan.FromMinutes(30);

    public static JwtConfiguration Load(IConfiguration configuration)
    {
        string? issuer = configuration["Jwt:Issuer"];
        string? audience = configuration["Jwt:Audience"];
        string? encodedKey = configuration["Jwt:SigningKey"];
        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience) || string.IsNullOrWhiteSpace(encodedKey))
            throw new InvalidOperationException("Backend Jwt:Issuer, Jwt:Audience and Jwt:SigningKey are required.");
        byte[] key;
        try { key = Convert.FromBase64String(encodedKey); }
        catch (FormatException) { throw new InvalidOperationException("Backend JWT signing key must be Base64."); }
        if (key.Length < 32) throw new InvalidOperationException("Backend JWT signing key requires at least 32 random bytes.");
        return new() { Issuer = issuer.Trim(), Audience = audience.Trim(), SigningKey = new SymmetricSecurityKey(key) };
    }

    public TokenValidationParameters ValidationParameters() => new()
    {
        ValidateIssuer = true, ValidIssuer = Issuer,
        ValidateAudience = true, ValidAudience = Audience,
        ValidateIssuerSigningKey = true, IssuerSigningKey = SigningKey,
        ValidateLifetime = true, RequireExpirationTime = true, RequireSignedTokens = true,
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256], ClockSkew = TimeSpan.Zero
    };
}
