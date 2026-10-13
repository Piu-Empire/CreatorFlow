using CreatorFlow.Controls;
using CreatorFlow.Theme;

namespace CreatorFlow.Forms.Projects;

public class ProjectModalForm : Form
{
    protected Panel Body { get; } = new() { Dock = DockStyle.Fill, Padding = new(24), AutoScroll = true };
    protected ProjectActionButton Confirm { get; } = new() { Name = "projectConfirm", Text = "Xác nhận", Width = 160, Height = 36 };
    private readonly ProjectActionButton cancel = new() { Name = "projectCancel", Text = "Hủy", Style = RoundButtonStyle.Secondary, Width = 92, Height = 36, DialogResult = DialogResult.Cancel };
    private Form? overlay;
    private Form? modalOwner;
    public ProjectModalForm(string title, string description, string action, int bodyHeight = 200, bool closeOnConfirm = false)
    {
        Text = title; Name = GetType().Name; Font = UITheme.FontBody; BackColor = UITheme.White;
        FormBorderStyle = FormBorderStyle.None; MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false; StartPosition = FormStartPosition.CenterParent;
        ClientSize = new(520, bodyHeight + 160); Padding = new(1); Confirm.Text = action;
        var header = new Panel { Dock = DockStyle.Top, Height = 92, Padding = new(24) };
        var heading = new Label { Text = title, Font = UITheme.FontH2, Bounds = new(24, 16, 440, 28), AutoEllipsis = true };
        var explanation = new Label { Text = description, ForeColor = UITheme.Neutral600, Bounds = new(24, 48, 450, 36), AutoEllipsis = true };
        var close = new ProjectActionButton { Name = "projectClose", Text = "×", AccessibleName = "Đóng", Style = RoundButtonStyle.Ghost, Size = new(32, 32), DialogResult = DialogResult.Cancel };
        header.Controls.AddRange([heading, explanation, close]);
        header.Layout += (_, _) =>
        {
            close.Location = new(Math.Max(0, header.ClientSize.Width - 16 - close.Width), 12);
            heading.Width = Math.Max(1, close.Left - heading.Left - 8);
            explanation.Width = Math.Max(1, header.ClientSize.Width - 48);
        };
        header.Paint += (_, e) => e.Graphics.DrawLine(Pens.Gainsboro, 0, header.Height - 1, header.Width, header.Height - 1);
        var footer = new Panel { Dock = DockStyle.Bottom, Height = 68, Padding = new(24, 16, 24, 16) };
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 268, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        cancel.Margin = new(0, 0, 8, 0); Confirm.Margin = Padding.Empty; buttons.Controls.AddRange([cancel, Confirm]); footer.Controls.Add(buttons);
        footer.Paint += (_, e) => e.Graphics.DrawLine(Pens.Gainsboro, 0, 0, footer.Width, 0);
        Controls.Add(Body); Controls.Add(footer); Controls.Add(header); CancelButton = cancel;
        if (closeOnConfirm) Confirm.Click += (_, _) => DialogResult = DialogResult.OK;
        SizeChanged += (_, _) =>
        {
            using var path = UITheme.CreateRoundedRectanglePath(new Rectangle(0, 0, Width, Height), 12);
            var old = Region; Region = new Region(path); old?.Dispose();
        };
        Paint += (_, e) => { using var path = UITheme.CreateRoundedRectanglePath(new Rectangle(0, 0, Width - 1, Height - 1), 12); using var pen = new Pen(UITheme.Neutral200); e.Graphics.DrawPath(pen, path); };
        Shown += (_, _) =>
        {
            using var path = UITheme.CreateRoundedRectanglePath(new Rectangle(0, 0, Width, Height), 12);
            var old = Region; Region = new Region(path); old?.Dispose(); cancel.Focus();
        };
    }
    public DialogResult ShowProjectDialog(Control owner)
    {
        modalOwner = owner.FindForm();
        if (modalOwner is null) return ShowDialog(owner);
        overlay = new Form { FormBorderStyle = FormBorderStyle.None, ShowInTaskbar = false, BackColor = Color.Black, Opacity = .6, Bounds = modalOwner.Bounds, StartPosition = FormStartPosition.Manual };
        modalOwner.LocationChanged += OwnerBoundsChanged; modalOwner.SizeChanged += OwnerBoundsChanged;
        try
        {
            overlay.Show(modalOwner);
            var screen = Screen.FromControl(modalOwner).WorkingArea;
            Size = new(Math.Min(Width, screen.Width - 32), Math.Min(Height, screen.Height - 32));
            return ShowDialog(overlay);
        }
        finally
        {
            modalOwner.LocationChanged -= OwnerBoundsChanged; modalOwner.SizeChanged -= OwnerBoundsChanged;
            overlay.Close(); overlay.Dispose(); overlay = null; modalOwner = null;
        }
    }
    private void OwnerBoundsChanged(object? sender, EventArgs e) { if (overlay is not null && modalOwner is not null) overlay.Bounds = modalOwner.Bounds; }
    protected override bool ProcessDialogKey(Keys keyData)
    {
        if (keyData == Keys.Enter && ActiveControl is Button button) { button.PerformClick(); return true; }
        return base.ProcessDialogKey(keyData);
    }
    protected override CreateParams CreateParams
    {
        get { var parameters = base.CreateParams; parameters.ClassStyle |= 0x00020000; return parameters; }
    }
}
