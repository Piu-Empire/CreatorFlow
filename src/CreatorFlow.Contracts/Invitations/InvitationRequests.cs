namespace CreatorFlow.Contracts.Invitations;

/// <summary>Owner mời một người vào Project theo email. Role là "Manager" hoặc "Creator" (không mời được Owner).</summary>
public sealed record InviteMemberRequest(string? Email, string? Role);
