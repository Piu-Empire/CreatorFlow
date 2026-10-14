using CreatorFlow.ApiClients;
using CreatorFlow.Controls;
using CreatorFlow.Services;
using CreatorFlow.Forms.Invitations;
using CreatorFlow.Theme;

namespace CreatorFlow.Forms.Projects;

public sealed class ProjectSelectionForm : Form
{
    private readonly AuthApiFacade auth;
    private readonly ProjectApiFacade projects;
    private readonly CancellationTokenSource lifetime = new();
    private readonly ProjectPickerControl picker;
    private bool busy;
    private bool released;
    public event EventHandler? ProjectReady;
    public ProjectSelectionForm(AuthApiFacade auth, ProjectApiFacade projects, InvitationInboxService? invitationInbox = null)
    {
        this.auth = auth; this.projects = projects;
        Text = "CreatorFlow — Chọn Project"; Name = nameof(ProjectSelectionForm);
        StartPosition = FormStartPosition.CenterScreen; Size = new(1040, 720); MinimumSize = new(720, 480);
        BackColor = UITheme.Neutral50; Font = UITheme.FontBody;
        picker = new ProjectPickerControl(projects); Controls.Add(picker);
        if (invitationInbox is not null)
        {
            var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 64, Padding = new Padding(32, 8, 32, 8), BackColor = UITheme.Neutral50 };
            var inbox = new ProjectActionButton { Text = "Lời mời của tôi", Style = RoundButtonStyle.Ghost, Width = 200, Height = 40 };
            footer.Controls.Add(inbox); Controls.Add(footer);
            inbox.Click += async (_, _) =>
            {
                if (busy || IsDisposed || !auth.Session.IsAuthenticated) return;
                busy = true; picker.Enabled = false;
                long authGeneration = auth.Session.Generation;
                try
                {
                    using var dialog = new MyInvitationsDialog(invitationInbox);
                    dialog.ShowDialog(this);
                    if (!IsDisposed && auth.Session.IsAuthenticated && authGeneration == auth.Session.Generation)
                        await picker.ReloadAsync();
                }
                finally { if (!IsDisposed) { busy = false; picker.Enabled = true; } }
            };
        }
        picker.SelectRequested += async id => await SelectAsync(id, true);
        picker.ProjectCreated += async created => await SelectAsync(created.ProjectId, false);
        Shown += (_, _) => BeginInvoke(new Action(async () => { if (!IsDisposed) await picker.ReloadAsync(); }));
    }
    private async Task SelectAsync(long id, bool confirmSelection)
    {
        if (busy || !auth.Session.IsAuthenticated || IsDisposed) return;
        busy = true;
        long authGeneration = auth.Session.Generation, generation = projects.Context.Generation;
        bool Valid() => !IsDisposed && !lifetime.IsCancellationRequested && auth.Session.IsAuthenticated &&
            authGeneration == auth.Session.Generation && generation == projects.Context.Generation;
        try
        {
            var access = await projects.AccessAsync(id, lifetime.Token);
            if (!Valid()) return;
            if (confirmSelection)
            {
                using var dialog = new ProjectSwitchDialog(access.Name, true);
                if (dialog.ShowProjectDialog(this) != DialogResult.OK || !Valid()) return;
                var latest = await projects.AccessAsync(id, lifetime.Token);
                if (!Valid()) return;
                if (latest.Name != access.Name) { picker.ShowSelectionError("Tên Project đã thay đổi. Vui lòng chọn lại để xác nhận."); return; }
                access = latest;
            }
            projects.Context.Apply(access);
            ProjectReady?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException) when (IsDisposed || lifetime.IsCancellationRequested || !auth.Session.IsAuthenticated) { }
        catch (Exception error) when (error is ApiException or ApiBusinessException)
        {
            if (Valid())
            {
                using var dialog = new ProjectModalForm("Không thể mở Project", error.Message, "Đóng", 64, true);
                dialog.ShowProjectDialog(this);
            }
        }
        finally { if (!IsDisposed) busy = false; }
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing && !released) { released = true; lifetime.Cancel(); lifetime.Dispose(); }
        base.Dispose(disposing);
    }
}
