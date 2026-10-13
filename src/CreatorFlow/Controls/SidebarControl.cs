using System.Drawing.Drawing2D;
using CreatorFlow.Theme;
using CreatorFlow.Helpers;

namespace CreatorFlow.Controls;

/// <summary>
/// Sidebar đen: Logo, chọn Project, nhóm PROJECT / WORK / AI, mục cài đặt + trạng thái hệ thống ở đáy.
/// </summary>
public class SidebarControl : UserControl
{
    private const int ItemH = 42;
    private const int ItemStep = 44;
    private int _navScrollOffset;

    public event EventHandler? ReviewQueueRequested;
    public event EventHandler? BoardRequested;
    public event EventHandler? MyTasksRequested;
    public event EventHandler? IdeaBankRequested;
    public event EventHandler? AdminWorkspaceRequested;
    public event EventHandler? AdminReportsRequested;
    public event EventHandler? CalendarRequested;
    public event EventHandler? ProjectSettingsRequested;
    public event EventHandler? InvitationsRequested;

    public event EventHandler? ProjectSelectionRequested;
    private readonly Button _projectButton;
    public bool ProjectManagementEnabled { get; set; }
    public Point ProjectMenuScreenLocation => _projectButton.PointToScreen(new Point(0, _projectButton.Height + 4));
    public int ProjectMenuAnchorWidth => _projectButton.Width;
    public void ShowProjectMenu(ToolStripDropDown menu) => menu.Show(_projectButton, new Point(0, _projectButton.Height + 4));

    public bool ProjectActionsEnabled { get; set; }
    public int BacklogCount = 0;
    public int MyWorkCount = 0;
    private bool _isSystemAdmin;
    public int ReviewQueueCount = 0;

    /// <summary>Bật/tắt nhóm menu quản trị hệ thống theo quyền admin.</summary>
    public void SetSystemAdmin(bool value)
    {
        if (_isSystemAdmin == value) return;
        _isSystemAdmin = value;
        BuildNavItems();
        Invalidate();
    }

    private enum NavIcon { Summary, Board, List, Calendar, Chart, Check, Review, Sparkle, Gear, Shield, Flag }

    private sealed class NavItem
    {
        public string Title = "";
        public NavIcon Icon;
        public int Y;                       // Toạ độ Y (từ trên, hoặc khoảng cách từ đáy nếu AnchorBottom)
        public bool AnchorBottom;
        public bool AlwaysEnabled;          // true: bấm được cả khi chưa có Project (Board, Invitations)
        public bool IsActive;
        public bool IsHovered;
        public Action? Action;
        public Func<int>? Badge;            // null: không badge; >0: số; <0: nhãn NEW
    }

    private readonly List<NavItem> _items = new();
    private readonly List<(string Text, int Y)> _groups = new();

    public SidebarControl()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Dock = DockStyle.Left;
        Width = 260;
        BackColor = UITheme.Black;
        DoubleBuffered = true;
        BuildNavItems();
        _projectButton = new Button { Text = "Chưa chọn dự án", Bounds = new Rectangle(14, 68, Width - 28, 60),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, FlatStyle = FlatStyle.Flat,
            BackColor = UITheme.Neutral900, ForeColor = UITheme.White, AutoEllipsis = true,
            AccessibleName = "Chọn Project", TabIndex = 0, Font = UITheme.FontBodyBold, Cursor = Cursors.Hand };
        UiTheme.StyleButton(_projectButton);
        _projectButton.BackColor = UITheme.Neutral900;
        _projectButton.FlatAppearance.MouseOverBackColor = UITheme.Neutral800;
        _projectButton.FlatAppearance.MouseDownBackColor = UITheme.Neutral700;
        _projectButton.Paint += (_, e) =>
        {
            e.Graphics.Clear(_projectButton.ClientRectangle.Contains(_projectButton.PointToClient(MousePosition)) ? UITheme.Neutral800 : UITheme.Neutral900);
            using var path = UITheme.CreateRoundedRectanglePath(new Rectangle(0, 0, _projectButton.Width - 1, _projectButton.Height - 1), 8);
            using var border = new Pen(UITheme.Neutral700);
            e.Graphics.DrawPath(border, path);
            UIIcons.Summary(e.Graphics, new Rectangle(12, 20, 20, 20), UITheme.White);
            TextRenderer.DrawText(e.Graphics, _projectButton.Text, _projectButton.Font, new Rectangle(44, 0, _projectButton.Width - 78, 60), UITheme.White,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            UIIcons.Chevron(e.Graphics, new Rectangle(_projectButton.Width - 26, 24, 12, 12), ChevronDir.Down, UITheme.SidebarText);
            if (_projectButton.Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics, new Rectangle(5, 5, _projectButton.Width - 10, _projectButton.Height - 10), UITheme.White, UITheme.Neutral900);
        };
        _projectButton.Click += (_, _) => ProjectSelectionRequested?.Invoke(this, EventArgs.Empty);
        Controls.Add(_projectButton);
    }

