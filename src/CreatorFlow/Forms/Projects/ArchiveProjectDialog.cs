using CreatorFlow.Theme;

namespace CreatorFlow.Forms.Projects;

public sealed class ArchiveProjectDialog : ProjectModalForm
{
    public ArchiveProjectDialog(string projectName) : base("Lưu trữ Project", "Project sẽ ngừng hoạt động, dữ liệu vẫn được giữ lại.", "Lưu trữ", 112)
    {
        Body.Controls.Add(new Label { Text = $"Lưu trữ Project “{projectName}”?\n\nBạn sẽ cần chọn Project khác để tiếp tục làm việc.", Dock = DockStyle.Fill, AutoEllipsis = true, ForeColor = UITheme.Ink });
        Confirm.Click += (_, _) => DialogResult = DialogResult.OK;
    }
}
