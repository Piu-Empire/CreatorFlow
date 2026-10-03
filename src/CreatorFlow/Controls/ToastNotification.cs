using System.Drawing.Drawing2D;
using CreatorFlow.Theme;

namespace CreatorFlow.Controls;

/// <summary>
/// Toast nổi góc trên phải. Loại bỏ emoji, chỉ dùng dấu checkmark ASCII.
/// </summary>
public class ToastNotification : Control
{
    private string _message = string.Empty;
    private System.Windows.Forms.Timer? _timer;

    public ToastNotification()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        Visible = false;
        Height  = 44;
        Width   = 340;
        Font    = UITheme.FontBodyBold;
    }

    public void ShowToast(Control parent, string message)
    {
        _message = message;
        if (Parent != parent)
        {
            parent.Controls.Add(this);
            BringToFront();
        }

        Location = new Point(parent.Width - Width - 32, 24);
        Visible  = true;
        BringToFront();
        Invalidate();

        _timer?.Stop();
        _timer?.Dispose();

        _timer = new System.Windows.Forms.Timer { Interval = 2600 };
        _timer.Tick += (_, _) =>
        {
            _timer.Stop();
            Visible = false;
        };
        _timer.Start();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);

        using (var bgBrush = new SolidBrush(UITheme.Black))
        using (var path = UITheme.CreateRoundedRectanglePath(bounds, 8))
        {
            g.FillPath(bgBrush, path);
        }

        // Dấu tick ASCII 'V'
        using (var checkFont  = new Font("Segoe UI", 12F, FontStyle.Bold))
        using (var checkBrush = new SolidBrush(UITheme.Success))
        {
            g.DrawString("V", checkFont, checkBrush, 14, 10);
        }

        using (var textBrush = new SolidBrush(UITheme.White))
        {
            var textRect = new RectangleF(36, 0, Width - 46, Height);
            var sf = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisWord };
            g.DrawString(_message, Font, textBrush, textRect, sf);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer?.Stop();
            _timer?.Dispose();
        }
        base.Dispose(disposing);
    }
}
