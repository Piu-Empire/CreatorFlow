using System.Drawing.Drawing2D;
using CreatorFlow.Theme;

namespace CreatorFlow.Controls;

/// <summary>
/// Thanh trên cùng: breadcrumb và toolbar chung; avatar mở Account ở góc phải.
/// </summary>
public class TopHeaderControl : UserControl
{
    private const int PadX = 32;
    private readonly Button _accountButton;
    private string _accountName = string.Empty;
    private Image? _accountAvatar;
    private bool _accountHovered;
    public event EventHandler? ProfileRequested;

    private string _statsText = "";
    private Rectangle _newContentRect;
    private bool _newContentHover;
    private string _authenticatedPage = "Board";
    private string _projectName = "Chưa chọn dự án";
    private bool _projectActionsEnabled;

    public void SetProjectContext(string? projectName, bool actionsEnabled)
    {
        _projectName = string.IsNullOrWhiteSpace(projectName) ? "Chưa chọn dự án" : projectName;
        _projectActionsEnabled = actionsEnabled;
        _newContentHover = false;
        Cursor = Cursors.Default;
        Invalidate();
    }

    public void SetAuthenticatedPage(string page)
    {
        _authenticatedPage = page;
        Invalidate();
    }

    public void ClearStats() { _statsText = string.Empty; Invalidate(); }

    public event EventHandler? NewContentClicked;

