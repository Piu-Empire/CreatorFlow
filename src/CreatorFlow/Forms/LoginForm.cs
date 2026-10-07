using CreatorFlow.Helpers;
using CreatorFlow.Services;

namespace CreatorFlow.Forms;

public partial class LoginForm : Form
{
    private readonly AuthApiFacade _authService;
    private CancellationTokenSource? _loginCancellation;
    private bool _isClosing;
    private bool _resetAvailable;
    private CancellationTokenSource? _readinessCancellation;

    public event EventHandler? LoginSucceeded;
    public event EventHandler? RegisterRequested;
    public event Action<string>? ForgotPasswordRequested;
    public event Action<string>? EmailVerificationRequested;

    public LoginForm(AuthApiFacade authService, string? email = null, string? message = null)
    {
        _authService = authService;
        InitializeComponent();
        emailTextBox.Text = email ?? string.Empty;
        messageLabel.Text = message ?? string.Empty;
        messageLabel.ForeColor = UiTheme.Muted;
    }

    private async void LoginButton_Click(object? sender, EventArgs e)
    {
        if (_loginCancellation is not null || _isClosing)
        {
            return;
        }

        using var cancellation = new CancellationTokenSource();
        _loginCancellation = cancellation;
        SetBusy(true);
        UiTheme.SetFieldError(emailField, null);
        UiTheme.SetFieldError(passwordField, null);
        messageLabel.ForeColor = UiTheme.Muted;
        messageLabel.Text = "Đang đăng nhập…";

        try
        {
            LoginResult result = await _authService.LoginAsync(
                emailTextBox.Text, passwordTextBox.Text, cancellation.Token);

            if (_isClosing || IsDisposed || cancellation.IsCancellationRequested)
            {
                return;
            }

            passwordTextBox.Clear();
            if (result.VerificationEmail is { } verificationEmail)
            {
                EmailVerificationRequested?.Invoke(verificationEmail);
                return;
            }
            if (result.Succeeded)
            {
                LoginSucceeded?.Invoke(this, EventArgs.Empty);
                return;
            }

            messageLabel.ForeColor = UiTheme.Error;
            messageLabel.Text = result.ErrorMessage;
            UiTheme.SetFieldError(emailField, string.Empty);
            UiTheme.SetFieldError(passwordField, string.Empty);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            // Closing the form cancels the pending login without showing an error.
        }
        finally
        {
            _loginCancellation = null;
            if (!_isClosing && !IsDisposed)
            {
                SetBusy(false);
                passwordTextBox.Focus();
            }
        }
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        using var cancellation = new CancellationTokenSource();
        _readinessCancellation = cancellation;
        try
        {
            bool available = await _authService.IsPasswordResetAvailableAsync(cancellation.Token);
            if (!_isClosing && !IsDisposed && !cancellation.IsCancellationRequested)
            {
                _resetAvailable = available;
                forgotLink.Enabled = available && _loginCancellation is null;
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        finally { _readinessCancellation = null; }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (!e.Cancel)
        {
            _isClosing = true;
            _loginCancellation?.Cancel();
            _readinessCancellation?.Cancel();
            passwordTextBox.Clear();
        }
    }

    private void SetBusy(bool busy)
    {
        loginButton.Enabled = !busy;
        emailTextBox.Enabled = !busy;
        passwordTextBox.Enabled = !busy;
        showPassword.Enabled = !busy;
        registerLink.Enabled = !busy;
        forgotLink.Enabled = !busy && _resetAvailable;
        loginButton.Text = busy ? "Đang đăng nhập…" : "Đăng nhập";
        UseWaitCursor = busy;
    }
}
