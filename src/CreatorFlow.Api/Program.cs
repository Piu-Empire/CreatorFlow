using CreatorFlow.Api.Endpoints;
using CreatorFlow.Api.ErrorHandling;
using CreatorFlow.Api.Services;
using CreatorFlow.Data;
using CreatorFlow.Repositories;

var builder = WebApplication.CreateBuilder(args);
string connectionString = DatabaseConfiguration.GetConnectionString(builder.Configuration);

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
app.MapHealthEndpoints();
app.Run();
