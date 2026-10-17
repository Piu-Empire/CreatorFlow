using System.Drawing.Drawing2D;
using System.Net;
using CreatorFlow.ApiClients;
using CreatorFlow.Contracts.Projects;
using CreatorFlow.Controls;
using CreatorFlow.Models.Enums;
using CreatorFlow.Services;
using CreatorFlow.Theme;

namespace CreatorFlow.Forms.ProjectSettings;

/// <summary>Owner view for the active members of one Project and their project-scoped roles.</summary>
public sealed partial class ProjectMembersDialog : Form
{
    private static readonly Color[] AvatarColors =
    [
        Color.FromArgb(99, 102, 241),
        Color.FromArgb(16, 185, 129),
        Color.FromArgb(14, 165, 233),
        Color.FromArgb(245, 158, 11),
        Color.FromArgb(236, 72, 153),
    ];

    private readonly ProjectApiFacade projects;
    private readonly long projectId;
    private readonly CancellationTokenSource lifetime = new();
    private readonly ToolTip toolTip = new();
    private int ownerCount;
    private int sequence;
    private bool released;

    public ProjectMembersDialog(ProjectApiFacade projects, long projectId, string projectName)
    {
        this.projects = projects;
        this.projectId = projectId;
        InitializeComponent();
        Font = UITheme.FontBody;
        BackColor = UITheme.White;
        AccessibleName = Text;
        headingLabel.Font = UITheme.FontPageTitle;
        headingLabel.ForeColor = UITheme.Ink;
        projectLabel.Text = string.IsNullOrWhiteSpace(projectName) ? $"Project #{projectId}" : projectName;
        projectLabel.Font = UITheme.FontBodyBold;
        noticeText.Font = UITheme.FontBody;
        message.ForeColor = UITheme.Neutral600;
        reload.Style = RoundButtonStyle.Secondary;
        close.Style = RoundButtonStyle.Primary;

        reload.Click += async (_, _) => await LoadMembersAsync();
        close.Click += (_, _) => Close();
        Shown += async (_, _) => await LoadMembersAsync();
        FormClosed += (_, _) => lifetime.Cancel();
    }

    private bool IsCurrent(int version, long generation) => !IsDisposed && !lifetime.IsCancellationRequested &&
        version == sequence && generation == projects.Context.Generation && projects.Context.Current?.ProjectId == projectId;

    private async Task LoadMembersAsync()
    {
        int version = ++sequence;
        long generation = projects.Context.Generation;
        SetBusy(true);
        SetMessage("Đang tải danh sách thành viên…");
        try
        {
            ProjectMembersResponse response = await projects.GetMembersAsync(projectId, lifetime.Token);
            if (!IsCurrent(version, generation)) return;
            ownerCount = response.Members.Count(member => member.Role == "OWNER");
            ClearMembers();
            foreach (ProjectMemberResponse member in response.Members) AddMember(member);
            SetMessage(response.Members.Count == 0 ? "Project chưa có thành viên đang hoạt động." : null);
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested || IsDisposed) { }
        catch (ApiBusinessException error)
        {
            if (IsCurrent(version, generation))
            {
                SetMessage(error.StatusCode == HttpStatusCode.Forbidden
                    ? "Chỉ Owner có thể xem và quản lý thành viên của Project."
                    : error.Message, UITheme.Danger);
                ClearMembers();
            }
        }
        catch (ApiException error)
        {
            if (IsCurrent(version, generation)) SetMessage(error.Message, UITheme.Danger);
        }
        finally
        {
            if (IsCurrent(version, generation)) SetBusy(false);
        }
    }

