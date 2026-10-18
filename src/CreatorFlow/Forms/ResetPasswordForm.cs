using CreatorFlow.Helpers;
using CreatorFlow.Services;
using CreatorFlow.Controls;

namespace CreatorFlow.Forms;

public partial class ResetPasswordForm : Form
{
    private AuthApiFacade? _authService;
    private string _email = string.Empty;
    private Guid _requestId;
    private CancellationTokenSource? _pending;
    private bool _closing;
    private long _resendAt;
    private readonly Dictionary<string, Panel> fields = new();
    public event Action<string>? LoginRequested;
    public event Action<string, string>? PasswordReset;

    public ResetPasswordForm()
    {
        InitializeComponent();
        ConfigureDesignableControls();
    }

    public ResetPasswordForm(AuthApiFacade authService, string email, Guid requestId) : this()
    {
        _authService = authService;
        _email = email;
        _requestId = requestId;
        emailTextBox.Text = email;
        StartCooldown();
    }

    private void ConfigureDesignableControls()
    {
        AuthenticationLayout.ConfigureResponsiveLayout(columns, brandingPanel, formArea, formColumn);
        UiTheme.StyleField(emailFieldBorder, emailTextBox);
        UiTheme.StyleField(codeFieldBorder, codeTextBox);
        UiTheme.StyleField(passwordFieldBorder, passwordTextBox);
        UiTheme.StyleField(confirmFieldBorder, confirmTextBox);
        UiTheme.StyleButton(resetButton);
        fields.Add("Code", codeField);
        fields.Add("NewPassword", passwordField);
        fields.Add("ConfirmPassword", confirmField);
        resetButton.Click += ResetButton_Click;
        resendLink.LinkClicked += ResendLink_Click;
        backLink.LinkClicked += BackLink_LinkClicked;
        showPassword.CheckedChanged += ShowPassword_CheckedChanged;
        cooldownTimer.Tick += CooldownTimer_Tick;
        emailTextBox.TextChanged += (_, _) => UiTheme.SetFieldError(emailField, null);
        codeTextBox.TextChanged += (_, _) => UiTheme.SetFieldError(fields["Code"], null);
        passwordTextBox.TextChanged += (_, _) => UiTheme.SetFieldError(fields["NewPassword"], null);
        confirmTextBox.TextChanged += (_, _) => UiTheme.SetFieldError(fields["ConfirmPassword"], null);
        formColumn.SizeChanged += FormColumn_SizeChanged;
        FormColumn_SizeChanged(this, EventArgs.Empty);
    }

    private void FormColumn_SizeChanged(object? sender, EventArgs e)
    {
        foreach (Panel field in new[] { emailField, fields["Code"], fields["NewPassword"], fields["ConfirmPassword"] })
        {
            if (field.Controls["fieldError"] is Label error)
                error.MaximumSize = new Size(Math.Max(1, formColumn.ClientSize.Width), 0);
        }
    }

    private void BackLink_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e) =>
        LoginRequested?.Invoke(_email);

    private void ShowPassword_CheckedChanged(object? sender, EventArgs e) =>
        passwordTextBox.UseSystemPasswordChar = confirmTextBox.UseSystemPasswordChar = !showPassword.Checked;

    private void CooldownTimer_Tick(object? sender, EventArgs e) => UpdateCooldown();

    private async void ResetButton_Click(object? sender, EventArgs e)
    {
        if (_closing || _pending is not null || _authService is null) return;
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
        if (_closing || _pending is not null || Environment.TickCount64 < _resendAt || _authService is null) return;
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
