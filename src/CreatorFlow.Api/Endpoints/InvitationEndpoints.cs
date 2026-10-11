using CreatorFlow.Api.Services.Invitations;
using CreatorFlow.Contracts.Invitations;

namespace CreatorFlow.Api.Endpoints;

/// <summary>
/// Mời User tham gia Project. Tất cả yêu cầu đăng nhập; quyền chi tiết do InvitationService kiểm tra.
/// Người thao tác luôn lấy từ JWT, không bao giờ nhận từ body/route.
/// </summary>
public static class InvitationEndpoints
{
    public static void MapInvitationEndpoints(this IEndpointRouteBuilder routes)
    {
        var api = routes.MapGroup("/api").RequireAuthorization();

        // Owner gửi lời mời (Manager/Creator) theo email → 201 + lời mời ở trạng thái Pending.
        api.MapPost("/projects/{projectId:long}/invitations",
            async (long projectId, InviteMemberRequest request, InvitationService service, CancellationToken token) =>
                AuthEndpoints.ToHttp(await service.InviteAsync(projectId, request, token)));

        // Owner xem các lời mời của Project (Pending/Accepted/Rejected/Expired/Cancelled), mới nhất trước.
        api.MapGet("/projects/{projectId:long}/invitations",
            async (long projectId, InvitationService service, CancellationToken token) =>
                AuthEndpoints.ToHttp(await service.GetInvitationsAsync(projectId, token)));
    }
}
