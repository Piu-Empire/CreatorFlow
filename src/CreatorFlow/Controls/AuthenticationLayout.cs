using CreatorFlow.Helpers;

namespace CreatorFlow.Controls;

internal sealed class AuthenticationLayout : UserControl
{
    private readonly TableLayoutPanel _columns;
    private readonly Panel _branding;
    private readonly Panel _formArea;
    private readonly TableLayoutPanel _formColumn;

    public TableLayoutPanel Body { get; }

    public AuthenticationLayout(string title, string subtitle)
    {
        Dock = DockStyle.Fill;
        BackColor = Color.White;
        Font = new Font("Segoe UI", 9.75f);
        _columns = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        _columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        _columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        _columns.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _branding = new Panel
        {
            Dock = DockStyle.Fill, BackColor = Color.Black,
            Padding = new Padding(48), Margin = Padding.Empty
        };
        var story = UiTheme.CreateFormBody();
        story.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        UiTheme.AddRow(story, new Label
        {
            Text = "CreatorFlow", AutoSize = true, ForeColor = Color.White,
            Font = new Font("Segoe UI", 22, FontStyle.Bold)
        }, 48);
        UiTheme.AddRow(story, new Label
        {
            Text = "Từ ý tưởng\nđến nội dung hoàn chỉnh.", AutoSize = true,
            MaximumSize = new Size(320, 0), ForeColor = Color.White,
            Font = new Font("Segoe UI", 22, FontStyle.Bold)
        }, 24);
        UiTheme.AddRow(story, new Label
        {
            Text = "Lên kế hoạch, phối hợp cùng nhóm và theo dõi hành trình sáng tạo ở một nơi.",
            AutoSize = true, MaximumSize = new Size(320, 0),
            ForeColor = Color.FromArgb(207, 196, 197)
        }, 32);
        UiTheme.AddRow(story, new Label
        {
            Text = "Ý tưởng  ·  Sản xuất  ·  Kiểm duyệt", AutoSize = true,
            ForeColor = Color.FromArgb(163, 163, 163)
        });
        _branding.Controls.Add(story);

        _formArea = new Panel
        {
            Dock = DockStyle.Fill, BackColor = Color.White,
            Padding = new Padding(48, 32, 48, 32), Margin = Padding.Empty, AutoScroll = true
        };
        _formColumn = UiTheme.CreateFormBody();
        _formColumn.Dock = DockStyle.None;
        _formColumn.Width = 440;
        _formColumn.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        UiTheme.AddRow(_formColumn, new Label
        {
            Text = "CREATORFLOW", AutoSize = true, ForeColor = UiTheme.Muted,
            Font = new Font("Segoe UI", 8.25f, FontStyle.Bold)
        }, 24);
        UiTheme.AddRow(_formColumn, new Label
        {
            Text = title, AutoSize = true, ForeColor = UiTheme.Heading,
            Font = new Font("Segoe UI", 22, FontStyle.Bold)
        }, 8);
        UiTheme.AddRow(_formColumn, new Label
        {
            Text = subtitle, AutoSize = true, MaximumSize = new Size(440, 0), ForeColor = UiTheme.Muted
        }, 32);
        Body = UiTheme.CreateFormBody();
        Body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        UiTheme.AddRow(_formColumn, Body, 0);
        _formArea.Controls.Add(_formColumn);
        _formArea.SizeChanged += (_, _) => PositionForm();
        _formArea.Layout += (_, _) => PositionForm();
        _columns.Controls.Add(_branding, 0, 0);
        _columns.Controls.Add(_formArea, 1, 0);
        Controls.Add(_columns);
    }

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        if (_formArea is null) return;
        float scale = DeviceDpi / 96f;
        bool wide = ClientSize.Width >= 1000 * scale;
        _branding.Visible = wide;
        _columns.ColumnStyles[0].SizeType = wide ? SizeType.Percent : SizeType.Absolute;
        _columns.ColumnStyles[0].Width = wide ? 42 : 0;
        _columns.ColumnStyles[1].Width = wide ? 58 : 100;
        PositionForm();
    }

    private void PositionForm()
    {
        if (_formColumn is null) return;
        float scale = DeviceDpi / 96f;
        int width = Math.Max(1, _formArea.ClientSize.Width - _formArea.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth);
        int targetWidth = Math.Min((int)(440 * scale), width);
        if (_formColumn.MinimumSize.Width != targetWidth)
        {
            _formColumn.MinimumSize = new Size(targetWidth, 0);
            _formColumn.MaximumSize = new Size(targetWidth, 0);
        }
        _formColumn.Left = Math.Max(_formArea.Padding.Left, (_formArea.ClientSize.Width - _formColumn.Width) / 2);
        _formColumn.Top = _formArea.Padding.Top;
    }
}
