using CreatorFlow.Controls;
using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Services;
using CreatorFlow.Services.AI;
using CreatorFlow.Services.Backends;
using CreatorFlow.Services.Exceptions;
using CreatorFlow.Theme;

namespace CreatorFlow.Forms.Board;

/// <summary>
/// Dialog AI Repurpose (SCRUM-59): chọn platform đích → AI đề xuất → người dùng sửa → xác nhận tạo Content mới.
/// AI không tự tạo/lưu; chỉ tạo khi bấm "Tạo Content mới" và xác nhận.
/// </summary>
public sealed class RepurposeDialog : Form
{
    private const int Pad = 24;

    private readonly ContentBoardCard _source;
    private readonly Content? _detail;
    private readonly string _sourceScript;
    private readonly IReadOnlyList<string> _platforms;
    private readonly IRepurposeAiService _ai;
    private readonly ContentService? _contentService;
    private readonly IContentDetailBackend? _backend;
    private readonly long _userId;

    private readonly ComboBox _cboPlatform = new();
    private readonly TextBox _txtInstruction = new();
    private readonly RoundedButton _btnGenerate = new();
    private readonly RoundedButton _btnCreate = new();
    private readonly RoundedButton _btnCancel = new();
    private readonly Label _lblStatus = new();
    private readonly TextBox _txtTitle = new();
    private readonly TextBox _txtDescription = new();
    private readonly TextBox _txtScript = new();

    private CancellationTokenSource? _cts;
    private bool _busy;
    private bool _hasPreview;
    private string _lastTitle = string.Empty;
    private string _lastDescription = string.Empty;
    private string _lastScript = string.Empty;

    public long CreatedContentId { get; private set; }

    public RepurposeDialog(
        ContentBoardCard source,
        Content? detail,
        string sourceScript,
        bool scriptLoadFailed,
        IReadOnlyList<string> platforms,
        IRepurposeAiService ai,
        ContentService? contentService,
        IContentDetailBackend? backend,
        long userId)
    {
        _source = source;
        _detail = detail;
        _sourceScript = sourceScript;
        _platforms = platforms;
        _ai = ai;
        _contentService = contentService;
        _backend = backend;
        _userId = userId;

        BuildLayout();
        LoadPlatforms();

        if (scriptLoadFailed)
            SetStatus("Không đọc được kịch bản gốc; AI sẽ dựa trên tiêu đề/mô tả.", UITheme.Warning);
        else if (!_ai.IsConfigured)
            SetStatus(NotConfiguredAiService.Message, UITheme.Danger);
    }

    private void BuildLayout()
    {
        Text = "Tái sử dụng nội dung bằng AI";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        BackColor = UITheme.Neutral50;
        Font = UITheme.FontBody;
        ClientSize = new Size(780, 720);
        Padding = new Padding(Pad);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = UITheme.Neutral50,
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildOptions(), 0, 1);

        _lblStatus.AutoSize = false;
        _lblStatus.Dock = DockStyle.Fill;
        _lblStatus.Height = 28;
        _lblStatus.Font = UITheme.FontBody;
        _lblStatus.ForeColor = UITheme.Neutral600;
        _lblStatus.TextAlign = ContentAlignment.MiddleLeft;
        root.Controls.Add(_lblStatus, 0, 2);

        root.Controls.Add(BuildPreview(), 0, 3);
        root.Controls.Add(BuildFooter(), 0, 4);

        AcceptButton = null;
        CancelButton = _btnCancel;

