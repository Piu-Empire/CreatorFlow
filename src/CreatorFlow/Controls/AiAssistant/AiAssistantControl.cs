using CreatorFlow.Controls;
using CreatorFlow.Controls.IdeaBank;
using CreatorFlow.Models;
using CreatorFlow.Services;
using CreatorFlow.Services.AI;
using CreatorFlow.Services.Backends;
using CreatorFlow.Services.Exceptions;
using CreatorFlow.Theme;

namespace CreatorFlow.Controls.AiAssistant;

/// <summary>
/// Trang Trợ lý AI (SCRUM-57): người dùng nhập chủ đề / nền tảng / mục tiêu, nhận gợi ý Idea/Title,
/// chỉnh sửa rồi lưu thành Idea hoặc Content. Có trạng thái đang gợi ý và trạng thái lỗi (nút Thử lại).
/// Layout dựng bằng code theo DashboardControl; cổng AI do BoardForm truyền vào.
/// </summary>
public sealed class AiAssistantControl : UserControl
{
    private readonly IdeaAssistService _assist;
    private readonly Func<List<string>> _platformProvider;
    private readonly IIdeaBackend? _ideaBackend;
    private readonly ToastNotification _toast;

    private readonly TextBox _txtTopic = new();
    private readonly CheckedListBox _clbPlatforms = new();
    private readonly TextBox _txtGoal = new();
    private readonly RoundedButton _btnSuggest = new();
    private readonly RoundedButton _btnRetry = new();
    private readonly Label _lblStatus = new();
    private readonly ListBox _lstSuggestions = new();
    private readonly TextBox _txtTitle = new();
    private readonly TextBox _txtDescription = new();
    private readonly TextBox _txtTags = new();
    private readonly RoundedButton _btnSaveIdea = new();
    private readonly RoundedButton _btnSaveContent = new();

    private IdeaSuggestRequest _lastRequest = new();
    private CancellationTokenSource? _requestCts;

    /// <summary>Phát khi có Idea/Content mới được lưu (BoardForm dùng để làm mới số Backlog).</summary>
    public event EventHandler? IdeasChanged;

    public event EventHandler? CloseRequested;

    public AiAssistantControl(
        IdeaAssistService assist,
        Func<List<string>> platformProvider,
        IIdeaBackend? ideaBackend)
    {
        _assist = assist;
        _platformProvider = platformProvider;
        _ideaBackend = ideaBackend;

        BackColor = UITheme.Neutral50;
        Padding = new Padding(32, 24, 32, 24);

        _toast = new ToastNotification();
        Controls.Add(_toast);

        BuildLayout();

        Load += (_, _) => LoadPlatforms();
        Disposed += (_, _) => _requestCts?.Cancel();
    }

