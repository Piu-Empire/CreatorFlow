using System.Drawing.Drawing2D;
using CreatorFlow.Theme;

namespace CreatorFlow.Controls;

/// <summary>Lưới lịch vẽ tay duy nhất cho cả 3 chế độ (không tạo control theo ô).</summary>
public class CalendarGridView : Control
{
    private const int HeaderH = 30;
    private const int ChipH = 24;
    private const int ChipGap = 4;
    private const int CellPad = 6;
    private const int MaxChipsPerCell = 3;

    private readonly List<(Rectangle Rect, long ContentId)> _chipHits = new();
    private readonly List<(Rectangle Rect, DateTime Date)> _moreHits = new();

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

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        bool hot = _chipHits.Any(h => h.Rect.Contains(e.Location)) ||
            _moreHits.Any(h => h.Rect.Contains(e.Location));
        Cursor = hot ? Cursors.Hand : Cursors.Default;
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
        _viewHeight = Height - (Unscheduled.Count == 0 ? 0 : UnscheduledFooterHeight);
        switch (View)
        {
            case CalendarView.Month: PaintMonth(g); break;
            case CalendarView.Week: PaintWeek(g); break;
            default: PaintDay(g); break;
        }

        PaintUnscheduled(g, Height);
    }

    private const int UnscheduledFooterHeight = 30 + 3 * (26 + 2);

    /// <summary>Chiều cao vùng vẽ lưới (trừ dải chưa xếp lịch).</summary>
    private int _viewHeight;

    // ---------- Month ----------

    private void PaintMonth(Graphics g)
    {
        string[] days = ["T2", "T3", "T4", "T5", "T6", "T7", "CN"];
        int colW = Width / 7;
        for (int i = 0; i < 7; i++)
        {
            var r = new Rectangle(i * colW, 0, colW, HeaderH);
            TextRenderer.DrawText(g, days[i], UITheme.FontLabelBold, r, UITheme.Neutral600,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }

        List<DateTime> cells = CalendarDateLogic.MonthGrid(CursorDate.Year, CursorDate.Month);
        int rowH = Math.Max(60, (_viewHeight - HeaderH) / 6);
        using var gridPen = new Pen(UITheme.Neutral200, 1f);
        for (int i = 0; i < 42; i++)
        {
            int col = i % 7, row = i / 7;
            var cell = new Rectangle(col * colW, HeaderH + row * rowH, colW, rowH);
            g.DrawRectangle(gridPen, cell);
            DateTime date = cells[i];
            bool inMonth = date.Month == CursorDate.Month;
            PaintDateNumber(g, date, new Rectangle(cell.X + CellPad, cell.Y + 2, 30, 22), inMonth);
            if (!inMonth) continue;
            PaintChips(g, Chips(date), new Rectangle(cell.X + CellPad, cell.Y + 26, cell.Width - CellPad * 2, rowH - 30), date);
        }
    }

    // ---------- Week ----------

    private void PaintWeek(Graphics g)
    {
        List<DateTime> days = CalendarDateLogic.WeekDays(CursorDate);
        int colW = Width / 7;
        for (int i = 0; i < 7; i++)
        {
            var col = new Rectangle(i * colW, 0, colW, _viewHeight);
            using (var gridPen = new Pen(UITheme.Neutral200, 1f))
                g.DrawRectangle(gridPen, col);
            DateTime date = days[i];
            PaintDateNumber(g, date, new Rectangle(col.X + CellPad, 4, 60, 22), true);
            PaintChips(g, Chips(date), new Rectangle(col.X + CellPad, HeaderH, col.Width - CellPad * 2, _viewHeight - HeaderH - 4), date);
        }
    }

    // ---------- Day ----------

    private void PaintDay(Graphics g)
    {
        DateTime date = CursorDate.Date;
        PaintDateNumber(g, date, new Rectangle(CellPad, 4, 200, 22), true);
        List<CalendarChip> chips = Chips(date);
        int y = HeaderH;
        foreach (CalendarChip chip in chips)
        {
            var rect = new Rectangle(CellPad, y, Width - CellPad * 2, ChipH + 8);
            if (rect.Bottom > _viewHeight - 4) break;
            PaintChip(g, chip, rect);
            y += ChipH + 8 + ChipGap;
        }

        if (chips.Count == 0)
        {
            TextRenderer.DrawText(g, "Không có content nào trong ngày này.", UITheme.FontBody,
                new Rectangle(CellPad, y, Width - CellPad * 2, 30), UITheme.Neutral600,
                TextFormatFlags.Left | TextFormatFlags.SingleLine);
        }
    }

    // ---------- Dải "Chưa xếp lịch" (AC5) ----------

    /// <summary>Vẽ dải cuối màn hình cho content không có ngày nào.</summary>
    private void PaintUnscheduled(Graphics g, int bottom)
    {
        if (Unscheduled.Count == 0) return;
        const int rowH = 26;
        int top = bottom - UnscheduledFooterHeight;
        using (var linePen = new Pen(UITheme.Neutral200, 1f))
            g.DrawLine(linePen, 0, top, Width, top);
        TextRenderer.DrawText(g, $"Chưa xếp lịch ({Unscheduled.Count})", UITheme.FontLabelBold,
            new Rectangle(CellPad, top + 4, Width - CellPad * 2, 22), UITheme.Neutral600,
            TextFormatFlags.Left | TextFormatFlags.SingleLine);
        int y = top + 28;
        foreach (Models.ContentBoardCard card in Unscheduled.Take(3))
        {
            var rect = new Rectangle(CellPad, y, Width - CellPad * 2, rowH);
            PaintUnscheduledRow(g, card, rect);
            y += rowH + 2;
        }
    }

    private void PaintUnscheduledRow(Graphics g, Models.ContentBoardCard card, Rectangle rect)
    {
        using (var path = UITheme.CreateRoundedRectanglePath(rect, 6))
        using (var fill = new SolidBrush(UITheme.Neutral100))
        using (var pen = new Pen(UITheme.Neutral200, 1f))
        {
            g.FillPath(fill, path);
            g.DrawPath(pen, path);
        }

        TextRenderer.DrawText(g, card.Code + " · " + card.Title, UITheme.FontBody, rect,
            UITheme.Neutral600,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine
                | TextFormatFlags.NoPrefix);
        _chipHits.Add((rect, card.ContentId));
    }

    // ---------- Khối dùng chung ----------

    private List<CalendarChip> Chips(DateTime date) =>
        ChipsByDate.TryGetValue(date.Date, out List<CalendarChip>? list) ? list : new List<CalendarChip>();

    private void PaintDateNumber(Graphics g, DateTime date, Rectangle rect, bool inMonth)
    {
        if (date.Date == DateTime.Today)
        {
            using var path = UITheme.CreateRoundedRectanglePath(new Rectangle(rect.X, rect.Y + 1, 20, 20), 10);
            using var fill = new SolidBrush(UITheme.Black);
            g.FillPath(fill, path);
            TextRenderer.DrawText(g, date.Day.ToString(), UITheme.FontLabelBold,
                new Rectangle(rect.X, rect.Y + 1, 20, 20), UITheme.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            return;
        }

        TextRenderer.DrawText(g, date.Day.ToString(), UITheme.FontBody, rect,
            inMonth ? UITheme.Ink : UITheme.Neutral400,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
    }

    private void PaintChips(Graphics g, List<CalendarChip> chips, Rectangle area, DateTime date)
    {
        int y = area.Y;
        int shown = 0;
        foreach (CalendarChip chip in chips)
        {
            if (shown >= MaxChipsPerCell) break;
            var rect = new Rectangle(area.X, y, area.Width, ChipH);
            if (rect.Bottom > area.Bottom) break;
            PaintChip(g, chip, rect);
            y += ChipH + ChipGap;
            shown++;
        }

        if (chips.Count > shown)
        {
            var more = new Rectangle(area.X, y, 60, 20);
            TextRenderer.DrawText(g, $"+{chips.Count - shown} nữa", UITheme.FontLabelBold, more,
                UITheme.Neutral600, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            _moreHits.Add((more, date));
        }
    }

    private void PaintChip(Graphics g, CalendarChip chip, Rectangle rect)
    {
        Color bg = chip.IsOverdue ? UITheme.DangerBg : UITheme.White;
        Color border = chip.IsOverdue ? UITheme.Danger : UITheme.Neutral200;
        using (var path = UITheme.CreateRoundedRectanglePath(rect, 6))
        using (var fill = new SolidBrush(bg))
        using (var pen = new Pen(border, 1f))
        {
            g.FillPath(fill, path);
            g.DrawPath(pen, path);
        }

        Color dot = chip.IsOverdue ? UITheme.Danger : UITheme.GetStageColor(chip.Status);
        using (var dotBrush = new SolidBrush(dot))
            g.FillEllipse(dotBrush, rect.X + 8, rect.Y + (rect.Height - 8) / 2, 8, 8);
        TextRenderer.DrawText(g, chip.Label, chip.IsOverdue ? UITheme.FontBodyBold : UITheme.FontBody,
            new Rectangle(rect.X + 22, rect.Y, rect.Width - 26, rect.Height),
            chip.IsOverdue ? UITheme.Danger : UITheme.Ink,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine
                | TextFormatFlags.NoPrefix);
        _chipHits.Add((rect, chip.ContentId));
    }
}