        FormClosed += (_, _) => _cts?.Cancel();
    }

    private Control BuildHeader()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            BackColor = UITheme.Neutral50,
            Margin = new Padding(0, 0, 0, 12),
        };

        panel.Controls.Add(new Label
        {
            Text = "Tái sử dụng nội dung bằng AI",
            Font = UITheme.FontH2,
            ForeColor = UITheme.Neutral900,
            AutoSize = true,
            Margin = Padding.Empty,
        });

        panel.Controls.Add(new Label
        {
            Text = $"{_source.Code} · {_source.Title}",
            Font = UITheme.FontBody,
            ForeColor = UITheme.Neutral600,
            AutoSize = true,
            Margin = new Padding(0, 4, 0, 0),
        });

        return panel;
    }

    private Control BuildOptions()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            AutoSize = true,
            BackColor = UITheme.Neutral50,
            Margin = new Padding(0, 0, 0, 8),
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        layout.Controls.Add(FieldLabel("Platform đích"), 0, 0);
        _cboPlatform.Dock = DockStyle.Fill;
        _cboPlatform.DropDownStyle = ComboBoxStyle.DropDownList;
        _cboPlatform.FlatStyle = FlatStyle.Flat;
        _cboPlatform.Margin = new Padding(0, 2, 0, 8);
        layout.Controls.Add(_cboPlatform, 1, 0);

        layout.Controls.Add(FieldLabel("Yêu cầu thêm"), 0, 1);
        _txtInstruction.Dock = DockStyle.Fill;
        _txtInstruction.Multiline = true;
        _txtInstruction.Height = 52;
        _txtInstruction.PlaceholderText = "VD: ngắn hơn 30 giây, giọng thân thiện, CTA mời bình luận...";
        _txtInstruction.Margin = new Padding(0, 2, 0, 8);
        layout.Controls.Add(_txtInstruction, 1, 1);

        layout.Controls.Add(new Label(), 0, 2);
        _btnGenerate.Text = "Đề xuất bằng AI";
        _btnGenerate.Style = RoundButtonStyle.Primary;
        _btnGenerate.Size = new Size(160, 36);
        _btnGenerate.Margin = new Padding(0, 0, 0, 4);
        _btnGenerate.Click += async (_, _) => await GenerateAsync();
        layout.Controls.Add(_btnGenerate, 1, 2);

        return layout;
    }

    private Control BuildPreview()
    {
        var box = new GroupBox
        {
            Text = "Nội dung đề xuất (sửa được trước khi tạo)",
            Dock = DockStyle.Fill,
            Font = UITheme.FontBodyBold,
            ForeColor = UITheme.Neutral700,
            BackColor = UITheme.White,
            Padding = new Padding(12),
            Margin = new Padding(0),
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            BackColor = UITheme.White,
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _txtTitle.Dock = DockStyle.Fill;
        _txtTitle.MaxLength = ContentService.MaxTitleLength;
        _txtTitle.Margin = new Padding(0, 2, 0, 8);
        _txtDescription.Dock = DockStyle.Fill;
        _txtDescription.Multiline = true;
        _txtDescription.ScrollBars = ScrollBars.Vertical;
        _txtDescription.MaxLength = ContentService.MaxDescriptionLength;
        _txtDescription.Margin = new Padding(0, 2, 0, 8);
        _txtScript.Dock = DockStyle.Fill;
        _txtScript.Multiline = true;
        _txtScript.ScrollBars = ScrollBars.Vertical;
        _txtScript.AcceptsReturn = true;
        _txtScript.WordWrap = true;
        // TextBox đếm \r\n là 2 ký tự; giới hạn thật do ContentService kiểm tra.
        _txtScript.MaxLength = ContentService.MaxScriptLength * 2;
        _txtScript.Margin = new Padding(0, 2, 0, 0);

        layout.Controls.Add(FieldLabel("Tiêu đề"), 0, 0);
        layout.Controls.Add(_txtTitle, 0, 1);
        layout.Controls.Add(FieldLabel("Mô tả"), 0, 2);
        layout.Controls.Add(_txtDescription, 0, 3);
        layout.Controls.Add(FieldLabel("Kịch bản"), 0, 4);
        layout.Controls.Add(_txtScript, 0, 5);

        box.Controls.Add(layout);
        return box;
    }

    private Control BuildFooter()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            AutoSize = true,
            BackColor = UITheme.Neutral50,
            Margin = new Padding(0, 12, 0, 0),
        };

        _btnCancel.Text = "Hủy";
        _btnCancel.Style = RoundButtonStyle.Secondary;
        _btnCancel.Size = new Size(100, 38);
        _btnCancel.Margin = new Padding(8, 0, 0, 0);
        _btnCancel.Click += (_, _) => Close();

        _btnCreate.Text = "Tạo Content mới";
        _btnCreate.Style = RoundButtonStyle.Primary;
        _btnCreate.Size = new Size(160, 38);
        _btnCreate.Enabled = false;
        _btnCreate.Margin = Padding.Empty;
        _btnCreate.Click += (_, _) => CreateContent();

        panel.Controls.Add(_btnCancel);
        panel.Controls.Add(_btnCreate);
        return panel;
    }

    private static Label FieldLabel(string text) => new()
    {
        Text = text,
        Font = UITheme.FontLabelBold,
        ForeColor = UITheme.Neutral600,
        AutoSize = false,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        Margin = Padding.Empty,
    };

    private void LoadPlatforms()
    {
        _cboPlatform.Items.Clear();
        foreach (string platform in _platforms)
            _cboPlatform.Items.Add(platform);

        if (_cboPlatform.Items.Count == 0)
        {
            SetStatus("Chưa có nền tảng nào đang bật. Hãy kiểm tra catalog platform.", UITheme.Danger);
            _btnGenerate.Enabled = false;
            return;
        }

        string? target = _platforms.FirstOrDefault(
            p => !_source.Platforms.Contains(p, StringComparer.OrdinalIgnoreCase));
        _cboPlatform.SelectedItem = target ?? _platforms[0];

        if (!_ai.IsConfigured)
            _btnGenerate.Enabled = false;
    }

    private async Task GenerateAsync()
    {
        if (_busy || _cboPlatform.SelectedItem is not string targetPlatform)
            return;

        if (HasEditedPreview() && MessageBox.Show(this,
                "Bạn đã sửa bản đề xuất. Tạo lại sẽ bỏ các chỉnh sửa này. Tiếp tục?",
                "Tạo lại đề xuất", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        SetBusy(true);
        SetStatus("Đang tạo bản đề xuất...", UITheme.Neutral600);

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        try
        {
            var request = new RepurposeAiRequest
            {
                SourceContentId = _source.ContentId,
                ProjectId = _source.ProjectId,
                RequestedByUserId = _userId,
                Title = _source.Title,
                Description = _detail?.Description ?? _source.Description,
                Script = string.IsNullOrWhiteSpace(_sourceScript) ? null : _sourceScript,
                ContentType = _detail?.ContentType,
                Priority = _source.Priority,
                SourcePlatforms = _source.Platforms.ToList(),
                TargetPlatform = targetPlatform,
                Instruction = string.IsNullOrWhiteSpace(_txtInstruction.Text) ? null : _txtInstruction.Text.Trim(),
            };

            RepurposeAiResponse response = await _ai.RequestRepurposeAsync(request, _cts.Token);
            if (!response.Succeeded)
            {
                SetStatus(response.ErrorMessage ?? "Không nhận được kết quả từ AI.", UITheme.Danger);
                return;
            }

            ApplyPreview(response);
        }
        catch (OperationCanceledException)
        {
            // Người dùng đóng dialog/hủy yêu cầu: không hiện lỗi.
        }
        catch (Exception)
        {
            SetStatus("Không thể kết nối dịch vụ AI lúc này. Vui lòng thử lại sau.", UITheme.Danger);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void ApplyPreview(RepurposeAiResponse response)
    {
        _txtTitle.Text = response.Title ?? _source.Title;
        _txtDescription.Text = response.Description ?? _detail?.Description ?? _source.Description;
        _txtScript.Text = (response.Script ?? string.Empty).Replace("\n", Environment.NewLine);

        _lastTitle = _txtTitle.Text;
        _lastDescription = _txtDescription.Text;
        _lastScript = _txtScript.Text;

        _btnCreate.Enabled = true;
        _hasPreview = true;
        if (response.IsStructured)
            SetStatus("Đã có bản đề xuất. Kiểm tra và sửa lại nếu cần, rồi bấm Tạo Content mới.", UITheme.Success);
        else
            SetStatus("AI trả về không đúng định dạng, vui lòng kiểm tra lại nội dung.", UITheme.Warning);
    }

    private bool HasEditedPreview() =>
        _hasPreview &&
        (_txtTitle.Text != _lastTitle || _txtDescription.Text != _lastDescription || _txtScript.Text != _lastScript);

    private void CreateContent()
    {
        if (_busy || _cboPlatform.SelectedItem is not string targetPlatform)
            return;

        string contentType = _detail?.ContentType is { Length: > 0 } sourceType &&
                             ContentService.AvailableContentTypes.Contains(sourceType, StringComparer.Ordinal)
            ? sourceType
            : ContentService.DefaultContentType;

        var draft = new ContentDraft
        {
            Title = _txtTitle.Text.Trim(),
            Description = _txtDescription.Text.Trim(),
            Script = _txtScript.Text,
            ContentType = contentType,
            Priority = _source.Priority,
            Platforms = new List<string> { targetPlatform },
            Sprint = _source.Sprint,
            SourceContentId = _source.ContentId,
        };

        string? error = ContentService.GetValidationError(draft);
        if (error is not null)
        {
            SetStatus(error, UITheme.Danger);
            return;
        }

        if (MessageBox.Show(this,
                $"Tạo Content mới từ {_source.Code} cho nền tảng {targetPlatform}?",
                "Xác nhận tạo Content", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        SetBusy(true);
        try
        {
            CreatedContentId = _backend != null
                ? _backend.Create(_source.ProjectId, ContentStatus.Idea, draft)
                : _contentService!.Create(_source.ProjectId, ContentStatus.Idea, draft, _userId);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (ContentValidationException ex)
        {
            SetStatus(ex.Message, UITheme.Danger);
        }
        catch (UnauthorizedWorkflowActionException ex)
        {
            SetStatus(ex.Message, UITheme.Danger);
        }
        catch (InvalidOperationException ex)
        {
            SetStatus(ex.Message, UITheme.Danger);
        }
        catch (Exception ex) when (ex is Npgsql.NpgsqlException or TimeoutException)
        {
            SetStatus("Không tạo được Content. Vui lòng kiểm tra kết nối và thử lại.", UITheme.Danger);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        _cboPlatform.Enabled = !busy;
        _txtInstruction.Enabled = !busy;
        _txtTitle.Enabled = !busy;
        _txtDescription.Enabled = !busy;
        _txtScript.Enabled = !busy;
        _btnGenerate.Enabled = !busy && _ai.IsConfigured && _cboPlatform.Items.Count > 0;
        _btnCreate.Enabled = !busy && _hasPreview && _cboPlatform.Items.Count > 0;
        _btnCancel.Enabled = !busy;
        UseWaitCursor = busy;
    }

    private void SetStatus(string message, Color color)
    {
        _lblStatus.Text = message;
        _lblStatus.ForeColor = color;
    }
}
