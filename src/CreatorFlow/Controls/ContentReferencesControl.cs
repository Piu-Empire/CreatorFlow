using CreatorFlow.ApiClients;
using CreatorFlow.Models;
using CreatorFlow.Services.Backends;
using CreatorFlow.Services.Exceptions;
using CreatorFlow.Theme;
using System.Diagnostics;

namespace CreatorFlow.Controls;

/// <summary>Hiển thị và quản lý URL tham khảo của Content qua backend API.</summary>
public sealed partial class ContentReferencesControl : UserControl
{
    private readonly List<ContentReference> _pendingAdditions = new();
    private readonly HashSet<long> _pendingDeletions = new();
    private IContentDetailBackend? _backend;
    private long? _contentId;
    private bool _canManage;
    private long _nextDraftReferenceId = -1;

    public event EventHandler? PendingChangesChanged;

    public bool HasPendingChanges => _pendingAdditions.Count > 0 || _pendingDeletions.Count > 0;

    public ContentReferencesControl()
    {
        InitializeComponent(); // dựng control: xem ContentReferencesControl.Designer.cs (kéo thả được)
        WireEvents();
    }

    /// <summary>Các sự kiện dùng lambda — đặt ngoài InitializeComponent vì Designer không đọc được lambda.</summary>
    private void WireEvents()
    {
        _btnAdd.Click += (_, _) => AddReference();
        _addButtonHost.Resize += (_, _) => _btnAdd.Location = new Point(Math.Max(0, _addButtonHost.ClientSize.Width - _btnAdd.Width), 0);
        _references.SelectedIndexChanged += (_, _) => UpdateActions();
        _references.DoubleClick += (_, _) => OpenSelectedReference();
        _btnDelete.Click += (_, _) => DeleteSelectedReference();
        _btnOpen.Click += (_, _) => OpenSelectedReference();
    }

    public void Initialize(IContentDetailBackend? backend) => _backend = backend;

    public void LoadReferences(long contentId)
    {
        _contentId = contentId;
        _pendingAdditions.Clear();
        _pendingDeletions.Clear();
        _nextDraftReferenceId = -1;
        _references.Items.Clear();
        _canManage = false;
        _lblStatus.Text = "Đang tải reference...";
        UpdateActions();

        if (_backend is null)
        {
            _lblStatus.Text = "References cần kết nối API.";
            return;
        }

        try
        {
            ContentReferenceList result = _backend.GetReferences(contentId);
            _canManage = result.CanManage;
            DisplayReferences(result.References);
            _lblStatus.Text = result.References.Count == 0 ? "Chưa có reference." : $"{result.References.Count} reference";
        }
        catch (Exception ex) when (IsExpectedApiError(ex))
        {
            _lblStatus.Text = "Không tải được reference. Content vẫn được giữ nguyên; hãy thử tải lại Content Detail.";
        }
        UpdateActions();
    }

    public void ClearReferences()
    {
        _contentId = null;
        _canManage = false;
        _pendingAdditions.Clear();
        _pendingDeletions.Clear();
        _nextDraftReferenceId = -1;
        _references.Items.Clear();
        _lblStatus.Text = string.Empty;
        UpdateActions();
        PendingChangesChanged?.Invoke(this, EventArgs.Empty);
    }

    private void AddReference()
    {
        if (_backend is null || _contentId is not long contentId) return;
        string displayName = _txtName.Text.Trim();
        if (displayName.Length is 0 or > 255)
        {
            _lblStatus.Text = "Tên reference phải từ 1 đến 255 ký tự.";
            return;
        }

        string rawUrl = _txtUrl.Text.Trim();
        if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            _lblStatus.Text = "URL phải bắt đầu bằng http:// hoặc https://.";
            return;
        }

        var reference = new ContentReference(_nextDraftReferenceId--, contentId, displayName,
            uri.AbsoluteUri, null, DateTime.UtcNow);
        _pendingAdditions.Add(reference);
        AddReferenceRow(reference);
        _txtName.Clear();
        _txtUrl.Clear();
        UpdatePendingState();
    }

    private void DeleteSelectedReference()
    {
        if (_backend is null || _contentId is null || SelectedReference() is not { } reference) return;
        if (MessageBox.Show(this, $"Xóa reference ‘{reference.DisplayName}’?", "Xác nhận xóa",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        if (reference.ReferenceId < 0)
            _pendingAdditions.RemoveAll(item => item.ReferenceId == reference.ReferenceId);
        else
            _pendingDeletions.Add(reference.ReferenceId);

        _references.Items.Remove(_references.SelectedItems[0]);
        UpdatePendingState();
    }

    public void SavePendingChanges()
    {
        if (!HasPendingChanges) return;
        if (_backend is null || _contentId is not long contentId)
            throw new InvalidOperationException("References are not connected to the API.");

        ContentReferenceList saved = _backend.SaveReferenceChanges(contentId,
            _pendingAdditions.Select(item => new ContentReferenceAddition(item.DisplayName, item.ResourceUrl)).ToList(),
            _pendingDeletions.ToList());
        _canManage = saved.CanManage;
        _pendingAdditions.Clear();
        _pendingDeletions.Clear();
        _nextDraftReferenceId = -1;
        DisplayReferences(saved.References);
        _lblStatus.Text = saved.References.Count == 0 ? "Chưa có reference." : $"{saved.References.Count} reference";
        UpdateActions();
        PendingChangesChanged?.Invoke(this, EventArgs.Empty);
    }

    private void DisplayReferences(IEnumerable<ContentReference> references)
    {
        _references.Items.Clear();
        foreach (ContentReference reference in references) AddReferenceRow(reference);
    }

    private void AddReferenceRow(ContentReference reference)
    {
        var item = new ListViewItem(reference.DisplayName) { Tag = reference };
        item.SubItems.Add(reference.ResourceUrl);
        _references.Items.Add(item);
    }

    private void UpdatePendingState()
    {
        if (HasPendingChanges)
            _lblStatus.Text = $"Có thay đổi reference chưa lưu ({_pendingAdditions.Count} thêm, {_pendingDeletions.Count} xóa).";
        else
            _lblStatus.Text = _references.Items.Count == 0 ? "Chưa có reference." : $"{_references.Items.Count} reference";
        UpdateActions();
        PendingChangesChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OpenSelectedReference()
    {
        if (SelectedReference() is not { } reference) return;
        if (!Uri.TryCreate(reference.ResourceUrl, UriKind.Absolute, out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            _lblStatus.Text = "URL reference không hợp lệ. Reference và Content vẫn được giữ nguyên.";
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
            _lblStatus.Text = "Đã gửi link tới trình duyệt mặc định.";
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or UriFormatException)
        {
            _lblStatus.Text = "Không mở được link. Reference và Content vẫn được giữ nguyên.";
        }
    }

    private ContentReference? SelectedReference() =>
        _references.SelectedItems.Count == 0 ? null : _references.SelectedItems[0].Tag as ContentReference;

    private void UpdateActions()
    {
        bool selected = SelectedReference() is not null;
        _btnAdd.Enabled = _canManage && _contentId.HasValue;
        _btnDelete.Enabled = _canManage && selected;
        _btnOpen.Enabled = selected;
        _txtName.Enabled = _canManage;
        _txtUrl.Enabled = _canManage;
    }

    private static bool IsExpectedApiError(Exception error) => error is ContentValidationException or
        UnauthorizedWorkflowActionException or InvalidOperationException or ApiException or ApiBusinessException;
}
