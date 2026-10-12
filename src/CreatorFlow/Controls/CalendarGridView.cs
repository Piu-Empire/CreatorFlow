using System.Drawing.Drawing2D;
using CreatorFlow.Theme;

namespace CreatorFlow.Controls;

/// <summary>Lưới lịch vẽ tay duy nhất cho cả 3 chế độ (không tạo control theo ô).</summary>
public class CalendarGridView : Control
{
    private const int MaxChipsPerCell = 3;
    private int S(int px) => (int)Math.Round(px * DeviceDpi / 96f);
    private int HeaderH => S(30);
    private int ChipH => S(24);
    private int ChipGap => S(4);
    private int CellPad => S(6);
    private int UnscheduledRowH => S(26);
    private int UnscheduledHeaderH => S(30);
    private static int Edge(int total, int parts, int i) => (int)((long)total * i / parts);
    private Rectangle _hoverRect;
    private string _tipText = string.Empty;
    private int _unschedTop;

    private readonly List<(Rectangle Rect, long ContentId, string Label)> _chipHits = new();
    private readonly List<(Rectangle Rect, DateTime Date)> _moreHits = new();
    private readonly ToolTip _tip = new();

    private static readonly string[] WeekdayNames = ["T2", "T3", "T4", "T5", "T6", "T7", "CN"];

    /// <summary>Chiều cao dải chưa xếp lịch theo đúng số dòng vẽ (không chiếm thừa).</summary>
    private int FooterHeight =>
        Unscheduled.Count == 0 ? 0 : UnscheduledHeaderH + Math.Min(3, Unscheduled.Count) * (UnscheduledRowH + 2);

    /// <summary>Tên thứ theo lịch Việt Nam (tuần bắt đầu Thứ 2).</summary>
    private static string WeekdayName(DateTime date) => WeekdayNames[((int)date.DayOfWeek + 6) % 7];

    public CalendarView View { get; set; } = CalendarView.Month;

    public DateTime CursorDate { get; set; } = DateTime.Today;

    public Dictionary<DateTime, List<CalendarChip>> ChipsByDate { get; set; } = new();

    public List<Models.ContentBoardCard> Unscheduled { get; set; } = new();

    public event Action<long>? ChipClicked;

    public event Action<DateTime>? MoreClicked;

