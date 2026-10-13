using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CreatorFlow.Api.Models.Auth;
using CreatorFlow.Contracts.Auth;
using Microsoft.IdentityModel.Tokens;

namespace CreatorFlow.Api.Authentication;

public sealed class JwtTokenIssuer(JwtConfiguration configuration, TimeProvider clock)
{
    public LoginResponse Issue(User user)
    {
        DateTimeOffset now = clock.GetUtcNow();
        DateTimeOffset expires = now.Add(configuration.Lifetime);
        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString(CultureInfo.InvariantCulture)),
            new("token_version", user.TokenVersion.ToString(CultureInfo.InvariantCulture)),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64)
        ];
        var token = new JwtSecurityToken(configuration.Issuer, configuration.Audience, claims,
            now.UtcDateTime, expires.UtcDateTime, new SigningCredentials(configuration.SigningKey, SecurityAlgorithms.HmacSha256));
        return new(new JwtSecurityTokenHandler().WriteToken(token), expires,
            new(user.UserId, user.Email, user.DisplayName, user.IsSystemAdmin));
    }
}
