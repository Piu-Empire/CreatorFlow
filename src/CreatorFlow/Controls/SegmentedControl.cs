using System.Drawing.Drawing2D;
using CreatorFlow.Theme;

namespace CreatorFlow.Controls;

/// <summary>
/// Bộ lọc dạng segmented: All | YouTube | TikTok | Instagram | Facebook.
/// Độ rộng mỗi ô theo độ dài chữ (giống mẫu), ô đang chọn nền đen chữ trắng.
/// Giá trị đầu tiên PHẢI là "All" vì BoardForm.ApplyFilters so sánh với chuỗi này.
/// </summary>
public class SegmentedControl : Control
{
    private const int CellPadX = 14;
    private const int Gap = 2;
    private const int Inset = 3;

    private readonly List<string> _items = new();
    private int _selectedIndex = 0;
    private int _hoveredIndex = -1;

    public event EventHandler? SelectedIndexChanged;

    public List<string> Items => _items;

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (value >= 0 && value < _items.Count && value != _selectedIndex)
            {
                _selectedIndex = value;
                Invalidate();
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public string SelectedValue =>
        _selectedIndex >= 0 && _selectedIndex < _items.Count
            ? _items[_selectedIndex] : string.Empty;

    public SegmentedControl()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        Font = UITheme.FontBody;
        Height = 36;
        Cursor = Cursors.Hand;
        _items.AddRange(new[] { "All", "YouTube", "TikTok", "Instagram", "Facebook" });
        Width = MeasureTotalWidth();
    }

    /// <summary>Rộng cần thiết để hiện đủ mọi ô.</summary>
    public int MeasureTotalWidth()
    {
        int w = Inset * 2;
        foreach (var t in _items) w += CellWidth(t) + Gap;
        return w - Gap;
    }

    private int CellWidth(string text) =>
        TextRenderer.MeasureText(text, Font, new Size(int.MaxValue, 24), TextFormatFlags.NoPadding).Width + CellPadX * 2;

    private List<Rectangle> GetCellRects()
    {
        var rects = new List<Rectangle>();
        int x = Inset;
        foreach (var t in _items)
        {
            int w = CellWidth(t);
            rects.Add(new Rectangle(x, Inset, w, Height - Inset * 2));
            x += w + Gap;
        }
        return rects;
    }

    private int GetIndexAt(Point p)
    {
        var rects = GetCellRects();
        for (int i = 0; i < rects.Count; i++)
            if (rects[i].Contains(p)) return i;
        return -1;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        int idx = GetIndexAt(e.Location);
        if (idx != _hoveredIndex) { _hoveredIndex = idx; Invalidate(); }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hoveredIndex = -1;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        int idx = GetIndexAt(e.Location);
        if (idx >= 0) SelectedIndex = idx;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.Clear(Parent?.BackColor ?? UITheme.White);

        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        using (var bgBrush = new SolidBrush(UITheme.White))
        using (var borderPen = new Pen(UITheme.Neutral200, 1f))
        using (var path = UITheme.CreateRoundedRectanglePath(bounds, 8))
        {
            g.FillPath(bgBrush, path);
            g.DrawPath(borderPen, path);
        }

        var rects = GetCellRects();
        for (int i = 0; i < _items.Count; i++)
        {
            var r = rects[i];
            bool isSel = i == _selectedIndex;
            bool isHov = i == _hoveredIndex && !isSel;

            if (isSel || isHov)
            {
                using var fill = new SolidBrush(isSel ? UITheme.Black : UITheme.Neutral100);
                using var p = UITheme.CreateRoundedRectanglePath(r, 6);
                g.FillPath(fill, p);
            }

            Color textColor = isSel ? UITheme.White : (isHov ? UITheme.Neutral900 : UITheme.Neutral700);
            TextRenderer.DrawText(g, _items[i], Font, r, textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        }
    }
}
