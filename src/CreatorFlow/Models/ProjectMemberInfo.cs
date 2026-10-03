namespace CreatorFlow.Models;

/// <summary>Thành viên của Project (dùng cho ComboBox chọn người phụ trách).</summary>
public class ProjectMemberInfo
{
    public long UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ProjectRole Role { get; set; }
}
