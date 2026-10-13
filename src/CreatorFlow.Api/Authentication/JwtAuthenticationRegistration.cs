using System.Data.Common;
using System.Globalization;
using CreatorFlow.Api.Models.Auth;
using CreatorFlow.Api.Repositories.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace CreatorFlow.Api.Authentication;

public static class JwtAuthenticationRegistration
{
    public static IServiceCollection AddCreatorFlowAuthentication(this IServiceCollection services, JwtConfiguration configuration)
    {
        services.AddSingleton(configuration);
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<JwtTokenIssuer>();
        services.AddScoped<CurrentAuthenticatedUser>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.IncludeErrorDetails = false;
            options.TokenValidationParameters = configuration.ValidationParameters();
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var subjects = context.Principal!.FindAll("sub").ToArray();
                    var versions = context.Principal.FindAll("token_version").ToArray();
                    if (subjects.Length != 1 || versions.Length != 1 ||
                        !long.TryParse(subjects[0].Value, NumberStyles.None, CultureInfo.InvariantCulture, out long id) || id <= 0 ||
                        !long.TryParse(versions[0].Value, NumberStyles.None, CultureInfo.InvariantCulture, out long version))
                    { context.Fail("Invalid authentication context."); return; }
                    User? user;
                    try
                    {
                        user = await context.HttpContext.RequestServices.GetRequiredService<IUserRepository>()
                            .FindByIdAsync(id, context.HttpContext.RequestAborted);
                    }
                    catch (Exception exception) when (exception is DbException or TimeoutException)
                    { throw new AuthBackendUnavailableException(); }
                    catch (OperationCanceledException) when (!context.HttpContext.RequestAborted.IsCancellationRequested)
                    { throw new AuthBackendUnavailableException(); }
                    if (user is null || user.TokenVersion != version || user.AccountStatus != AccountStatus.Active || user.EmailVerifiedAt is null)
                    { context.Fail("Authentication is no longer valid."); return; }
                    context.HttpContext.RequestServices.GetRequiredService<CurrentAuthenticatedUser>().User = user;
                },
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception is AuthBackendUnavailableException) throw new AuthBackendUnavailableException();
                    return Task.CompletedTask;
                }
            };
        });
        services.AddAuthorization();
        return services;
    }
}
