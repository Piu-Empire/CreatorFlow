using CreatorFlow.Helpers;
using CreatorFlow.Services;
using CreatorFlow.Controls;

namespace CreatorFlow.Forms;

public partial class RegisterForm : Form
{
    private AuthApiFacade? _authService;
    private CancellationTokenSource? _pending;
    private bool _closing;
    private bool _verificationAvailable;
    private CancellationTokenSource? _readiness;
    private readonly Dictionary<string, Panel> fields = new();
    public event EventHandler? LoginRequested;
    public event Action<string>? Registered;

    public RegisterForm()
    {
        InitializeComponent();
        ConfigureDesignableControls();
    }

    public RegisterForm(AuthApiFacade authService) : this()
    {
        _authService = authService;
        registerButton.Enabled = false;
    }

    private void ConfigureDesignableControls()
    {
        AuthenticationLayout.ConfigureResponsiveLayout(columns, brandingPanel, formArea, formColumn);
        UiTheme.StyleField(nameFieldBorder, nameTextBox);
        UiTheme.StyleField(emailFieldBorder, emailTextBox);
        UiTheme.StyleField(passwordFieldBorder, passwordTextBox);
        UiTheme.StyleField(confirmFieldBorder, confirmTextBox);
        UiTheme.StyleButton(registerButton);
        fields.Add("DisplayName", nameField);
        fields.Add("Email", emailField);
        fields.Add("Password", passwordField);
        fields.Add("ConfirmPassword", confirmField);
        showPassword.CheckedChanged += ShowPassword_CheckedChanged;
        registerButton.Click += RegisterButton_Click;
        loginLink.LinkClicked += LoginLink_LinkClicked;
        nameTextBox.TextChanged += (_, _) => UiTheme.SetFieldError(nameField, null);
        emailTextBox.TextChanged += (_, _) => UiTheme.SetFieldError(emailField, null);
        passwordTextBox.TextChanged += (_, _) => UiTheme.SetFieldError(passwordField, null);
        confirmTextBox.TextChanged += (_, _) => UiTheme.SetFieldError(confirmField, null);
        formColumn.SizeChanged += FormColumn_SizeChanged;
        FormColumn_SizeChanged(this, EventArgs.Empty);
        registerButton.Enabled = false;
    }

    private void FormColumn_SizeChanged(object? sender, EventArgs e)
    {
        foreach (Panel field in fields.Values)
        {
            if (field.Controls["fieldError"] is Label error)
                error.MaximumSize = new Size(Math.Max(1, formColumn.ClientSize.Width), 0);
        }
    }

    private void ShowPassword_CheckedChanged(object? sender, EventArgs e) =>
        passwordTextBox.UseSystemPasswordChar = confirmTextBox.UseSystemPasswordChar = !showPassword.Checked;

    private void LoginLink_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e) =>
        LoginRequested?.Invoke(this, EventArgs.Empty);

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (_authService is null) return;
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
        if (_pending is not null || _closing || !_verificationAvailable || _authService is null) return;
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
