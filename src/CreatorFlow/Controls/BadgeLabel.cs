using CreatorFlow.Theme;

namespace CreatorFlow.Controls;

/// <summary>Nhãn pill nhỏ dùng cho badge trong header. Kéo-thả được từ Toolbox sau khi build.</summary>
public sealed class BadgeLabel : Control
{
    private Color _bg = UITheme.Neutral800;
    private Color _fg = UITheme.White;

    public BadgeLabel(string text, Color bg, Color fg)
    {
        Text = text;
        _bg = bg;
        _fg = fg;
        Init();
    }

    /// <summary>Dựng cho Designer (Toolbox): set Text/BgColor/FgColor ở cửa sổ Properties.</summary>
    public BadgeLabel()
    {
        Init();
    }

    /// <summary>Màu nền pill (set được trên Designer).</summary>
    public Color BgColor
    {
        get => _bg;
        set { _bg = value; Invalidate(); }
    }

    /// <summary>Màu chữ pill (set được trên Designer).</summary>
    public Color FgColor
    {
        get => _fg;
        set { _fg = value; Invalidate(); }
    }

    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        Size = new Size(TextRenderer.MeasureText(Text, Font).Width + 20, 20);
    }

    private void Init()
    {
        Font = UITheme.FontMicro;
        BackColor = UITheme.White;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        Size = new Size(TextRenderer.MeasureText(Text, Font).Width + 20, 20);
    }

    protected override void OnPaint(PaintEventArgs e) =>
        UITheme.DrawBadge(e.Graphics, Text, _bg, _fg, new Rectangle(0, 0, Width - 1, Height - 1));
}
