using CreatorFlow.ApiClients;
using CreatorFlow.Controls;
using CreatorFlow.Contracts.Projects;
using CreatorFlow.Helpers;
using CreatorFlow.Services;
using CreatorFlow.Theme;

namespace CreatorFlow.Forms.Projects;

public sealed class CreateProjectDialog : Form
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
    private readonly ProjectActionButton confirm = new()
    {
        Name = "createProjectConfirm",
        Text = "Tạo Project",
        Width = 162,
        Height = 52
    };
    private Panel nameField = null!;
    private Panel descriptionField = null!;
    private bool released;

    public ProjectResponse? CreatedProject { get; private set; }

    public CreateProjectDialog(ProjectApiFacade projects, bool discardDraft = false)
    {
        this.projects = projects;

        Name = nameof(CreateProjectDialog);
        Text = "Tạo Project";
        Font = UITheme.FontBody;
        BackColor = UITheme.White;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MaximizeBox = false;
        MinimizeBox = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(760, 590);

        var subtitle = discardDraft
            ? "Project mới sẽ được mở sau khi tạo. Bản nháp chưa lưu sẽ bị bỏ."
            : "Tạo không gian làm việc cho nội dung và thành viên của bạn.";
        var header = BuildHeader(subtitle);
        var body = BuildBody();
        var footer = BuildFooter();

        Controls.Add(body);
        Controls.Add(footer);
        Controls.Add(header);

        CancelButton = footer.Controls.OfType<FlowLayoutPanel>()
            .SelectMany(buttons => buttons.Controls.OfType<ProjectActionButton>())
            .First(button => button.Name == "createProjectCancel");
        AcceptButton = confirm;
        confirm.Click += Save;
        Shown += (_, _) => name.Focus();
    }

    private Panel BuildHeader(string subtitle)
    {
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 130,
            BackColor = UITheme.White,
        };
        var title = new Label
        {
            Text = "Tạo Project",
            Location = new Point(38, 24),
            Size = new Size(header.ClientSize.Width - 76, 34),
            Font = UITheme.FontPageTitle,
            ForeColor = UITheme.Ink,
            AutoEllipsis = true
        };
        var explanation = new Label
        {
            Text = subtitle,
            Location = new Point(38, 68),
            Size = new Size(header.ClientSize.Width - 76, 30),
            Font = new Font("Segoe UI", 12F),
            ForeColor = UITheme.Neutral600,
            AutoEllipsis = true
        };

        header.Controls.Add(title);
        header.Controls.Add(explanation);
        header.SizeChanged += (_, _) =>
        {
            title.Width = Math.Max(1, header.ClientSize.Width - 76);
            explanation.Width = Math.Max(1, header.ClientSize.Width - 76);
        };
        header.Paint += (_, e) =>
        {
            using var pen = new Pen(UITheme.Neutral100);
            e.Graphics.DrawLine(pen, 0, header.ClientSize.Height - 1,
                header.ClientSize.Width, header.ClientSize.Height - 1);
        };
        return header;
    }

    private Panel BuildBody()
    {
        var body = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(38, 24, 38, 20),
            BackColor = UITheme.White,
            AutoScroll = true
        };
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
        body.Controls.Add(fields);
        return body;
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

    private Panel BuildFooter()
    {
        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 88,
            Padding = new Padding(38, 17, 38, 17),
            BackColor = UITheme.Neutral50
        };
        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 274,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty
        };
        var cancel = new ProjectActionButton
        {
            Name = "createProjectCancel",
            Text = "Hủy",
            Style = RoundButtonStyle.Secondary,
            DialogResult = DialogResult.Cancel,
            Width = 96,
            Height = 52,
            Margin = new Padding(0, 0, 16, 0)
        };
        confirm.Margin = Padding.Empty;
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(confirm);
        footer.Controls.Add(buttons);
        footer.Paint += (_, e) =>
        {
            using var pen = new Pen(UITheme.Neutral100);
            e.Graphics.DrawLine(pen, 0, 0, footer.ClientSize.Width, 0);
        };
        return footer;
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
