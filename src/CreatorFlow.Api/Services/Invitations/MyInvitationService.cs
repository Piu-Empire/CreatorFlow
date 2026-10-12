using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Models.Invitations;
using CreatorFlow.Api.Repositories.Invitations;
using CreatorFlow.Api.Services.Auth;
using CreatorFlow.Contracts.Invitations;

namespace CreatorFlow.Api.Services.Invitations;

/// <summary>
/// Nghiệp vụ phía NGƯỜI ĐƯỢC MỜI: xem lời mời gửi tới mình, chấp nhận hoặc từ chối. Mọi quy tắc được kiểm tra Ở ĐÂY và ở transaction của repository:
///   - người thao tác (và email dùng để khớp lời mời) luôn lấy từ JWT, không bao giờ nhận từ body/route;
///   - chỉ lời mời gửi tới email của chính mình mới thao tác được (lời mời của người khác → 404, không lộ là có tồn tại);
///   - chỉ lời mời Pending còn hạn mới trả lời được;
///   - chấp nhận tạo thành viên Project đúng một lần; từ chối không tạo thành viên.
/// </summary>
public sealed class MyInvitationService(IMyInvitationRepository repository, CurrentAuthenticatedUser current, TimeProvider timeProvider)
{
    public async Task<AuthOperationResult<MyInvitationsResponse>> GetMineAsync(CancellationToken token = default)
    {
        if (current.User is not { } user) return Unauthorized<MyInvitationsResponse>();

        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        var rows = await repository.GetByEmailAsync(user.Email, token);
        return new(new MyInvitationsResponse(rows.Select(r => ToResponse(r, now)).ToList()));
    }

    public Task<AuthOperationResult<MyInvitationResponse>> AcceptAsync(long invitationId, CancellationToken token = default) =>
        DecideAsync(invitationId, accept: true, token);

    public Task<AuthOperationResult<MyInvitationResponse>> RejectAsync(long invitationId, CancellationToken token = default) =>
        DecideAsync(invitationId, accept: false, token);

    private async Task<AuthOperationResult<MyInvitationResponse>> DecideAsync(long invitationId, bool accept, CancellationToken token)
    {
        if (current.User is not { } user) return Unauthorized<MyInvitationResponse>();

        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        InvitationDecisionResult result = await repository.DecideAsync(invitationId, user.UserId, user.Email, InvitationRules.StatusFor(accept), now, token);

        return result.Outcome switch
        {
            InvitationDecisionOutcome.Decided when result.Invitation is { } decided =>
                new AuthOperationResult<MyInvitationResponse>(ToResponse(decided, now)),
            InvitationDecisionOutcome.NotFound =>
                AuthOperationResult<MyInvitationResponse>.Fail("not_found", "Không tìm thấy lời mời này.", status: 404),
            InvitationDecisionOutcome.Expired =>
                AuthOperationResult<MyInvitationResponse>.Fail("invitation_expired", "Lời mời đã hết hạn.", status: 409),
            InvitationDecisionOutcome.NotPending =>
                AuthOperationResult<MyInvitationResponse>.Fail("invitation_not_pending", NotPendingMessage(result.CurrentStatus), status: 409),
            _ => AuthOperationResult<MyInvitationResponse>.Fail("not_found", "Không tìm thấy lời mời này.", status: 404),
        };
    }

    // ------------------------------------------------------------------ helpers

    private static string NotPendingMessage(InvitationStatus? status) => status switch
    {
        InvitationStatus.Accepted => "Lời mời này đã được chấp nhận trước đó.",
        InvitationStatus.Rejected => "Lời mời này đã bị từ chối trước đó.",
        InvitationStatus.Cancelled => "Lời mời này đã bị huỷ.",
        InvitationStatus.Expired => "Lời mời đã hết hạn.",
        _ => "Lời mời này không còn chờ phản hồi.",
    };

    private static MyInvitationResponse ToResponse(MyInvitation i, DateTime nowUtc) => new(
        i.InvitationId, i.ProjectId, i.ProjectName, i.Role.ToString(),
        InvitationRules.EffectiveStatus(i.Status, i.ExpiresAt, nowUtc).ToString(),
        i.InvitedByName, i.CreatedAt, i.ExpiresAt, i.RespondedAt);

    private static AuthOperationResult<T> Unauthorized<T>() =>
        AuthOperationResult<T>.Fail("unauthorized", "Vui lòng đăng nhập lại.", status: 401);
}
