using CreatorFlow.Helpers;
using CreatorFlow.Services;

namespace CreatorFlow.Controls;

partial class ProfileControl
{
    private TextBox nameTextBox = null!, avatarTextBox = null!, emailTextBox = null!;
    private TextBox currentPassword = null!, newPassword = null!, confirmPassword = null!;
    private Label messageLabel = null!, initialsLabel = null!, accountLabel = null!;
    private PictureBox avatarPicture = null!;
    private Button saveButton = null!, cancelButton = null!, changeButton = null!, logoutButton = null!;
    private Button chooseAvatarButton = null!, removeAvatarButton = null!;
    private CheckBox showPassword = null!;
    private readonly Dictionary<string, Panel> fields = new();

    private void InitializeComponent()
    {
        Dock = DockStyle.Fill;
        AutoScroll = true;
        BackColor = UiTheme.Surface;
        Font = new Font("Segoe UI", 9.75f);
        Padding = new Padding(32);
        var body = UiTheme.CreateFormBody();
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        UiTheme.AddRow(body, new Label { Text = "Hồ sơ cá nhân", AutoSize = true,
            Font = new Font("Segoe UI", 22, FontStyle.Bold), ForeColor = UiTheme.Heading }, 8);
        messageLabel = new Label { AutoSize = true, ForeColor = UiTheme.Error, TabStop = false };
        UiTheme.AddRow(body, messageLabel, 16);
        var profileCard = UiTheme.CreateFormBody();
        profileCard.BackColor = Color.White;
        profileCard.Padding = new Padding(20);
        profileCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        UiTheme.StyleCard(profileCard);
        UiTheme.AddRow(profileCard, new Label { Text = "Thông tin cá nhân", AutoSize = true,
            Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = UiTheme.Heading }, 4);
        UiTheme.AddRow(profileCard, new Label { Text = "Cập nhật ảnh đại diện và tên hiển thị của bạn.",
            AutoSize = true, ForeColor = UiTheme.Muted }, 20);
        var avatarGroup = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
        avatarGroup.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56));
        avatarGroup.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        avatarGroup.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var avatar = new Panel { Size = new Size(40, 40), Margin = Padding.Empty, TabStop = false };
        initialsLabel = new Label { Text = "?", Size = new Size(40, 40), BackColor = Color.Black,
            ForeColor = Color.White, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 12, FontStyle.Bold) };
        avatarPicture = new PictureBox { Size = new Size(40, 40), SizeMode = PictureBoxSizeMode.Zoom, Visible = false };
        UiTheme.StyleAvatar(initialsLabel);
        UiTheme.StyleAvatar(avatarPicture);
        avatar.Controls.Add(initialsLabel);
        avatar.Controls.Add(avatarPicture);
        avatarPicture.BringToFront();
        emailTextBox = new TextBox { Name = "profileEmail", ReadOnly = true, TabStop = false };
        accountLabel = new Label { AutoSize = true, ForeColor = UiTheme.Muted };
        nameTextBox = new TextBox { Name = "profileDisplayName", MaxLength = 150 };
        avatarTextBox = new TextBox { Name = "profileAvatarUrl" };
        avatarTextBox.TextChanged += (_, _) =>
        {
            if (_busy || _loaded is null) return;
            _avatarRequest?.Cancel();
            _pendingAvatar = null;
            _avatarChange = CreatorFlow.Models.AvatarChange.Url;
        };
        var imageActions = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top, WrapContents = true, Margin = Padding.Empty };
        chooseAvatarButton = new Button { Text = "Chọn ảnh từ máy", Size = new Size(160, 40),
            Margin = new Padding(0, 0, 8, 8), TabIndex = 0 };
        removeAvatarButton = new Button { Text = "Xóa ảnh", Size = new Size(100, 40),
            Margin = new Padding(0, 0, 0, 8), TabIndex = 1 };
        StyleProfileSecondaryButton(chooseAvatarButton);
        StyleProfileSecondaryButton(removeAvatarButton, ghost: true);
        chooseAvatarButton.Click += ChooseAvatarButton_Click;
        removeAvatarButton.Click += RemoveAvatarButton_Click;
        imageActions.Controls.Add(chooseAvatarButton);
        imageActions.Controls.Add(removeAvatarButton);
        avatarGroup.Controls.Add(avatar, 0, 0);
        avatarGroup.Controls.Add(imageActions, 1, 0);
        UiTheme.AddRow(profileCard, avatarGroup, 4);
        UiTheme.AddRow(profileCard, new Label { Text = "JPEG hoặc PNG, tối đa 2 MiB. Ảnh chưa được lưu cho đến khi bạn bấm Lưu thay đổi.",
            AutoSize = true, ForeColor = UiTheme.Muted }, 12);
        fields.Add("AvatarUrl", UiTheme.AddField(profileCard, "Hoặc dùng URL ảnh (tùy chọn)", avatarTextBox));
        UiTheme.AddRow(profileCard, new Label { Text = "Dùng URL HTTP/HTTPS. Nếu không có ảnh hoặc ảnh lỗi, hiển thị chữ viết tắt.",
            AutoSize = true, ForeColor = UiTheme.Muted }, 24);
        fields.Add("DisplayName", UiTheme.AddField(profileCard, "Tên hiển thị", nameTextBox));
        UiTheme.AddField(profileCard, "Email (chỉ đọc)", emailTextBox);
        UiTheme.AddRow(profileCard, accountLabel, 20);
        var actions = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true, FlowDirection = FlowDirection.RightToLeft };
        saveButton = new Button { Text = "Lưu thay đổi", Size = new Size(152, 40),
            Margin = Padding.Empty, TabIndex = 1 };
        cancelButton = new Button { Text = "Hủy thay đổi", Size = new Size(140, 40),
            Margin = new Padding(0, 0, 8, 8), TabIndex = 0 };
        UiTheme.StyleButton(saveButton);
        StyleProfileSecondaryButton(cancelButton);
        saveButton.Click += SaveButton_Click;
        cancelButton.Click += (_, _) => { RenderProfile(); ClearPasswords(); ClearFieldErrors(); messageLabel.Text = string.Empty; };
        actions.Controls.Add(saveButton);
        actions.Controls.Add(cancelButton);
        UiTheme.AddRow(profileCard, actions, 0);
        var passwordCard = UiTheme.CreateFormBody();
        passwordCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        passwordCard.Padding = new Padding(20);
        passwordCard.BackColor = Color.White;
        UiTheme.StyleCard(passwordCard);
        UiTheme.AddRow(passwordCard, new Label { Text = "Bảo mật", AutoSize = true,
            Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = UiTheme.Heading }, 4);
        UiTheme.AddRow(passwordCard, new Label { Text = "Đổi mật khẩu thành công sẽ đăng xuất và yêu cầu bạn đăng nhập lại.",
            AutoSize = true, ForeColor = UiTheme.Muted }, 20);
        UiTheme.AddRow(passwordCard, new Label { Text = "Đổi mật khẩu", AutoSize = true,
            Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = UiTheme.Heading }, 12);
        currentPassword = new TextBox { Name = "currentPassword", UseSystemPasswordChar = true };
        newPassword = new TextBox { Name = "newPassword", UseSystemPasswordChar = true };
        confirmPassword = new TextBox { Name = "confirmPassword", UseSystemPasswordChar = true };
        fields.Add("CurrentPassword", UiTheme.AddField(passwordCard, "Mật khẩu hiện tại", currentPassword));
        fields.Add("NewPassword", UiTheme.AddField(passwordCard, "Mật khẩu mới", newPassword));
        fields.Add("ConfirmPassword", UiTheme.AddField(passwordCard, "Xác nhận mật khẩu mới", confirmPassword));
        showPassword = new CheckBox { Text = "Hiện mật khẩu", AutoSize = true };
        showPassword.CheckedChanged += (_, _) => currentPassword.UseSystemPasswordChar =
            newPassword.UseSystemPasswordChar = confirmPassword.UseSystemPasswordChar = !showPassword.Checked;
        UiTheme.AddRow(passwordCard, showPassword);
        UiTheme.AddRow(passwordCard, new Label { Text = AuthUiHints.PasswordPolicyDescription,
            AutoSize = true, ForeColor = UiTheme.Muted });
        changeButton = new Button { Text = "Đổi mật khẩu và đăng xuất", Size = new Size(248, 44),
            Margin = Padding.Empty };
        UiTheme.StyleButton(changeButton);
        changeButton.Click += ChangeButton_Click;
        var passwordActions = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true, FlowDirection = FlowDirection.RightToLeft };
        passwordActions.Controls.Add(changeButton);
        UiTheme.AddRow(passwordCard, passwordActions, 0);
        UiTheme.AddRow(passwordCard, new Label { Text = "Phiên đăng nhập", AutoSize = true,
            Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = UiTheme.Heading, Padding = new Padding(0, 24, 0, 0) }, 8);
        UiTheme.AddRow(passwordCard, new Label { Text = "Đăng xuất khỏi CreatorFlow trên thiết bị này.",
            AutoSize = true, ForeColor = UiTheme.Muted }, 12);
        logoutButton = new Button { Name = "profileLogout", Text = "Đăng xuất", Size = new Size(152, 40), Margin = Padding.Empty };
        StyleProfileSecondaryButton(logoutButton);
        logoutButton.Click += (_, _) => LogoutRequested?.Invoke(this, EventArgs.Empty);
        var sessionActions = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true, FlowDirection = FlowDirection.RightToLeft };
        sessionActions.Controls.Add(logoutButton);
        UiTheme.AddRow(passwordCard, sessionActions, 0);
        var cards = new TableLayoutPanel { Name = "profileCards", AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Top, Margin = Padding.Empty };
        profileCard.Name = "personalInformationCard";
        passwordCard.Name = "securityCard";
        profileCard.Anchor = passwordCard.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        profileCard.Dock = passwordCard.Dock = DockStyle.Top;
        bool? twoColumns = null;
        void ArrangeCards()
        {
            bool wide = body.ClientSize.Width >= (int)Math.Round(960 * DeviceDpi / 96f);
            if (twoColumns == wide) return;
            twoColumns = wide;
            cards.SuspendLayout();
            cards.Controls.Clear();
            cards.ColumnStyles.Clear();
            cards.RowStyles.Clear();
            cards.ColumnCount = wide ? 3 : 1;
            cards.RowCount = wide ? 1 : 2;
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, wide ? 60 : 100));
            if (wide)
            {
                cards.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, (int)Math.Round(24 * DeviceDpi / 96f)));
                cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            }
            cards.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            if (!wide) cards.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            profileCard.Margin = wide ? Padding.Empty : new Padding(0, 0, 0, (int)Math.Round(24 * DeviceDpi / 96f));
            passwordCard.Margin = Padding.Empty;
            cards.Controls.Add(profileCard, 0, 0);
            cards.Controls.Add(passwordCard, wide ? 2 : 0, wide ? 0 : 1);
            cards.ResumeLayout(true);
        }
        UiTheme.AddRow(body, cards, 0);
        Controls.Add(body);
        body.SizeChanged += (_, _) => { ArrangeCards(); WrapLabels(body); };
        DpiChangedAfterParent += (_, _) =>
        {
            Padding = new Padding((int)Math.Round(32 * DeviceDpi / 96f));
            twoColumns = null;
            ArrangeCards();
        };
        profileCard.SizeChanged += (_, _) => WrapLabels(profileCard);
        passwordCard.SizeChanged += (_, _) => WrapLabels(passwordCard);
        ArrangeCards();
        WrapLabels(body);
    }

    private static void StyleProfileSecondaryButton(Button button, bool ghost = false)
    {
        UiTheme.StyleButton(button);
        button.BackColor = ghost ? Color.White : Color.FromArgb(245, 245, 245);
        button.ForeColor = ghost ? Color.FromArgb(64, 64, 64) : Color.FromArgb(38, 38, 38);
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = UiTheme.Border;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(229, 229, 229);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(212, 212, 212);
    }

    private static void WrapLabels(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            if (child is Label label && label.AutoSize)
                label.MaximumSize = new Size(Math.Max(1, parent.ClientSize.Width - parent.Padding.Horizontal), 0);
            if (child.HasChildren) WrapLabels(child);
        }
    }
}