    public CalendarGridView()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = UITheme.White;
        Cursor = Cursors.Default;
        Font = UITheme.FontBody;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _tip.Dispose();
        base.Dispose(disposing);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var chip = _chipHits.FirstOrDefault(h => h.Rect.Contains(e.Location));
        bool more = _moreHits.Any(h => h.Rect.Contains(e.Location));
        Cursor = chip != default || more ? Cursors.Hand : Cursors.Default;
        string tip = chip != default ? chip.Label : string.Empty;
        if (tip != _tipText) { _tipText = tip; _tip.SetToolTip(this, tip); }
        Rectangle hover = chip != default ? chip.Rect : Rectangle.Empty;
        if (hover != _hoverRect) { _hoverRect = hover; Invalidate(); }
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        Focus(); // để nhận cuộn chuột
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_hoverRect != Rectangle.Empty) { _hoverRect = Rectangle.Empty; Invalidate(); }
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        if (Unscheduled.Count <= 3 || e.Y < Height - FooterHeight) return;
        _unschedTop = Math.Clamp(_unschedTop + (e.Delta < 0 ? 1 : -1), 0, Unscheduled.Count - 3);
        Invalidate();
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        var chip = _chipHits.FirstOrDefault(h => h.Rect.Contains(e.Location));
        if (chip != default)
        {
            ChipClicked?.Invoke(chip.ContentId);
            return;
        }

        var more = _moreHits.FirstOrDefault(h => h.Rect.Contains(e.Location));
        if (more != default) MoreClicked?.Invoke(more.Date);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.Clear(UITheme.White);
        _chipHits.Clear();
        _moreHits.Clear();
        _viewHeight = Height - FooterHeight;
        _unschedTop = Math.Clamp(_unschedTop, 0, Math.Max(0, Unscheduled.Count - 3));
        switch (View)
        {
            case CalendarView.Month: PaintMonth(g); break;
            case CalendarView.Week: PaintWeek(g); break;
            default: PaintDay(g); break;
        }

        PaintUnscheduled(g, Height);
    }

    /// <summary>Chiều cao vùng vẽ lưới (trừ dải chưa xếp lịch).</summary>
    private int _viewHeight;

    // ---------- Month ----------

    private void PaintMonth(Graphics g)
    {
        int gridH = _viewHeight - HeaderH;
        using (var headBrush = new SolidBrush(UITheme.Neutral50))
            g.FillRectangle(headBrush, 0, 0, Width, HeaderH);
        for (int i = 0; i < 7; i++)
        {
            int x = Edge(Width, 7, i);
            var r = new Rectangle(x, 0, Edge(Width, 7, i + 1) - x, HeaderH);
            TextRenderer.DrawText(g, WeekdayNames[i], UITheme.FontLabelBold, r, UITheme.Neutral600,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }

        List<DateTime> cells = CalendarDateLogic.MonthGrid(CursorDate.Year, CursorDate.Month);
        using var gridPen = new Pen(UITheme.Neutral200, 1f);
        using var offBrush = new SolidBrush(UITheme.Neutral50);
        for (int i = 0; i < 42; i++)
        {
            int col = i % 7, row = i / 7;
            int x = Edge(Width, 7, col);
            int y = HeaderH + Edge(gridH, 6, row);
            var cell = new Rectangle(x, y, Edge(Width, 7, col + 1) - x, HeaderH + Edge(gridH, 6, row + 1) - y);
            DateTime date = cells[i];
            bool inMonth = date.Month == CursorDate.Month;
            if (!inMonth) g.FillRectangle(offBrush, cell);
            g.DrawRectangle(gridPen, cell.X, cell.Y, cell.Width - 1, cell.Height - 1);
            PaintDateNumber(g, date, new Rectangle(cell.X + CellPad, cell.Y + S(2), S(30), S(22)), inMonth);
            if (!inMonth) continue;
            PaintChips(g, Chips(date), new Rectangle(cell.X + CellPad, cell.Y + S(26), cell.Width - CellPad * 2, cell.Height - S(30)), date);
        }
    }

    // ---------- Week ----------

    private void PaintWeek(Graphics g)
    {
        List<DateTime> days = CalendarDateLogic.WeekDays(CursorDate);
        using var gridPen = new Pen(UITheme.Neutral200, 1f);
        using var todayBrush = new SolidBrush(UITheme.Neutral50);
        for (int i = 0; i < 7; i++)
        {
            int x = Edge(Width, 7, i);
            var col = new Rectangle(x, 0, Edge(Width, 7, i + 1) - x, _viewHeight);
            DateTime date = days[i];
            if (date.Date == DateTime.Today) g.FillRectangle(todayBrush, col);
            g.DrawRectangle(gridPen, col.X, col.Y, col.Width - 1, col.Height - 1);
            TextRenderer.DrawText(g, WeekdayNames[i], UITheme.FontLabel,
                new Rectangle(col.X + CellPad, S(2), col.Width - CellPad * 2, S(16)),
                UITheme.Neutral600, TextFormatFlags.Left | TextFormatFlags.SingleLine);
            PaintDateNumber(g, date, new Rectangle(col.X + CellPad, S(18), S(60), S(22)), true);
            PaintChips(g, Chips(date),
                new Rectangle(col.X + CellPad, S(46), col.Width - CellPad * 2, _viewHeight - S(46) - CellPad),
                date, int.MaxValue);
        }
    }

    // ---------- Day ----------

    private void PaintDay(Graphics g)
    {
        DateTime date = CursorDate.Date;
        TextRenderer.DrawText(g, WeekdayName(date), UITheme.FontLabel, new Rectangle(CellPad, S(2), S(200), S(16)),
            UITheme.Neutral600, TextFormatFlags.Left | TextFormatFlags.SingleLine);
        PaintDateNumber(g, date, new Rectangle(CellPad, S(18), S(200), S(22)), true);
        TextRenderer.DrawText(g, "Cả ngày", UITheme.FontLabelBold,
            new Rectangle(CellPad, S(46), Width - CellPad * 2, S(18)),
            UITheme.Neutral600, TextFormatFlags.Left | TextFormatFlags.SingleLine);

        List<CalendarChip> chips = Chips(date);
        int y = S(70);
        int rowH = ChipH + S(8);
        foreach (CalendarChip chip in chips)
        {
            var rect = new Rectangle(CellPad, y, Width - CellPad * 2, rowH);
            if (rect.Bottom > _viewHeight - S(4)) break;
            PaintChip(g, chip, rect);
            y += rowH + ChipGap;
        }

        if (chips.Count == 0)
            TextRenderer.DrawText(g, "Không có content nào trong ngày này.", UITheme.FontBody,
                new Rectangle(CellPad, y, Width - CellPad * 2, S(30)), UITheme.Neutral600,
                TextFormatFlags.Left | TextFormatFlags.SingleLine);
    }

    // ---------- Dải "Chưa xếp lịch" (AC5) ----------

    /// <summary>Vẽ dải cuối màn hình cho content không có ngày nào.</summary>
    private void PaintUnscheduled(Graphics g, int bottom)
    {
        if (Unscheduled.Count == 0) return;
        int top = bottom - FooterHeight;
        using (var linePen = new Pen(UITheme.Neutral200, 1f))
            g.DrawLine(linePen, 0, top, Width, top);
        string title = $"Chưa xếp lịch ({Unscheduled.Count})"
            + (Unscheduled.Count > 3 ? " · cuộn chuột để xem thêm" : string.Empty);
        TextRenderer.DrawText(g, title, UITheme.FontLabelBold,
            new Rectangle(CellPad, top + S(4), Width - CellPad * 2, S(22)), UITheme.Neutral600,
            TextFormatFlags.Left | TextFormatFlags.SingleLine);
        int y = top + UnscheduledHeaderH - S(2);
        foreach (Models.ContentBoardCard card in Unscheduled.Skip(_unschedTop).Take(3))
        {
            PaintUnscheduledRow(g, card, new Rectangle(CellPad, y, Width - CellPad * 2, UnscheduledRowH));
            y += UnscheduledRowH + 2;
        }
    }

    private void PaintUnscheduledRow(Graphics g, Models.ContentBoardCard card, Rectangle rect)
    {
        using (var path = UITheme.CreateRoundedRectanglePath(rect, S(6)))
        using (var fill = new SolidBrush(rect == _hoverRect ? UITheme.Neutral200 : UITheme.Neutral100))
        using (var pen = new Pen(UITheme.Neutral200, 1f))
        {
            g.FillPath(fill, path);
            g.DrawPath(pen, path);
        }

        string text = card.Code + " · " + card.Title + " · chưa có ngày";
        TextRenderer.DrawText(g, text, UITheme.FontBody,
            new Rectangle(rect.X + S(8), rect.Y, rect.Width - S(12), rect.Height), UITheme.Neutral600,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis
                | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
        _chipHits.Add((rect, card.ContentId, text));
    }

    // ---------- Khối dùng chung ----------

    private List<CalendarChip> Chips(DateTime date) =>
        ChipsByDate.TryGetValue(date.Date, out List<CalendarChip>? list) ? list : new List<CalendarChip>();

    private void PaintDateNumber(Graphics g, DateTime date, Rectangle rect, bool inMonth)
    {
        if (date.Date == DateTime.Today)
        {
            int d = S(20);
            var circle = new Rectangle(rect.X, rect.Y + 1, d, d);
            using var path = UITheme.CreateRoundedRectanglePath(circle, d / 2);
            using var fill = new SolidBrush(UITheme.Black);
            g.FillPath(fill, path);
            TextRenderer.DrawText(g, date.Day.ToString(), UITheme.FontLabelBold, circle, UITheme.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            return;
        }

        TextRenderer.DrawText(g, date.Day.ToString(), UITheme.FontBody, rect,
            inMonth ? UITheme.Ink : UITheme.Neutral400,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
    }

    private void PaintChips(Graphics g, List<CalendarChip> chips, Rectangle area, DateTime date, int max = MaxChipsPerCell)
    {
        int y = area.Y;
        int shown = 0;
        foreach (CalendarChip chip in chips)
        {
            if (shown >= max) break;
            var rect = new Rectangle(area.X, y, area.Width, ChipH);
            if (rect.Bottom > area.Bottom) break;
            PaintChip(g, chip, rect);
            y += ChipH + ChipGap;
            shown++;
        }

        if (chips.Count > shown && y + S(18) <= area.Bottom)
        {
            string moreText = $"+{chips.Count - shown} nữa";
            int w = TextRenderer.MeasureText(moreText, UITheme.FontLabelBold).Width + S(8);
            var more = new Rectangle(area.X, y, w, S(18));
            TextRenderer.DrawText(g, moreText, UITheme.FontLabelBold, more, UITheme.Neutral600,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            _moreHits.Add((more, date));
        }
    }

    private void PaintChip(Graphics g, CalendarChip chip, Rectangle rect)
    {
        Color bg = chip.IsOverdue ? UITheme.DangerBg : rect == _hoverRect ? UITheme.Neutral100 : UITheme.White;
        Color border = chip.IsOverdue ? UITheme.Danger : UITheme.Neutral200;
        using (var path = UITheme.CreateRoundedRectanglePath(rect, S(6)))
        using (var fill = new SolidBrush(bg))
        using (var pen = new Pen(border, 1f))
        {
            if (chip.IsPublish) pen.DashStyle = DashStyle.Dash;
            g.FillPath(fill, path);
            g.DrawPath(pen, path);
        }

        Color dot = chip.IsOverdue ? UITheme.Danger : UITheme.GetStageColor(chip.Status);
        int d = S(8);
        var dotRect = new Rectangle(rect.X + S(8), rect.Y + (rect.Height - d) / 2, d, d);
        if (chip.IsPublish)
        {
            using var ring = new Pen(dot, 2f);
            g.DrawEllipse(ring, dotRect.X + 1, dotRect.Y + 1, d - 2, d - 2);
        }
        else
        {
            using var dotBrush = new SolidBrush(dot);
            g.FillEllipse(dotBrush, dotRect);
        }

        TextRenderer.DrawText(g, chip.Label, chip.IsOverdue ? UITheme.FontBodyBold : UITheme.FontBody,
            new Rectangle(rect.X + S(22), rect.Y, rect.Width - S(26), rect.Height),
            chip.IsOverdue ? UITheme.Danger : UITheme.Ink,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis
                | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
        _chipHits.Add((rect, chip.ContentId, chip.Label));
    }
}
