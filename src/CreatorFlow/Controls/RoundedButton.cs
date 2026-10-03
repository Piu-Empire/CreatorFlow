using System.Drawing.Drawing2D;
using CreatorFlow.Theme;

namespace CreatorFlow.Controls;

public enum RoundButtonStyle
{
    Primary,
    Secondary,
    Danger,
    Ghost
}

/// <summary>
/// Nút bấm phẳng hiện đại bo góc chuẩn 8px theo mục 8 của UI Style Guide.
/// </summary>
public class RoundedButton : Button
{
    private RoundButtonStyle _style = RoundButtonStyle.Primary;
    private int _borderRadius = 8;
    private bool _isHovered;
    private bool _isPressed;
    private bool _showPlusIcon;

    /// <summary>Vẽ dấu "+" bên trái chữ (nút "New Content", "Add Ticket").</summary>
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool ShowPlusIcon
    {
        get => _showPlusIcon;
        set { _showPlusIcon = value; Invalidate(); }
    }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public RoundButtonStyle Style
    {
        get => _style;
        set { _style = value; Invalidate(); }
    }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public int BorderRadius
    {
        get => _borderRadius;
        set { _borderRadius = value; Invalidate(); }
    }

    public RoundedButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Font = UITheme.FontBodyBold;
        Cursor = Cursors.Hand;
        Padding = new Padding(12, 6, 12, 6);
        Height = 36;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _isHovered = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _isHovered = false;
        _isPressed = false;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        base.OnMouseDown(mevent);
        _isPressed = true;
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        base.OnMouseUp(mevent);
        _isPressed = false;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        var g = pevent.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);

        Color bg;
        Color textColor;
        Color borderColor = Color.Transparent;

        if (!Enabled)
        {
            bg = UITheme.Neutral100;
            textColor = UITheme.Neutral400;
            borderColor = UITheme.Neutral200;
        }
        else
        {
            switch (_style)
            {
                case RoundButtonStyle.Primary:
                    bg = _isPressed ? UITheme.Neutral800 : (_isHovered ? UITheme.Neutral900 : UITheme.Black);
                    textColor = UITheme.White;
                    break;

                case RoundButtonStyle.Secondary:
                    bg = _isPressed ? UITheme.Neutral200 : (_isHovered ? Color.FromArgb(240, 240, 240) : UITheme.Neutral100);
                    textColor = UITheme.Neutral800;
                    borderColor = UITheme.Neutral200;
                    break;

                case RoundButtonStyle.Danger:
                    bg = _isPressed ? ColorTranslator.FromHtml("#B91C1C") : (_isHovered ? ColorTranslator.FromHtml("#EF4444") : UITheme.Danger);
                    textColor = UITheme.White;
                    break;

                case RoundButtonStyle.Ghost:
                default:
                    bg = _isPressed ? UITheme.Neutral100 : (_isHovered ? UITheme.Neutral50 : Color.Transparent);
                    textColor = UITheme.Neutral700;
                    borderColor = UITheme.Neutral200;
                    break;
            }
        }

        using var path = UITheme.CreateRoundedRectanglePath(rect, _borderRadius);
        using var brush = new SolidBrush(bg);
        g.FillPath(brush, path);

        if (borderColor != Color.Transparent)
        {
            using var pen = new Pen(borderColor, 1f);
            g.DrawPath(pen, path);
        }

        // Vẽ icon / chữ
        if (_showPlusIcon)
        {
            int textW = TextRenderer.MeasureText(Text, Font, new Size(int.MaxValue, Height), TextFormatFlags.NoPadding).Width;
            int total = 16 + 8 + textW;
            int x = (Width - total) / 2;
            UIIcons.Plus(g, new Rectangle(x, (Height - 16) / 2, 16, 16), textColor, 2f);
            TextRenderer.DrawText(g, Text, Font, new Rectangle(x + 24, 0, textW + 4, Height), textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            return;
        }

        using var textBrush = new SolidBrush(textColor);
        using var sf = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.NoWrap
        };
        g.DrawString(Text, Font, textBrush, rect, sf);
    }
}
