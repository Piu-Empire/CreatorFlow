using CreatorFlow.Models;
using CreatorFlow.Services;
using CreatorFlow.Services.Exceptions;
using CreatorFlow.Theme;

namespace CreatorFlow.Controls;

/// <summary>
/// Khu vực nhập/chỉnh sửa kịch bản (tab Script của Content Detail). Toàn bộ nghiệp vụ nằm ở <see cref="ScriptService"/>;
/// control chỉ hiển thị, theo dõi thay đổi chưa lưu và gọi service.
///   - Lưu riêng bằng nút "Lưu kịch bản" / Ctrl+S, độc lập với nút "Lưu thay đổi" của drawer.
///   - Chỉ đọc (kèm lý do) khi User không có quyền sửa hoặc Content đã Published/Archived.
///   - "Gửi sang AI" gửi bản ĐÃ LƯU nên yêu cầu lưu trước.
/// </summary>
public class ScriptEditorControl : UserControl
{
    private readonly TextBox _txtScript;
    private readonly Label _lblNotice;
    private readonly Label _lblStats;
    private readonly RoundedButton _btnSave;
    private readonly RoundedButton _btnAi;

    private ScriptService? _service;
    private ScriptDocument? _document;
    private long _userId;
    private bool _loading;
    private bool _dirty;
    private bool _aiBusy;
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
            Text = "Gửi sang AI",
            Style = RoundButtonStyle.Secondary,
            Size = new Size(120, 36),
            Margin = new Padding(8, 6, 0, 6),
            Enabled = false,
        };
        _btnAi.Click += async (_, _) => await SendToAiAsync();

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

        // Control Fill thêm đầu tiên để các thanh dock Top/Bottom được chia chỗ trước.
        Controls.Add(_txtScript);
        Controls.Add(_lblNotice);
        Controls.Add(bottomBar);
    }

    public void Initialize(ScriptService service) => _service = service;

    // ---------- API công khai ----------

    /// <summary>Nạp script của Content. Lỗi (không phải thành viên, không tìm thấy, lỗi DB) hiện ngay trên control và khóa ô nhập.</summary>
    public void LoadScript(long contentId, long userId)
    {
        if (_service == null)
            throw new InvalidOperationException("Phải gọi Initialize() trước khi LoadScript().");

        _userId = userId;
        try
        {
            ApplyDocument(_service.Open(contentId, userId));
        }
        catch (Exception ex) when (ex is UnauthorizedWorkflowActionException or InvalidOperationException
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
        SetNotice(null, UITheme.Neutral700);
        UpdateState();
    }

    // ---------- Nội bộ ----------

    private void ApplyDocument(ScriptDocument document)
    {
        _document = document;
        SetText(document.Text);
        _dirty = false;
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

        try
        {
            ScriptDocument saved = _service.Save(_document.ContentId, _txtScript.Text, _document.Version, _userId);
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
        catch (Exception ex) when (ex is Npgsql.NpgsqlException or TimeoutException)
        {
            MessageBox.Show(this, "Không lưu được kịch bản. Vui lòng kiểm tra kết nối và thử lại.",
                "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task SendToAiAsync()
    {
        if (_service == null || _document == null || _aiBusy)
            return;

        if (_dirty)
        {
            MessageBox.Show(this, "Hãy lưu kịch bản trước khi gửi sang AI (AI nhận bản đã lưu).",
                "Gửi sang AI", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _aiCancellation?.Dispose();
        _aiCancellation = new CancellationTokenSource();
        _aiBusy = true;
        UseWaitCursor = true;
        UpdateState();

        try
        {
            // PrepareAiRequest đọc DB đồng bộ trên luồng UI (đúng như các service khác dùng chung 1 connection);
            // chỉ lời gọi AI mới thực sự await.
            var response = await _service.SendToAiAsync(_document.ContentId, _userId,
                cancellationToken: _aiCancellation.Token);
            if (IsDisposed)
                return;

            if (response.Succeeded)
            {
                string text = response.Text ?? string.Empty;
                if (text.Length > 3000) text = text[..3000] + "…";
                MessageBox.Show(this, text, "Gợi ý từ AI", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(this, response.ErrorMessage, "Gửi sang AI", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (OperationCanceledException)
        {
            // Control đã bị đóng: bỏ qua kết quả.
        }
        catch (ContentValidationException ex)
        {
            if (!IsDisposed)
                MessageBox.Show(this, ex.Message, "Gửi sang AI", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (UnauthorizedWorkflowActionException ex)
        {
            if (!IsDisposed)
                MessageBox.Show(this, ex.Message, "Không đủ quyền", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            _aiBusy = false;
            if (!IsDisposed)
            {
                UseWaitCursor = false;
                UpdateState();
            }
        }
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
        _btnAi.Enabled = loaded && !_aiBusy;

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
