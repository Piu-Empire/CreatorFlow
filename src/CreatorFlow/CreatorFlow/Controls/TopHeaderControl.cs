using System.Drawing.Drawing2D;
using CreatorFlow.Services;
using CreatorFlow.Theme;

namespace CreatorFlow.Controls;

/// <summary>
/// Thanh trên cùng: Breadcrumb | Ô tìm kiếm (Ctrl K) | + New Content | Chuông | Avatar + tên + vai trò.
/// </summary>
public class TopHeaderControl : UserControl
{
    private const int PadX = 32;

    private string _statsText = "";
    private Rectangle _newContentRect;
    private bool _newContentHover;

    public event EventHandler? NewContentClicked;

    public TopHeaderControl()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Dock = DockStyle.Top;
        Height = 68;
        BackColor = UITheme.White;
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
        bool hover = _newContentRect.Contains(e.Location);
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
        if (_newContentRect.Contains(e.Location))
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
        var bodyFont = new Font("Segoe UI", 10F);
        var bodyBold = new Font("Segoe UI", 10F, FontStyle.Bold);
        const TextFormatFlags tf = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;

        // ===== Phải → trái =====
        int right = Width - PadX;

        // Người dùng: [Avatar] Tên / Vai trò  v
        string userName = string.IsNullOrWhiteSpace(CurrentSession.CurrentUserName) ? "Elena Vance" : CurrentSession.CurrentUserName;
        var nameSize = TextRenderer.MeasureText(g, userName, UITheme.FontBodyBold, new Size(int.MaxValue, 20), TextFormatFlags.NoPadding);
        var roleSize = TextRenderer.MeasureText(g, "Lead Producer", UITheme.FontLabel, new Size(int.MaxValue, 20), TextFormatFlags.NoPadding);
        int textW = Math.Max(nameSize.Width, roleSize.Width);

        UIIcons.Chevron(g, new Rectangle(right - 22, cy - 11, 22, 22), ChevronDir.Down, UITheme.Neutral600, 1.8f);
        right -= 22 + 6;

        int textX = right - textW;
        TextRenderer.DrawText(g, userName, UITheme.FontBodyBold, new Rectangle(textX, cy - 18, textW + 6, 18), UITheme.Ink, tf);
        TextRenderer.DrawText(g, "Lead Producer", UITheme.FontLabel, new Rectangle(textX, cy, textW + 6, 16), UITheme.Neutral600, tf);
        right = textX - 10;

        string initials = "EV";
        var parts = userName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2) initials = $"{parts[0][0]}{parts[^1][0]}";
        else if (parts.Length == 1 && parts[0].Length >= 2) initials = parts[0].Substring(0, 2);
        UITheme.DrawAvatar(g, initials, new Rectangle(right - 40, cy - 20, 40, 40), UITheme.Neutral800, UITheme.White, UITheme.FontLabelBold);
        right -= 40 + 16;

        // Vạch ngăn dọc
        using (var sepPen = new Pen(UITheme.Neutral200, 1f))
            g.DrawLine(sepPen, right, cy - 14, right, cy + 14);
        right -= 16;

        // Chuông + chấm đỏ
        var bellRect = new Rectangle(right - 26, cy - 13, 26, 26);
        UIIcons.Bell(g, bellRect, UITheme.Ink, 1.8f);
        using (var dot = new SolidBrush(UITheme.Danger))
            g.FillEllipse(dot, bellRect.Right - 9, bellRect.Y - 1, 8, 8);
        right -= 26 + 20;

        // Nút + New Content
        const int btnW = 156, btnH = 42;
        _newContentRect = new Rectangle(right - btnW, cy - btnH / 2, btnW, btnH);
        using (var btnBrush = new SolidBrush(_newContentHover ? UITheme.Neutral800 : UITheme.Black))
        using (var btnPath = UITheme.CreateRoundedRectanglePath(_newContentRect, 8))
            g.FillPath(btnBrush, btnPath);
        int tW = TextRenderer.MeasureText(g, "New Content", UITheme.FontBodyBold, new Size(int.MaxValue, 20), TextFormatFlags.NoPadding).Width;
        int contentX = _newContentRect.X + (btnW - (18 + 10 + tW)) / 2;
        UIIcons.Plus(g, new Rectangle(contentX, cy - 9, 18, 18), UITheme.White, 2f);
        TextRenderer.DrawText(g, "New Content", UITheme.FontBodyBold, new Rectangle(contentX + 28, _newContentRect.Y, tW + 6, btnH), UITheme.White,
            tf | TextFormatFlags.SingleLine);
        right = _newContentRect.X - 24;

        // ===== Trái → phải =====
        int x = PadX;
        string[] crumbs = { "CreatorFlow", "Studio Alpha", "Board" };
        for (int i = 0; i < crumbs.Length; i++)
        {
            bool last = i == crumbs.Length - 1;
            var font = last ? bodyBold : bodyFont;
            int w = TextRenderer.MeasureText(g, crumbs[i], font, new Size(int.MaxValue, 24), TextFormatFlags.NoPadding).Width;
            TextRenderer.DrawText(g, crumbs[i], font, new Rectangle(x, cy - 12, w + 4, 24), last ? UITheme.Ink : UITheme.Neutral600, tf);
            x += w + 8;
            if (!last)
            {
                UIIcons.Chevron(g, new Rectangle(x, cy - 10, 20, 20), ChevronDir.Right, UITheme.Neutral400, 1.6f);
                x += 20 + 8;
            }
        }
        bodyFont.Dispose();
        bodyBold.Dispose();

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
            TextRenderer.DrawText(g, "Search tasks, docs, creators...", UITheme.FontBody,
                new Rectangle(sr.X + 40, sr.Y, sr.Width - 110, sr.Height), UITheme.Neutral400, tf | TextFormatFlags.EndEllipsis);

            var chip = new Rectangle(sr.Right - 62, sr.Y + 9, 50, 22);
            using (var chipBrush = new SolidBrush(UITheme.Neutral200))
            using (var chipPath = UITheme.CreateRoundedRectanglePath(chip, 5))
                g.FillPath(chipBrush, chipPath);
            TextRenderer.DrawText(g, "Ctrl K", UITheme.FontMicro, chip, UITheme.Neutral600,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }
}
