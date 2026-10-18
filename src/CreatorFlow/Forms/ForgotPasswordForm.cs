using CreatorFlow.Helpers;
using CreatorFlow.Services;
using CreatorFlow.Controls;

namespace CreatorFlow.Forms;

public partial class ForgotPasswordForm : Form
{
    private AuthApiFacade? _authService;
    private CancellationTokenSource? _pending;
    private bool _closing;
    public event Action<string>? LoginRequested;
    public event Action<string, Guid>? CodeRequested;

    public ForgotPasswordForm()
    {
        InitializeComponent();
        ConfigureDesignableControls();
    }

    public ForgotPasswordForm(AuthApiFacade authService, string email) : this()
    {
        _authService = authService;
        emailTextBox.Text = email;
    }

    private void ConfigureDesignableControls()
    {
        AuthenticationLayout.ConfigureResponsiveLayout(columns, brandingPanel, formArea, formColumn);
        UiTheme.StyleField(emailFieldBorder, emailTextBox);
        UiTheme.StyleButton(sendButton);
        sendButton.Click += SendButton_Click;
        backLink.LinkClicked += BackLink_LinkClicked;
        emailTextBox.TextChanged += (_, _) => UiTheme.SetFieldError(emailField, null);
    }

    private void BackLink_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e) =>
        LoginRequested?.Invoke(emailTextBox.Text.Trim());

    private async void SendButton_Click(object? sender, EventArgs e)
    {
        if (_closing || _pending is not null || _authService is null) return;
        using var cancellation = new CancellationTokenSource();
        _pending = cancellation;
        SetBusy(true);
        UiTheme.SetFieldError(emailField, null);
        messageLabel.Text = "Đang xử lý yêu cầu…";
        try
        {
            PasswordResetResult result = await _authService.RequestPasswordResetAsync(emailTextBox.Text, cancellation.Token);
            if (_closing || IsDisposed || cancellation.IsCancellationRequested) return;
            if (result.Succeeded && result.RequestId is { } requestId)
            {
                CodeRequested?.Invoke(emailTextBox.Text.Trim(), requestId);
                return;
            }
            messageLabel.Text = result.Message;
            if (result.ErrorField == "Email") UiTheme.SetFieldError(emailField, result.Message);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        finally { _pending = null; if (!_closing && !IsDisposed) SetBusy(false); }
    }

    private void SetBusy(bool busy)
    {
        sendButton.Enabled = backLink.Enabled = emailTextBox.Enabled = !busy;
        sendButton.Text = busy ? "Đang xử lý…" : "Gửi mã xác nhận";
        UseWaitCursor = busy;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (e.Cancel) return;
        _closing = true;
        _pending?.Cancel();
    }
}