    private static long ProjectId => CurrentSession.CurrentProjectId;
    private static long UserId => CurrentSession.CurrentUserId;

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = UITheme.Neutral50,
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72f));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var header = new Panel { Dock = DockStyle.Fill, BackColor = UITheme.Neutral50 };
        header.Controls.Add(new Label
        {
            Text = "Trợ lý AI",
            Font = UITheme.FontPageTitle,
            ForeColor = UITheme.Ink,
            AutoSize = true,
            Location = new Point(0, 0),
        });
        header.Controls.Add(new Label
        {
            Text = "Nhập chủ đề, nền tảng và mục tiêu để nhận gợi ý ý tưởng; chỉnh sửa trước khi lưu thành Idea hoặc Content.",
            Font = UITheme.FontBody,
            ForeColor = UITheme.Neutral600,
            AutoSize = true,
            Location = new Point(1, 40),
        });
        var btnClose = new ProjectActionButton
        {
            Text = "Đóng",
            Style = RoundButtonStyle.Secondary,
            Width = 90,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(Width, 0),
        };
        btnClose.Click += (_, _) => CloseRequested?.Invoke(this, EventArgs.Empty);
        header.Controls.Add(btnClose);
        header.Resize += (_, _) => btnClose.Location = new Point(header.Width - btnClose.Width, 6);

        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = UITheme.Neutral50,
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 380f));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        body.Controls.Add(BuildInputCard(), 0, 0);
        body.Controls.Add(BuildResultCard(), 1, 0);

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(body, 0, 1);
        Controls.Add(root);
    }

    private Control BuildInputCard()
    {
        var card = new IdeaCardPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 20, 0), Padding = new Padding(18) };

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 8, BackColor = UITheme.White };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 140f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        layout.Controls.Add(FieldLabel("Chủ đề *"), 0, 0);
        _txtTopic.Dock = DockStyle.Fill;
        _txtTopic.Font = UITheme.FontBody;
        _txtTopic.PlaceholderText = "Ví dụ: mẹo học tiếng Anh cho người mới";
        layout.Controls.Add(_txtTopic, 0, 1);

        layout.Controls.Add(FieldLabel("Nền tảng"), 0, 2);
        _clbPlatforms.Dock = DockStyle.Fill;
        _clbPlatforms.Font = UITheme.FontBody;
        _clbPlatforms.CheckOnClick = true;
        _clbPlatforms.IntegralHeight = false;
        _clbPlatforms.BorderStyle = BorderStyle.FixedSingle;
        layout.Controls.Add(_clbPlatforms, 0, 3);

        layout.Controls.Add(FieldLabel("Mục tiêu *"), 0, 4);
        _txtGoal.Dock = DockStyle.Fill;
        _txtGoal.Font = UITheme.FontBody;
        _txtGoal.Multiline = true;
        _txtGoal.ScrollBars = ScrollBars.Vertical;
        _txtGoal.PlaceholderText = "Ví dụ: tăng lượt theo dõi, giới thiệu sản phẩm mới…";
        layout.Controls.Add(_txtGoal, 0, 5);

        _btnSuggest.Text = "Gợi ý";
        _btnSuggest.Style = RoundButtonStyle.Primary;
        _btnSuggest.ShowPlusIcon = true;
        _btnSuggest.Dock = DockStyle.Fill;
        _btnSuggest.Click += async (_, _) => await RunSuggestAsync();
        layout.Controls.Add(_btnSuggest, 0, 6);

        _lblStatus.Dock = DockStyle.Fill;
        _lblStatus.Font = UITheme.FontBody;
        _lblStatus.ForeColor = UITheme.Neutral600;
        _lblStatus.AutoEllipsis = true;
        layout.Controls.Add(_lblStatus, 0, 7);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildResultCard()
    {
        var card = new IdeaCardPanel { Dock = DockStyle.Fill, Padding = new Padding(18) };

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 7, BackColor = UITheme.White };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 42f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32f));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));

        layout.Controls.Add(FieldLabel("Gợi ý từ AI"), 0, 0);
        _lstSuggestions.Dock = DockStyle.Fill;
        _lstSuggestions.Font = UITheme.FontBody;
        _lstSuggestions.BorderStyle = BorderStyle.FixedSingle;
        _lstSuggestions.IntegralHeight = false;
        _lstSuggestions.SelectedIndexChanged += (_, _) => OnSuggestionSelected();
        layout.Controls.Add(_lstSuggestions, 0, 1);

        layout.Controls.Add(FieldLabel("Chỉnh sửa trước khi lưu"), 0, 2);
        _txtTitle.Dock = DockStyle.Fill;
        _txtTitle.Font = UITheme.FontBody;
        _txtTitle.PlaceholderText = "Tiêu đề";
        layout.Controls.Add(_txtTitle, 0, 3);

        _txtDescription.Dock = DockStyle.Fill;
        _txtDescription.Font = UITheme.FontBody;
        _txtDescription.Multiline = true;
        _txtDescription.ScrollBars = ScrollBars.Vertical;
        _txtDescription.PlaceholderText = "Mô tả";
        layout.Controls.Add(_txtDescription, 0, 4);

        _txtTags.Dock = DockStyle.Fill;
        _txtTags.Font = UITheme.FontBody;
        _txtTags.PlaceholderText = "Tags, cách nhau bởi dấu phẩy (bỏ trống = dùng nền tảng đã chọn)";
        layout.Controls.Add(_txtTags, 0, 5);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = UITheme.White };
        _btnSaveIdea.Text = "Lưu thành Idea";
        _btnSaveIdea.Style = RoundButtonStyle.Primary;
        _btnSaveIdea.Width = 150;
        _btnSaveIdea.Margin = new Padding(0, 0, 10, 0);
        _btnSaveIdea.Click += (_, _) => SaveSuggestion(convertToContent: false);

        _btnSaveContent.Text = "Lưu thành Content";
        _btnSaveContent.Style = RoundButtonStyle.Secondary;
        _btnSaveContent.Width = 165;
        _btnSaveContent.Click += (_, _) => SaveSuggestion(convertToContent: true);

        _btnRetry.Text = "Thử lại";
        _btnRetry.Style = RoundButtonStyle.Ghost;
        _btnRetry.Width = 100;
        _btnRetry.Visible = false;
        _btnRetry.Click += async (_, _) => await RunSuggestAsync();

        actions.Controls.Add(_btnSaveIdea);
        actions.Controls.Add(_btnSaveContent);
        actions.Controls.Add(_btnRetry);
        layout.Controls.Add(actions, 0, 6);

        card.Controls.Add(layout);
        return card;
    }

    private static Label FieldLabel(string text) => new()
    {
        Text = text,
        Font = UITheme.FontLabelBold,
        ForeColor = UITheme.Neutral600,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.BottomLeft,
    };

    private void LoadPlatforms()
    {
        _clbPlatforms.Items.Clear();
        try
        {
            foreach (string platform in _platformProvider())
            {
                _clbPlatforms.Items.Add(platform, false);
            }
        }
        catch
        {
            // Không lấy được danh sách nền tảng: vẫn cho phép gợi ý (không chọn nền tảng nào).
        }

        UpdateButtons();
    }

    private IdeaSuggestRequest BuildRequest() => new()
    {
        Topic = _txtTopic.Text.Trim(),
        Goal = _txtGoal.Text.Trim(),
        Platforms = _clbPlatforms.CheckedItems.Cast<string>().ToList(),
    };

    private async Task RunSuggestAsync()
    {
        IdeaSuggestRequest request = BuildRequest();
        if (request.Topic.Length == 0)
        {
            ShowStatus("Vui lòng nhập chủ đề.", isError: true);
            _txtTopic.Focus();
            return;
        }

        if (request.Goal.Length == 0)
        {
            ShowStatus("Vui lòng nhập mục tiêu.", isError: true);
            _txtGoal.Focus();
            return;
        }

        _requestCts?.Cancel();
        _requestCts = new CancellationTokenSource();
        ShowStatus("Đang gợi ý…", isError: false);
        SetBusy(true);
        _lstSuggestions.Items.Clear();
        _txtTitle.Clear();
        _txtDescription.Clear();

        IdeaAssistResult result;
        try
        {
            result = await _assist.SuggestAsync(request, _requestCts.Token);
        }
        catch (Exception)
        {
            result = IdeaAssistResult.Failure("Có lỗi xảy ra khi gọi AI. Vui lòng thử lại.");
        }

        if (IsDisposed)
        {
            return;
        }

        SetBusy(false);
        if (!result.Succeeded)
        {
            ShowStatus(result.ErrorMessage ?? "Không nhận được gợi ý.", isError: true);
            _btnRetry.Visible = true;
            return;
        }

        _lastRequest = request;
        foreach (IdeaSuggestion suggestion in result.Suggestions)
        {
            _lstSuggestions.Items.Add(new SuggestionEntry(suggestion));
        }

        _btnRetry.Visible = false;
        ShowStatus($"Đã nhận {result.Suggestions.Count} gợi ý. Chọn một gợi ý để chỉnh sửa.", isError: false);
        if (_lstSuggestions.Items.Count > 0)
        {
            _lstSuggestions.SelectedIndex = 0;
        }
    }

    private void OnSuggestionSelected()
    {
        if (_lstSuggestions.SelectedItem is SuggestionEntry entry)
        {
            _txtTitle.Text = entry.Suggestion.Title;
            _txtDescription.Text = entry.Suggestion.Description;
        }

        UpdateButtons();
    }

    private void SaveSuggestion(bool convertToContent)
    {
        if (_ideaBackend is null)
        {
            ShowStatus("Không có nguồn dữ liệu để lưu.", isError: true);
            return;
        }

        string title = _txtTitle.Text.Trim();
        if (title.Length == 0)
        {
            ShowStatus("Vui lòng nhập tiêu đề trước khi lưu.", isError: true);
            _txtTitle.Focus();
            return;
        }

        IdeaSuggestRequest request = BuildRequest();
        List<string>? tags = ParseTags();
        IdeaDraft draft = IdeaSuggestionDraftMapper.ToDraft(request, title, _txtDescription.Text, tags);

        try
        {
            long ideaId = _ideaBackend.Create(ProjectId, draft, UserId);
            if (convertToContent)
            {
                _ideaBackend.ConvertToContent(ideaId, UserId);
            }
        }
        catch (IdeaValidationException ex)
        {
            ShowStatus(ex.Message, isError: true);
            return;
        }
        catch (UnauthorizedWorkflowActionException ex)
        {
            ShowStatus(ex.Message, isError: true);
            return;
        }
        catch (Exception)
        {
            ShowStatus("Không lưu được. Vui lòng thử lại.", isError: true);
            return;
        }

        ShowStatus(convertToContent ? "Đã lưu Idea và chuyển thành Content." : "Đã lưu thành Idea.", isError: false);
        IdeasChanged?.Invoke(this, EventArgs.Empty);
        _toast.ShowToast(this, convertToContent ? "Da luu thanh Content" : "Da luu thanh Idea");
    }

    private List<string>? ParseTags()
    {
        List<string> tags = _txtTags.Text
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        return tags.Count == 0 ? null : tags;
    }

    private void SetBusy(bool busy)
    {
        _btnSuggest.Enabled = !busy;
        _txtTopic.Enabled = !busy;
        _clbPlatforms.Enabled = !busy;
        _txtGoal.Enabled = !busy;
        UseWaitCursor = busy;
    }

    private void UpdateButtons()
    {
        bool canCreate = _ideaBackend?.CanCreate(ProjectId, UserId) == true && _lstSuggestions.Items.Count > 0;
        _btnSaveIdea.Enabled = canCreate;
        _btnSaveContent.Enabled = canCreate;
    }

    private void ShowStatus(string message, bool isError)
    {
        _lblStatus.Text = message;
        _lblStatus.ForeColor = isError ? UITheme.Danger : UITheme.Neutral600;
        if (!isError)
        {
            _btnRetry.Visible = false;
        }
    }

    /// <summary>Một dòng trong danh sách gợi ý: hiện tiêu đề kèm mô tả ngắn.</summary>
    private sealed class SuggestionEntry
    {
        public SuggestionEntry(IdeaSuggestion suggestion) => Suggestion = suggestion;

        public IdeaSuggestion Suggestion { get; }

        public override string ToString() =>
            string.IsNullOrWhiteSpace(Suggestion.Description)
                ? Suggestion.Title
                : $"{Suggestion.Title} — {Suggestion.Description}";
    }
}
