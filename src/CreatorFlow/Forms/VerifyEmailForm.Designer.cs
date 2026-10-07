using CreatorFlow.Controls;
using CreatorFlow.Helpers;

namespace CreatorFlow.Forms;
partial class VerifyEmailForm
{
    private TextBox emailTextBox = null!, codeTextBox = null!;
    private Label messageLabel = null!;
    private Button resetButton = null!;
    private LinkLabel resendLink = null!, backLink = null!;
    private System.Windows.Forms.Timer cooldownTimer = null!;
    private readonly Dictionary<string, Panel> fields = new();

    private void InitializeComponent()
    {
        var layout = new AuthenticationLayout("Xác minh email", "Nhập mã 6 chữ số được gửi tới email của bạn. Mã có hiệu lực 10 phút.");
        emailTextBox = new TextBox { ReadOnly = true, TabStop = false };
        codeTextBox = new TextBox { MaxLength = 6 };
        UiTheme.AddField(layout.Body, "Email", emailTextBox);
        fields.Add("Code", UiTheme.AddField(layout.Body, "Mã xác nhận", codeTextBox));
        messageLabel = new Label { AutoSize = true, ForeColor = UiTheme.Error, MaximumSize = new Size(440, 0) };
        UiTheme.AddRow(layout.Body, messageLabel);
        resetButton = new Button { Text = "Xác minh email", Height = 44, Enabled = false };
        UiTheme.StyleButton(resetButton);
        resetButton.Click += ResetButton_Click;
        UiTheme.AddRow(layout.Body, resetButton);
        resendLink = UiTheme.CreateLink("Gửi mã xác minh");
        resendLink.LinkClicked += ResendLink_Click;
        UiTheme.AddRow(layout.Body, resendLink);
        backLink = UiTheme.CreateLink("← Quay về đăng nhập");
        backLink.LinkClicked += (_, _) => LoginRequested?.Invoke(_email);
        UiTheme.AddRow(layout.Body, backLink);
        cooldownTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        cooldownTimer.Tick += (_, _) => UpdateCooldown();
        Controls.Add(layout);
        AcceptButton = resetButton;
        AutoScaleDimensions = new SizeF(96, 96);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1120, 800);
        MinimumSize = new Size(540, 620);
        StartPosition = FormStartPosition.CenterScreen;
        Text = "CreatorFlow · Xác minh email";
    }
}
