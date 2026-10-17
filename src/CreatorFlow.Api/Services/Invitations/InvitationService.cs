using System.Buffers.Text;
using System.Security.Cryptography;
using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Models.Invitations;
using CreatorFlow.Api.Models.Tasks;
using CreatorFlow.Api.Repositories.Invitations;
using CreatorFlow.Api.Services.Auth;
using CreatorFlow.Contracts.Invitations;
using Microsoft.Extensions.Logging;

namespace CreatorFlow.Api.Services.Invitations;

/// <summary>
/// Nghiệp vụ mời User vào Project. Mọi quyền được kiểm tra Ở ĐÂY (không tin giao diện):
///   - chỉ Owner của Project được mời và xem danh sách lời mời;
///   - email hợp lệ, vai trò mời là Manager/Creator;
///   - không mời người đã là thành viên đang hoạt động, không mời trùng khi còn lời mời Pending còn hạn;
///   - lời mời luôn thuộc đúng Project trên route và được lưu ở trạng thái Pending.
/// Người thao tác luôn lấy từ JWT, không bao giờ nhận từ body/route.
/// </summary>
public sealed class InvitationService(IInvitationRepository repository, CurrentAuthenticatedUser current, TimeProvider timeProvider,
    IEmailSender emailSender, ILogger<InvitationService> logger)
{
    public async Task<AuthOperationResult<InvitationResponse>> InviteAsync(
        long projectId, InviteMemberRequest request, CancellationToken token = default)
    {
        if (current.User is not { } user) return Unauthorized<InvitationResponse>();

        ProjectRole? role = await repository.GetRoleAsync(projectId, user.UserId, token);
        if (role is null) return NotMember<InvitationResponse>();
        if (!InvitationRules.CanInvite(role)) return Forbidden<InvitationResponse>("Chỉ Owner mới được mời thành viên.");

        if (AuthRules.ValidateEmail(request.Email) is { } emailError)
            return AuthOperationResult<InvitationResponse>.Fail("validation", emailError, "email");
        if (!InvitationRules.TryParseInviteRole(request.Role, out ProjectRole inviteRole))
            return AuthOperationResult<InvitationResponse>.Fail("validation", "Vai trò mời phải là Manager hoặc Creator.", "role");

        string email = request.Email!.Trim();

        if (await repository.IsActiveMemberByEmailAsync(projectId, email, token))
            return AuthOperationResult<InvitationResponse>.Fail("already_member", "Người này đã là thành viên của Project.", "email", 409);

        if (!emailSender.IsConfigured)
            return AuthOperationResult<InvitationResponse>.Fail("email_not_configured",
                "Dịch vụ gửi email lời mời chưa được cấu hình.", status: 503);

        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        ProjectInvitation? created = await repository.CreatePendingAsync(
            projectId, email, inviteRole, user.UserId, NewToken(), now, now + InvitationRules.Lifetime, token);

        if (created is null)
            return AuthOperationResult<InvitationResponse>.Fail("invitation_pending", "Email này đã có lời mời đang chờ phản hồi.", "email", 409);

        try
        {
            await emailSender.SendProjectInvitationAsync(email, projectId, inviteRole.ToString(), user.DisplayName,
                created.ExpiresAt, token);
        }
        catch (OperationCanceledException)
        {
            await CancelUndeliveredInvitationAsync(created);
            throw;
        }
        catch (EmailDeliveryException)
        {
            await CancelUndeliveredInvitationAsync(created);
            return AuthOperationResult<InvitationResponse>.Fail("email_delivery_failed",
                "Không gửi được email lời mời. Hãy kiểm tra cấu hình email rồi thử lại.", status: 503);
        }

        return new AuthOperationResult<InvitationResponse>(ToResponse(created, now), Status: 201);
    }

    private async Task CancelUndeliveredInvitationAsync(ProjectInvitation invitation)
    {
        try
        {
            bool cancelled = await repository.CancelPendingAsync(invitation.InvitationId,
                timeProvider.GetUtcNow().UtcDateTime, CancellationToken.None);
            if (!cancelled)
                logger.LogWarning("Invitation {InvitationId} was no longer pending after email delivery failed.", invitation.InvitationId);
        }
        catch (Exception exception)
        {
            logger.LogError("Could not cancel invitation {InvitationId} after email delivery failed. Failure type: {FailureType}.",
                invitation.InvitationId, exception.GetType().Name);
        }
    }

    public async Task<AuthOperationResult<InvitationsResponse>> GetInvitationsAsync(long projectId, CancellationToken token = default)
    {
        if (current.User is not { } user) return Unauthorized<InvitationsResponse>();

        ProjectRole? role = await repository.GetRoleAsync(projectId, user.UserId, token);
        if (role is null) return NotMember<InvitationsResponse>();
        if (!InvitationRules.CanViewInvitations(role)) return Forbidden<InvitationsResponse>("Chỉ Owner mới được xem lời mời của Project.");

        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        var rows = await repository.GetByProjectAsync(projectId, token);
        return new(new InvitationsResponse(rows.Select(r => ToResponse(r, now)).ToList()));
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>Token ngẫu nhiên 256 bit (43 ký tự base64url). Chỉ lưu ở DB, không trả ra API.</summary>
    private static string NewToken() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));

    private static InvitationResponse ToResponse(ProjectInvitation i, DateTime nowUtc) => new(
        i.InvitationId, i.ProjectId, i.InvitedEmail, i.Role.ToString(),
        InvitationRules.EffectiveStatus(i.Status, i.ExpiresAt, nowUtc).ToString(),
        i.InvitedByName, i.CreatedAt, i.ExpiresAt, i.RespondedAt);

    private static AuthOperationResult<T> Unauthorized<T>() =>
        AuthOperationResult<T>.Fail("unauthorized", "Vui lòng đăng nhập lại.", status: 401);

    private static AuthOperationResult<T> Forbidden<T>(string message) =>
        AuthOperationResult<T>.Fail("forbidden", message, status: 403);

    private static AuthOperationResult<T> NotMember<T>() => Forbidden<T>("Bạn không thuộc Project này.");
}
