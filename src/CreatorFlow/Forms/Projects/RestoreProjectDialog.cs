using CreatorFlow.Theme;

namespace CreatorFlow.Forms.Projects;

public sealed class RestoreProjectDialog : ProjectModalForm
{
    public RestoreProjectDialog(string projectName)
        : base("Khôi phục Project", "Đưa Project đã lưu trữ trở lại hoạt động.", "Khôi phục", 112)
    {
        Body.Controls.Add(new Label
        {
            Text = $"Khôi phục Project “{projectName}”?\n\nCác thành viên có thể tiếp tục làm việc trong Project sau khi khôi phục.",
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            ForeColor = UITheme.Ink
        });
        Confirm.Click += (_, _) => DialogResult = DialogResult.OK;
    }
}
