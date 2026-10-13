using CreatorFlow.Controls;
using CreatorFlow.Helpers;

namespace CreatorFlow.Forms;

partial class LoginForm
{
    private TextBox emailTextBox = null!, passwordTextBox = null!;
    private Button loginButton = null!;
    private Label messageLabel = null!;
    private CheckBox showPassword = null!;
    private LinkLabel registerLink = null!;
    private LinkLabel forgotLink = null!;
    private Panel emailField = null!, passwordField = null!;

    private void InitializeComponent()
    {
        var layout = new AuthenticationLayout("Chào mừng trở lại", "Đăng nhập để tiếp tục công việc của bạn.");
        emailTextBox = new TextBox { Name = "emailTextBox", MaxLength = 255 };
        passwordTextBox = new TextBox { Name = "passwordTextBox", UseSystemPasswordChar = true };
        emailField = UiTheme.AddField(layout.Body, "Email", emailTextBox);
        passwordField = UiTheme.AddField(layout.Body, "Mật khẩu", passwordTextBox);
        showPassword = new CheckBox { Text = "Hiện mật khẩu", AutoSize = true };
        showPassword.CheckedChanged += (_, _) => passwordTextBox.UseSystemPasswordChar = !showPassword.Checked;
        var passwordActions = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, RowCount = 1 };
        passwordActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        passwordActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        showPassword.Margin = Padding.Empty;
        passwordActions.Controls.Add(showPassword, 0, 0);
        forgotLink = UiTheme.CreateLink("Quên mật khẩu?");
        forgotLink.Name = "forgotPasswordLink";
        forgotLink.Enabled = false;
        forgotLink.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        forgotLink.Margin = Padding.Empty;
        forgotLink.LinkClicked += (_, _) => ForgotPasswordRequested?.Invoke(emailTextBox.Text.Trim());
        passwordActions.Controls.Add(forgotLink, 1, 0);
        UiTheme.AddRow(layout.Body, passwordActions);
        messageLabel = new Label { AutoSize = true, ForeColor = UiTheme.Error, MaximumSize = new Size(440, 0) };
        UiTheme.AddRow(layout.Body, messageLabel);
        loginButton = new Button { Text = "Đăng nhập", Height = 44, Cursor = Cursors.Hand };
        loginButton.Click += LoginButton_Click;
        UiTheme.StyleButton(loginButton);
        UiTheme.AddRow(layout.Body, loginButton, 24);
        registerLink = UiTheme.CreateLink("Chưa có tài khoản? Đăng ký");
        registerLink.LinkClicked += (_, _) => RegisterRequested?.Invoke(this, EventArgs.Empty);
        UiTheme.AddRow(layout.Body, registerLink, 0);
        Controls.Add(layout);
        AcceptButton = loginButton;
        AutoScaleDimensions = new SizeF(96, 96);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1120, 760);
        MinimumSize = new Size(540, 620);
        StartPosition = FormStartPosition.CenterScreen;
        Text = "CreatorFlow · Đăng nhập";
    }
}
