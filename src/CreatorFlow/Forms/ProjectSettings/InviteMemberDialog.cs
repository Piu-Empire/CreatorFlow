using System.Net.Mail;
using CreatorFlow.Contracts.Invitations;
using CreatorFlow.Services;
using CreatorFlow.Services.Exceptions;
using CreatorFlow.Theme;

namespace CreatorFlow.Forms.ProjectSettings;

/// <summary>
/// Dialog "Mời thành viên" (SCRUM-26): Owner nhập email + chọn vai trò (Manager / Creator) để gửi lời mời vào Project,
/// và xem các lời mời đã gửi kèm trạng thái. Giao diện tĩnh nằm trong InviteMemberDialog.Designer.cs.
/// Dialog chỉ kiểm tra nhanh email để báo lỗi ngay; QUYỀN (chỉ Owner), trùng thành viên, trùng lời mời do API quyết định
/// và thông báo lỗi của API được hiện nguyên văn dưới ô nhập. Gọi API chạy nền (Task.Run) để không treo giao diện.
/// </summary>
public partial class InviteMemberDialog : Form
{
    private enum MessageKind { Info, Success, Error }

    private readonly ProjectInvitationService _service;
    private readonly long _projectId;
    private bool _canInvite = true;

    /// <summary>Constructor không tham số CHỈ để Visual Studio Designer mở được form.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public InviteMemberDialog()
    {
        InitializeComponent();
        _service = null!;
    }

    public InviteMemberDialog(ProjectInvitationService service, long projectId, string projectName)
    {
        InitializeComponent();
        _service = service;
        _projectId = projectId;

        lblSub.Text = string.IsNullOrWhiteSpace(projectName) ? $"Project #{projectId}" : projectName;
        cboRole.Items.Add("Creator");
        cboRole.Items.Add("Manager");
        cboRole.SelectedIndex = 0;

        AcceptButton = btnInvite;
        CancelButton = btnClose;
        Shown += async (_, _) => await LoadInvitationsAsync();
    }

    // ------------------------------------------------------------------ tải danh sách

    private async Task LoadInvitationsAsync()
    {
        SetBusy(true);
        ShowMessage("Đang tải danh sách lời mời...", MessageKind.Info);
        try
        {
            var invitations = await Task.Run(() => _service.GetInvitations(_projectId));
            if (IsDisposed) return;

            lvInvitations.Items.Clear();
            foreach (var invitation in invitations)
                lvInvitations.Items.Add(ToItem(invitation));
            UpdateListTitle();
            ShowMessage(null, MessageKind.Info);
        }
        catch (UnauthorizedWorkflowActionException ex)
        {
            if (IsDisposed) return;
            _canInvite = false; // không phải Owner / không thuộc Project / hết phiên: khoá việc gửi lời mời
            ShowMessage(ex.Message, MessageKind.Error);
        }
        catch (ContentValidationException ex)
        {
            if (IsDisposed) return;
            ShowMessage(ex.Message, MessageKind.Error); // vd mất mạng: vẫn cho thử gửi, API sẽ kiểm tra lại
        }
        catch (InvalidOperationException ex) // API trả 404 (vd API chưa có chức năng mời, Project không tồn tại)
        {
            if (IsDisposed) return;
            ShowMessage(ex.Message, MessageKind.Error);
        }
        finally
        {
            if (!IsDisposed) SetBusy(false);
        }
    }

    // ------------------------------------------------------------------ gửi lời mời

    private async void btnInvite_Click(object? sender, EventArgs e)
    {
        ShowMessage(null, MessageKind.Info);

        string email = txtEmail.Text.Trim();
        if (email.Length == 0)
        {
            ShowMessage("Vui lòng nhập email người được mời.", MessageKind.Error);
            txtEmail.Focus();
            return;
        }
        if (!MailAddress.TryCreate(email, out MailAddress? parsed) || !string.Equals(parsed.Address, email, StringComparison.OrdinalIgnoreCase))
        {
            ShowMessage("Email không hợp lệ.", MessageKind.Error);
            txtEmail.Focus();
            return;
        }

        string role = (string)cboRole.SelectedItem!;
        SetBusy(true);
        try
        {
            InvitationResponse created = await Task.Run(() => _service.Invite(_projectId, email, role));
            if (IsDisposed) return;

            lvInvitations.Items.Insert(0, ToItem(created));
            UpdateListTitle();
            txtEmail.Clear();
            ShowMessage($"Đã gửi lời mời tới {created.InvitedEmail} ({RoleText(created.Role)}) — trạng thái: {StatusText(created.Status)}.", MessageKind.Success);
        }
        catch (UnauthorizedWorkflowActionException ex)
        {
            if (IsDisposed) return;
            _canInvite = false;
            ShowMessage(ex.Message, MessageKind.Error);
        }
        catch (ContentValidationException ex)
        {
            if (IsDisposed) return;
            ShowMessage(ex.Message, MessageKind.Error); // vd: đã là thành viên, đã có lời mời đang chờ, role không hợp lệ
            txtEmail.Focus();
        }
        catch (InvalidOperationException ex) // API trả 404 (vd API chưa có chức năng mời, Project không tồn tại)
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

    private static ListViewItem ToItem(InvitationResponse invitation)
    {
        var item = new ListViewItem(invitation.InvitedEmail);
        item.SubItems.Add(RoleText(invitation.Role));
        item.SubItems.Add(StatusText(invitation.Status));
        item.SubItems.Add(invitation.ExpiresAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"));
        return item;
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

    private void UpdateListTitle() => lblList.Text = $"Lời mời đã gửi ({lvInvitations.Items.Count})";

    private void SetBusy(bool busy)
    {
        txtEmail.Enabled = !busy && _canInvite;
        cboRole.Enabled = !busy && _canInvite;
        btnInvite.Enabled = !busy && _canInvite;
        UseWaitCursor = busy;
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