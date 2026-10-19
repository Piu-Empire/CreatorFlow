using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Services;
using CreatorFlow.Services.AI;
using CreatorFlow.Services.Backends;
using CreatorFlow.Services.Exceptions;
using CreatorFlow.Theme;

namespace CreatorFlow.Controls;

/// <summary>
/// Khu vực nhập/chỉnh sửa kịch bản (tab Script của Content Detail). Toàn bộ nghiệp vụ nằm ở <see cref="ScriptService"/>;
/// control chỉ hiển thị, theo dõi thay đổi chưa lưu và gọi service.
///   - Lưu riêng bằng nút "Lưu kịch bản" / Ctrl+S, độc lập với nút "Lưu thay đổi" của drawer.
///   - Chỉ đọc (kèm lý do) khi User không có quyền sửa hoặc Content đã Published/Archived.
///   - SCRUM-58: "Trợ lý AI" sinh/gợi ý Outline hoặc Script từ thông tin Content. Kết quả chỉ nằm ở vùng
///     xem trước (không tự ghi đè kịch bản); người dùng bấm "Chèn vào cuối"/"Thay thế toàn bộ" (có xác nhận)
///     rồi mới "Lưu kịch bản". AI không tự lưu.
/// </summary>
public class ScriptEditorControl : UserControl
{
    private readonly TextBox _txtScript;
    private readonly Label _lblNotice;
    private readonly Label _lblStats;
    private readonly RoundedButton _btnSave;
    private readonly RoundedButton _btnAi;

    // SCRUM-58: vùng trợ lý AI (xem trước + chèn/thay thế). Gán trong BuildAiPanel() nên không readonly.
    private Panel _pnlAi = null!;
    private ComboBox _cboAiType = null!;
    private TextBox _txtAiInstruction = null!;
    private RoundedButton _btnAiGenerate = null!;
    private Label _lblAiStatus = null!;
    private TextBox _txtAiPreview = null!;
    private RoundedButton _btnAiAppend = null!;
    private RoundedButton _btnAiReplace = null!;

    private ScriptService? _service;
    private IContentDetailBackend? _backend; // chế độ API: mở/lưu script qua CreatorFlow.Api
    private ScriptDocument? _document;
    private long _userId;
    private bool _loading;
    private bool _dirty;
    private bool _aiBusy;
    private bool _aiContentApplied; // kịch bản có nội dung do AI đưa vào (để xác nhận khi lưu)
    private CancellationTokenSource? _aiCancellation;

    /// <summary>Phát sinh sau khi lưu thành công, kèm nội dung script đã chuẩn hóa vừa lưu.</summary>
    public event EventHandler<string>? ScriptSaved;

    /// <summary>true nếu trong ô đang có thay đổi chưa lưu.</summary>
    public bool IsDirty => _dirty;

    public ScriptEditorControl()
    {
        BackColor = UITheme.Neutral50;

        _txtScript = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Segoe UI", 10.5F),
            BorderStyle = BorderStyle.FixedSingle,
            AcceptsReturn = true,
            WordWrap = true,
            // TextBox mặc định chỉ nhận 32.767 ký tự và sẽ cắt âm thầm khi dán script dài.
            // Nới rộng gấp đôi giới hạn nghiệp vụ vì mỗi dấu xuống dòng của TextBox chiếm 2 ký tự (\r\n);
            // giới hạn thật do ScriptService kiểm tra và báo lỗi rõ ràng.
            MaxLength = ScriptService.MaxScriptLength * 2,
            PlaceholderText = "Kịch bản: [Hook 0-3s] ... [Body] ... [Call To Action] ...",
            ReadOnly = true,
        };
        _txtScript.TextChanged += (_, _) => OnScriptTextChanged();
        _txtScript.KeyDown += (_, e) =>
        {
            if (e.Control && e.KeyCode == Keys.S)
            {
                e.SuppressKeyPress = true;
                SaveScript();
            }
        };

        _lblNotice = new Label
        {
            Dock = DockStyle.Top,
            Height = 30,
            Padding = new Padding(10, 0, 10, 0),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = UITheme.FontBody,
            BackColor = UITheme.Neutral100,
            ForeColor = UITheme.Neutral700,
            Visible = false,
        };

