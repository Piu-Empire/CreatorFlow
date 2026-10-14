using CreatorFlow.Controls;
using CreatorFlow.Theme;

namespace CreatorFlow.Forms.Projects;

public sealed class ProjectSwitchDialog : ProjectModalForm
{
    public ProjectSwitchDialog(string targetName, bool opening, bool discardChanges = false)
        : base(opening ? "Mở Project" : "Chuyển Project", "Xác nhận Project bạn muốn làm việc.", opening ? "Mở Project" : "Chuyển Project", 128)
    {
        Body.Controls.Add(new Label { Dock = DockStyle.Fill, Text = $"Bạn muốn {(opening ? "mở" : "chuyển sang")} Project “{targetName}”?" +
            (discardChanges ? "\n\nThay đổi hồ sơ/nội dung chưa lưu sẽ bị bỏ khi chuyển thành công." : ""), Font = UITheme.FontBody, AutoEllipsis = true });
        Confirm.Click += (_, _) => DialogResult = DialogResult.OK;
    }
}
