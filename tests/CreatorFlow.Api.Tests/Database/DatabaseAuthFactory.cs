using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Repositories.Auth;
using CreatorFlow.Api.Services.Auth;
using CreatorFlow.Api.Tests.Services;
using System.Security.Cryptography;

namespace CreatorFlow.Api.Tests.Database;

internal static class DatabaseAuthFactory
{
    public static AuthService Create(IUserRepository repository) => new(repository,
        new EmailVerificationService(repository, new CurrentAuthenticatedUser(),
            new EmailOtpCodeProtector(RandomNumberGenerator.GetBytes(32)), new TestEmailSender(), new ImmediateClock()),
        new JwtTokenIssuer(JwtTests.Configuration(), TimeProvider.System));
}
