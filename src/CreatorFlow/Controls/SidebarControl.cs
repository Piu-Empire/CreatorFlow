using System.Drawing.Drawing2D;
using CreatorFlow.Theme;

namespace CreatorFlow.Controls;

/// <summary>
/// Sidebar đen: Logo, chọn Project, nhóm PROJECT / WORK / AI, mục cài đặt + trạng thái hệ thống ở đáy.
/// </summary>
public class SidebarControl : UserControl
{
    private const int ItemH = 42;
    private const int ItemStep = 44;

    public event EventHandler? ReviewQueueRequested;
    public event EventHandler? BoardRequested;
    public event EventHandler? MyTasksRequested;
    public event EventHandler? ProfileRequested;

    private readonly Button _accountButton;
    private string _accountName = string.Empty;
    private string _accountEmail = string.Empty;
    private Image? _accountAvatar;
    private bool _accountHovered;
    private bool _hasAuthenticatedAccount;

    public int BacklogCount = 28;
    public int MyWorkCount = 0;
    public int ReviewQueueCount = 2;

    private enum NavIcon { Summary, Board, List, Calendar, Chart, Check, Review, Sparkle, Gear }

    private sealed class NavItem
    {
        public string Title = "";
        public NavIcon Icon;
        public int Y;                       // Toạ độ Y (từ trên, hoặc khoảng cách từ đáy nếu AnchorBottom)
        public bool AnchorBottom;
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
        _accountButton = new Button { Name = "authenticatedAccount", Text = string.Empty,
            Bounds = new Rectangle(14, 68, Width - 28, 60), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat, BackColor = UITheme.Black, UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand, Visible = false, TabIndex = 0, AccessibleRole = AccessibleRole.PushButton };
        _accountButton.FlatAppearance.BorderSize = 0;
        _accountButton.Paint += PaintAccount;
        _accountButton.MouseEnter += (_, _) => { _accountHovered = true; _accountButton.Invalidate(); };
        _accountButton.MouseLeave += (_, _) => { _accountHovered = false; _accountButton.Invalidate(); };
        _accountButton.GotFocus += (_, _) => _accountButton.Invalidate();
        _accountButton.LostFocus += (_, _) => _accountButton.Invalidate();
        _accountButton.Click += (_, _) => ProfileRequested?.Invoke(this, EventArgs.Empty);
        Controls.Add(_accountButton);
    }

    // The shell transfers ownership of its decoded image to this control.
    public void SetAuthenticatedAccount(string name, string email, Image? avatar)
    {
        _accountName = name;
        _accountEmail = email;
        _hasAuthenticatedAccount = true;
        Image? previous = _accountAvatar;
        _accountAvatar = avatar;
        if (!ReferenceEquals(previous, avatar)) previous?.Dispose();
        _accountButton.AccessibleName = $"Mở hồ sơ cá nhân của {name}";
        _accountButton.Visible = true;
        _accountButton.Invalidate();
        Invalidate();
    }

