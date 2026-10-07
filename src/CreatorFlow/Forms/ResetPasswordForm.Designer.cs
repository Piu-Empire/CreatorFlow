using CreatorFlow.Controls;
using CreatorFlow.Helpers;
using CreatorFlow.Services;

namespace CreatorFlow.Forms;

partial class ResetPasswordForm
{
    private TextBox emailTextBox = null!, codeTextBox = null!, passwordTextBox = null!, confirmTextBox = null!;
    private Label messageLabel = null!;
    private Button resetButton = null!;
    private CheckBox showPassword = null!;
    private LinkLabel resendLink = null!, backLink = null!;
    private System.Windows.Forms.Timer cooldownTimer = null!;
    private readonly Dictionary<string, Panel> fields = new();

    private void InitializeComponent()
    {
        var layout = new AuthenticationLayout("Đặt lại mật khẩu", "Nếu tài khoản đủ điều kiện, bạn sẽ nhận được mã xác nhận qua email. Mã có hiệu lực 10 phút.");
        emailTextBox = new TextBox { Name = "resetEmail", ReadOnly = true, TabStop = false };
        codeTextBox = new TextBox { Name = "verificationCode", MaxLength = 6 };
        passwordTextBox = new TextBox { Name = "newPassword", UseSystemPasswordChar = true };
        confirmTextBox = new TextBox { Name = "confirmPassword", UseSystemPasswordChar = true };
        UiTheme.AddField(layout.Body, "Email", emailTextBox);
        fields.Add("Code", UiTheme.AddField(layout.Body, "Mã xác nhận (6 chữ số)", codeTextBox));
        resendLink = UiTheme.CreateLink("Gửi lại mã");
        resendLink.LinkClicked += ResendLink_Click;
        UiTheme.AddRow(layout.Body, resendLink);
        fields.Add("NewPassword", UiTheme.AddField(layout.Body, "Mật khẩu mới", passwordTextBox));
        fields.Add("ConfirmPassword", UiTheme.AddField(layout.Body, "Xác nhận mật khẩu mới", confirmTextBox));
        showPassword = new CheckBox { Text = "Hiện mật khẩu", AutoSize = true };
        showPassword.CheckedChanged += (_, _) => passwordTextBox.UseSystemPasswordChar =
            confirmTextBox.UseSystemPasswordChar = !showPassword.Checked;
        UiTheme.AddRow(layout.Body, showPassword, 8);
        UiTheme.AddRow(layout.Body, new Label { Text = AuthUiHints.PasswordPolicyDescription, AutoSize = true,
            ForeColor = UiTheme.Muted, MaximumSize = new Size(440, 0) });
        messageLabel = new Label { AutoSize = true, ForeColor = UiTheme.Error, MaximumSize = new Size(440, 0) };
        UiTheme.AddRow(layout.Body, messageLabel);
        resetButton = new Button { Text = "Đặt lại mật khẩu", Height = 44, Cursor = Cursors.Hand };
        UiTheme.StyleButton(resetButton);
        resetButton.Click += ResetButton_Click;
        UiTheme.AddRow(layout.Body, resetButton, 24);
        backLink = UiTheme.CreateLink("← Quay về đăng nhập");
        backLink.LinkClicked += (_, _) => LoginRequested?.Invoke(_email);
        UiTheme.AddRow(layout.Body, backLink, 0);
        cooldownTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        cooldownTimer.Tick += (_, _) => UpdateCooldown();
        Controls.Add(layout);
        AcceptButton = resetButton;
        AutoScaleDimensions = new SizeF(96, 96);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1120, 900);
        MinimumSize = new Size(540, 700);
        StartPosition = FormStartPosition.CenterScreen;
        Text = "CreatorFlow · Đặt lại mật khẩu";
    }
}
