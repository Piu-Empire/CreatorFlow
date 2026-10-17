using System.Drawing.Drawing2D;
using CreatorFlow.Theme;

namespace CreatorFlow.Controls.IdeaBank;

/// <summary>Panel nền trắng có viền bo góc dùng cho các bề mặt trên trang Idea Bank.</summary>
public sealed class IdeaCardPanel : Panel
{
    public int Radius { get; set; } = 10;
    public Color BorderColor { get; set; } = Color.FromArgb(228, 234, 243);

    public IdeaCardPanel()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (Width < 3 || Height < 3) return;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = UITheme.CreateRoundedRectanglePath(new Rectangle(0, 0, Width - 1, Height - 1), Radius);
        using var pen = new Pen(BorderColor, 1f);
        e.Graphics.DrawPath(pen, path);
    }
}