    private void PaintAccount(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(UITheme.Black);
        var bounds = new Rectangle(0, 0, _accountButton.Width - 1, _accountButton.Height - 1);
        using var path = UITheme.CreateRoundedRectanglePath(bounds, 10);
        using var fill = new SolidBrush(_accountHovered ? UITheme.Neutral800 : UITheme.Neutral900);
        using var border = new Pen(UITheme.Neutral800);
        g.FillPath(fill, path);
        g.DrawPath(border, path);
        var avatarBounds = new Rectangle(12, 12, 36, 36);
        if (_accountAvatar is null)
        {
            string[] words = _accountName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string initials = words.Length == 0 ? "?" :
                (System.Globalization.StringInfo.GetNextTextElement(words[0]) +
                 (words.Length > 1 ? System.Globalization.StringInfo.GetNextTextElement(words[^1]) : string.Empty)).ToUpperInvariant();
            UITheme.DrawAvatar(g, initials, avatarBounds, UITheme.White, UITheme.Black, UITheme.FontLabelBold);
        }
        else
        {
            var state = g.Save();
            using var circle = new GraphicsPath();
            circle.AddEllipse(avatarBounds);
            g.SetClip(circle);
            g.DrawImage(_accountAvatar, avatarBounds);
            g.Restore(state);
        }
        const TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
            TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
        int textWidth = Math.Max(0, bounds.Width - 72);
        TextRenderer.DrawText(g, _accountName, UITheme.FontBodyBold, new Rectangle(60, 10, textWidth, 20), UITheme.White, flags);
        TextRenderer.DrawText(g, _accountEmail, UITheme.FontLabel, new Rectangle(60, 30, textWidth, 18), UITheme.SidebarTextMuted, flags);
        if (_accountButton.Focused) ControlPaint.DrawFocusRectangle(g, Rectangle.Inflate(bounds, -4, -4), UITheme.White, UITheme.Neutral900);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _accountAvatar?.Dispose();
            _accountAvatar = null;
            _accountName = _accountEmail = string.Empty;
            _hasAuthenticatedAccount = false;
        }
        base.Dispose(disposing);
    }

    private void BuildNavItems()
    {
        _items.Clear();
        _groups.Clear();

        int y = 140;
        _groups.Add(("PROJECT", y)); y += 26;
        _items.Add(new NavItem { Title = "Summary", Icon = NavIcon.Summary, Y = y }); y += ItemStep;
        _items.Add(new NavItem { Title = "Board", Icon = NavIcon.Board, Y = y, IsActive = true, Action = () => BoardRequested?.Invoke(this, EventArgs.Empty) }); y += ItemStep;
        _items.Add(new NavItem { Title = "Backlog", Icon = NavIcon.List, Y = y, Badge = () => BacklogCount }); y += ItemStep;
        _items.Add(new NavItem { Title = "Calendar", Icon = NavIcon.Calendar, Y = y }); y += ItemStep;
        _items.Add(new NavItem { Title = "Reports", Icon = NavIcon.Chart, Y = y }); y += ItemStep;

        y += 10;
        _groups.Add(("WORK", y)); y += 26;
        _items.Add(new NavItem { Title = "My Tasks", Icon = NavIcon.Check, Y = y, Badge = () => MyWorkCount, Action = () => MyTasksRequested?.Invoke(this, EventArgs.Empty) }); y += ItemStep;
        _items.Add(new NavItem { Title = "Review Queue", Icon = NavIcon.Review, Y = y, Badge = () => ReviewQueueCount, Action = () => ReviewQueueRequested?.Invoke(this, EventArgs.Empty) }); y += ItemStep;

        y += 10;
        _groups.Add(("AI", y)); y += 26;
        _items.Add(new NavItem { Title = "AI Assistant", Icon = NavIcon.Sparkle, Y = y, Badge = () => -1 });

        // Neo đáy: cách đáy 100px (phía trên dòng trạng thái)
        _items.Add(new NavItem { Title = "Project Settings", Icon = NavIcon.Gear, Y = 100, AnchorBottom = true });
    }

    private Rectangle GetItemRect(NavItem item)
    {
        int y = item.AnchorBottom ? Height - item.Y : item.Y;
        return new Rectangle(12, y, Width - 24, ItemH);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        bool needsRepaint = false;
        bool anyHover = false;
        foreach (var item in _items)
        {
            bool hovered = GetItemRect(item).Contains(e.Location);
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
            if (GetItemRect(item).Contains(e.Location)) { item.Action?.Invoke(); break; }
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

        // 2. Project selector
        if (!_hasAuthenticatedAccount)
        {
        var card = new Rectangle(14, 68, Width - 28, 60);
        using (var cBrush = new SolidBrush(ColorTranslator.FromHtml("#171717")))
        using (var cPen = new Pen(ColorTranslator.FromHtml("#262626"), 1f))
        using (var cPath = UITheme.CreateRoundedRectanglePath(new Rectangle(card.X, card.Y, card.Width - 1, card.Height - 1), 10))
        {
            g.FillPath(cBrush, cPath);
            g.DrawPath(cPen, cPath);
        }

        var av = new Rectangle(card.X + 12, card.Y + 12, 36, 36);
        using (var avBrush = new SolidBrush(UITheme.White))
        using (var avPath = UITheme.CreateRoundedRectanglePath(av, 8))
            g.FillPath(avBrush, avPath);
        TextRenderer.DrawText(g, "SA", UITheme.FontLabelBold, av, UITheme.Black,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

        TextRenderer.DrawText(g, "Studio Alpha", UITheme.FontBodyBold, new Rectangle(av.Right + 10, card.Y + 11, card.Width - 90, 20), UITheme.White, tf);
        TextRenderer.DrawText(g, "Q3 Release", UITheme.FontLabel, new Rectangle(av.Right + 10, card.Y + 31, card.Width - 90, 18), UITheme.SidebarTextMuted, tf);
        UIIcons.Chevron(g, new Rectangle(card.Right - 34, card.Y + 12, 22, 18), ChevronDir.Up, UITheme.SidebarText, 1.6f);
        UIIcons.Chevron(g, new Rectangle(card.Right - 34, card.Y + 28, 22, 18), ChevronDir.Down, UITheme.SidebarText, 1.6f);
        }

        // 3. Nhãn nhóm
        foreach (var (text, y) in _groups)
            TextRenderer.DrawText(g, text, UITheme.FontMicro, new Rectangle(22, y, Width - 44, 20), UITheme.SidebarTextMuted, tf);

        // 4. Đường kẻ trên vùng đáy
        int dividerY = Height - 100 - 8;
        using (var divPen = new Pen(Color.FromArgb(40, 255, 255, 255), 1f))
            g.DrawLine(divPen, 0, dividerY, Width, dividerY);

        // 5. Nav items
        foreach (var item in _items)
        {
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

            Color fg = (item.IsActive || item.IsHovered) ? UITheme.White : UITheme.SidebarText;
            var iconRect = new Rectangle(rect.X + 14, rect.Y + (ItemH - 22) / 2, 22, 22);
            DrawNavIcon(g, item.Icon, iconRect, fg);

            var font = item.IsActive ? UITheme.FontBodyBold : UITheme.FontNav;
            TextRenderer.DrawText(g, item.Title, font, new Rectangle(rect.X + 48, rect.Y, rect.Width - 100, rect.Height), fg, tf);

            int badge = item.Badge?.Invoke() ?? 0;
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
        }
    }
}