using CreatorFlow.Models.Enums;
using System.Drawing.Drawing2D;
using CreatorFlow.Models;
using CreatorFlow.Theme;

namespace CreatorFlow.Controls;

/// <summary>
/// Thẻ Content trên Production Board (theo mẫu AI Studio):
/// [Chip mã] ....... [Priority] [Pill nền tảng]
/// Tiêu đề đậm (tối đa 2 dòng) + mô tả mờ (tối đa 2 dòng)
/// ─────────────────────────────────────────
/// [Avatar] hạn/thời lượng ................ ‹ ›
/// </summary>
public class KanbanCardControl : Control
{
    private const int PadX = 16;
    private const int TopRowY = 14;
    private const int TopRowH = 30;
    private const int TitleH = 40;
    private const int DescH = 32;
    private const int FooterH = 46;

    private readonly ContentBoardCard _card;
    private bool _isSelected;
    private bool _isHovered;
    private int _hoverArrow; // -1: prev, 0: none, 1: next

    public ContentBoardCard Card => _card;

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; Invalidate(); }
    }

    public event EventHandler<ContentBoardCard>? CardClicked;
    public event EventHandler<ContentBoardCard>? MoveNextRequested;
    public event EventHandler<ContentBoardCard>? MovePrevRequested;

    private bool HasDescription => !string.IsNullOrWhiteSpace(_card.Description);
    private bool HasPrev => _card.Status > ContentStatus.Idea;
    private bool HasNext => _card.Status < ContentStatus.Published;

    public KanbanCardControl(ContentBoardCard card)
    {
        _card = card;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        Width = 300;
        Height = CalculateHeight();
        Margin = new Padding(0, 0, 0, 12);
        Cursor = Cursors.Hand;
        Font = UITheme.FontBody;
    }

    private int CalculateHeight()
    {
        int h = TopRowY + TopRowH + 10 + TitleH;
        if (HasDescription) h += 4 + DescH;
        h += 12 + FooterH;
        return h;
    }

    private int DividerY => Height - FooterH;

    private void GetArrowRects(out Rectangle prev, out Rectangle next)
    {
        prev = Rectangle.Empty;
        next = Rectangle.Empty;
        int y = DividerY + (FooterH - 24) / 2;
        int right = Width - PadX;
        if (HasNext)
        {
            next = new Rectangle(right - 24, y, 24, 24);
            right -= 28;
        }
        if (HasPrev)
            prev = new Rectangle(right - 24, y, 24, 24);
    }

    private int ArrowAt(Point p)
    {
        GetArrowRects(out var prev, out var next);
        if (!next.IsEmpty && next.Contains(p)) return 1;
        if (!prev.IsEmpty && prev.Contains(p)) return -1;
        return 0;
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _isHovered = true; Invalidate(); }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _isHovered = false;
        _hoverArrow = 0;
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        int h = ArrowAt(e.Location);
        if (h != _hoverArrow) { _hoverArrow = h; Invalidate(); }
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        int arrow = ArrowAt(e.Location);
        // Luôn gọi sự kiện ở cuối hàm: BoardForm có thể reload lại board (dispose thẻ này).
        if (arrow == 1) MoveNextRequested?.Invoke(this, _card);
        else if (arrow == -1) MovePrevRequested?.Invoke(this, _card);
        else CardClicked?.Invoke(this, _card);
    }

    private static string PriorityText(Priority p) => p switch
    {
        Priority.High => "High",
        Priority.Medium => "Med",
        _ => "Low",
    };

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.Clear(UITheme.ColumnBody);

        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);

        // 1. Nền + viền
        using (var path = UITheme.CreateRoundedRectanglePath(bounds, 12))
        using (var bgBrush = new SolidBrush(_card.IsOverdue ? UITheme.OverdueBg : UITheme.White))
        {
            g.FillPath(bgBrush, path);
            if (_isSelected)
            {
                using var selPath = UITheme.CreateRoundedRectanglePath(new Rectangle(1, 1, Width - 3, Height - 3), 11);
                using var selPen = new Pen(UITheme.Black, 2f);
                g.DrawPath(selPen, selPath);
            }
            else
            {
                using var pen = new Pen(_isHovered ? UITheme.Neutral400 : UITheme.Neutral200, 1f);
                g.DrawPath(pen, path);
            }
        }

        const TextFormatFlags oneLine = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis;

        // 2. Hàng trên: chip mã (trái) | priority + pill nền tảng (phải)
        var codeSize = TextRenderer.MeasureText(g, _card.Code, UITheme.FontMono, new Size(int.MaxValue, 20), TextFormatFlags.NoPadding);
        var codeRect = new Rectangle(PadX, TopRowY + 3, codeSize.Width + 20, 24);
        using (var chipBrush = new SolidBrush(UITheme.Neutral100))
        using (var chipPen = new Pen(UITheme.Neutral200, 1f))
        using (var chipPath = UITheme.CreateRoundedRectanglePath(codeRect, 6))
        {
            g.FillPath(chipBrush, chipPath);
            g.DrawPath(chipPen, chipPath);
        }
        TextRenderer.DrawText(g, _card.Code, UITheme.FontMono, codeRect, UITheme.Neutral600,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | oneLine);

        int right = Width - PadX;
        if (_card.Platforms.Count > 0)
        {
            string platform = _card.Platforms[0];
            int pillW = UIIcons.MeasurePlatformPill(g, platform, TopRowH);
            var pillRect = new Rectangle(right - pillW, TopRowY, pillW, TopRowH);
            UIIcons.DrawPlatformPill(g, platform, pillRect);
            right = pillRect.X - 8;
        }

        var (prioBg, prioText, _) = UITheme.GetPriorityStyle(_card.Priority);
        string prioLabel = PriorityText(_card.Priority);
        var prioSize = TextRenderer.MeasureText(g, prioLabel, UITheme.FontLabelBold, new Size(int.MaxValue, 20), TextFormatFlags.NoPadding);
        var prioRect = new Rectangle(right - (prioSize.Width + 18), TopRowY + 4, prioSize.Width + 18, 22);
        using (var pBrush = new SolidBrush(prioBg))
        using (var pPath = UITheme.CreateRoundedRectanglePath(prioRect, 6))
            g.FillPath(pBrush, pPath);
        TextRenderer.DrawText(g, prioLabel, UITheme.FontLabelBold, prioRect, prioText,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | oneLine);

        // 3. Tiêu đề (2 dòng)
        int y = TopRowY + TopRowH + 10;
        TextRenderer.DrawText(g, _card.Title, UITheme.FontCardTitle,
            new Rectangle(PadX, y, Width - PadX * 2, TitleH), UITheme.Ink,
            TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
        y += TitleH;

        // 4. Mô tả (2 dòng)
        if (HasDescription)
        {
            y += 4;
            TextRenderer.DrawText(g, _card.Description, UITheme.FontLabel,
                new Rectangle(PadX, y, Width - PadX * 2, DescH), UITheme.Neutral600,
                TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
        }

        // 5. Đường kẻ + footer
        int dy = DividerY;
        using (var divPen = new Pen(UITheme.Neutral100, 1f))
            g.DrawLine(divPen, PadX, dy, Width - PadX, dy);

        int fy = dy + (FooterH - 24) / 2;
        string initials = "--";
        if (!string.IsNullOrWhiteSpace(_card.AssigneeName))
        {
            var parts = _card.AssigneeName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            initials = parts.Length >= 2
                ? $"{parts[0][0]}{parts[^1][0]}"
                : (parts[0].Length >= 2 ? parts[0].Substring(0, 2) : parts[0]);
        }
        UITheme.DrawAvatar(g, initials, new Rectangle(PadX, fy, 24, 24), Color.FromArgb(64, 64, 64), UITheme.White, UITheme.FontMicro);

        // Ưu tiên hiện thời lượng dự kiến (giống mẫu "24 min"); nếu chưa có thì hiện hạn chót.
        string timeText = !string.IsNullOrWhiteSpace(_card.EstimatedDuration)
            ? _card.EstimatedDuration
            : (_card.Deadline.HasValue ? _card.Deadline.Value.ToString("dd/MM") : "Chưa có hạn");
        if (_card.IsOverdue) timeText = "Overdue · " + timeText;
        TextRenderer.DrawText(g, timeText, UITheme.FontLabel,
            new Rectangle(PadX + 24 + 8, fy, Width / 2, 24),
            _card.IsOverdue ? UITheme.OverdueText : UITheme.Neutral600,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | oneLine);

        // 6. Mũi tên chuyển giai đoạn
        GetArrowRects(out var prevRect, out var nextRect);
        DrawArrow(g, prevRect, ChevronDir.Left, _hoverArrow == -1);
        DrawArrow(g, nextRect, ChevronDir.Right, _hoverArrow == 1);
    }

    private static void DrawArrow(Graphics g, Rectangle r, ChevronDir dir, bool hovered)
    {
        if (r.IsEmpty) return;
        if (hovered)
        {
            using var hb = new SolidBrush(UITheme.Neutral100);
            using var hp = UITheme.CreateRoundedRectanglePath(r, 6);
            g.FillPath(hb, hp);
        }
        UIIcons.Chevron(g, r, dir, hovered ? UITheme.Black : UITheme.Neutral800, 2f);
    }
}
