using CreatorFlow.Theme;
using CreatorFlow.Helpers;
using CreatorFlow.Controls;

namespace CreatorFlow.Forms.Projects;

public sealed class DeleteProjectDialog : ProjectModalForm
{
    public DeleteProjectDialog(string projectName)
        : base("Xóa Project vĩnh viễn", "Hành động này không thể hoàn tác.", "Xóa vĩnh viễn", 216)
    {
        Confirm.Style = RoundButtonStyle.Danger; Confirm.Enabled = false;
        var layout = UiTheme.CreateFormBody(); layout.ColumnStyles.Add(new(SizeType.Percent, 100));
        UiTheme.AddRow(layout, new Label { Text = "Project và dữ liệu liên quan sẽ bị xóa vĩnh viễn: nội dung, phân công, review, lịch sử, metrics và liên kết.", AutoSize = true, MaximumSize = new(472, 0), ForeColor = UITheme.Danger });
        UiTheme.AddRow(layout, new Label { Text = projectName, AutoSize = true, MaximumSize = new(472, 0), Font = UITheme.FontBodyBold });
        var input = new TextBox { MaxLength = 0, AccessibleName = "Nhập chính xác tên Project" };
        UiTheme.AddField(layout, "Nhập chính xác tên Project để xác nhận", input); Body.Controls.Add(layout);
        input.TextChanged += (_, _) => Confirm.Enabled = string.Equals(input.Text, projectName, StringComparison.Ordinal);
        Confirm.Click += (_, _) => { if (Confirm.Enabled) DialogResult = DialogResult.OK; };
    }
}
