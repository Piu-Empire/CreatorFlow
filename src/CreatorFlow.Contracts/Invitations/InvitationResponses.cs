namespace CreatorFlow.Contracts.Invitations;

/// <summary>
/// Một lời mời vào Project. Role / Status là tên enum PascalCase ("Manager", "Pending", "Accepted"...).
/// Lời mời Pending đã quá ExpiresAt được trả về là "Expired". Token của lời mời không bao giờ rời khỏi Backend.
/// </summary>
public sealed record InvitationResponse(
    long InvitationId,
    long ProjectId,
    string InvitedEmail,
    string Role,
    string Status,
    string InvitedByName,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    DateTime? RespondedAt);

/// <summary>Các lời mời của một Project, mới nhất trước.</summary>
public sealed record InvitationsResponse(IReadOnlyList<InvitationResponse> Invitations);
