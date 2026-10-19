using System.Drawing.Drawing2D;
using CreatorFlow.Models;
using CreatorFlow.Theme;

namespace CreatorFlow.Controls;

/// <summary>
/// Panel thả xuống hiển thị danh sách thông báo nghiệp vụ (SCRUM-50) dưới icon Bell.
/// Control chỉ hiển thị; không gọi API trực tiếp — BoardForm xử lý dữ liệu và sự kiện.
/// </summary>
public partial class NotificationPanelControl : UserControl
{
    private readonly Label _titleLabel;
    private readonly Label _emptyLabel;
    private readonly LinkLabel _markAllLink;
    private readonly Panel _itemsHost;
    private readonly Panel _scrollHost;

    public event EventHandler<long>? ItemClicked;
    public event EventHandler? MarkAllRequested;
    public event EventHandler? CloseRequested;

    public NotificationPanelControl()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        Size = new Size(360, 420);
        BackColor = UITheme.White;
        Padding = new Padding(0);

        _titleLabel = new Label
        {
            Text = "Notifications",
            Font = UITheme.FontBodyBold,
            ForeColor = UITheme.Ink,
            AutoSize = false,
            Height = 24,
            Dock = DockStyle.Top,
            Padding = new Padding(16, 12, 16, 4),
        };

        _markAllLink = new LinkLabel
        {
            Text = "Mark all as read",
            Font = UITheme.FontLabel,
            LinkColor = UITheme.Neutral600,
            ActiveLinkColor = UITheme.Ink,
            AutoSize = true,
            Dock = DockStyle.Top,
            Padding = new Padding(16, 0, 16, 8),
        };
        _markAllLink.LinkClicked += (_, _) => MarkAllRequested?.Invoke(this, EventArgs.Empty);

        _scrollHost = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = UITheme.White,
        };
        _itemsHost = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = UITheme.White,
        };
        _scrollHost.Controls.Add(_itemsHost);

        _emptyLabel = new Label
        {
            Text = "No notifications.",
            Font = UITheme.FontBody,
            ForeColor = UITheme.Neutral600,
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Visible = false,
        };

        Controls.Add(_emptyLabel);
        Controls.Add(_scrollHost);
        Controls.Add(_markAllLink);
        Controls.Add(_titleLabel);

        // Click outside handled by BoardForm; clicking the panel itself should not close.
        Paint += (_, e) =>
        {
            using var pen = new Pen(UITheme.Neutral200, 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        };
    }

    public void SetItems(IReadOnlyList<NotificationItem> items, bool schemaReady = true, string? schemaMessage = null)
    {
        _itemsHost.Controls.Clear();
        if (!schemaReady)
        {
            _emptyLabel.Text = schemaMessage ?? "Notifications require database migration 09_notifications.sql.";
            _emptyLabel.Visible = true;
            _scrollHost.Visible = false;
            _markAllLink.Visible = false;
            return;
        }
        _markAllLink.Visible = true;
        _emptyLabel.Text = "No notifications.";
        _emptyLabel.Visible = items.Count == 0;
        _scrollHost.Visible = items.Count > 0;

        int y = 0;
        foreach (NotificationItem item in items)
        {
            var row = BuildRow(item);
            row.Location = new Point(0, y);
            row.Width = _itemsHost.Width > 0 ? _itemsHost.Width : Width;
            _itemsHost.Controls.Add(row);
            y += row.Height;
        }
        _itemsHost.Height = y;
    }

    private Panel BuildRow(NotificationItem item)
    {
        var row = new Panel
        {
            Height = 74,
            Dock = DockStyle.Top,
            Padding = new Padding(16, 10, 16, 10),
            BackColor = item.IsRead ? UITheme.White : UITheme.Neutral50,
            Cursor = Cursors.Hand,
            Tag = item.NotificationId,
        };

        var unreadDot = new Panel
        {
            Size = new Size(8, 8),
            Location = new Point(6, 14),
            BackColor = item.IsRead ? Color.Transparent : UITheme.Danger,
        };
        if (!item.IsRead)
        {
            unreadDot.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var b = new SolidBrush(UITheme.Danger);
                e.Graphics.FillEllipse(b, 0, 0, 7, 7);
            };
        }

        var title = new Label
        {
            Text = item.Title,
            Font = UITheme.FontBodyBold,
            ForeColor = UITheme.Ink,
            AutoSize = false,
            Location = new Point(22, 8),
            Size = new Size(Width - 50, 20),
        };
        var message = new Label
        {
            Text = item.Message,
            Font = UITheme.FontLabel,
            ForeColor = UITheme.Neutral600,
            AutoSize = false,
            Location = new Point(22, 28),
            Size = new Size(Width - 50, 28),
        };
        var time = new Label
        {
            Text = item.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
            Font = UITheme.FontLabel,
            ForeColor = UITheme.Neutral400,
            AutoSize = false,
            Location = new Point(22, 56),
            Size = new Size(Width - 50, 14),
        };

        row.Controls.Add(unreadDot);
        row.Controls.Add(title);
        row.Controls.Add(message);
        row.Controls.Add(time);

        row.Click += (_, _) => ItemClicked?.Invoke(this, item.NotificationId);
        foreach (Control c in row.Controls) c.Click += (_, _) => ItemClicked?.Invoke(this, item.NotificationId);

        // Hover effect
        row.MouseEnter += (_, _) => row.BackColor = UITheme.Neutral100;
        row.MouseLeave += (_, _) => row.BackColor = item.IsRead ? UITheme.White : UITheme.Neutral50;

        return row;
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        foreach (Control row in _itemsHost.Controls)
            row.Width = _itemsHost.Width > 0 ? _itemsHost.Width : Width;
    }
}
