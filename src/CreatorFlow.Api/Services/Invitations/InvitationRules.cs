using CreatorFlow.Api.Models.Invitations;
using CreatorFlow.Api.Models.Tasks;

namespace CreatorFlow.Api.Services.Invitations;

/// <summary>
/// Quy tắc nghiệp vụ mời thành viên vào Project, thực thi Ở BACKEND.
/// Hàm kiểm tra là hàm thuần (không đụng DB) để InvitationService và test dùng chung.
/// </summary>
public static class InvitationRules
{
    /// <summary>Lời mời hết hạn sau 7 ngày kể từ lúc tạo.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    /// <summary>Chỉ Owner được mời thành viên.</summary>
    public static bool CanInvite(ProjectRole? role) => role is ProjectRole.Owner;

    /// <summary>Chỉ Owner được xem danh sách lời mời của Project.</summary>
    public static bool CanViewInvitations(ProjectRole? role) => role is ProjectRole.Owner;

    /// <summary>Chỉ mời được Manager hoặc Creator (schema cấm role OWNER trong lời mời). Chỉ nhận chữ cái: không nhận số ("1") hay danh sách ("Owner,Manager").</summary>
    public static bool TryParseInviteRole(string? value, out ProjectRole role)
    {
        role = default;
        string text = value?.Trim() ?? string.Empty;
        return text.Length > 0 && text.All(char.IsLetter)
            && Enum.TryParse(text, ignoreCase: true, out role)
            && role is ProjectRole.Manager or ProjectRole.Creator;
    }

    /// <summary>Quyết định của người được mời: chấp nhận → Accepted, từ chối → Rejected.</summary>
    public static InvitationStatus StatusFor(bool accept) => accept ? InvitationStatus.Accepted : InvitationStatus.Rejected;

    /// <summary>Pending nhưng đã quá hạn thì được coi là Expired (DB chỉ đổi status khi có lời mời mới cho cùng email).</summary>
    public static InvitationStatus EffectiveStatus(InvitationStatus status, DateTime expiresAtUtc, DateTime nowUtc) =>
        status == InvitationStatus.Pending && expiresAtUtc <= nowUtc ? InvitationStatus.Expired : status;
}
