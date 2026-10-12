using CreatorFlow.Contracts.Invitations;
using CreatorFlow.Services;
using CreatorFlow.Services.Exceptions;
using CreatorFlow.Theme;

namespace CreatorFlow.Forms.Invitations;

/// <summary>
/// Dialog "Lời mời của tôi": người dùng xem các lời mời tham gia Project gửi tới email tài khoản của mình,
/// chọn một lời mời đang chờ rồi Chấp nhận hoặc Từ chối. Giao diện tĩnh nằm trong MyInvitationsDialog.Designer.cs.
/// Dialog chỉ bật/tắt nút theo trạng thái hiển thị; HẠN DÙNG, TRẠNG THÁI và việc TẠO THÀNH VIÊN do API quyết định,
/// thông báo lỗi của API được hiện nguyên văn. Gọi API chạy nền (Task.Run) để không treo giao diện.
/// </summary>
public partial class MyInvitationsDialog : Form
{
    private enum MessageKind { Info, Success, Error }

    private const string PendingStatus = "Pending";

    private readonly InvitationInboxService _service;
    private bool _busy;

    /// <summary>Constructor không tham số CHỈ để Visual Studio Designer mở được form.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public MyInvitationsDialog()
    {
        InitializeComponent();
        _service = null!;
    }

    public MyInvitationsDialog(InvitationInboxService service)
    {
        InitializeComponent();
        _service = service;

        CancelButton = btnClose;
        lvInvitations.SelectedIndexChanged += (_, _) => UpdateButtons();
        Shown += async (_, _) => await LoadInvitationsAsync();
        UpdateButtons();
    }

    // ------------------------------------------------------------------ tải danh sách

    private async Task LoadInvitationsAsync()
    {
        SetBusy(true);
        ShowMessage("Đang tải danh sách lời mời...", MessageKind.Info);
        try
        {
            var invitations = await Task.Run(() => _service.GetMine());
            if (IsDisposed) return;

            lvInvitations.Items.Clear();
            foreach (var invitation in invitations)
                lvInvitations.Items.Add(ToItem(invitation));
            UpdateListTitle();
            SelectFirstPending();
            ShowMessage(invitations.Count == 0 ? "Bạn chưa có lời mời nào." : null, MessageKind.Info);
        }
        catch (UnauthorizedWorkflowActionException ex)
        {
            if (IsDisposed) return;
            ShowMessage(ex.Message, MessageKind.Error); // hết phiên đăng nhập
        }
        catch (ContentValidationException ex)
        {
            if (IsDisposed) return;
            ShowMessage(ex.Message, MessageKind.Error); // vd mất mạng
        }
        catch (InvalidOperationException ex) // API trả 404 (vd API chưa có chức năng này)
        {
            if (IsDisposed) return;
            ShowMessage(ex.Message, MessageKind.Error);
        }
        finally
        {
            if (!IsDisposed) SetBusy(false);
        }
    }

    // ------------------------------------------------------------------ chấp nhận / từ chối

    private async void btnAccept_Click(object? sender, EventArgs e) => await DecideAsync(accept: true);

    private async void btnReject_Click(object? sender, EventArgs e) => await DecideAsync(accept: false);

    private async Task DecideAsync(bool accept)
    {
        if (SelectedInvitation is not { Status: PendingStatus } selected) return;

        ShowMessage(null, MessageKind.Info);
        SetBusy(true);
        try
        {
            MyInvitationResponse updated = await Task.Run(() =>
                accept ? _service.Accept(selected.InvitationId) : _service.Reject(selected.InvitationId));
            if (IsDisposed) return;

            ReplaceItem(updated);
            ShowMessage(accept
                ? $"Đã chấp nhận lời mời vào Project \"{updated.ProjectName}\"."
                : $"Đã từ chối lời mời vào Project \"{updated.ProjectName}\".", MessageKind.Success);
        }
        catch (UnauthorizedWorkflowActionException ex)
        {
            if (IsDisposed) return;
            ShowMessage(ex.Message, MessageKind.Error);
        }
        catch (ContentValidationException ex)
        {
            if (IsDisposed) return;
            // Lời mời đã hết hạn / đã được trả lời ở nơi khác: tải lại để danh sách phản ánh đúng trạng thái, rồi báo lỗi của API.
            string message = ex.Message;
            await LoadInvitationsAsync();
            if (!IsDisposed) ShowMessage(message, MessageKind.Error);
        }
        catch (InvalidOperationException ex) // API trả 404: không có lời mời này
        {
            if (IsDisposed) return;
            ShowMessage(ex.Message, MessageKind.Error);
        }
        finally
        {
            if (!IsDisposed) SetBusy(false);
        }
    }

    // ------------------------------------------------------------------ hiển thị

    private MyInvitationResponse? SelectedInvitation =>
        lvInvitations.SelectedItems.Count == 1 ? lvInvitations.SelectedItems[0].Tag as MyInvitationResponse : null;

    private static ListViewItem ToItem(MyInvitationResponse invitation)
    {
        var item = new ListViewItem(invitation.ProjectName) { Tag = invitation };
        item.SubItems.Add(RoleText(invitation.Role));
        item.SubItems.Add(invitation.InvitedByName);
        item.SubItems.Add(StatusText(invitation.Status));
        item.SubItems.Add(invitation.ExpiresAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"));
        return item;
    }

    private void ReplaceItem(MyInvitationResponse updated)
    {
        for (int i = 0; i < lvInvitations.Items.Count; i++)
        {
            if (lvInvitations.Items[i].Tag is MyInvitationResponse current && current.InvitationId == updated.InvitationId)
            {
                lvInvitations.Items[i] = ToItem(updated);
                lvInvitations.Items[i].Selected = true;
                break;
            }
        }
        UpdateListTitle();
        UpdateButtons();
    }

    private void SelectFirstPending()
    {
        foreach (ListViewItem item in lvInvitations.Items)
        {
            if (item.Tag is MyInvitationResponse { Status: PendingStatus })
            {
                item.Selected = true;
                item.EnsureVisible();
                return;
            }
        }
    }

    private static string RoleText(string role) => role switch
    {
        "Manager" => "Manager",
        "Creator" => "Creator",
        _ => role,
    };

    private static string StatusText(string status) => status switch
    {
        "Pending" => "Đang chờ",
        "Accepted" => "Đã chấp nhận",
        "Rejected" => "Đã từ chối",
        "Expired" => "Hết hạn",
        "Cancelled" => "Đã huỷ",
        _ => status,
    };

    private void UpdateListTitle()
    {
        int pending = lvInvitations.Items.Cast<ListViewItem>()
            .Count(i => i.Tag is MyInvitationResponse { Status: PendingStatus });
        lblList.Text = $"Lời mời ({lvInvitations.Items.Count}) — đang chờ: {pending}";
    }

    private void UpdateButtons()
    {
        bool canDecide = !_busy && SelectedInvitation is { Status: PendingStatus };
        btnAccept.Enabled = canDecide;
        btnReject.Enabled = canDecide;
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        UseWaitCursor = busy;
        UpdateButtons();
    }

    private void ShowMessage(string? message, MessageKind kind)
    {
        _lblMessage.ForeColor = kind switch
        {
            MessageKind.Error => UITheme.Danger,
            MessageKind.Success => UITheme.Success,
            _ => UITheme.Neutral600,
        };
        _lblMessage.Text = message ?? string.Empty;
    }
}