    public void SetProjectName(string? name)
    {
        _projectButton.Text = string.IsNullOrWhiteSpace(name) ? "Chưa chọn dự án" : name;
        _projectButton.AccessibleName = "Chọn Project: " + (name ?? "Chưa chọn");
    }

    private void BuildNavItems()
    {
        _navScrollOffset = 0;
        _items.Clear();
        _groups.Clear();

        int y = 172;
        _groups.Add(("PROJECT", y)); y += 26;
        _items.Add(new NavItem { Title = "Summary", Icon = NavIcon.Summary, Y = y }); y += ItemStep;
        _items.Add(new NavItem { Title = "Board", Icon = NavIcon.Board, Y = y, IsActive = true, AlwaysEnabled = true, Action = () => BoardRequested?.Invoke(this, EventArgs.Empty) }); y += ItemStep;
        _items.Add(new NavItem { Title = "Backlog", Icon = NavIcon.List, Y = y, Badge = () => BacklogCount, Action = () => IdeaBankRequested?.Invoke(this, EventArgs.Empty) }); y += ItemStep;
        _items.Add(new NavItem { Title = "Calendar", Icon = NavIcon.Calendar, Y = y, AlwaysEnabled = true, Action = () => CalendarRequested?.Invoke(this, EventArgs.Empty) }); y += ItemStep;
        _items.Add(new NavItem { Title = "Reports", Icon = NavIcon.Chart, Y = y }); y += ItemStep;

        y += 10;
        _groups.Add(("WORK", y)); y += 26;
        _items.Add(new NavItem { Title = "My Tasks", Icon = NavIcon.Check, Y = y, Badge = () => MyWorkCount, Action = () => MyTasksRequested?.Invoke(this, EventArgs.Empty) }); y += ItemStep;
        _items.Add(new NavItem { Title = "Review Queue", Icon = NavIcon.Review, Y = y, Badge = () => ReviewQueueCount, Action = () => ReviewQueueRequested?.Invoke(this, EventArgs.Empty) }); y += ItemStep;

        y += 10;
        _groups.Add(("AI", y)); y += 26;
        _items.Add(new NavItem { Title = "AI Assistant", Icon = NavIcon.Sparkle, Y = y, Badge = () => -1 });

        if (_isSystemAdmin)
        {
            y += ItemStep + 10;
            _groups.Add(("ADMIN", y)); y += 26;
            _items.Add(new NavItem { Title = "Quản trị người dùng", Icon = NavIcon.Shield, Y = y, AlwaysEnabled = true,
                Action = () => AdminWorkspaceRequested?.Invoke(this, EventArgs.Empty) }); y += ItemStep;
            _items.Add(new NavItem { Title = "Báo cáo vi phạm", Icon = NavIcon.Flag, Y = y, AlwaysEnabled = true,
                Action = () => AdminReportsRequested?.Invoke(this, EventArgs.Empty) });
        }

        // Neo đáy: cách đáy 100px (phía trên dòng trạng thái); Invitations nằm ngay phía trên Project Settings.
        _items.Add(new NavItem { Title = "Invitations", Icon = NavIcon.List, Y = 144, AnchorBottom = true, AlwaysEnabled = true, Action = () => InvitationsRequested?.Invoke(this, EventArgs.Empty) });
        _items.Add(new NavItem { Title = "Project Settings", Icon = NavIcon.Gear, Y = 100, AnchorBottom = true, Action = () => ProjectSettingsRequested?.Invoke(this, EventArgs.Empty) });
    }

