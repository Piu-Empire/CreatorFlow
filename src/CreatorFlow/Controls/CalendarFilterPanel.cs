using CreatorFlow.Models.Enums;
using CreatorFlow.Theme;

namespace CreatorFlow.Controls;

/// <summary>
/// Nội dung panel "Bộ lọc" của màn Lịch Content: Project / Creator / Platform / Status.
/// Chỉ dựng UI và đọc lựa chọn; CalendarControl quyết định lọc và vẽ lại lịch.
/// </summary>
public sealed class CalendarFilterPanel : UserControl
{
    private const int Pad = 12;
    private const int FieldW = 240;
    private const int LabelH = 18;
    private const int ComboH = 28;
    private const int RowGap = 10;

    private readonly ComboBox _cboProject = NewCombo();
    private readonly ComboBox _cboCreator = NewCombo();
    private readonly ComboBox _cboPlatform = NewCombo();
    private readonly ComboBox _cboStatus = NewCombo();
    private readonly RoundedButton _btnClear;

    private readonly List<long?> _projectIds = new() { null };
    private readonly List<long?> _creatorIds = new() { null };
    private List<ContentStatus> _statusValues = new();

    private bool _suppress;

    /// <summary>Một tiêu chí vừa đổi (CalendarControl lọc lại và cập nhật badge).</summary>
    public event EventHandler? FiltersChanged;

    public CalendarFilterPanel()
    {
        BackColor = UITheme.White;
        Width = FieldW + Pad * 2;

        _btnClear = new RoundedButton
        {
            Text = "Xóa bộ lọc",
            Style = RoundButtonStyle.Secondary,
            Size = new Size(FieldW, 34),
        };
        _btnClear.Click += (_, _) => Reset();

        _cboProject.SelectedIndexChanged += (_, _) => RaiseChanged();
        _cboCreator.SelectedIndexChanged += (_, _) => RaiseChanged();
        _cboPlatform.SelectedIndexChanged += (_, _) => RaiseChanged();
        _cboStatus.SelectedIndexChanged += (_, _) => RaiseChanged();

        int y = Pad;
        AddField("Project", _cboProject, ref y);
        AddField("Creator", _cboCreator, ref y);
        AddField("Platform", _cboPlatform, ref y);
        AddField("Status", _cboStatus, ref y);

        _btnClear.Location = new Point(Pad, y + 2);
        Controls.Add(_btnClear);
        Height = y + 2 + _btnClear.Height + Pad;
    }

    public long? ProjectId => SelectedId(_cboProject, _projectIds);
    public long? CreatorId => SelectedId(_cboCreator, _creatorIds);
    public string? Platform => _cboPlatform.SelectedIndex <= 0 ? null : _cboPlatform.SelectedItem as string;

    public ContentStatus? Status =>
        _cboStatus.SelectedIndex > 0 && _cboStatus.SelectedIndex < _statusValues.Count
            ? _statusValues[_cboStatus.SelectedIndex]
            : null;

    /// <summary>Số tiêu chí đang bật (để hiện badge trên nút "Bộ lọc").</summary>
    public int ActiveCount =>
        (ProjectId is null ? 0 : 1) + (CreatorId is null ? 0 : 1) +
        (Platform is null ? 0 : 1) + (Status is null ? 0 : 1);

    /// <summary>Danh sách Status cố định (mục đầu tiên là "Tất cả").</summary>
    public void SetStatuses(IReadOnlyList<(string Label, ContentStatus Status)> items)
    {
        _suppress = true;
        try
        {
            _statusValues = items.Skip(1).Select(i => i.Status).ToList();
            _cboStatus.Items.Clear();
            _cboStatus.Items.AddRange(items.Select(i => i.Label).ToArray());
            _cboStatus.SelectedIndex = 0;
        }
        finally
        {
            _suppress = false;
        }
    }

    /// <summary>Danh sách Platform cố định (mục đầu tiên là "Tất cả").</summary>
    public void SetPlatforms(IReadOnlyList<string> platforms)
    {
        _suppress = true;
        try
        {
            _cboPlatform.Items.Clear();
            _cboPlatform.Items.AddRange(platforms.ToArray());
            _cboPlatform.SelectedIndex = 0;
        }
        finally
        {
            _suppress = false;
        }
    }

    /// <summary>Dựng lại danh sách Project từ dữ liệu, giữ lựa chọn hiện tại nếu còn.</summary>
    public void SetProjects(IReadOnlyList<(long Id, string Name)> projects)
    {
        long? selected = ProjectId;
        _suppress = true;
        try
        {
            _projectIds.Clear();
            _projectIds.Add(null);
            _cboProject.Items.Clear();
            _cboProject.Items.Add("Tất cả project");
            foreach ((long id, string name) in projects)
            {
                _projectIds.Add(id);
                _cboProject.Items.Add(name);
            }
            int index = selected is { } value ? _projectIds.IndexOf(value) : 0;
            _cboProject.SelectedIndex = index >= 0 ? index : 0;
        }
        finally
        {
            _suppress = false;
        }
    }

    /// <summary>Dựng lại danh sách Creator từ dữ liệu, giữ lựa chọn hiện tại nếu còn.</summary>
    public void SetCreators(IReadOnlyList<(long Id, string Name)> creators)
    {
        long? selected = CreatorId;
        _suppress = true;
        try
        {
            _creatorIds.Clear();
            _creatorIds.Add(null);
            _cboCreator.Items.Clear();
            _cboCreator.Items.Add("Tất cả creator");
            foreach ((long id, string name) in creators)
            {
                _creatorIds.Add(id);
                _cboCreator.Items.Add(name);
            }
            int index = selected is { } value ? _creatorIds.IndexOf(value) : 0;
            _cboCreator.SelectedIndex = index >= 0 ? index : 0;
        }
        finally
        {
            _suppress = false;
        }
    }

    /// <summary>Đưa mọi tiêu chí về "Tất cả" và báo cho CalendarControl lọc lại.</summary>
    public void Reset()
    {
        _suppress = true;
        try
        {
            _cboProject.SelectedIndex = 0;
            _cboCreator.SelectedIndex = 0;
            _cboPlatform.SelectedIndex = 0;
            _cboStatus.SelectedIndex = 0;
        }
        finally
        {
            _suppress = false;
        }
        RaiseChanged();
    }

    private void RaiseChanged()
    {
        if (!_suppress) FiltersChanged?.Invoke(this, EventArgs.Empty);
    }

    private void AddField(string label, ComboBox combo, ref int y)
    {
        Controls.Add(new Label
        {
            Text = label,
            Font = UITheme.FontLabelBold,
            ForeColor = UITheme.Neutral600,
            AutoSize = true,
            Location = new Point(Pad, y),
        });
        combo.Location = new Point(Pad, y + LabelH);
        combo.Size = new Size(FieldW, ComboH);
        Controls.Add(combo);
        y += LabelH + ComboH + RowGap;
    }

    private static long? SelectedId(ComboBox combo, List<long?> ids) =>
        combo.SelectedIndex > 0 && combo.SelectedIndex < ids.Count ? ids[combo.SelectedIndex] : null;

    private static ComboBox NewCombo() => new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        FlatStyle = FlatStyle.Flat,
        Font = UITheme.FontBody,
        BackColor = UITheme.White,
        ForeColor = UITheme.Ink,
    };
}
