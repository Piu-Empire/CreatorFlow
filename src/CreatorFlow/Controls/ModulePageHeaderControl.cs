using System.Drawing.Drawing2D;
using CreatorFlow.Theme;

namespace CreatorFlow.Controls;

/// <summary>
/// Đầu trang "Production Board":
/// Hàng 1: Tiêu đề + pill theo Sprint hiện tại | ô Search | bộ lọc nền tảng | "All Sprints" | + Add Ticket
/// Hàng 2: Mô tả trang.
/// </summary>
public class ModulePageHeaderControl : UserControl
{
    public const string AllSprintsFilter = "All Sprints";

    private const int RightPad = 32;
    private const int ControlH = 38;
    private const int Gap = 12;
    private const int SprintW = 132;
    private const int MinSearchW = 100;
    private const int DescY = ControlH + 8;   // hàng 2: mô tả trang
    private const int DescH = 22;

    private readonly TextBox _txtSearch;
    private readonly SegmentedControl _segmentedFilter;
    private readonly RoundedButton _btnAddTicket;
    private readonly ContextMenuStrip _mnuSprintFilter;
    private string _selectedSprint = AllSprintsFilter;

    private Rectangle _searchRect;
    private Rectangle _sprintRect;
    private bool _sprintVisible = true;      // ẩn khi header quá hẹp (vd. drawer chi tiết đang mở)
    private bool _segOnSecondRow;            // bộ lọc nền tảng xuống hàng 2 khi hàng 1 không đủ chỗ

    public event EventHandler? CreateContentClicked;
    public event EventHandler<string>? SearchTextChanged;
    public event EventHandler<string>? PlatformFilterChanged;
    public event EventHandler<string>? SprintFilterChanged;

    public string SearchText => _txtSearch.Text.Trim();
    public string SelectedPlatform => _segmentedFilter.SelectedValue;
    public string SelectedSprint => _selectedSprint;

    public void SetContentCreationEnabled(bool enabled) => _btnAddTicket.Enabled = enabled;

    public void ResetFilters()
    {
        _txtSearch.Clear();
        _segmentedFilter.SelectedIndex = 0;
        SetSprintFilter(AllSprintsFilter);
    }

    public ModulePageHeaderControl()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = UITheme.Neutral50;
        Dock = DockStyle.Top;
        Height = 88;

        _txtSearch = new TextBox
        {
            PlaceholderText = "Search board...",
            Font = UITheme.FontBody,
            BorderStyle = BorderStyle.None,
            BackColor = UITheme.White,
            ForeColor = UITheme.Ink,
        };
        _txtSearch.TextChanged += (_, _) => SearchTextChanged?.Invoke(this, _txtSearch.Text);

        _segmentedFilter = new SegmentedControl { Height = 34 };
        _segmentedFilter.SelectedIndexChanged += (_, _) => PlatformFilterChanged?.Invoke(this, _segmentedFilter.SelectedValue);

        _mnuSprintFilter = new ContextMenuStrip
        {
            ShowImageMargin = false,
            ShowCheckMargin = true,
            Font = UITheme.FontBody,
        };
        AddSprintFilterOption(AllSprintsFilter);
        foreach (string sprint in CreatorFlow.Contracts.ContentDetail.SprintCatalog.Available)
            AddSprintFilterOption(sprint);

        _btnAddTicket = new ProjectActionButton
        {
            Text = "Add Ticket",
            Style = RoundButtonStyle.Primary,
            ShowPlusIcon = true,
            Width = 130,
            Height = ControlH,
        };
        _btnAddTicket.Click += (_, _) => CreateContentClicked?.Invoke(this, EventArgs.Empty);