        _lblStats = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = UITheme.FontBody,
            ForeColor = UITheme.Neutral600,
        };

        _btnAi = new RoundedButton
        {
            Text = "Trợ lý AI",
            Style = RoundButtonStyle.Secondary,
            Size = new Size(120, 36),
            Margin = new Padding(8, 6, 0, 6),
            Enabled = false,
        };
        _btnAi.Click += (_, _) => ToggleAiPanel();

        _btnSave = new RoundedButton
        {
            Text = "Lưu kịch bản",
            Style = RoundButtonStyle.Primary,
            Size = new Size(130, 36),
            Margin = new Padding(8, 6, 0, 6),
            Enabled = false,
            Visible = false,
        };
        _btnSave.Click += (_, _) => SaveScript();

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = UITheme.Neutral50,
        };
        buttons.Controls.Add(_btnAi);
        buttons.Controls.Add(_btnSave);

        var bottomBar = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = UITheme.Neutral50 };
        bottomBar.Controls.Add(_lblStats);
        bottomBar.Controls.Add(buttons);

        BuildAiPanel();

        // Control Fill thêm đầu tiên để các thanh dock Top/Bottom được chia chỗ trước.
        Controls.Add(_txtScript);
        Controls.Add(_lblNotice);
        Controls.Add(_pnlAi);
        Controls.Add(bottomBar);
    }

    /// <summary>Dựng vùng xem trước của trợ lý AI (ẩn cho tới khi bấm "Trợ lý AI").</summary>
    private void BuildAiPanel()
    {
        _pnlAi = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 250,
            Visible = false,
            BackColor = UITheme.Neutral100,
            Padding = new Padding(10, 6, 10, 10),
        };

        _txtAiPreview = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Segoe UI", 10F),
            BorderStyle = BorderStyle.FixedSingle,
            AcceptsReturn = true,
            WordWrap = true,
        };
        _txtAiPreview.TextChanged += (_, _) => UpdateState();

        var aiActions = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 180,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = UITheme.Neutral100,
            Padding = new Padding(8, 0, 0, 0),
        };

        _btnAiAppend = new RoundedButton
        {
            Text = "Chèn vào cuối",
            Style = RoundButtonStyle.Secondary,
            Size = new Size(160, 34),
            Margin = new Padding(0, 0, 0, 8),
            Enabled = false,
        };
        _btnAiAppend.Click += (_, _) => ApplyAiText(replace: false);

        _btnAiReplace = new RoundedButton
        {
            Text = "Thay thế toàn bộ",
            Style = RoundButtonStyle.Secondary,
            Size = new Size(160, 34),
            Margin = new Padding(0, 0, 0, 8),
            Enabled = false,
        };
        _btnAiReplace.Click += (_, _) => ApplyAiText(replace: true);

        var btnClose = new RoundedButton
        {
            Text = "Đóng",
            Style = RoundButtonStyle.Ghost,
            Size = new Size(160, 34),
            Margin = new Padding(0, 0, 0, 8),
        };
        btnClose.Click += (_, _) => _pnlAi.Visible = false;

        aiActions.Controls.Add(_btnAiAppend);
        aiActions.Controls.Add(_btnAiReplace);
        aiActions.Controls.Add(btnClose);

        _lblAiStatus = new Label
        {
            Dock = DockStyle.Top,
            Height = 26,
            Padding = new Padding(2, 0, 0, 0),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = UITheme.FontBody,
            ForeColor = UITheme.Neutral600,
            BackColor = UITheme.Neutral100,
        };

        var aiHeader = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 46,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = UITheme.Neutral100,
            Padding = new Padding(0, 4, 0, 0),
        };

        var lblType = new Label
        {
            Text = "Tạo:",
            AutoSize = true,
            Font = UITheme.FontBody,
            ForeColor = UITheme.Neutral700,
            Margin = new Padding(0, 9, 6, 0),
        };

        _cboAiType = new ComboBox
        {
            Width = 150,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = UITheme.FontBody,
            Margin = new Padding(0, 6, 12, 0),
        };
        _cboAiType.Items.AddRange(["Dàn ý (Outline)", "Kịch bản (Script)"]);
        _cboAiType.SelectedIndex = 0;

        _txtAiInstruction = new TextBox
        {
            Width = 320,
            Font = UITheme.FontBody,
            PlaceholderText = "Yêu cầu thêm cho AI (tùy chọn)…",
            Margin = new Padding(0, 6, 12, 0),
        };

        _btnAiGenerate = new RoundedButton
        {
            Text = "Gợi ý",
            Style = RoundButtonStyle.Primary,
            Size = new Size(96, 34),
            Margin = new Padding(0, 4, 0, 0),
        };
        _btnAiGenerate.Click += async (_, _) => await GenerateAiAsync();

        aiHeader.Controls.Add(lblType);
        aiHeader.Controls.Add(_cboAiType);
        aiHeader.Controls.Add(_txtAiInstruction);
        aiHeader.Controls.Add(_btnAiGenerate);

        // Thứ tự add quyết định thứ tự dock (add sau dock trước): header trên, status dưới, actions phải, preview phần còn lại.
        _pnlAi.Controls.Add(_txtAiPreview);
        _pnlAi.Controls.Add(aiActions);
        _pnlAi.Controls.Add(_lblAiStatus);
        _pnlAi.Controls.Add(aiHeader);
    }

    public void Initialize(ScriptService service, IContentDetailBackend? backend = null)
    {
        _service = service;
        _backend = backend;
    }

    // ---------- API công khai ----------

    /// <summary>Nạp script của Content. Lỗi (không phải thành viên, không tìm thấy, lỗi DB) hiện ngay trên control và khóa ô nhập.</summary>
    public void LoadScript(long contentId, long userId)
    {
        if (_service == null)
            throw new InvalidOperationException("Phải gọi Initialize() trước khi LoadScript().");

        _userId = userId;
        try
        {
            ApplyDocument(_backend != null ? _backend.OpenScript(contentId) : _service.Open(contentId, userId));
        }
        catch (Exception ex) when (ex is UnauthorizedWorkflowActionException or InvalidOperationException
                                       or ContentValidationException
                                       or Npgsql.NpgsqlException or TimeoutException)
        {
            _document = null;
            SetText(string.Empty);
            _dirty = false;
            SetNotice(ex is Npgsql.NpgsqlException or TimeoutException
                ? "Không tải được kịch bản. Vui lòng kiểm tra kết nối và thử lại."
                : ex.Message, UITheme.Danger);
            UpdateState();
        }
    }

    /// <summary>Xóa nội dung và bỏ trạng thái "chưa lưu" (gọi khi drawer đóng).</summary>
    public void Reset()
    {
        _document = null;
        SetText(string.Empty);
        _dirty = false;
        _aiContentApplied = false;
        _pnlAi.Visible = false;
        _txtAiPreview.Text = string.Empty;
        SetAiStatus(string.Empty, UITheme.Neutral600);
        SetNotice(null, UITheme.Neutral700);
        UpdateState();
    }

    // ---------- Nội bộ ----------

    private void ApplyDocument(ScriptDocument document)
    {
        _document = document;
        SetText(document.Text);
        _dirty = false;
        _aiContentApplied = false;
        SetNotice(document.CanEdit ? null : document.Access.ReadOnlyReason, UITheme.Neutral700);
        UpdateState();
    }

    private void SetText(string normalizedText)
    {
        _loading = true;
        try
        {
            // TextBox cần "\r\n" để xuống dòng; script lưu bằng "\n".
            _txtScript.Text = normalizedText.Replace("\n", Environment.NewLine);
            _txtScript.SelectionStart = 0;
            _txtScript.SelectionLength = 0;
        }
        finally
        {
            _loading = false;
        }
    }

    private void OnScriptTextChanged()
    {
        if (_loading || _document == null)
            return;

        _dirty = true;
        if (_lblNotice.ForeColor == UITheme.Success)
            SetNotice(null, UITheme.Neutral700); // bỏ dòng "Đã lưu" cũ khi bắt đầu sửa tiếp
        UpdateState();
    }

    private void SaveScript()
    {
        if (_service == null || _document == null || !_document.CanEdit || !_dirty)
            return;

        // AC2: nội dung do AI đưa vào phải được người dùng xác nhận trước khi lưu.
        if (_aiContentApplied)
        {
            var confirm = MessageBox.Show(this,
                "Kịch bản đang có nội dung do AI gợi ý. Lưu lại nội dung này?",
                "Xác nhận lưu kịch bản", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes)
                return;
        }

        try
        {
            ScriptDocument saved = _backend != null
                ? _backend.SaveScript(_document.ContentId, _txtScript.Text, _document.Version)
                : _service.Save(_document.ContentId, _txtScript.Text, _document.Version, _userId);
            ApplyDocument(saved);
            SetNotice("Đã lưu kịch bản.", UITheme.Success);
            ScriptSaved?.Invoke(this, saved.Text);
        }
        catch (ScriptConflictException ex)
        {
            var answer = MessageBox.Show(this,
                ex.Message + "\n\nTải lại bản mới nhất? (Chọn Không để giữ nội dung đang gõ và sao chép ra nơi khác trước.)",
                "Xung đột kịch bản", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (answer == DialogResult.Yes)
                LoadScript(_document.ContentId, _userId);
        }
        catch (ContentValidationException ex)
        {
            MessageBox.Show(this, ex.Message, "Không thể lưu kịch bản", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (UnauthorizedWorkflowActionException ex)
        {
            MessageBox.Show(this, ex.Message, "Không đủ quyền", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(this, ex.Message, "Không thể lưu kịch bản", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex) when (ex is Npgsql.NpgsqlException or TimeoutException)
        {
            MessageBox.Show(this, "Không lưu được kịch bản. Vui lòng kiểm tra kết nối và thử lại.",
                "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ---------- Trợ lý AI (SCRUM-58) ----------

    private void ToggleAiPanel()
    {
        if (_pnlAi.Visible)
        {
            _pnlAi.Visible = false;
            return;
        }

        _pnlAi.Visible = true;
        if (_cboAiType.SelectedIndex < 0)
            _cboAiType.SelectedIndex = 0;
        _txtAiInstruction.Focus();
    }

    /// <summary>Loại yêu cầu AI theo lựa chọn trên UI (mặc định Outline).</summary>
    private AiRequestType SelectedAiType =>
        _cboAiType.SelectedIndex == 1 ? AiRequestType.Script : AiRequestType.Outline;

    private async Task GenerateAiAsync()
    {
        if (_service == null || _document == null || _aiBusy)
            return;

        if (_dirty)
        {
            MessageBox.Show(this, "Hãy lưu kịch bản trước khi gửi sang AI (AI nhận bản đã lưu).",
                "Trợ lý AI", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _aiCancellation?.Dispose();
        _aiCancellation = new CancellationTokenSource();
        _aiBusy = true;
        UpdateState();
        SetAiStatus("Đang tạo gợi ý từ AI…", UITheme.Neutral600);

        try
        {
            // PrepareAiRequest đọc DB đồng bộ trên luồng UI (đúng như các service khác dùng chung 1 connection);
            // chỉ lời gọi AI mới thực sự await.
            var response = await _service.SendToAiAsync(_document.ContentId, _userId,
                SelectedAiType, _txtAiInstruction.Text, _aiCancellation.Token);
            if (IsDisposed)
                return;

            if (response.Succeeded)
            {
                _txtAiPreview.Text = (response.Text ?? string.Empty).Replace("\n", Environment.NewLine);
                _btnAiGenerate.Text = "Gợi ý";
                SetAiStatus("AI đã tạo xong. Xem/chỉnh rồi chọn cách đưa vào kịch bản.", UITheme.Success);
            }
            else
            {
                _btnAiGenerate.Text = "Thử lại";
                SetAiStatus(response.ErrorMessage ?? "Không nhận được gợi ý từ AI.", UITheme.Danger);
            }
        }
        catch (OperationCanceledException)
        {
            // Control đã bị đóng: bỏ qua kết quả.
        }
        catch (ContentValidationException ex)
        {
            if (!IsDisposed)
            {
                _btnAiGenerate.Text = "Thử lại";
                SetAiStatus(ex.Message, UITheme.Danger);
            }
        }
        catch (UnauthorizedWorkflowActionException ex)
        {
            if (!IsDisposed)
            {
                _btnAiGenerate.Text = "Thử lại";
                SetAiStatus(ex.Message, UITheme.Danger);
            }
        }
        finally
        {
            _aiBusy = false;
            if (!IsDisposed)
                UpdateState();
        }
    }

    /// <summary>
    /// Đưa kết quả AI vào ô kịch bản khi người dùng bấm nút. AC1: không bao giờ tự ghi đè;
    /// "thay thế toàn bộ" phải qua hộp xác nhận khi kịch bản đang có nội dung.
    /// </summary>
    private void ApplyAiText(bool replace)
    {
        if (_document == null || !_document.CanEdit)
            return;

        string addition = _txtAiPreview.Text;
        if (ScriptTextAnalyzer.Normalize(addition).Length == 0)
        {
            MessageBox.Show(this, "Chưa có nội dung AI để đưa vào kịch bản.",
                "Trợ lý AI", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (replace && ScriptTextAnalyzer.Normalize(_txtScript.Text).Length > 0)
        {
            var confirm = MessageBox.Show(this,
                "Thay thế TOÀN BỘ kịch bản hiện có bằng nội dung AI?",
                "Thay thế kịch bản", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes)
                return;
        }

        string combined = replace
            ? ScriptAiApply.Replace(addition)
            : ScriptAiApply.Append(_txtScript.Text, addition);

        SetText(combined);
        _dirty = true;
        _aiContentApplied = true;
        SetNotice(replace ? "Đã thay kịch bản bằng nội dung AI (chưa lưu)." : "Đã chèn nội dung AI vào kịch bản (chưa lưu).",
            UITheme.Neutral700);
        UpdateState();
    }

    private void SetAiStatus(string? text, Color color)
    {
        _lblAiStatus.Text = text ?? string.Empty;
        _lblAiStatus.ForeColor = color;
    }

    private void SetNotice(string? text, Color color)
    {
        _lblNotice.Text = text ?? string.Empty;
        _lblNotice.ForeColor = color;
        _lblNotice.Visible = !string.IsNullOrEmpty(text);
    }

    private void UpdateState()
    {
        bool loaded = _document != null;
        bool canEdit = loaded && _document!.CanEdit;

        _txtScript.ReadOnly = !canEdit;
        _btnSave.Visible = canEdit;
        _btnSave.Enabled = canEdit && _dirty;
        _btnAi.Enabled = loaded;
        _btnAiGenerate.Enabled = loaded && !_aiBusy;

        bool hasPreview = ScriptTextAnalyzer.Normalize(_txtAiPreview.Text).Length > 0;
        _btnAiAppend.Enabled = canEdit && hasPreview && !_aiBusy;
        _btnAiReplace.Enabled = canEdit && hasPreview && !_aiBusy;

        ScriptStatistics stats = ScriptTextAnalyzer.Analyze(_txtScript.Text);
        bool overLimit = stats.Characters > ScriptService.MaxScriptLength;
        _lblStats.ForeColor = overLimit ? UITheme.Danger : UITheme.Neutral600;
        _lblStats.Text = $"{stats.Words:N0} từ · {stats.Characters:N0}/{ScriptService.MaxScriptLength:N0} ký tự · " +
                         $"~{FormatDuration(stats.EstimatedSpeakingSeconds)}" +
                         (_dirty ? " · chưa lưu" : string.Empty);
    }

    private static string FormatDuration(int seconds) =>
        seconds < 60 ? $"{seconds} giây" : $"{seconds / 60} phút {seconds % 60:00} giây";

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _aiCancellation?.Cancel();
            _aiCancellation?.Dispose();
        }
        base.Dispose(disposing);
    }
}
