using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text.Json;
using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Models.Auth;
using CreatorFlow.Contracts.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.Api.Tests.Services;

[TestClass]
[TestCategory("AuthApiUnit")]
public sealed class JwtTests
{
    [TestMethod]
    public void Token_HasThirtyMinuteLifetimeAndBoundVersion_WithoutSensitiveUserFields()
    {
        var config = Configuration();
        var clock = new FixedClock();
        var user = new User { UserId = 42, Email = "fixture@example.test", DisplayName = "Fixture",
            PasswordHash = "DEV_HASH_FIXTURE", EmailVerifiedAt = clock.GetUtcNow(), AccountStatus = AccountStatus.Active,
            TokenVersion = 7 };
        LoginResponse response = new JwtTokenIssuer(config, clock).Issue(user);
        Assert.AreEqual(clock.GetUtcNow().AddMinutes(30), response.ExpiresAtUtc);
        var handler = new JwtSecurityTokenHandler();
        var parameters = config.ValidationParameters();
        parameters.ValidateLifetime = false; // Stable fake-clock fixture; expiry claim asserted independently.
        var principal = handler.ValidateToken(response.AccessToken, parameters, out _);
        Assert.AreEqual("7", principal.FindFirst("token_version")!.Value);
        var jwt = handler.ReadJwtToken(response.AccessToken);
        Assert.AreEqual(clock.GetUtcNow().AddMinutes(30).UtcDateTime, jwt.ValidTo);
        Assert.IsFalse(jwt.Claims.Any(claim => claim.Type.Contains("password", StringComparison.OrdinalIgnoreCase) || claim.Type.Contains("otp", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(JsonSerializer.Serialize(response.CurrentUser).Contains("PasswordHash", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Token_RejectsWrongSignatureIssuerAudienceAndExpiredLifetime()
    {
        var config = Configuration();
        var user = new User { UserId = 42, Email = "fixture@example.test", DisplayName = "Fixture", PasswordHash = "DEV_HASH_FIXTURE" };
        string token = new JwtTokenIssuer(config, new FixedClock()).Issue(user).AccessToken;
        var handler = new JwtSecurityTokenHandler();
        var wrongKey = Configuration().ValidationParameters();
        wrongKey.ValidateLifetime = false;
        Assert.Throws<SecurityTokenException>(() => handler.ValidateToken(token, wrongKey, out _));
        var wrongIssuer = config.ValidationParameters(); wrongIssuer.ValidateLifetime = false; wrongIssuer.ValidIssuer = "different";
        Assert.Throws<SecurityTokenInvalidIssuerException>(() => handler.ValidateToken(token, wrongIssuer, out _));
        var wrongAudience = config.ValidationParameters(); wrongAudience.ValidateLifetime = false; wrongAudience.ValidAudience = "different";
        Assert.Throws<SecurityTokenInvalidAudienceException>(() => handler.ValidateToken(token, wrongAudience, out _));
        Assert.Throws<SecurityTokenExpiredException>(() => handler.ValidateToken(token, config.ValidationParameters(), out _));
    }

    [TestMethod]
    public void ConfigurationMissingMalformedAndShortKey_AreRejectedWithoutEchoingSecret()
    {
        foreach (string key in new[] { "not-base64-fixture", Convert.ToBase64String(new byte[16]) })
        {
            var settings = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
                { ["Jwt:Issuer"] = "fixture", ["Jwt:Audience"] = "fixture-client", ["Jwt:SigningKey"] = key }).Build();
            var error = Assert.ThrowsExactly<InvalidOperationException>(() => JwtConfiguration.Load(settings));
            Assert.IsFalse(error.Message.Contains(key, StringComparison.Ordinal));
        }
        Assert.ThrowsExactly<InvalidOperationException>(() => JwtConfiguration.Load(new ConfigurationBuilder().Build()));
    }

    internal static JwtConfiguration Configuration() => new()
    {
        Issuer = "fixture", Audience = "fixture-client", SigningKey = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32))
    };
    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
    }
}