        Controls.AddRange(new Control[] { _txtSearch, _segmentedFilter, _btnAddTicket });
        Resize += (_, _) => LayoutControls();
        LayoutControls();
    }

    private int TitleBlockWidth
    {
        get
        {
            int titleW = TextRenderer.MeasureText("Production Board", UITheme.FontPageTitle, new Size(int.MaxValue, 40), TextFormatFlags.NoPadding).Width;
            return titleW + 12 + 132;
        }
    }

    private void LayoutControls()
    {
        if (Width <= 0) return;

        int y = 0;
        int right = Width - RightPad;

        _btnAddTicket.Location = new Point(right - _btnAddTicket.Width, y);
        right -= _btnAddTicket.Width + Gap;

        int segW = _segmentedFilter.Width;
        int segY1 = y + (ControlH - _segmentedFilter.Height) / 2;
        int leftLimit = TitleBlockWidth + 24;
        int space = right - leftLimit;   // chỗ trống ở hàng 1, giữa tiêu đề và nút Add Ticket

        _sprintVisible = false;
        _segOnSecondRow = false;
        _searchRect = Rectangle.Empty;
        _txtSearch.Visible = false;

        // Thứ tự ưu tiên khi hẹp: Add Ticket > bộ lọc nền tảng > All Sprints > Search
        if (space >= segW + Gap + SprintW)
        {
            _sprintVisible = true;
            _sprintRect = new Rectangle(right - SprintW, y, SprintW, ControlH);
            right -= SprintW + Gap;
            space -= SprintW + Gap;

            _segmentedFilter.Location = new Point(right - segW, segY1);
            right -= segW + Gap;
            space -= segW + Gap;
        }
        else if (space >= segW)
        {
            _segmentedFilter.Location = new Point(right - segW, segY1);
            right -= segW + Gap;
            space -= segW + Gap;
        }
        else
        {
            // Hàng 1 không đủ chỗ cho bộ lọc: đưa xuống hàng 2 (canh phải, cùng hàng với mô tả)
            _segOnSecondRow = true;
            _segmentedFilter.Location = new Point(Width - RightPad - segW, DescY + (DescH - _segmentedFilter.Height) / 2);

            if (space >= SprintW)
            {
                _sprintVisible = true;
                _sprintRect = new Rectangle(right - SprintW, y, SprintW, ControlH);
                right -= SprintW + Gap;
                space -= SprintW + Gap;
            }
        }

        int searchW = Math.Min(220, space);
        if (searchW >= MinSearchW)
        {
            _txtSearch.Visible = true;
            _searchRect = new Rectangle(right - searchW, y, searchW, ControlH);
            _txtSearch.Width = searchW - 44;
            _txtSearch.Location = new Point(_searchRect.X + 36, _searchRect.Y + (ControlH - _txtSearch.Height) / 2);
        }
        Invalidate();
    }

    public void UpdateReviewQueueBadge(int count)
    {
        // Không dùng trong thiết kế mới
    }

    private void AddSprintFilterOption(string sprint)
    {
        var item = new ToolStripMenuItem(sprint)
        {
            Checked = string.Equals(sprint, _selectedSprint, StringComparison.OrdinalIgnoreCase),
        };
        item.Click += (_, _) => SetSprintFilter(sprint);
        _mnuSprintFilter.Items.Add(item);
    }

    private void SetSprintFilter(string sprint)
    {
        if (string.Equals(_selectedSprint, sprint, StringComparison.OrdinalIgnoreCase)) return;

        _selectedSprint = sprint;
        foreach (ToolStripMenuItem item in _mnuSprintFilter.Items.OfType<ToolStripMenuItem>())
            item.Checked = string.Equals(item.Text, sprint, StringComparison.OrdinalIgnoreCase);

        Invalidate();
        SprintFilterChanged?.Invoke(this, _selectedSprint);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left && _sprintVisible && _sprintRect.Contains(e.Location))
            _mnuSprintFilter.Show(this, new Point(_sprintRect.Left, _sprintRect.Bottom));
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        Cursor = _sprintVisible && _sprintRect.Contains(e.Location) ? Cursors.Hand : Cursors.Default;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.Clear(BackColor);

        // Tiêu đề
        var titleSize = TextRenderer.MeasureText(g, "Production Board", UITheme.FontPageTitle, new Size(int.MaxValue, 40), TextFormatFlags.NoPadding);
        TextRenderer.DrawText(g, "Production Board", UITheme.FontPageTitle, new Rectangle(0, 0, titleSize.Width + 4, ControlH), UITheme.Ink,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

        // Pill cho Sprint mặc định hiện tại
        var pill = new Rectangle(titleSize.Width + 14, (ControlH - 26) / 2 + 2, 132, 26);
        using (var pillBrush = new SolidBrush(UITheme.Neutral200))
        using (var pillPath = UITheme.CreateRoundedRectanglePath(pill, 13))
            g.FillPath(pillBrush, pillPath);
        TextRenderer.DrawText(g, $"{CreatorFlow.Contracts.ContentDetail.SprintCatalog.DefaultSprint} Pipeline", UITheme.FontLabelBold, pill, UITheme.Neutral700,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

        // Mô tả
        int descW = _segOnSecondRow ? _segmentedFilter.Left - 16 : Width - RightPad;
        TextRenderer.DrawText(g, "Real-time content lifecycle tracker from raw hook to final scheduled release.",
            UITheme.FontBody, new Rectangle(0, DescY, Math.Max(100, descW), DescH), UITheme.Neutral600,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);

        // Ô Search (nền trắng, viền xám, icon kính lúp)
        if (!_searchRect.IsEmpty)
        {
            using (var fill = new SolidBrush(UITheme.White))
            using (var pen = new Pen(UITheme.Neutral200, 1f))
            using (var path = UITheme.CreateRoundedRectanglePath(new Rectangle(_searchRect.X, _searchRect.Y, _searchRect.Width - 1, _searchRect.Height - 1), 8))
            {
                g.FillPath(fill, path);
                g.DrawPath(pen, path);
            }
            UIIcons.Search(g, new Rectangle(_searchRect.X + 12, _searchRect.Y + (ControlH - 18) / 2, 18, 18), UITheme.Neutral400);
        }

        // Sprint filter dropdown
        if (_sprintVisible)
        {
            using (var fill = new SolidBrush(UITheme.White))
            using (var pen = new Pen(UITheme.Neutral200, 1f))
            using (var path = UITheme.CreateRoundedRectanglePath(new Rectangle(_sprintRect.X, _sprintRect.Y, _sprintRect.Width - 1, _sprintRect.Height - 1), 8))
            {
                g.FillPath(fill, path);
                g.DrawPath(pen, path);
            }
            TextRenderer.DrawText(g, _selectedSprint, UITheme.FontBody,
                new Rectangle(_sprintRect.X + 14, _sprintRect.Y, _sprintRect.Width - 44, _sprintRect.Height), UITheme.Neutral800,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            UIIcons.Chevron(g, new Rectangle(_sprintRect.Right - 30, _sprintRect.Y + 7, 24, 24), ChevronDir.Down, UITheme.Neutral800, 1.8f);
        }
    }
}
