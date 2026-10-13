using CreatorFlow.Helpers;
using CreatorFlow.Services;

namespace CreatorFlow.Forms;

public partial class ResetPasswordForm : Form
{
    private readonly AuthApiFacade _authService;
    private readonly string _email;
    private Guid _requestId;
    private CancellationTokenSource? _pending;
    private bool _closing;
    private long _resendAt;
    public event Action<string>? LoginRequested;
    public event Action<string, string>? PasswordReset;

    public ResetPasswordForm(AuthApiFacade authService, string email, Guid requestId)
    {
        _authService = authService;
        _email = email;
        _requestId = requestId;
        InitializeComponent();
        emailTextBox.Text = email;
        StartCooldown();
    }

    private async void ResetButton_Click(object? sender, EventArgs e)
    {
        if (_closing || _pending is not null) return;
        using var cancellation = new CancellationTokenSource();
        _pending = cancellation;
        SetBusy(true);
        foreach (Panel field in fields.Values) UiTheme.SetFieldError(field, null);
        messageLabel.Text = "Đang đặt lại mật khẩu…";
        try
        {
            PasswordResetResult result = await _authService.ResetPasswordAsync(_requestId,
                codeTextBox.Text, passwordTextBox.Text, confirmTextBox.Text, cancellation.Token);
            if (_closing || IsDisposed || cancellation.IsCancellationRequested) return;
            if (result.Succeeded)
            {
                ClearSecrets();
                PasswordReset?.Invoke(_email, result.Message);
                return;
            }
            messageLabel.Text = result.Message;
            if (result.ErrorField is { } key && fields.TryGetValue(key, out Panel? field))
                UiTheme.SetFieldError(field, result.Message);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        finally { _pending = null; if (!_closing && !IsDisposed) SetBusy(false); }
    }

    private async void ResendLink_Click(object? sender, LinkLabelLinkClickedEventArgs e)
    {
        if (_closing || _pending is not null || Environment.TickCount64 < _resendAt) return;
        using var cancellation = new CancellationTokenSource();
        _pending = cancellation;
        SetBusy(true);
        messageLabel.Text = "Đang xử lý yêu cầu…";
        try
        {
            PasswordResetResult result = await _authService.ResendPasswordResetAsync(_email, cancellation.Token);
            if (_closing || IsDisposed || cancellation.IsCancellationRequested) return;
            messageLabel.Text = result.Message;
            if (result.Succeeded && result.RequestId is { } requestId)
            {
                _requestId = requestId;
                codeTextBox.Clear();
                UiTheme.SetFieldError(fields["Code"], null);
                StartCooldown();
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        finally { _pending = null; if (!_closing && !IsDisposed) SetBusy(false); }
    }

    private void StartCooldown()
    {
        _resendAt = Environment.TickCount64 + AuthUiHints.ResendSeconds * 1000L;
        cooldownTimer.Start();
        UpdateCooldown();
    }

    private void UpdateCooldown()
    {
        long seconds = Math.Max(0, (_resendAt - Environment.TickCount64 + 999) / 1000);
        resendLink.Text = seconds > 0 ? $"Gửi lại mã sau {seconds}s" : "Gửi lại mã";
        resendLink.Enabled = seconds == 0 && _pending is null;
        if (seconds == 0) cooldownTimer.Stop();
    }

    private void SetBusy(bool busy)
    {
        resetButton.Enabled = backLink.Enabled = showPassword.Enabled = !busy;
        codeTextBox.Enabled = passwordTextBox.Enabled = confirmTextBox.Enabled = !busy;
        resetButton.Text = busy ? "Đang xử lý…" : "Đặt lại mật khẩu";
        UseWaitCursor = busy;
        UpdateCooldown();
    }

    private void ClearSecrets()
    {
        codeTextBox.Clear();
        passwordTextBox.Clear();
        confirmTextBox.Clear();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (e.Cancel) return;
        _closing = true;
        _pending?.Cancel();
        cooldownTimer.Stop();
        ClearSecrets();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !IsDisposed) cooldownTimer.Dispose();
        base.Dispose(disposing);
    }
}
