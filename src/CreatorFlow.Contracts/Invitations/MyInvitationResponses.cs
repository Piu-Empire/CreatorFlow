namespace CreatorFlow.Contracts.Invitations;

/// <summary>
/// Một lời mời gửi tới người dùng hiện tại. Role / Status là tên enum PascalCase ("Manager", "Pending", "Accepted"...).
/// Lời mời Pending đã quá ExpiresAt được trả về là "Expired". Token của lời mời không bao giờ rời khỏi Backend.
/// </summary>
public sealed record MyInvitationResponse(
    long InvitationId,
    long ProjectId,
    string ProjectName,
    string Role,
    string Status,
    string InvitedByName,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    DateTime? RespondedAt);

/// <summary>Các lời mời gửi tới người dùng hiện tại (khớp theo email tài khoản), mới nhất trước.</summary>
public sealed record MyInvitationsResponse(IReadOnlyList<MyInvitationResponse> Invitations);
