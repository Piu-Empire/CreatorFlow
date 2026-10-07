using CreatorFlow.Helpers;
using CreatorFlow.Services;

namespace CreatorFlow.Forms;

public partial class RegisterForm : Form
{
    private readonly AuthApiFacade _authService;
    private CancellationTokenSource? _pending;
    private bool _closing;
    private bool _verificationAvailable;
    private CancellationTokenSource? _readiness;
    public event EventHandler? LoginRequested;
    public event Action<string>? Registered;

    public RegisterForm(AuthApiFacade authService)
    {
        _authService = authService;
        InitializeComponent();
        registerButton.Enabled = false;
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        using var cancellation = new CancellationTokenSource();
        _readiness = cancellation;
        try
        {
            _verificationAvailable = await _authService.IsEmailVerificationAvailableAsync(cancellation.Token);
            if (_closing || IsDisposed) return;
            registerButton.Enabled = _verificationAvailable && _pending is null;
            if (!_verificationAvailable) messageLabel.Text = "Đăng ký chưa sẵn sàng: cần cấu hình email và schema xác minh.";
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        finally { _readiness = null; }
    }

    private async void RegisterButton_Click(object? sender, EventArgs e)
    {
        if (_pending is not null || _closing || !_verificationAvailable) return;
        using var cancellation = new CancellationTokenSource();
        _pending = cancellation;
        SetBusy(true);
        foreach (Panel field in fields.Values) UiTheme.SetFieldError(field, null);
        messageLabel.Text = "Đang tạo tài khoản…";
        try
        {
            UserOperationResult result = await _authService.RegisterAsync(
                nameTextBox.Text, emailTextBox.Text, passwordTextBox.Text, confirmTextBox.Text, cancellation.Token);
            if (_closing || IsDisposed || cancellation.IsCancellationRequested) return;
            if (result.Succeeded)
            {
                passwordTextBox.Clear();
                confirmTextBox.Clear();
                Registered?.Invoke(emailTextBox.Text.Trim());
                return;
            }
            messageLabel.Text = result.ErrorMessage;
            if (result.ErrorField is { } key && fields.TryGetValue(key, out Panel? field))
                UiTheme.SetFieldError(field, result.ErrorMessage);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        finally
        {
            _pending = null;
            if (!_closing && !IsDisposed) SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        foreach (TextBox field in new[] { nameTextBox, emailTextBox, passwordTextBox, confirmTextBox })
            field.Enabled = !busy;
        registerButton.Enabled = !busy && _verificationAvailable;
        loginLink.Enabled = showPassword.Enabled = !busy;
        registerButton.Text = busy ? "Đang tạo tài khoản…" : "Tạo tài khoản";
        UseWaitCursor = busy;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (e.Cancel) return;
        _closing = true;
        _pending?.Cancel();
        _readiness?.Cancel();
        passwordTextBox.Clear();
        confirmTextBox.Clear();
    }
}