    private Rectangle GetItemRect(NavItem item)
    {
        int y = item.AnchorBottom ? Height - item.Y : item.Y;
        return new Rectangle(12, item.AnchorBottom ? y : y - _navScrollOffset, Width - 24, ItemH);
    }

    public void ScrollNavigation(int delta)
    {
        int bottom = _items.Where(item => !item.AnchorBottom).Select(item => item.Y + ItemH).DefaultIfEmpty(0).Max();
        int max = Math.Max(0, bottom - Math.Max(172, Height - 152));
        _navScrollOffset = Math.Clamp(_navScrollOffset - delta / 120 * ItemStep, 0, max);
        Invalidate();
    }

    private bool IsNavigationVisible(NavItem item) => item.AnchorBottom ||
        GetItemRect(item).Top >= 172 && GetItemRect(item).Bottom <= Height - 152;

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        bool needsRepaint = false;
        bool anyHover = false;
        foreach (var item in _items)
        {
            bool hovered = IsNavigationVisible(item) && IsItemEnabled(item) && GetItemRect(item).Contains(e.Location);
            anyHover |= hovered && item.Action != null;
            if (item.IsHovered != hovered) { item.IsHovered = hovered; needsRepaint = true; }
        }
        Cursor = anyHover ? Cursors.Hand : Cursors.Default;
        if (needsRepaint) Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        foreach (var item in _items) item.IsHovered = false;
        Cursor = Cursors.Default;
        Invalidate();
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        foreach (var item in _items)
        {
            if (IsNavigationVisible(item) && IsItemEnabled(item) && GetItemRect(item).Contains(e.Location))
            { item.Action?.Invoke(); break; }
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.Clear(UITheme.Black);

        const TextFormatFlags tf = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;

        // 1. Logo
        UIIcons.LogoMark(g, new Rectangle(20, 20, 28, 28), UITheme.White, UITheme.Black);
        using (var brandFont = new Font("Segoe UI", 13F, FontStyle.Bold))
            TextRenderer.DrawText(g, "CreatorFlow", brandFont, new Rectangle(58, 18, Width - 70, 32), UITheme.White, tf);

        // 3. Nhãn nhóm
        foreach (var (text, y) in _groups)
            if (y - _navScrollOffset >= 172 && y - _navScrollOffset + 20 <= Height - 152)
                TextRenderer.DrawText(g, text, UITheme.FontMicro, new Rectangle(22, y - _navScrollOffset, Width - 44, 20), UITheme.SidebarTextMuted, tf);

        // 4. Đường kẻ trên vùng đáy
        int dividerY = Height - 100 - 8;
        using (var divPen = new Pen(Color.FromArgb(40, 255, 255, 255), 1f))
            g.DrawLine(divPen, 0, dividerY, Width, dividerY);

        // 5. Nav items
        foreach (var item in _items)
        {
            if (!IsNavigationVisible(item)) continue;
            var rect = GetItemRect(item);

            if (item.IsActive || item.IsHovered)
            {
                using var bg = new SolidBrush(item.IsActive ? UITheme.SidebarItemActive : UITheme.SidebarItemHover);
                using var path = UITheme.CreateRoundedRectanglePath(rect, 8);
                g.FillPath(bg, path);
            }
            if (item.IsActive)
            {
                using var bar = new SolidBrush(UITheme.White);
                g.FillRectangle(bar, rect.X, rect.Y + 8, 3, rect.Height - 16);
            }

            Color fg = !IsItemEnabled(item) ? UITheme.SidebarTextFaint :
                (item.IsActive || item.IsHovered) ? UITheme.White : UITheme.SidebarText;
            var iconRect = new Rectangle(rect.X + 14, rect.Y + (ItemH - 22) / 2, 22, 22);
            DrawNavIcon(g, item.Icon, iconRect, fg);

            var font = item.IsActive ? UITheme.FontBodyBold : UITheme.FontNav;
            TextRenderer.DrawText(g, item.Title, font, new Rectangle(rect.X + 48, rect.Y, rect.Width - 100, rect.Height), fg, tf);

            int badge = ProjectActionsEnabled ? item.Badge?.Invoke() ?? 0 : 0;
            if (badge > 0)
            {
                string t = badge.ToString();
                int bw = Math.Max(28, TextRenderer.MeasureText(t, UITheme.FontMicro).Width + 14);
                var br = new Rectangle(rect.Right - 12 - bw, rect.Y + (ItemH - 22) / 2, bw, 22);
                using var bb = new SolidBrush(UITheme.SidebarBadge);
                using var bp = UITheme.CreateRoundedRectanglePath(br, 7);
                g.FillPath(bb, bp);
                TextRenderer.DrawText(g, t, UITheme.FontMicro, br, UITheme.SidebarText,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
            else if (badge < 0)
            {
                var br = new Rectangle(rect.Right - 12 - 40, rect.Y + (ItemH - 22) / 2, 40, 22);
                using var bb = new SolidBrush(UITheme.Neutral200);
                using var bp = UITheme.CreateRoundedRectanglePath(br, 7);
                g.FillPath(bb, bp);
                TextRenderer.DrawText(g, "NEW", UITheme.FontMicro, br, UITheme.Black,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
        }

        // 6. Trạng thái hệ thống
        int statusY = Height - 44;
        using (var dotBrush = new SolidBrush(UITheme.Success))
            g.FillEllipse(dotBrush, 24, statusY + 18, 9, 9);
        TextRenderer.DrawText(g, "Operational", UITheme.FontLabelBold, new Rectangle(42, statusY + 12, 120, 22), UITheme.SidebarText, tf);
        TextRenderer.DrawText(g, "v2.4.0", UITheme.FontLabel, new Rectangle(Width - 20 - 60, statusY + 12, 60, 22), UITheme.SidebarTextFaint,
            TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }

    private static void DrawNavIcon(Graphics g, NavIcon icon, Rectangle r, Color c)
    {
        switch (icon)
        {
            case NavIcon.Summary: UIIcons.Summary(g, r, c); break;
            case NavIcon.Board: UIIcons.Board(g, r, c); break;
            case NavIcon.List: UIIcons.List(g, r, c); break;
            case NavIcon.Calendar: UIIcons.Calendar(g, r, c); break;
            case NavIcon.Chart: UIIcons.Chart(g, r, c); break;
            case NavIcon.Check: UIIcons.CheckCircle(g, r, c); break;
            case NavIcon.Review: UIIcons.ReviewQueue(g, r, c); break;
            case NavIcon.Sparkle: UIIcons.Sparkle(g, r, c); break;
            case NavIcon.Gear: UIIcons.Gear(g, r, c); break;
            case NavIcon.Shield: UIIcons.Shield(g, r, c); break;
            case NavIcon.Flag: UIIcons.Flag(g, r, c); break;
        }
    }

    /// <summary>Mục AlwaysEnabled thao tác được cả khi chưa có project, các mục khác cần project.</summary>
    private bool IsItemEnabled(NavItem item) => ProjectActionsEnabled || item.AlwaysEnabled || ProjectManagementEnabled && item.Icon == NavIcon.Gear;

    /// <summary>Đánh dấu mục đang mở (BoardForm gọi khi chuyển màn hình).</summary>
    public void SetActiveMenu(string title)
    {
        foreach (NavItem item in _items) item.IsActive = item.Title == title;
        Invalidate();
    }
}
