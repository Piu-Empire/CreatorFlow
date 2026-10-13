using CreatorFlow.Api.Services.Auth;
using CreatorFlow.Contracts.Auth;
using Microsoft.AspNetCore.Mvc;

namespace CreatorFlow.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        var auth = routes.MapGroup("/api/auth").AllowAnonymous();
        auth.MapPost("/register", async (RegisterRequest request, AuthService service, CancellationToken token) =>
            ToHttp(await service.RegisterAsync(request, token)));
        auth.MapPost("/login", async (LoginRequest request, AuthService service, CancellationToken token) =>
            ToHttp(await service.LoginAsync(request, token)));
        auth.MapGet("/capabilities", async (EmailVerificationService verification, PasswordResetService reset, CancellationToken token) =>
            Results.Ok(new AuthCapabilitiesResponse(await verification.IsEmailVerificationAvailableAsync(token),
                await reset.IsPasswordResetAvailableAsync(token))));
        auth.MapPost("/email-verification/requests", async (EmailCodeRequest request, EmailVerificationService service, CancellationToken token) =>
        {
            if (!await service.IsEmailVerificationAvailableAsync(token)) return Problem("email_unavailable", "Xác minh email chưa sẵn sàng.", 503);
            PasswordResetResult result = await service.RequestEmailVerificationAsync(request.Email, token);
            if (!result.Succeeded) return Problem("verification_request_failed", result.Message, result.RequestId.HasValue ? 503 : 400, result.ErrorField, result.RequestId);
            return Results.Json(new VerificationRequestResponse(result.RequestId!.Value, result.Message, 60), statusCode: 202);
        });
        auth.MapPost("/email-verification/confirm", async (VerifyEmailRequest request, EmailVerificationService service, CancellationToken token) =>
        {
            PasswordResetResult result = await service.VerifyEmailAsync(request.RequestId, request.Code, token);
            return result.Succeeded ? Results.Ok(new ReloginResponse(true)) : Problem("invalid_verification", result.Message, 400, result.ErrorField);
        });
        auth.MapPost("/password-reset/requests", async (EmailCodeRequest request, PasswordResetService service, CancellationToken token) =>
        {
            if (!await service.IsPasswordResetAvailableAsync(token)) return Problem("reset_unavailable", "Khôi phục mật khẩu chưa sẵn sàng.", 503);
            PasswordResetResult result = await service.RequestPasswordResetAsync(request.Email, token);
            return result.Succeeded ? Results.Json(new ResetRequestResponse(result.RequestId!.Value, result.Message), statusCode: 202) :
                Problem("reset_request_failed", result.Message, 400, result.ErrorField);
        });
        auth.MapPost("/password-reset/confirm", async (ResetPasswordRequest request, PasswordResetService service, CancellationToken token) =>
        {
            PasswordResetResult result = await service.ResetPasswordAsync(request.RequestId, request.Code, request.NewPassword, request.ConfirmPassword, token);
            return result.Succeeded ? Results.Ok(new ReloginResponse(true)) : Problem("invalid_reset", result.Message, 400, result.ErrorField);
        });
    }

    internal static IResult ToHttp<T>(AuthOperationResult<T> result) => result.Succeeded ?
        Results.Json(result.Value, statusCode: result.Status) : Problem(result.ErrorCode!, result.Message!, result.Status, result.ErrorField);

    internal static IResult Problem(string code, string message, int status, string? field = null, Guid? requestId = null)
    {
        var extensions = new Dictionary<string, object?> { ["code"] = code };
        if (field is not null) extensions["errors"] = new Dictionary<string, string[]> { [field] = [message] };
        if (requestId.HasValue) extensions["requestId"] = requestId;
        return Results.Problem(statusCode: status, title: message, extensions: extensions);
    }
}
