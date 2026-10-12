using CreatorFlow.Api.Services.Invitations;

namespace CreatorFlow.Api.Endpoints;

/// <summary>
/// Người được mời xem lời mời gửi tới mình và chấp nhận / từ chối. Tất cả yêu cầu đăng nhập.
/// Người thao tác (và email dùng để khớp lời mời) luôn lấy từ JWT, không bao giờ nhận từ body/route.
/// </summary>
public static class MyInvitationEndpoints
{
    public static void MapMyInvitationEndpoints(this IEndpointRouteBuilder routes)
    {
        var api = routes.MapGroup("/api/invitations").RequireAuthorization();

        // Các lời mời gửi tới email của tôi (mọi trạng thái), mới nhất trước.
        api.MapGet("/mine",
            async (MyInvitationService service, CancellationToken token) =>
                AuthEndpoints.ToHttp(await service.GetMineAsync(token)));

        // Chấp nhận → tạo thành viên Project đúng một lần; trả về lời mời đã ở trạng thái Accepted.
        api.MapPost("/{invitationId:long}/accept",
            async (long invitationId, MyInvitationService service, CancellationToken token) =>
                AuthEndpoints.ToHttp(await service.AcceptAsync(invitationId, token)));

        // Từ chối → không tạo thành viên; trả về lời mời đã ở trạng thái Rejected.
        api.MapPost("/{invitationId:long}/reject",
            async (long invitationId, MyInvitationService service, CancellationToken token) =>
                AuthEndpoints.ToHttp(await service.RejectAsync(invitationId, token)));
    }
}
