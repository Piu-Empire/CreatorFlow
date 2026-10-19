using CreatorFlow.Api.Endpoints;
using CreatorFlow.Api.ErrorHandling;
using CreatorFlow.Api.Services;
using CreatorFlow.Data;
using CreatorFlow.Repositories;
using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Repositories.Admin;
using CreatorFlow.Api.Repositories.Metrics;
using CreatorFlow.Api.Repositories.Tasks;
using CreatorFlow.Api.Services.Admin;
using CreatorFlow.Api.Services.Metrics;
using Microsoft.AspNetCore.Authorization;
using CreatorFlow.Api.Configuration;
using CreatorFlow.Api.Services.Auth;
using CreatorFlow.Api.Repositories.Auth;
using CreatorFlow.Api.Services.Tasks;
using CreatorFlow.Api.Repositories.Workflow;
using CreatorFlow.Api.Services.Workflow;
using CreatorFlow.Api.Repositories.ReviewQueue;
using CreatorFlow.Api.Services.ReviewQueue;
using CreatorFlow.Api.Repositories.Invitations;
using CreatorFlow.Api.Services.Invitations;
using CreatorFlow.Api.Repositories.Ideas;
using CreatorFlow.Api.Services.Ideas;
using CreatorFlow.Api.Repositories.Board;
using CreatorFlow.Api.Services.Board;
using CreatorFlow.Api.Repositories.Search;
using CreatorFlow.Api.Services.Search;
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
    services.GetRequiredService<CurrentAuthenticatedUser>(), protector, services.GetRequiredService<IEmailSender>(), services.GetRequiredService<TimeProvider>(),
    services.GetRequiredService<ILogger<EmailVerificationService>>()));
builder.Services.AddScoped(services => new PasswordResetService(services.GetRequiredService<IUserRepository>(),
    services.GetRequiredService<CurrentAuthenticatedUser>(), protector, services.GetRequiredService<IEmailSender>(), services.GetRequiredService<TimeProvider>(),
    services.GetRequiredService<ILogger<PasswordResetService>>()));
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<IAdminUserRepository, AdminUserRepository>();
builder.Services.AddScoped<AdminUserService>();
builder.Services.AddScoped<IAdminReportRepository, AdminReportRepository>();
builder.Services.AddScoped<AdminReportService>();
builder.Services.AddScoped<ITaskRepository, TaskRepository>(); // BƯỚC 0a: thiếu, MetricService cần GetRoleAsync/GetContentAsync
builder.Services.AddScoped<IMetricRepository, MetricRepository>();
builder.Services.AddScoped<MetricService>();
builder.Services.AddAuthorizationBuilder().AddPolicy("SystemAdminOnly", policy =>
    policy.RequireAssertion(context =>
        context.Resource is HttpContext http &&
        http.RequestServices.GetRequiredService<CurrentAuthenticatedUser>().User is { IsSystemAdmin: true }));
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<IWorkflowRepository, WorkflowRepository>();
builder.Services.AddScoped<WorkflowService>();
builder.Services.AddScoped<IReviewQueueRepository, ReviewQueueRepository>();
builder.Services.AddScoped<ReviewQueueService>();
builder.Services.AddScoped<IInvitationRepository, InvitationRepository>();
builder.Services.AddScoped<InvitationService>();
builder.Services.AddScoped<IMyInvitationRepository, MyInvitationRepository>();
builder.Services.AddScoped<MyInvitationService>();
builder.Services.AddScoped<IIdeaRepository, IdeaRepository>();
builder.Services.AddScoped<IdeaService>();
builder.Services.AddScoped<IBoardCardRepository, BoardCardRepository>();
builder.Services.AddScoped<BoardService>();
builder.Services.AddScoped<IGlobalSearchRepository, GlobalSearchRepository>();
builder.Services.AddScoped<GlobalSearchService>();
builder.Services.AddScoped<CreatorFlow.Api.Repositories.ContentDetail.IContentDetailRepository, CreatorFlow.Api.Repositories.ContentDetail.ContentDetailRepository>();
builder.Services.AddScoped<CreatorFlow.Api.Services.ContentDetail.ContentDetailService>();
builder.Services.AddScoped<CreatorFlow.Api.Repositories.Projects.IProjectRepository, CreatorFlow.Api.Repositories.Projects.ProjectRepository>();
builder.Services.AddScoped<CreatorFlow.Api.Services.Projects.ProjectService>();
builder.Services.AddScoped<CreatorFlow.Api.Services.Projects.ProjectMembersService>();
builder.Services.AddSingleton<IAvatarImageProcessor, AvatarImageProcessor>();

builder.Services.AddSingleton<IDbConnectionFactory>(new NpgsqlConnectionFactory(connectionString));
builder.Services.AddScoped<IPlanRepository, PlanRepository>();
builder.Services.AddSingleton<HealthService>();
builder.Services.AddScoped<IDatabaseReadinessRepository, DatabaseReadinessRepository>();
builder.Services.AddScoped<DatabaseReadinessService>();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
});
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

var app = builder.Build();
string version = typeof(Program).Assembly
    .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
    .OfType<System.Reflection.AssemblyInformationalVersionAttribute>().Single().InformationalVersion;
app.Logger.LogInformation("CreatorFlow API starting. Version: {Version}; Environment: {Environment}; MailConfigured: {MailConfigured}",
    version, app.Environment.EnvironmentName, mail is not null);
app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    await next(context);
    if (context.Response.StatusCode >= 400)
        app.Logger.LogWarning("API request rejected. TraceId: {TraceId}; StatusCode: {StatusCode}",
            context.TraceIdentifier, context.Response.StatusCode);
});
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthEndpoints();
app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapAdminEndpoints();
app.MapReportEndpoints();
app.MapMetricEndpoints();
app.MapTaskEndpoints();
app.MapWorkflowEndpoints();
app.MapReviewQueueEndpoints();
app.MapInvitationEndpoints();
app.MapMyInvitationEndpoints();
app.MapProjectEndpoints();
app.MapIdeaEndpoints();
app.MapBoardEndpoints();
app.MapGlobalSearchEndpoints();
app.MapContentDetailEndpoints();
app.Run();

public partial class Program { }
