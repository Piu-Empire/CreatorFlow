namespace CreatorFlow.Models;

/// <summary>
/// Script của một Content như editor nhìn thấy: nội dung đã chuẩn hóa + quyền + số liệu.
/// <see cref="Version"/> là dấu vân tay của bản đang lưu; gửi lại khi Save để phát hiện người khác đã sửa trước.
/// </summary>
public sealed record ScriptDocument
{
    public long ContentId { get; init; }

    public long ProjectId { get; init; }

    /// <summary>Nội dung đã chuẩn hóa, xuống dòng là "\n" (UI tự đổi sang Environment.NewLine khi hiển thị).</summary>
    public string Text { get; init; } = string.Empty;

    public string Version { get; init; } = string.Empty;

    public ScriptAccess Access { get; init; } = ScriptAccess.Denied(string.Empty);

    public ScriptStatistics Statistics { get; init; } = ScriptStatistics.Empty;

    public bool CanEdit => Access.CanEdit;
}
