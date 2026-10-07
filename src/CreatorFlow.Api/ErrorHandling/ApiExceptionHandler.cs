using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CreatorFlow.Api.Authentication;

namespace CreatorFlow.Api.ErrorHandling;

public sealed class ApiExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (context.RequestAborted.IsCancellationRequested)
        {
            return true;
        }

        // Do not log exception messages that could contain SQL or credentials.
        logger.LogError("API request failed. TraceId: {TraceId}", context.TraceIdentifier);
        bool authUnavailable = exception is AuthBackendUnavailableException;
        context.Response.StatusCode = authUnavailable ? StatusCodes.Status503ServiceUnavailable : StatusCodes.Status500InternalServerError;
        var problem = new ProblemDetails
        {
            Status = context.Response.StatusCode,
            Title = authUnavailable ? "Authentication is temporarily unavailable." : "An unexpected server error occurred."
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;
        bool written = await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problem
        });
        if (!written)
        {
            await context.Response.WriteAsJsonAsync(problem, cancellationToken);
        }
        return true;
    }
}
