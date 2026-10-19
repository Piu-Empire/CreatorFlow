using CreatorFlow.ApiClients;
using CreatorFlow.Controls;
using CreatorFlow.Contracts.Projects;
using CreatorFlow.Helpers;
using CreatorFlow.Services;
using CreatorFlow.Theme;

namespace CreatorFlow.Forms.Projects;

public sealed partial class CreateProjectDialog : Form
{
    private readonly ProjectApiFacade projects;
    private readonly CancellationTokenSource lifetime = new();
    private readonly TextBox name = new() { MaxLength = 0, AccessibleName = "Tên Project" };
    private readonly TextBox description = new()
    {
        MaxLength = 0,
        Multiline = true,
        AcceptsReturn = true,
        ScrollBars = ScrollBars.Vertical,
        AccessibleName = "Mô tả"
    };
    private readonly Label message = new()
    {
        AutoSize = true,
        Visible = false,
        ForeColor = UITheme.Danger,
        Font = UITheme.FontBody
    };
    private Panel nameField = null!;
    private Panel descriptionField = null!;
    private bool released;

    public ProjectResponse? CreatedProject { get; private set; }

    public CreateProjectDialog(ProjectApiFacade projects, bool discardDraft = false)
    {
        this.projects = projects;
        InitializeComponent();
        Font = UITheme.FontBody;
        BackColor = UITheme.White;
        headerPanel.BackColor = UITheme.White;
        bodyPanel.BackColor = UITheme.White;
        footerPanel.BackColor = UITheme.Neutral50;
        titleLabel.Font = UITheme.FontPageTitle;
        titleLabel.ForeColor = UITheme.Ink;
        explanationLabel.ForeColor = UITheme.Neutral600;

        var subtitle = discardDraft
            ? "Project mới sẽ được mở sau khi tạo. Bản nháp chưa lưu sẽ bị bỏ."
            : "Tạo không gian làm việc cho nội dung và thành viên của bạn.";
        explanationLabel.Text = subtitle;
        BuildFields();
        confirm.Click += Save;
        Shown += (_, _) => name.Focus();
    }

    private void BuildFields()
    {
        var fields = UiTheme.CreateFormBody();
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        nameField = UiTheme.AddField(fields, "Tên Project", name);
        descriptionField = UiTheme.AddField(fields, "Mô tả (tùy chọn)", description);
        nameField.Controls["fieldBorder"]!.Height = 56;
        descriptionField.Controls["fieldBorder"]!.Height = 160;

        StyleField(nameField);
        StyleField(descriptionField);
        UiTheme.AddRow(fields, message, 0);
        fields.SizeChanged += (_, _) => message.MaximumSize = new Size(Math.Max(1, fields.ClientSize.Width), 0);
        bodyPanel.Controls.Add(fields);
    }

    private static void StyleField(Panel field)
    {
        var label = field.Controls.OfType<Label>()
            .First(control => control.Name != "fieldError");
        label.ForeColor = UITheme.Ink;
        label.Font = new Font("Segoe UI", 12F, FontStyle.Bold);

        if (field.Controls["fieldBorder"] is Panel border && border.Controls.OfType<TextBox>().FirstOrDefault() is { } input)
        {
            input.Font = new Font("Segoe UI", 12F);
            input.AccessibleName = label.Text;
            border.Invalidate();
        }
    }

    private void HeaderPanel_Paint(object? sender, PaintEventArgs e)
    {
        using var pen = new Pen(UITheme.Neutral100);
        e.Graphics.DrawLine(pen, 0, headerPanel.ClientSize.Height - 1,
            headerPanel.ClientSize.Width, headerPanel.ClientSize.Height - 1);
    }

    private void FooterPanel_Paint(object? sender, PaintEventArgs e)
    {
        using var pen = new Pen(UITheme.Neutral100);
        e.Graphics.DrawLine(pen, 0, 0, footerPanel.ClientSize.Width, 0);
    }

    private async void Save(object? sender, EventArgs e)
    {
        if (!confirm.Enabled) return;
        UiTheme.SetFieldError(nameField, null);
        UiTheme.SetFieldError(descriptionField, null);
        confirm.Enabled = false;
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
            if (!IsDisposed) confirm.Enabled = true;
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
