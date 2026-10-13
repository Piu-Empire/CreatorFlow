using CreatorFlow.Api.Services.Auth;
using CreatorFlow.Contracts.Auth;
using CreatorFlow.Contracts.Users;

namespace CreatorFlow.Api.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder routes)
    {
        var me = routes.MapGroup("/api/users/me").RequireAuthorization();
        me.MapGet("/", async (UserService service, CancellationToken token) => AuthEndpoints.ToHttp(await service.GetProfileAsync(token)));
        me.MapGet("/avatar", async (UserService service, HttpResponse response, CancellationToken token) =>
        {
            var result = await service.GetAvatarAsync(token);
            response.Headers.CacheControl = "no-store";
            return result.Succeeded ? Results.Bytes(result.Value!.ImageData, "image/png") : AuthEndpoints.ToHttp(result);
        });
        me.MapPost("/password", async (ChangePasswordRequest request, UserService service, CancellationToken token) =>
            AuthEndpoints.ToHttp(await service.ChangePasswordAsync(request, token)));
        me.MapPut("/profile", async (HttpRequest request, UserService service, CancellationToken token) =>
        {
            if (!request.HasFormContentType) return AuthEndpoints.Problem("validation", "Yêu cầu hồ sơ phải dùng multipart form.", 400);
            var form = await request.ReadFormAsync(token);
            string[] allowed = ["displayName", "avatarUrl", "avatarAction"];
            if (form.Keys.Any(key => !allowed.Contains(key, StringComparer.Ordinal)) || form.Files.Any(file => file.Name != "image") || form.Files.Count > 1)
                return AuthEndpoints.Problem("validation", "Yêu cầu chứa trường hồ sơ không được phép.", 400);
            if (!Enum.TryParse(form["avatarAction"].ToString(), ignoreCase: true, out AvatarAction action) || !Enum.IsDefined(action))
                return AuthEndpoints.Problem("validation", "Nguồn ảnh không hợp lệ.", 400, "AvatarUrl");
            byte[]? image = null;
            if (form.Files.Count == 1)
            {
                var file = form.Files[0];
                if (action != AvatarAction.Upload || file.Length is <= 0 or > 2097152)
                    return AuthEndpoints.Problem("validation", "Ảnh phải là JPEG/PNG tối đa 2 MiB.", 400, "AvatarUrl");
                await using var stream = file.OpenReadStream();
                image = new byte[checked((int)file.Length)];
                await stream.ReadExactlyAsync(image, token);
            }
            return AuthEndpoints.ToHttp(await service.SaveProfileAsync(form["displayName"], form["avatarUrl"], action, image, token));
        });
    }
}
