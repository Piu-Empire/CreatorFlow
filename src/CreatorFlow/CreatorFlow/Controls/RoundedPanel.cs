using System.Drawing.Drawing2D;
using CreatorFlow.Theme;

namespace CreatorFlow.Controls;

/// <summary>
/// Panel nền + viền bo góc (dùng cho ô giá trị, thẻ nhóm trong drawer chi tiết).
/// Có thể vẽ thêm mũi tên dropdown bên phải để trông giống ô chọn.
/// </summary>
public class RoundedPanel : Panel
{
    private Color _fillColor = UITheme.White;
    private Color _borderColor = UITheme.Neutral200;
    private int _radius = 8;
    private bool _showChevron;

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Color FillColor { get => _fillColor; set { _fillColor = value; Invalidate(); } }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Color BorderColor { get => _borderColor; set { _borderColor = value; Invalidate(); } }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public int Radius { get => _radius; set { _radius = value; Invalidate(); } }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool ShowChevron { get => _showChevron; set { _showChevron = value; Invalidate(); } }

    public RoundedPanel()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = UITheme.White;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? UITheme.White);

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = UITheme.CreateRoundedRectanglePath(rect, _radius);
        using var fill = new SolidBrush(_fillColor);
        using var pen = new Pen(_borderColor, 1f);
        g.FillPath(fill, path);
        g.DrawPath(pen, path);

        if (_showChevron)
            UIIcons.Chevron(g, new Rectangle(Width - 34, (Height - 22) / 2, 22, 22), ChevronDir.Down, UITheme.Neutral800, 1.8f);
    }
}
