using CreatorFlow.Theme;

namespace CreatorFlow.Controls;

public sealed class ProjectListRowControl : Button
{
    private string subtitle = string.Empty;
    private string badge = string.Empty;
    private bool hovered;
    private readonly ToolTip fullName = new();
    public bool ShowDivider { get; set; } = true;
    public bool IsCurrent { get; set; }
    public bool SelectableProject { get; set; } = true;
    public bool Compact { get; set; }
    public string Subtitle { get => subtitle; set { subtitle = value; AccessibleDescription = subtitle + " · " + badge; fullName.SetToolTip(this, Text + "\n" + subtitle); Invalidate(); } }
    public string Badge { get => badge; set { badge = value; AccessibleDescription = subtitle + " · " + badge; Invalidate(); } }

    public ProjectListRowControl()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0;
        UseVisualStyleBackColor = false; BackColor = UITheme.White; ForeColor = UITheme.Ink;
        Font = UITheme.FontBodyBold; Height = 72; Margin = Padding.Empty;
        TextAlign = ContentAlignment.MiddleLeft; Cursor = Cursors.Hand; UseMnemonic = false;
        TextChanged += (_, _) => fullName.SetToolTip(this, Text);
    }
    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hovered = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hovered = false; Invalidate(); }
    protected override void OnClick(EventArgs e)
    {
        if (SelectableProject) base.OnClick(e);
    }
    protected override bool ProcessDialogKey(Keys keyData)
    {
        if (keyData == Keys.Enter && SelectableProject) { PerformClick(); return true; }
        return base.ProcessDialogKey(keyData);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(hovered && SelectableProject ? UITheme.Neutral50 : IsCurrent ? UITheme.Neutral100 : BackColor);
        int iconSize = Compact ? 24 : 32, iconX = 12, iconY = (Height - iconSize) / 2;
        using (var fill = new SolidBrush(UITheme.Neutral100)) g.FillEllipse(fill, iconX, iconY, iconSize, iconSize);
        UIIcons.Summary(g, new Rectangle(iconX + 6, iconY + 6, iconSize - 12, iconSize - 12), UITheme.Neutral600);
        int badgeWidth = Compact ? (IsCurrent ? 28 : 0) : Math.Min(140, TextRenderer.MeasureText(badge, UITheme.FontLabel).Width + 20);
        int textX = iconX + iconSize + 12;
        var flags = TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter;
        TextRenderer.DrawText(g, Text, Font, new Rectangle(textX, 8, Math.Max(1, Width - textX - badgeWidth - 24), string.IsNullOrEmpty(subtitle) ? Height - 16 : 26), UITheme.Ink, flags);
        if (!string.IsNullOrEmpty(subtitle)) TextRenderer.DrawText(g, subtitle, UITheme.FontLabel,
            new Rectangle(textX, 34, Math.Max(1, Width - textX - badgeWidth - 24), 22), UITheme.Neutral600, flags);
        if (Compact && IsCurrent) UIIcons.CheckCircle(g, new Rectangle(Width - 30, (Height - 18) / 2, 18, 18), UITheme.Ink);
        else if (!Compact && !string.IsNullOrEmpty(badge))
        {
            var rect = new Rectangle(Width - badgeWidth - 12, (Height - 24) / 2, badgeWidth, 24);
            using var path = UITheme.CreateRoundedRectanglePath(rect, 12);
            using var fill = new SolidBrush(UITheme.Neutral100); g.FillPath(fill, path);
            TextRenderer.DrawText(g, badge, UITheme.FontLabel, rect, UITheme.Neutral700, flags | TextFormatFlags.HorizontalCenter);
        }
        if (ShowDivider) { using var divider = new Pen(UITheme.Neutral200); g.DrawLine(divider, 12, Height - 1, Width - 12, Height - 1); }
        if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(g, new Rectangle(4, 4, Width - 8, Height - 8));
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) fullName.Dispose();
        base.Dispose(disposing);
    }
}
