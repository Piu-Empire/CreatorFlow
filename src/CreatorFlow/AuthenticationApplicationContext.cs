using CreatorFlow.Forms;
using CreatorFlow.Forms.Board;
using CreatorFlow.Services;

namespace CreatorFlow;

internal sealed class AuthenticationApplicationContext : ApplicationContext
{
    private readonly AuthApiFacade auth;
    private readonly Func<BoardForm> createShell;
    private AuthenticationHostForm? authenticationHost;
    private bool switching;
    private readonly System.Windows.Forms.Timer expiryTimer = new() { Interval = 1000 };
    public AuthenticationApplicationContext(AuthApiFacade auth, Func<BoardForm> createShell)
    {
        this.auth = auth;
        this.createShell = createShell;
        auth.Session.Cleared += SessionCleared;
        expiryTimer.Tick += (_, _) => { if (!switching && MainForm is BoardForm && !auth.Session.IsAuthenticated) auth.Logout(); };
        expiryTimer.Start();
        ShowLogin();
    }
    private void SessionCleared(object? sender, EventArgs e)
    {
        CurrentSession.CurrentUserId = 0;
        CurrentSession.CurrentUserName = string.Empty;
        CurrentSession.CurrentProjectId = 0;
        CurrentSession.CurrentProjectName = string.Empty;
        if (!switching && MainForm is BoardForm) ShowLogin();
    }
    private void ShowLogin(string? email = null, string? message = null)
    {
        var form = new LoginForm(auth, email, message);
        form.RegisterRequested += (_, _) => ShowRegister();
        form.EmailVerificationRequested += ShowVerify;
        form.ForgotPasswordRequested += ShowForgot;
        form.LoginSucceeded += (_, _) =>
        {
            if (!auth.Session.IsAuthenticated || auth.Session.CurrentUser is not { } user) return;
            OpenBoard(user.UserId, user.DisplayName);
        };
        ShowAuthenticationPage(form);
    }
    private void ShowRegister()
    {
        var form = new RegisterForm(auth);
        form.LoginRequested += (_, _) => ShowLogin();
        form.Registered += ShowVerify;
        ShowAuthenticationPage(form);
    }
    private void ShowVerify(string email)
    {
        var form = new VerifyEmailForm(auth, email);
        form.LoginRequested += value => ShowLogin(value);
        form.EmailVerified += (value, message) => ShowLogin(value, message);
        ShowAuthenticationPage(form);
    }
    private void ShowForgot(string email)
    {
        var form = new ForgotPasswordForm(auth, email);
        form.LoginRequested += value => ShowLogin(value);
        form.CodeRequested += (value, id) => ShowReset(value, id);
        ShowAuthenticationPage(form);
    }
    private void ShowReset(string email, Guid id)
    {
        var form = new ResetPasswordForm(auth, email, id);
        form.LoginRequested += value => ShowLogin(value);
        form.PasswordReset += (value, message) => ShowLogin(value, message);
        ShowAuthenticationPage(form);
    }

    private void ShowAuthenticationPage(Form page)
    {
        switching = true;
        try
        {
            Form? previousMainForm = null;
            if (authenticationHost is null || authenticationHost.IsDisposed)
            {
                previousMainForm = MainForm;
                authenticationHost = new AuthenticationHostForm();
                MainForm = authenticationHost;
            }

            authenticationHost.ShowPage(page);
            if (!authenticationHost.Visible) authenticationHost.Show();
            authenticationHost.WindowState = FormWindowState.Maximized;
            if (previousMainForm is not null)
            {
                previousMainForm.Close();
                previousMainForm.Dispose();
            }
        }
        finally { switching = false; }
    }

    private void OpenBoard(long userId, string displayName)
    {
        switching = true;
        try
        {
            CurrentSession.CurrentUserId = userId;
            CurrentSession.CurrentUserName = displayName;
            CurrentSession.CurrentProjectId = 0;
            CurrentSession.CurrentProjectName = string.Empty;

            var board = createShell();
            Form? previous = MainForm;
            MainForm = board;
            board.Show();
            previous?.Close();
            previous?.Dispose();
            authenticationHost = null;
        }
        finally { switching = false; }
    }

    protected override void ExitThreadCore()
    {
        switching = true;
        expiryTimer.Stop();
        expiryTimer.Dispose();
        auth.Session.Cleared -= SessionCleared;
        auth.Logout();
        SessionCleared(this, EventArgs.Empty);
        base.ExitThreadCore();
    }
}
