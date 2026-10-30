using CreatorFlow.Forms;
using CreatorFlow.Forms.Board;
using CreatorFlow.Forms.Projects;
using CreatorFlow.Services;

namespace CreatorFlow;

internal sealed class AuthenticationApplicationContext : ApplicationContext
{
    private readonly AuthApiFacade auth;
    private readonly Func<BoardForm> createShell;
    private readonly ProjectApiFacade projects;
    private readonly InvitationInboxService? invitationInbox;
    private AuthenticationHostForm? authenticationHost;
    private bool switching;
    private readonly System.Windows.Forms.Timer expiryTimer = new() { Interval = 1000 };

    public AuthenticationApplicationContext(
        AuthApiFacade auth,
        Func<BoardForm> createShell,
        ProjectApiFacade projects,
        InvitationInboxService? invitationInbox = null)
    {
        this.auth = auth;
        this.createShell = createShell;
        this.projects = projects;
        this.invitationInbox = invitationInbox;
        auth.Session.Cleared += SessionCleared;
        projects.Context.Changed += ProjectContextChanged;
        expiryTimer.Tick += (_, _) =>
        {
            if (!switching && (MainForm is BoardForm or ProjectSelectionForm) && !auth.Session.IsAuthenticated)
                auth.Logout();
        };
        expiryTimer.Start();
        ShowLogin();
    }

    private void SessionCleared(object? sender, EventArgs e)
    {
        CurrentSession.CurrentUserId = 0;
        CurrentSession.CurrentUserName = string.Empty;
        CurrentSession.CurrentProjectId = 0;
        CurrentSession.CurrentProjectName = string.Empty;
        CurrentSession.CurrentUserRole = null;
        if (MainForm is not (BoardForm or ProjectSelectionForm)) return;
        long generation = auth.Session.Generation;
        if (MainForm is { IsHandleCreated: true, IsDisposed: false } pending)
            pending.BeginInvoke(new Action(() =>
            {
                if (!switching && !pending.IsDisposed && ReferenceEquals(MainForm, pending) &&
                    generation == auth.Session.Generation && !auth.Session.IsAuthenticated) ShowLogin();
            }));
    }

    private void ProjectContextChanged(object? sender, EventArgs e)
    {
        if (projects.Context.Current is not null || !auth.Session.IsAuthenticated || MainForm is not BoardForm) return;
        if (!switching) ShowProjectSelection();
        else if (MainForm is { IsHandleCreated: true, IsDisposed: false } pending)
            pending.BeginInvoke(new Action(() =>
            {
                if (ReferenceEquals(MainForm, pending) && projects.Context.Current is null && auth.Session.IsAuthenticated)
                    ShowProjectSelection();
            }));
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
            CurrentSession.CurrentUserId = user.UserId;
            CurrentSession.CurrentUserName = user.DisplayName;
            CurrentSession.CurrentProjectId = 0;
            CurrentSession.CurrentProjectName = string.Empty;
            CurrentSession.CurrentUserRole = null;
            ShowProjectSelection();
        };
        ShowAuthenticationPage(form);
    }

    private void ShowProjectSelection()
    {
        var form = new ProjectSelectionForm(auth, projects, invitationInbox);
        form.ProjectReady += (_, _) =>
        {
            if (!ReferenceEquals(MainForm, form) || !auth.Session.IsAuthenticated || projects.Context.Current is null) return;
            SwitchMainForm(createShell());
        };
        SwitchMainForm(form);
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

    private void SwitchMainForm(Form next)
    {
        switching = true;
        try
        {
            Form? previous = MainForm;
            MainForm = next;
            next.Show();
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
        projects.Context.Changed -= ProjectContextChanged;
        auth.Logout();
        SessionCleared(this, EventArgs.Empty);
        base.ExitThreadCore();
    }
}
