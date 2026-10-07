using CreatorFlow.Helpers;
using CreatorFlow.Services;

namespace CreatorFlow.Forms;

public partial class ForgotPasswordForm : Form
{
    private readonly AuthApiFacade _authService;
    private CancellationTokenSource? _pending;
    private bool _closing;
    public event Action<string>? LoginRequested;
    public event Action<string, Guid>? CodeRequested;

    public ForgotPasswordForm(AuthApiFacade authService, string email)
    {
        _authService = authService;
        InitializeComponent();
        emailTextBox.Text = email;
    }

    private async void SendButton_Click(object? sender, EventArgs e)
    {
        if (_closing || _pending is not null) return;
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
