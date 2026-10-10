using CreatorFlow.Theme;

namespace CreatorFlow.Controls;

/// <summary>Nhãn pill nhỏ dùng cho badge trong header các màn hình admin.</summary>
public sealed class BadgeLabel : Control
{
    private readonly Color _bg;
    private readonly Color _fg;

    public BadgeLabel(string text, Color bg, Color fg)
    {
        Text = text;
        _bg = bg;
        _fg = fg;
        Font = UITheme.FontMicro;
        BackColor = UITheme.White;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        Size = new Size(TextRenderer.MeasureText(text, Font).Width + 20, 20);
    }

    protected override void OnPaint(PaintEventArgs e) =>
        UITheme.DrawBadge(e.Graphics, Text, _bg, _fg, new Rectangle(0, 0, Width - 1, Height - 1));
}
