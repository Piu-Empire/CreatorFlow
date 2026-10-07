using CreatorFlow.Controls;
using CreatorFlow.Helpers;

namespace CreatorFlow.Forms;

partial class ForgotPasswordForm
{
    private TextBox emailTextBox = null!;
    private Panel emailField = null!;
    private Label messageLabel = null!;
    private Button sendButton = null!;
    private LinkLabel backLink = null!;

    private void InitializeComponent()
    {
        var layout = new AuthenticationLayout("Quên mật khẩu?", "Nhập email tài khoản để yêu cầu mã xác nhận và đặt lại mật khẩu.");
        emailTextBox = new TextBox { Name = "resetEmail", MaxLength = 255 };
        emailField = UiTheme.AddField(layout.Body, "Email", emailTextBox);
        UiTheme.AddRow(layout.Body, new Label { AutoSize = true, ForeColor = UiTheme.Muted,
            MaximumSize = new Size(440, 0), Text = "Mã gồm 6 chữ số, có hiệu lực trong 10 phút. Không chia sẻ mã với người khác." });
        messageLabel = new Label { AutoSize = true, ForeColor = UiTheme.Error, MaximumSize = new Size(440, 0) };
        UiTheme.AddRow(layout.Body, messageLabel);
        sendButton = new Button { Text = "Gửi mã xác nhận", Height = 44, Cursor = Cursors.Hand };
        UiTheme.StyleButton(sendButton);
        sendButton.Click += SendButton_Click;
        UiTheme.AddRow(layout.Body, sendButton, 24);
        backLink = UiTheme.CreateLink("← Quay về đăng nhập");
        backLink.LinkClicked += (_, _) => LoginRequested?.Invoke(emailTextBox.Text.Trim());
        UiTheme.AddRow(layout.Body, backLink, 0);
        Controls.Add(layout);
        AcceptButton = sendButton;
        AutoScaleDimensions = new SizeF(96, 96);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1120, 760);
        MinimumSize = new Size(540, 620);
        StartPosition = FormStartPosition.CenterScreen;
        Text = "CreatorFlow · Quên mật khẩu";
    }
}
