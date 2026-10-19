using CreatorFlow.Theme;

namespace CreatorFlow.Controls;

/// <summary>Ô tìm kiếm theo style guide: khung trắng viền xám bo 8px + icon kính lúp.</summary>
public class SearchBox : RoundedPanel
{
    private readonly TextBox _box = new();

    /// <summary>Từ khóa đã trim (rỗng nếu chỉ có khoảng trắng).</summary>
    public string QueryText => _box.Text.Trim();

    public string PlaceholderText
    {
        get => _box.PlaceholderText;
        set => _box.PlaceholderText = value;
    }

    /// <summary>Xóa nội dung ô tìm kiếm (dùng khi "Xóa bộ lọc").</summary>
    public void Clear() => _box.Clear();

    public new event EventHandler? TextChanged;

    /// <summary>Nhấn Enter trong ô tìm kiếm.</summary>
    public event EventHandler? SearchSubmitted;

    public SearchBox()
    {
        FillColor = UITheme.White;
        BorderColor = UITheme.Neutral300;
        Radius = 8;
        _box.BorderStyle = BorderStyle.None;
        _box.BackColor = UITheme.White;
        _box.Font = UITheme.FontBody;
        _box.Location = new Point(36, 8);
        _box.Width = Width - 48;
        _box.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _box.TextChanged += (_, e) => TextChanged?.Invoke(this, e);
        _box.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                SearchSubmitted?.Invoke(this, EventArgs.Empty);
            }
        };
        Controls.Add(_box);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        UIIcons.Search(e.Graphics, new Rectangle(11, (Height - 18) / 2, 18, 18), UITheme.Neutral400);
    }
}
