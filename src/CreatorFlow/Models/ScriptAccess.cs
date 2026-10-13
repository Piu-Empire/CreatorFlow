namespace CreatorFlow.Models;

/// <summary>Kết quả kiểm tra quyền SỬA script của một User trên một Content (quyền XEM = là thành viên Project).</summary>
/// <param name="CanEdit">true nếu được sửa.</param>
/// <param name="ReadOnlyReason">Lý do chỉ đọc (null khi được sửa).</param>
/// <param name="IsContentLocked">true nếu chỉ đọc vì trạng thái Content (Published/Archived), false nếu vì thiếu quyền.</param>
public sealed record ScriptAccess(bool CanEdit, string? ReadOnlyReason, bool IsContentLocked)
{
    public static readonly ScriptAccess Editable = new(true, null, false);

    public static ScriptAccess Locked(string reason) => new(false, reason, true);

    public static ScriptAccess Denied(string reason) => new(false, reason, false);
}
