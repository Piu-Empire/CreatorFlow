using System.Drawing.Drawing2D;
using CreatorFlow.Theme;

namespace CreatorFlow.Helpers;

internal static class UiTheme
{
    public static readonly Color Heading = UITheme.Ink;
    public static readonly Color Muted = UITheme.Neutral600;
    public static readonly Color Border = UITheme.Neutral200;
    public static readonly Color FieldBorder = UITheme.Neutral300;
    public static readonly Color Surface = UITheme.Neutral50;
    public static readonly Color Error = UITheme.Danger;

    public static TableLayoutPanel CreateFormBody() => new()
    {
        AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
        Dock = DockStyle.Top, ColumnCount = 1, RowCount = 0, Margin = Padding.Empty
    };

    public static void AddRow(TableLayoutPanel body, Control control, int bottomGap = 16)
    {
        int row = body.RowCount++;
        body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        control.Dock = DockStyle.Top;
        control.TabIndex = row;
        control.Margin = new Padding(0, 0, 0, bottomGap);
        body.Controls.Add(control, 0, row);
    }

    public static Panel AddField(TableLayoutPanel body, string caption, TextBox textBox)
    {
        var field = CreateFormBody();
        field.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var label = new Label
        {
            Text = caption, AutoSize = true, ForeColor = Muted,
            Font = new Font("Segoe UI", 8.25f, FontStyle.Bold)
        };
        var border = new Panel
        {
            Name = "fieldBorder", Height = 40, BackColor = Color.White,
            Padding = new Padding(12, 10, 12, 8)
        };
        textBox.Dock = DockStyle.Fill;
        textBox.BorderStyle = BorderStyle.None;
        textBox.Font = new Font("Segoe UI", 9.75f);
        textBox.ForeColor = Heading;
        textBox.BackColor = Color.White;
        textBox.AccessibleName = caption;
        border.Controls.Add(textBox);
        var error = new Label
        {
            Name = "fieldError", AutoSize = true, Visible = false,
            ForeColor = Error, Font = new Font("Segoe UI", 8.25f)
        };
        field.SizeChanged += (_, _) => error.MaximumSize = new Size(Math.Max(1, field.ClientSize.Width), 0);
        AddRow(field, label, 4);
        AddRow(field, border, 4);
        AddRow(field, error, 0);
        AddRow(body, field);
        StyleField(border, textBox);
        textBox.TextChanged += (_, _) => error.Visible = false;
        return field;
    }

    public static void SetFieldError(Panel field, string? message)
    {
        if (field.Controls["fieldError"] is Label label)
        {
            label.Text = message ?? string.Empty;
            label.Visible = !string.IsNullOrEmpty(message);
        }
        if (field.Controls["fieldBorder"] is Panel border)
        {
            border.ForeColor = message is null ? FieldBorder : Error;
            border.Invalidate();
        }
    }

    public static LinkLabel CreateLink(string text) => new()
    {
        Text = text, AutoSize = true, LinkColor = Heading,
        ActiveLinkColor = Color.Black, VisitedLinkColor = Heading,
        LinkBehavior = LinkBehavior.HoverUnderline,
        Font = new Font("Segoe UI", 9.75f)
    };

    public static void StyleButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(38, 38, 38);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(64, 64, 64);
        button.BackColor = Color.Black;
        button.ForeColor = Color.White;
        button.UseVisualStyleBackColor = false;
        RoundCorners(button, 8);
    }

    public static void StyleAvatar(Control avatar) => RoundCorners(avatar, 20);

    public static void StyleCard(Panel card)
    {
        RoundCorners(card, 12);
        card.Paint += (_, e) => DrawBorder(e.Graphics, card, Border, 12, 1);
    }

    public static void StyleField(Panel container, TextBox textBox)
    {
        container.ForeColor = FieldBorder;
        RoundCorners(container, 8);
        container.Paint += (_, e) => DrawBorder(
            e.Graphics,
            container,
            container.ForeColor == Error ? Error : textBox.Focused ? Color.Black : FieldBorder,
            8,
            textBox.Focused ? 2 : 1);
        textBox.Enter += (_, _) => container.Invalidate();
        textBox.Leave += (_, _) => container.Invalidate();
        textBox.TextChanged += (_, _) =>
        {
            container.ForeColor = FieldBorder;
            container.Invalidate();
        };
    }

    private static void RoundCorners(Control control, int radius)
    {
        void UpdateRegion()
        {
            if (control.Width <= 0 || control.Height <= 0)
            {
                return;
            }

            using GraphicsPath path = RoundedRectangle(
                new RectangleF(0, 0, control.Width, control.Height),
                radius * control.DeviceDpi / 96f);
            Region? previous = control.Region;
            control.Region = new Region(path);
            previous?.Dispose();
        }

        control.SizeChanged += (_, _) => UpdateRegion();
        control.DpiChangedAfterParent += (_, _) => UpdateRegion();
        UpdateRegion();
    }

    private static void DrawBorder(Graphics graphics, Control control, Color color, int radius, int width)
    {
        float scale = control.DeviceDpi / 96f;
        float stroke = width * scale;
        using GraphicsPath path = RoundedRectangle(
            new RectangleF(stroke / 2, stroke / 2,
                control.Width - stroke, control.Height - stroke), radius * scale);
        using var pen = new Pen(color, stroke);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.DrawPath(pen, path);
    }

    private static GraphicsPath RoundedRectangle(RectangleF bounds, float radius)
    {
        var path = new GraphicsPath();
        float diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