    public TopHeaderControl()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Dock = DockStyle.Top;
        Height = 68;
        BackColor = UITheme.White;
        _accountButton = new Button { Name = "authenticatedAccount", Text = string.Empty,
            Bounds = new Rectangle(Math.Max(0, Width - PadX - 40), 14, 40, 40),
            Anchor = AnchorStyles.Top | AnchorStyles.Right, FlatStyle = FlatStyle.Flat,
            BackColor = UITheme.White, UseVisualStyleBackColor = false, Cursor = Cursors.Hand,
            Visible = false, TabIndex = 0, AccessibleRole = AccessibleRole.PushButton };
        _accountButton.FlatAppearance.BorderSize = 0;
        _accountButton.Paint += PaintAccount;
        _accountButton.MouseEnter += (_, _) => { _accountHovered = true; _accountButton.Invalidate(); };
        _accountButton.MouseLeave += (_, _) => { _accountHovered = false; _accountButton.Invalidate(); };
        _accountButton.GotFocus += (_, _) => _accountButton.Invalidate();
        _accountButton.LostFocus += (_, _) => _accountButton.Invalidate();
        _accountButton.Click += (_, _) => ProfileRequested?.Invoke(this, EventArgs.Empty);
        Controls.Add(_accountButton);
    }

    // The shell transfers ownership of its decoded avatar to this control.
    public void SetAuthenticatedAccount(string name, Image? avatar)
    {
        _accountName = name;
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
        g.Clear(_accountHovered ? UITheme.Neutral50 : UITheme.White);
        var bounds = new Rectangle(4, 4, 32, 32);
        if (_accountAvatar is null)
        {
            string[] words = _accountName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string initials = words.Length == 0 ? "?" :
                (System.Globalization.StringInfo.GetNextTextElement(words[0]) +
                 (words.Length > 1 ? System.Globalization.StringInfo.GetNextTextElement(words[^1]) : string.Empty)).ToUpperInvariant();
            UITheme.DrawAvatar(g, initials, bounds, UITheme.Black, UITheme.White, UITheme.FontLabelBold);
        }
        else
        {
            var state = g.Save();
            using var circle = new GraphicsPath();
            circle.AddEllipse(bounds);
            g.SetClip(circle);
            g.DrawImage(_accountAvatar, bounds);
            g.Restore(state);
        }
        using var border = new Pen(UITheme.Neutral200);
        g.DrawEllipse(border, bounds);
        if (_accountButton.Focused)
            ControlPaint.DrawFocusRectangle(g, new Rectangle(1, 1, 37, 37), UITheme.Black, UITheme.White);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _accountAvatar?.Dispose();
            _accountAvatar = null;
        }
        base.Dispose(disposing);
    }

    public void UpdateStats(int totalCards, int overdueCount)
    {
        _statsText = overdueCount > 0
            ? $"{totalCards} items · {overdueCount} overdue"
            : $"{totalCards} items in production";
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        bool hover = _projectActionsEnabled && _newContentRect.Contains(e.Location);
        if (hover != _newContentHover)
        {
            _newContentHover = hover;
            Cursor = hover ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_newContentHover) { _newContentHover = false; Cursor = Cursors.Default; Invalidate(); }
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (e.Button == MouseButtons.Left && _projectActionsEnabled && _newContentRect.Contains(e.Location))
            NewContentClicked?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.Clear(UITheme.White);

        using (var borderPen = new Pen(UITheme.Neutral200, 1f))
            g.DrawLine(borderPen, 0, Height - 1, Width, Height - 1);

        int cy = Height / 2;
        using var bodyFont = new Font("Segoe UI", 10F);
        using var bodyBold = new Font("Segoe UI", 10F, FontStyle.Bold);
        const TextFormatFlags tf = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;

        // Reserve the rightmost position for the authenticated avatar.
        int right = Width - PadX - (_accountButton.Visible ? 40 + 20 : 0);
        var bellRect = new Rectangle(right - 26, cy - 13, 26, 26);
        UIIcons.Bell(g, bellRect, UITheme.Neutral600, 1.8f);
        right -= 26 + 20;
        // Nút + New Content
        const int btnW = 156, btnH = 42;
        _newContentRect = new Rectangle(right - btnW, cy - btnH / 2, btnW, btnH);
        using (var btnBrush = new SolidBrush(!_projectActionsEnabled ? UITheme.Neutral200 :
            _newContentHover ? UITheme.Neutral800 : UITheme.Black))
        using (var btnPath = UITheme.CreateRoundedRectanglePath(_newContentRect, 8))
            g.FillPath(btnBrush, btnPath);
        int tW = TextRenderer.MeasureText(g, "New Content", UITheme.FontBodyBold, new Size(int.MaxValue, 20), TextFormatFlags.NoPadding).Width;
        int contentX = _newContentRect.X + (btnW - (18 + 10 + tW)) / 2;
        Color actionColor = _projectActionsEnabled ? UITheme.White : UITheme.Neutral600;
        UIIcons.Plus(g, new Rectangle(contentX, cy - 9, 18, 18), actionColor, 2f);
        TextRenderer.DrawText(g, "New Content", UITheme.FontBodyBold, new Rectangle(contentX + 28, _newContentRect.Y, tW + 6, btnH), actionColor,
            tf | TextFormatFlags.SingleLine);
        right = _newContentRect.X - 24;

        // ===== Trái → phải =====
        int x = PadX;
        string[] crumbs = { "CreatorFlow", _projectName, _authenticatedPage };
        for (int i = 0; i < crumbs.Length; i++)
        {
            bool last = i == crumbs.Length - 1;
            var font = last ? bodyBold : bodyFont;
            int availableWidth = Math.Max(0, right - x - (crumbs.Length - i - 1) * 28);
            int w = Math.Min(availableWidth, TextRenderer.MeasureText(g, crumbs[i], font, new Size(int.MaxValue, 24), TextFormatFlags.NoPadding).Width);
            TextRenderer.DrawText(g, crumbs[i], font, new Rectangle(x, cy - 12, w, 24), last ? UITheme.Ink : UITheme.Neutral600,
                tf | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
            x += w + 8;
            if (!last)
            {
                if (x + 20 <= right)
                    UIIcons.Chevron(g, new Rectangle(x, cy - 10, 20, 20), ChevronDir.Right, UITheme.Neutral400, 1.6f);
                x += 20 + 8;
            }
        }

        // Ô tìm kiếm (chỉ hiển thị nếu đủ chỗ)
        int searchX = x + 56;
        int searchW = Math.Min(400, right - searchX);
        if (searchW >= 200)
        {
            var sr = new Rectangle(searchX, cy - 20, searchW, 40);
            using (var sBrush = new SolidBrush(UITheme.Neutral50))
            using (var sPen = new Pen(UITheme.Neutral200, 1f))
            using (var sPath = UITheme.CreateRoundedRectanglePath(new Rectangle(sr.X, sr.Y, sr.Width - 1, sr.Height - 1), 8))
            {
                g.FillPath(sBrush, sPath);
                g.DrawPath(sPen, sPath);
            }
            UIIcons.Search(g, new Rectangle(sr.X + 12, sr.Y + 11, 18, 18), UITheme.Neutral400);
            // Global search has no controller yet; do not advertise a working shortcut.
            TextRenderer.DrawText(g, "Tìm kiếm chung chưa khả dụng", UITheme.FontBody,
                new Rectangle(sr.X + 40, sr.Y, sr.Width - 52, sr.Height), UITheme.Neutral400, tf | TextFormatFlags.EndEllipsis);
        }
    }
}
