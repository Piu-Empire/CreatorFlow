using CreatorFlow.Api.Endpoints;
using CreatorFlow.Api.ErrorHandling;
using CreatorFlow.Api.Services;
using CreatorFlow.Data;
using CreatorFlow.Repositories;
using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Configuration;
using CreatorFlow.Api.Services.Auth;
using CreatorFlow.Api.Repositories.Auth;
using Microsoft.AspNetCore.Http.Features;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
string connectionString = DatabaseConfiguration.GetConnectionString(builder.Configuration);
JwtConfiguration jwt = JwtConfiguration.Load(builder.Configuration);
PasswordResetConfiguration? mail = PasswordResetConfiguration.Load(builder.Configuration);
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 2097152 + 65536);
builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = 2097152 + 65536);
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow);
builder.Services.AddCreatorFlowAuthentication(jwt);
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddSingleton<IEmailSender>(mail is null ? new UnavailableEmailSender() : new SmtpEmailSender(mail));
EmailOtpCodeProtector? protector = mail is null ? null : new EmailOtpCodeProtector(mail.VerifierKey);
builder.Services.AddScoped(services => new EmailVerificationService(services.GetRequiredService<IUserRepository>(),
    services.GetRequiredService<CurrentAuthenticatedUser>(), protector, services.GetRequiredService<IEmailSender>(), services.GetRequiredService<TimeProvider>()));
builder.Services.AddScoped(services => new PasswordResetService(services.GetRequiredService<IUserRepository>(),
    services.GetRequiredService<CurrentAuthenticatedUser>(), protector, services.GetRequiredService<IEmailSender>(), services.GetRequiredService<TimeProvider>()));
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddSingleton<IAvatarImageProcessor, AvatarImageProcessor>();

builder.Services.AddSingleton<IDbConnectionFactory>(new NpgsqlConnectionFactory(connectionString));
builder.Services.AddScoped<IPlanRepository, PlanRepository>();
builder.Services.AddSingleton<HealthService>();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
});
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthEndpoints();
app.MapAuthEndpoints();
app.MapUserEndpoints();
app.Run();

public partial class Program { }
