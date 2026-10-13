using CreatorFlow.Api.Models.Tasks;

namespace CreatorFlow.Api.Models.Invitations;

/// <summary>Khớp enum PostgreSQL invitation_status (PENDING, ACCEPTED, REJECTED, EXPIRED, CANCELLED).</summary>
public enum InvitationStatus { Pending, Accepted, Rejected, Expired, Cancelled }

/// <summary>Một dòng project_invitations kèm tên người mời. Token không nằm trong model vì không được trả ra ngoài.</summary>
public sealed record ProjectInvitation(
    long InvitationId,
    long ProjectId,
    string InvitedEmail,
    ProjectRole Role,
    InvitationStatus Status,
    string InvitedByName,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    DateTime? RespondedAt);