    private void AddMember(ProjectMemberResponse member)
    {
        bool lastOwner = member.Role == "OWNER" && ownerCount <= 1;
        var row = new Panel
        {
            Dock = DockStyle.Top,
            Height = 80,
            BackColor = UITheme.White,
            Margin = Padding.Empty,
            Padding = new Padding(0, 4, 0, 4),
            AccessibleName = $"{member.DisplayName}, {member.Role}",
        };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            RowCount = 1,
            BackColor = UITheme.White,
            Padding = new Padding(8, 2, 8, 2),
            Margin = Padding.Empty,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 58));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 108));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 54));

        var avatar = new Panel
        {
            Width = 48,
            Height = 48,
            Location = new Point(3, 10),
            Anchor = AnchorStyles.Left,
            BackColor = UITheme.White,
            Margin = new Padding(0, 0, 8, 0),
            AccessibleName = $"Ảnh đại diện {member.DisplayName}",
        };
        Color avatarColor = AvatarColors[(int)((member.UserId % AvatarColors.Length + AvatarColors.Length) % AvatarColors.Length)];
        string initials = GetInitials(member.DisplayName);
        avatar.Paint += (_, e) => UITheme.DrawAvatar(e.Graphics, initials, new Rectangle(1, 1, 46, 46), avatarColor, UITheme.White, UITheme.FontBodyBold);

        var identity = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = UITheme.White,
            Margin = new Padding(2, 0, 10, 0),
        };
        identity.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        identity.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        identity.Controls.Add(new Label
        {
            Text = member.DisplayName,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.BottomLeft,
            Font = UITheme.FontH3,
            ForeColor = UITheme.Ink,
            AutoEllipsis = true,
            Margin = Padding.Empty,
        }, 0, 0);
        identity.Controls.Add(new Label
        {
            Text = $"{member.Email}  ·  Tham gia {member.JoinedAt.ToLocalTime():dd/MM/yyyy}",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.TopLeft,
            Font = UITheme.FontBody,
            ForeColor = Color.FromArgb(100, 116, 139),
            AutoEllipsis = true,
            Margin = Padding.Empty,
        }, 0, 1);

        var role = new ComboBox
        {
            Dock = DockStyle.Top,
            Height = 36,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            IntegralHeight = false,
            MaxDropDownItems = 3,
            Font = UITheme.FontBodyBold,
            ForeColor = Color.FromArgb(51, 65, 85),
            BackColor = UITheme.White,
            Margin = new Padding(4, 0, 8, 0),
            AccessibleName = $"Vai trò của {member.DisplayName}",
        };
        role.Items.AddRange(["OWNER", "MANAGER", "CREATOR"]);
        role.SelectedItem = member.Role;
        role.Enabled = !lastOwner;

        var change = new ProjectActionButton
        {
            Text = "Đổi vai trò",
            Dock = DockStyle.Top,
            Height = 36,
            Style = RoundButtonStyle.Primary,
            Margin = new Padding(0, 0, 4, 0),
            AccessibleName = $"Đổi vai trò của {member.DisplayName}",
        };
        var remove = new DangerOutlineButton
        {
            Text = "Gỡ",
            Dock = DockStyle.Top,
            Height = 36,
            Enabled = !lastOwner,
            Margin = new Padding(4, 0, 0, 0),
            AccessibleName = $"Gỡ {member.DisplayName} khỏi Project",
        };

        // Keep the action visible and clickable even before a selection changes. The click handler
        // explains that there is nothing to save when the selected role is still the current role.
        change.Click += async (_, _) => await ChangeRoleAsync(member, role.SelectedItem?.ToString());
        remove.Click += async (_, _) => await RemoveMemberAsync(member);
        if (lastOwner)
        {
            toolTip.SetToolTip(role, "Không thể đổi vai trò Owner cuối cùng.");
            toolTip.SetToolTip(remove, "Không thể gỡ Owner cuối cùng.");
        }

        layout.Controls.Add(avatar, 0, 0);
        layout.Controls.Add(identity, 1, 0);
        layout.Controls.Add(VerticalCenter(role, 18, 8), 2, 0);
        layout.Controls.Add(VerticalCenter(change, 18, 4), 3, 0);
        layout.Controls.Add(VerticalCenter(remove, 18, 0), 4, 0);
        row.Controls.Add(layout);
        row.Paint += (_, e) =>
        {
            using var pen = new Pen(UITheme.Neutral100);
            e.Graphics.DrawLine(pen, 0, row.Height - 1, row.Width, row.Height - 1);
        };

        int rowIndex = members.RowCount++;
        members.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
        members.Controls.Add(row, 0, rowIndex);
    }

    private void ClearMembers()
    {
        foreach (Control child in members.Controls.Cast<Control>().ToArray())
        {
            members.Controls.Remove(child);
            child.Dispose();
        }
        members.RowStyles.Clear();
        members.RowCount = 0;
    }

    private async Task ChangeRoleAsync(ProjectMemberResponse member, string? role)
    {
        if (string.IsNullOrWhiteSpace(role) || string.Equals(role, member.Role, StringComparison.OrdinalIgnoreCase))
        {
            const string noChanges = "Chưa có bất kỳ thay đổi nào hết.";
            SetMessage(noChanges, Color.FromArgb(180, 83, 9));
            MessageBox.Show(this, noChanges, "Đổi vai trò", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        await MutateAsync(async token => await projects.UpdateMemberRoleAsync(projectId, member.UserId, role, token),
            $"Đã đổi vai trò của {member.DisplayName} thành {role}.");
    }

    private async Task RemoveMemberAsync(ProjectMemberResponse member)
    {
        if (MessageBox.Show(this, $"Gỡ {member.DisplayName} khỏi Project? Người này sẽ mất quyền truy cập vào Project này.",
                "Gỡ thành viên", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        await MutateAsync(async token => await projects.RemoveMemberAsync(projectId, member.UserId, token),
            $"Đã gỡ {member.DisplayName} khỏi Project.");
    }

    private async Task MutateAsync(Func<CancellationToken, Task<ProjectMemberMutationResponse>> operation, string successMessage)
    {
        if (lifetime.IsCancellationRequested || projects.Context.Current?.ProjectId != projectId) return;
        int version = ++sequence;
        long generation = projects.Context.Generation;
        bool sent = false;
        SetBusy(true);
        SetMessage("Đang lưu thay đổi…");
        try
        {
            sent = true;
            await operation(lifetime.Token);
            if (!IsCurrent(version, generation)) return;

            await projects.RevalidateAsync(lifetime.Token);
            if (IsDisposed || lifetime.IsCancellationRequested) return;
            if (projects.Context.Current is not { Role: ProjectRole.Owner } current || current.ProjectId != projectId)
            {
                Close();
                return;
            }

            await LoadMembersAsync();
            if (!IsDisposed) SetMessage(successMessage, UITheme.Success);
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested || IsDisposed) { }
        catch (ApiBusinessException error)
        {
            if (IsDisposed) return;
            if (sent && ((int)error.StatusCode >= 500 || error.StatusCode == HttpStatusCode.Conflict))
            {
                await LoadMembersAsync();
                if (!IsDisposed) SetMessage(error.Message, UITheme.Danger);
            }
            else SetMessage(error.Message, UITheme.Danger);
        }
        catch (ApiException error)
        {
            if (IsDisposed) return;
            SetMessage("Chưa xác định được kết quả. Đang tải lại danh sách; không gửi lại thao tác.", Color.FromArgb(180, 83, 9));
            if (sent) await LoadMembersAsync();
            if (!IsDisposed) SetMessage(error.Message + " Danh sách đã được tải lại.", UITheme.Danger);
        }
        finally
        {
            if (!IsDisposed && !lifetime.IsCancellationRequested) SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        reload.Enabled = !busy;
        close.Enabled = !busy;
        members.Enabled = !busy;
        UseWaitCursor = busy;
    }

    private void SetMessage(string? text, Color? color = null)
    {
        message.Text = text ?? string.Empty;
        message.Visible = !string.IsNullOrWhiteSpace(text);
        message.ForeColor = color ?? UITheme.Neutral600;
    }

    private void NoticePanel_SizeChanged(object? sender, EventArgs e) => SetRoundedRegion(noticePanel, 12);

    private void NoticePanel_Paint(object? sender, PaintEventArgs e)
    {
        if (noticePanel.Width <= 1 || noticePanel.Height <= 1) return;
        using var path = UITheme.CreateRoundedRectanglePath(
            new Rectangle(0, 0, noticePanel.Width - 1, noticePanel.Height - 1), 12);
        using var pen = new Pen(Color.FromArgb(253, 230, 138));
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.DrawPath(pen, path);
    }

    private void FooterPanel_Paint(object? sender, PaintEventArgs e)
    {
        using var pen = new Pen(UITheme.Neutral100);
        e.Graphics.DrawLine(pen, 0, 0, footerPanel.ClientSize.Width, 0);
    }

    private static void SetRoundedRegion(Control control, int radius)
    {
        if (control.Width <= 0 || control.Height <= 0) return;
        using var path = UITheme.CreateRoundedRectanglePath(new Rectangle(0, 0, control.Width, control.Height), radius);
        Region? previous = control.Region;
        control.Region = new Region(path);
        previous?.Dispose();
    }

    private static string GetInitials(string displayName)
    {
        string[] parts = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return "??";
        if (parts.Length > 1) return string.Concat(char.ToUpperInvariant(parts[0][0]), char.ToUpperInvariant(parts[^1][0]));
        return parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant();
    }

    private static Panel VerticalCenter(Control control, int topPadding, int rightPadding)
    {
        var host = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, topPadding, rightPadding, 0),
            BackColor = UITheme.White,
            Margin = Padding.Empty,
        };
        host.Controls.Add(control);
        return host;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !released)
        {
            released = true;
            lifetime.Cancel();
            lifetime.Dispose();
            toolTip.Dispose();
        }
        base.Dispose(disposing);
    }

    private sealed class DangerOutlineButton : Button
    {
        private bool hovered;

        public DangerOutlineButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = UITheme.White;
            ForeColor = UITheme.Danger;
            Font = UITheme.FontBody;
            Cursor = Cursors.Hand;
            UseVisualStyleBackColor = false;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hovered = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = UITheme.CreateRoundedRectanglePath(bounds, 8);
            using var fill = new SolidBrush(hovered ? Color.FromArgb(255, 241, 242) : UITheme.White);
            using var border = new Pen(Color.FromArgb(254, 205, 211));
            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawPath(border, path);
            TextRenderer.DrawText(e.Graphics, Text, Font, bounds, Enabled ? UITheme.Danger : UITheme.Neutral400,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }
    }
}
