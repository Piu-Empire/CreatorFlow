using System.Drawing.Drawing2D;
using CreatorFlow.Models;
using CreatorFlow.Theme;

namespace CreatorFlow.Controls;

/// <summary>
/// Một cột Kanban dạng "thẻ bo góc": header trắng (tên giai đoạn + số lượng + mô tả + nút +),
/// thân xám nhạt chứa danh sách thẻ cuộn dọc. Cột rỗng hiện "No items in this stage".
/// </summary>
public class KanbanColumnControl : UserControl
{
    public const int ColumnWidth = 340;
    private const int HeaderH = 68;
    private const int Radius = 12;
    private const int EmptyBodyH = 96;

    /// <summary>Chiều cao gọn của cột khi chưa có thẻ nào (giống mẫu: cột rỗng thấp hơn cột có thẻ).</summary>
    public const int EmptyColumnHeight = HeaderH + EmptyBodyH + 12;

    private readonly ContentStatus _status;
    private readonly FlowLayoutPanel _cardsFlow;
    private int _cardCount;
    private Rectangle _addButtonRect;
    private bool _addButtonHover;

    public ContentStatus Status => _status;
    public FlowLayoutPanel CardsFlow => _cardsFlow;

    /// <summary>Bấm nút "+" ở header cột. Không phát sinh nếu giai đoạn không cho tạo trực tiếp (VD Review).</summary>
    public event EventHandler<ContentStatus>? AddCardRequested;

    public KanbanColumnControl(ContentStatus status)
    {
        _status = status;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Width = ColumnWidth;
        BackColor = UITheme.Neutral50;
        // Chừa 1px viền, HeaderH cho header vẽ tay, 12px đáy để không đè lên góc bo
        Padding = new Padding(1, HeaderH, 1, 12);

        _cardsFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = UITheme.ColumnBody,
            Padding = new Padding(12, 12, 12, 0),
        };
        _cardsFlow.SizeChanged += (_, _) => LayoutCards();
        Controls.Add(_cardsFlow);

        _addButtonRect = new Rectangle(Width - 16 - 24, 18, 24, 24);
    }

    /// <summary>Cột Review không cho tạo trực tiếp — phải đi qua Submit for Review từ Editing.</summary>
    private bool CanAddDirectly => _status != ContentStatus.Review;

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        _addButtonRect = new Rectangle(Width - 16 - 24, 18, 24, 24);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        bool hover = CanAddDirectly && _addButtonRect.Contains(e.Location);
        if (hover != _addButtonHover)
        {
            _addButtonHover = hover;
            Cursor = hover ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_addButtonHover) { _addButtonHover = false; Cursor = Cursors.Default; Invalidate(); }
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (CanAddDirectly && _addButtonRect.Contains(e.Location))
            AddCardRequested?.Invoke(this, _status);
    }

    public bool IsEmpty => _cardCount == 0;

    public void SetCardCount(int count)
    {
        _cardCount = count;
        _cardsFlow.Visible = count > 0;   // cột rỗng: để phần thân do OnPaint vẽ chữ "No items..."
        Invalidate();
    }

    /// <summary>Xoá và dispose toàn bộ thẻ (tránh rò rỉ khi reload board).</summary>
    public void ClearCards()
    {
        var old = _cardsFlow.Controls.Cast<Control>().ToList();
        _cardsFlow.Controls.Clear();
        foreach (var c in old) c.Dispose();
    }

    /// <summary>Thẻ luôn rộng bằng vùng nội dung của cột (tự trừ thanh cuộn khi có).</summary>
    public void LayoutCards()
    {
        int w = _cardsFlow.ClientSize.Width - _cardsFlow.Padding.Horizontal;
        if (w <= 0) return;
        foreach (Control c in _cardsFlow.Controls)
            if (c.Width != w) c.Width = w;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.Clear(UITheme.Neutral50);

        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        using var outline = UITheme.CreateRoundedRectanglePath(bounds, Radius);

        // Thân cột
        using (var bodyBrush = new SolidBrush(UITheme.ColumnBody))
            g.FillPath(bodyBrush, outline);

        // Header trắng (cắt theo góc bo phía trên)
        var state = g.Save();
        g.SetClip(outline);
        using (var headBrush = new SolidBrush(UITheme.White))
            g.FillRectangle(headBrush, 0, 0, Width, HeaderH);
        g.Restore(state);

        using (var divPen = new Pen(UITheme.Neutral200, 1f))
            g.DrawLine(divPen, 1, HeaderH - 1, Width - 2, HeaderH - 1);
        using (var borderPen = new Pen(UITheme.Neutral200, 1f))
            g.DrawPath(borderPen, outline);

        const TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis;

        // Tên giai đoạn + badge số lượng
        string title = UITheme.GetStageDisplayName(_status);
        var titleFont = UITheme.FontBodyBold;
        var titleSize = TextRenderer.MeasureText(g, title, titleFont, new Size(int.MaxValue, 24), TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
        TextRenderer.DrawText(g, title, titleFont, new Rectangle(16, 16, Width - 80, 22), UITheme.Ink,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | flags);

        string countStr = _cardCount.ToString();
        var countSize = TextRenderer.MeasureText(g, countStr, UITheme.FontMicro, new Size(int.MaxValue, 20), TextFormatFlags.NoPadding);
        var countRect = new Rectangle(16 + titleSize.Width + 8, 18, Math.Max(22, countSize.Width + 12), 18);
        using (var countBrush = new SolidBrush(UITheme.Neutral100))
        using (var countPath = UITheme.CreateRoundedRectanglePath(countRect, 6))
            g.FillPath(countBrush, countPath);
        TextRenderer.DrawText(g, countStr, UITheme.FontMicro, countRect, UITheme.Neutral600,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | flags);

        // Mô tả giai đoạn
        TextRenderer.DrawText(g, UITheme.GetStageSubtitle(_status), UITheme.FontLabel,
            new Rectangle(16, 40, Width - 64, 18), UITheme.Neutral600,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | flags);

        // Nút + (tô nền khi hover, mờ đi nếu giai đoạn không cho tạo trực tiếp)
        if (_addButtonHover)
        {
            using var hb = new SolidBrush(UITheme.Neutral100);
            using var hp = UITheme.CreateRoundedRectanglePath(_addButtonRect, 6);
            g.FillPath(hb, hp);
        }
        UIIcons.Plus(g, _addButtonRect, CanAddDirectly ? UITheme.Neutral400 : UITheme.Neutral200, 2f);

        // Trạng thái rỗng
        if (_cardCount == 0)
        {
            using var italic = new Font("Segoe UI", 9.5F, FontStyle.Italic);
            TextRenderer.DrawText(g, "No items in this stage", italic,
                new Rectangle(1, HeaderH, Width - 2, EmptyBodyH), UITheme.Neutral400,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | flags);
        }
    }
}