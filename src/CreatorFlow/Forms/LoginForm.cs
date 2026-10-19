using CreatorFlow.Controls;
using CreatorFlow.Helpers;
using CreatorFlow.Services;

namespace CreatorFlow.Forms;

public partial class LoginForm : Form
{
    private readonly AuthApiFacade? _authService;
    private CancellationTokenSource? _loginCancellation;
    private bool _isClosing;
    private bool _resetAvailable;
    private CancellationTokenSource? _readinessCancellation;

    public event EventHandler? LoginSucceeded;
    public event EventHandler? RegisterRequested;
    public event Action<string>? ForgotPasswordRequested;
    public event Action<string>? EmailVerificationRequested;

    public LoginForm()
    {
        InitializeComponent();
        ConfigureDesignableControls();
    }

    public LoginForm(AuthApiFacade authService, string? email = null, string? message = null) : this()
    {
        _authService = authService;
        emailTextBox.Text = email ?? string.Empty;
        messageLabel.Text = message ?? string.Empty;
        messageLabel.ForeColor = UiTheme.Muted;
    }

    private void ConfigureDesignableControls()
    {
        AuthenticationLayout.ConfigureResponsiveLayout(columns, brandingPanel, formArea, formColumn);
        UiTheme.StyleField(emailBorder, emailTextBox);
        UiTheme.StyleField(passwordBorder, passwordTextBox);
        UiTheme.StyleButton(loginButton);
        showPassword.CheckedChanged += ShowPassword_CheckedChanged;
        forgotLink.LinkClicked += ForgotLink_LinkClicked;
        registerLink.LinkClicked += RegisterLink_LinkClicked;
        loginButton.Click += LoginButton_Click;
        emailTextBox.TextChanged += (_, _) => UiTheme.SetFieldError(emailField, null);
        passwordTextBox.TextChanged += (_, _) => UiTheme.SetFieldError(passwordField, null);
    }

    private void ShowPassword_CheckedChanged(object? sender, EventArgs e) =>
        passwordTextBox.UseSystemPasswordChar = !showPassword.Checked;

    private void ForgotLink_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e) =>
        ForgotPasswordRequested?.Invoke(emailTextBox.Text.Trim());

    private void RegisterLink_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e) =>
        RegisterRequested?.Invoke(this, EventArgs.Empty);

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
            if (_authService is null) return;
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
        if (_authService is null) return;
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
