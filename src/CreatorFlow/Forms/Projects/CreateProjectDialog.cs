using CreatorFlow.ApiClients;
using CreatorFlow.Contracts.Projects;
using CreatorFlow.Helpers;
using CreatorFlow.Services;
using CreatorFlow.Theme;

namespace CreatorFlow.Forms.Projects;

public sealed class CreateProjectDialog : ProjectModalForm
{
    private readonly ProjectApiFacade projects;
    private readonly CancellationTokenSource lifetime = new();
    private readonly TextBox name = new() { MaxLength = 0, AccessibleName = "Tên Project" };
    private readonly TextBox description = new()
    {
        MaxLength = 0, Multiline = true, AcceptsReturn = true,
        ScrollBars = ScrollBars.Vertical, AccessibleName = "Mô tả"
    };
    private readonly Label message = new() { AutoSize = true, Visible = false, ForeColor = UITheme.Danger };
    private readonly Panel nameField;
    private readonly Panel descriptionField;
    private bool released;
    public ProjectResponse? CreatedProject { get; private set; }

    public CreateProjectDialog(ProjectApiFacade projects, bool discardDraft = false)
        : base("Tạo Project", discardDraft ? "Project mới sẽ được mở sau khi tạo. Bản nháp chưa lưu sẽ bị bỏ." : "Tạo không gian làm việc cho nội dung và thành viên của bạn.", "Tạo Project", 288)
    {
        this.projects = projects;
        var layout = UiTheme.CreateFormBody();
        layout.ColumnStyles.Add(new(SizeType.Percent, 100));
        nameField = UiTheme.AddField(layout, "Tên Project", name);
        descriptionField = UiTheme.AddField(layout, "Mô tả (tùy chọn)", description);
        descriptionField.Controls["fieldBorder"]!.Height = 100;
        UiTheme.AddRow(layout, message, 0);
        layout.SizeChanged += (_, _) => message.MaximumSize = new(Math.Max(1, layout.ClientSize.Width), 0);
        Body.Controls.Add(layout);
        Confirm.Click += Save;
        Shown += (_, _) => name.Focus();
    }

    private async void Save(object? sender, EventArgs e)
    {
        if (!Confirm.Enabled) return;
        UiTheme.SetFieldError(nameField, null);
        UiTheme.SetFieldError(descriptionField, null);
        Confirm.Enabled = false;
        message.Text = "Đang tạo Project…";
        message.Visible = true;

        try
        {
            CreatedProject = await projects.CreateAsync(name.Text, description.Text, lifetime.Token);
            if (!IsDisposed && !lifetime.IsCancellationRequested)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        }
        catch (OperationCanceledException) when (IsDisposed || lifetime.IsCancellationRequested) { }
        catch (Exception error) when (error is ApiException or ApiBusinessException)
        {
            if (IsDisposed) return;
            if (error is ApiBusinessException business && business.ErrorField is "Name" or "Description")
            {
                UiTheme.SetFieldError(business.ErrorField == "Name" ? nameField : descriptionField, business.Message);
                message.Visible = false;
            }
            else
            {
                message.Text = error.Message;
                message.Visible = true;
            }
        }
        finally
        {
            if (!IsDisposed) Confirm.Enabled = true;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !released)
        {
            released = true;
            lifetime.Cancel();
            lifetime.Dispose();
        }
        base.Dispose(disposing);
    }
}
