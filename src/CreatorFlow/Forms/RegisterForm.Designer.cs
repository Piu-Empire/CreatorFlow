using CreatorFlow.Controls;
using CreatorFlow.Helpers;
using CreatorFlow.Services;

namespace CreatorFlow.Forms;

partial class RegisterForm
{
    private TextBox nameTextBox = null!, emailTextBox = null!, passwordTextBox = null!, confirmTextBox = null!;
    private Button registerButton = null!;
    private Label messageLabel = null!;
    private CheckBox showPassword = null!;
    private LinkLabel loginLink = null!;
    private readonly Dictionary<string, Panel> fields = new();

    private void InitializeComponent()
    {
        var layout = new AuthenticationLayout("Tạo tài khoản", "Bắt đầu làm việc cùng CreatorFlow.");
        nameTextBox = new TextBox { Name = "displayNameTextBox", MaxLength = 150 };
        emailTextBox = new TextBox { Name = "emailTextBox", MaxLength = 255 };
        passwordTextBox = new TextBox { Name = "passwordTextBox", UseSystemPasswordChar = true };
        confirmTextBox = new TextBox { Name = "confirmPasswordTextBox", UseSystemPasswordChar = true };
        fields.Add("DisplayName", UiTheme.AddField(layout.Body, "Tên hiển thị", nameTextBox));
        fields.Add("Email", UiTheme.AddField(layout.Body, "Email", emailTextBox));
        fields.Add("Password", UiTheme.AddField(layout.Body, "Mật khẩu", passwordTextBox));
        fields.Add("ConfirmPassword", UiTheme.AddField(layout.Body, "Xác nhận mật khẩu", confirmTextBox));
        showPassword = new CheckBox { Text = "Hiện mật khẩu", AutoSize = true };
        showPassword.CheckedChanged += (_, _) =>
            passwordTextBox.UseSystemPasswordChar = confirmTextBox.UseSystemPasswordChar = !showPassword.Checked;
        UiTheme.AddRow(layout.Body, showPassword, 8);
        UiTheme.AddRow(layout.Body, new Label { Text = AuthUiHints.PasswordPolicyDescription, AutoSize = true,
            ForeColor = UiTheme.Muted, MaximumSize = new Size(440, 0) });
        messageLabel = new Label { AutoSize = true, ForeColor = UiTheme.Error, MaximumSize = new Size(440, 0) };
        UiTheme.AddRow(layout.Body, messageLabel);
        registerButton = new Button { Text = "Tạo tài khoản", Height = 44, Cursor = Cursors.Hand };
        registerButton.Click += RegisterButton_Click;
        UiTheme.StyleButton(registerButton);
        UiTheme.AddRow(layout.Body, registerButton, 24);
        loginLink = UiTheme.CreateLink("Đã có tài khoản? Đăng nhập");
        loginLink.LinkClicked += (_, _) => LoginRequested?.Invoke(this, EventArgs.Empty);
        UiTheme.AddRow(layout.Body, loginLink, 0);
        Controls.Add(layout);
        AcceptButton = registerButton;
        AutoScaleDimensions = new SizeF(96, 96);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1120, 900);
        MinimumSize = new Size(540, 700);
        StartPosition = FormStartPosition.CenterScreen;
        Text = "CreatorFlow · Đăng ký";
    }
}
