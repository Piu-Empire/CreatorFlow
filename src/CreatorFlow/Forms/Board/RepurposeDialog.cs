using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using CreatorFlow.Controls;
using CreatorFlow.ApiClients;
using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Services.AI;
using CreatorFlow.Services.Backends;
using CreatorFlow.Services.Exceptions;
using CreatorFlow.Theme;

namespace CreatorFlow.Forms.Board;

/// <summary>
/// Dialog AI Repurpose (SCRUM-38): chọn platform đích → AI đề xuất → người dùng sửa → xác nhận tạo Content mới.
/// AI không tự tạo/lưu; chỉ tạo khi người dùng xác nhận.
/// </summary>
public sealed class RepurposeDialog : Form
{
    private const int ContentInset = 18;
    private const int PlatformGap = 8;

    private readonly ContentBoardCard _source;
    private readonly Content? _detail;
    private readonly string _sourceScript;
    private readonly IReadOnlyList<string> _platforms;
    private readonly IReadOnlyList<string> _targetPlatforms;
    private readonly IRepurposeAiService _ai;
    private readonly IContentDetailBackend _backend;
    private readonly long _userId;

    private readonly List<ProjectActionButton> _platformTiles = new();
    private readonly HashSet<ProjectActionButton> _sourcePlatformTiles = new();
    private readonly List<ProjectActionButton> _instructionChips = new();
    private readonly ToolTip _toolTip = new();
    private readonly TextBox _txtInstruction = new();
    private readonly ProjectActionButton _btnGenerate = new();
    private readonly ProjectActionButton _btnRegenerate = new();
    private readonly ProjectActionButton _btnCreate = new();
    private readonly ProjectActionButton _btnCancel = new();
    private readonly Label _lblStatus = new();
    private readonly Label _lblPreviewState = new();
    private readonly TextBox _txtTitle = new();
    private readonly TextBox _txtDescription = new();
    private readonly TextBox _txtScript = new();

    private string? _selectedPlatform;
    private CancellationTokenSource? _cts;
    private bool _busy;
    private bool _hasPreview;
    private string _lastTitle = string.Empty;
    private string _lastDescription = string.Empty;
    private string _lastScript = string.Empty;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cts?.Dispose();
            _toolTip.Dispose();
        }

        base.Dispose(disposing);
    }

    public long CreatedContentId { get; private set; }

    public RepurposeDialog(
        ContentBoardCard source,
        Content? detail,
        string sourceScript,
        bool scriptLoadFailed,
        IReadOnlyList<string> platforms,
        IRepurposeAiService ai,
        IContentDetailBackend backend,
        long userId)
    {
        _source = source;
        _detail = detail;
        _sourceScript = sourceScript;
        _platforms = platforms.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        _targetPlatforms = _platforms
            .Where(platform => !source.Platforms.Contains(platform, StringComparer.OrdinalIgnoreCase))
            .ToArray();
        _ai = ai ?? throw new ArgumentNullException(nameof(ai));
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        _userId = userId;

        BuildLayout(scriptLoadFailed);
        LoadPlatforms();
        SetBusy(false);
    }

    private void BuildLayout(bool scriptLoadFailed)
    {
        Text = "Tái sử dụng nội dung bằng AI";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        BackColor = UITheme.White;
        Font = UITheme.FontBody;
        ClientSize = new Size(1000, 980);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = UITheme.White,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));

        var body = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = false,
            BackColor = UITheme.White,
        };
        var contentWidth = ClientSize.Width - ContentInset * 2;
        int y = 16;

        var sourceLabel = new Label
        {
            Text = $"{_source.Code}  ·  {_source.Title}",
            Font = UITheme.FontBody,
            ForeColor = UITheme.Neutral600,
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(ContentInset + 2, y),
            Size = new Size(contentWidth - 4, 20),
        };
        body.Controls.Add(sourceLabel);
        y += sourceLabel.Height + 10;

        var platformLabel = SectionLabel("NỀN TẢNG");
        platformLabel.Location = new Point(ContentInset, y);
        body.Controls.Add(platformLabel);
        y += platformLabel.Height + 6;

        int platformRows = Math.Max(1, (int)Math.Ceiling(_platforms.Count / 4d));
        var platformPanel = BuildPlatformChoices(contentWidth, platformRows);
        platformPanel.Location = new Point(ContentInset, y);
        body.Controls.Add(platformPanel);
        y += platformPanel.Height + 12;

        var instructionLabel = SectionLabel("YÊU CẦU");
        instructionLabel.Location = new Point(ContentInset, y);
        body.Controls.Add(instructionLabel);
        y += instructionLabel.Height + 6;

        var instructionPanel = BuildInstructionPanel(contentWidth);
        instructionPanel.Location = new Point(ContentInset, y);
        body.Controls.Add(instructionPanel);
        y += instructionPanel.Height + 6;

        var instructionChips = BuildInstructionChips(contentWidth);
        instructionChips.Location = new Point(ContentInset + 2, y);
        body.Controls.Add(instructionChips);
        y += instructionChips.Height + 8;

        var generateRow = new FlowLayoutPanel
        {
            Location = new Point(ContentInset, y),
            Size = new Size(contentWidth, 44),
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = UITheme.White,
            Padding = Padding.Empty,
            Margin = Padding.Empty,
        };
        _btnGenerate.Text = "Tạo đề xuất";
        _btnGenerate.Style = RoundButtonStyle.Primary;
        _btnGenerate.ShowPlusIcon = true;
        _btnGenerate.AutoSize = false;
        _btnGenerate.Size = new Size(190, 44);
        _btnGenerate.Margin = Padding.Empty;
        _btnGenerate.Click += async (_, _) => await GenerateAsync();
        generateRow.Controls.Add(_btnGenerate);
        body.Controls.Add(generateRow);
        y += generateRow.Height + 10;

        _lblStatus.Location = new Point(ContentInset + 2, y);
        _lblStatus.Size = new Size(contentWidth - 4, 22);
        _lblStatus.Font = UITheme.FontLabel;
        _lblStatus.ForeColor = UITheme.Neutral600;
        _lblStatus.TextAlign = ContentAlignment.MiddleLeft;
        _lblStatus.Text = !_ai.IsConfigured
            ? NotConfiguredAiService.Message
            : scriptLoadFailed
                ? "Không đọc được kịch bản gốc; AI sẽ dựa trên tiêu đề và mô tả."
                : string.Empty;
        if (!_ai.IsConfigured)
            _lblStatus.ForeColor = UITheme.Danger;
        body.Controls.Add(_lblStatus);
        y += _lblStatus.Height + 4;

        var divider = BuildResultDivider(contentWidth);
        divider.Location = new Point(ContentInset, y);
        body.Controls.Add(divider);
        y += divider.Height + 2;

        var previewCard = BuildPreviewCard(contentWidth);
        previewCard.Location = new Point(ContentInset, y);
        body.Controls.Add(previewCard);
        y += previewCard.Height + 6;

        root.Controls.Add(body, 0, 0);
        root.Controls.Add(BuildFooter(), 0, 1);
        Controls.Add(root);

        AcceptButton = null;
        CancelButton = _btnCancel;
        FormClosed += (_, _) => _cts?.Cancel();
    }

    private Control BuildPlatformChoices(int width, int rows)
    {
        int height = rows * 44 + Math.Max(0, rows - 1) * PlatformGap;
        var flow = new FlowLayoutPanel
        {
            Size = new Size(width, Math.Max(44, height)),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoScroll = false,
            BackColor = UITheme.White,
            Padding = Padding.Empty,
            Margin = Padding.Empty,
        };

        if (_platforms.Count == 0)
        {
            flow.Controls.Add(new Label
            {
                Text = "Không có platform nào đang được bật trong Project.",
                ForeColor = UITheme.Neutral600,
                Font = UITheme.FontLabel,
                TextAlign = ContentAlignment.MiddleLeft,
                Size = new Size(width, 36),
            });
            return flow;
        }

        const int columns = 4;
        int tileWidth = (width - PlatformGap * (columns - 1)) / columns;
        for (int index = 0; index < _platforms.Count; index++)
        {
            string platform = _platforms[index];
            int column = index % columns;
            int row = index / columns;
            bool isSourcePlatform = _source.Platforms.Contains(platform, StringComparer.OrdinalIgnoreCase);
            var tile = new ProjectActionButton
            {
                Text = $"{PlatformGlyph(platform)}  {platform}{(isSourcePlatform ? " · Nguồn" : string.Empty)}",
                Style = isSourcePlatform ? RoundButtonStyle.Secondary : RoundButtonStyle.Ghost,
                AutoSize = false,
                Size = new Size(tileWidth, 44),
                Margin = new Padding(0, 0,
                    column == columns - 1 ? 0 : PlatformGap,
                    row == rows - 1 ? 0 : PlatformGap),
                Enabled = !isSourcePlatform,
                Tag = platform,
            };
            tile.Click += (_, _) => SelectPlatform(platform);
            _platformTiles.Add(tile);
            if (isSourcePlatform)
                _sourcePlatformTiles.Add(tile);
            flow.Controls.Add(tile);
        }

        return flow;
    }

    private static string PlatformGlyph(string platform) => platform.Trim().ToLowerInvariant() switch
    {
        "youtube" => "▶",
        "tiktok" => "♪",
        "instagram" => "◎",
        "facebook" => "ⓕ",
        _ => "•",
    };

    private BorderPanel BuildInstructionPanel(int width)
    {
        var panel = new BorderPanel
        {
            Size = new Size(width, 64),
            BackColor = UITheme.White,
            BorderColor = UITheme.Neutral400,
            CornerRadius = 10,
        };

        _txtInstruction.Multiline = true;
        _txtInstruction.BorderStyle = BorderStyle.None;
        _txtInstruction.BackColor = UITheme.White;
        _txtInstruction.Font = UITheme.FontBody;
        _txtInstruction.ForeColor = UITheme.Ink;
        _txtInstruction.PlaceholderText = "Gợi ý phong cách, thời lượng, call-to-action...";
        _txtInstruction.Location = new Point(10, 7);
        _txtInstruction.Size = new Size(width - 20, 48);
        _txtInstruction.MaxLength = 1000;
        panel.Controls.Add(_txtInstruction);

        return panel;
    }

    private Control BuildInstructionChips(int width)
    {
        var chips = new FlowLayoutPanel
        {
            Size = new Size(width, 32),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = UITheme.White,
            Padding = Padding.Empty,
            Margin = Padding.Empty,
        };
        AddInstructionChip(chips, "Dưới 30s", "Dưới 30 giây");
        AddInstructionChip(chips, "Hài hước", "Giọng hài hước");
        AddInstructionChip(chips, "Kèm CTA", "Kèm lời kêu gọi hành động (CTA)");
        return chips;
    }

    private void AddInstructionChip(Control parent, string caption, string instruction)
    {
        var chip = new ProjectActionButton
        {
            Text = caption,
            Style = RoundButtonStyle.Primary,
            AutoSize = false,
            Size = new Size(caption == "Kèm CTA" ? 96 : 84, 30),
            Margin = new Padding(0, 0, 6, 0),
        };
        chip.Click += (_, _) => AppendInstruction(instruction);
        _instructionChips.Add(chip);
        parent.Controls.Add(chip);
    }

    private static Label SectionLabel(string text) => new()
    {
        Text = text,
        Font = UITheme.FontLabelBold,
        ForeColor = UITheme.Neutral600,
        AutoSize = false,
        TextAlign = ContentAlignment.MiddleLeft,
        Size = new Size(200, 18),
    };

    private static Control BuildResultDivider(int width)
    {
        var divider = new TableLayoutPanel
        {
            Size = new Size(width, 26),
            ColumnCount = 3,
            RowCount = 1,
            BackColor = UITheme.White,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
        };
        divider.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        divider.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        divider.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        foreach (int column in new[] { 0, 2 })
            divider.Controls.Add(new Panel
            {
                BackColor = UITheme.Neutral300,
                Height = 1,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 13, 0, 12),
            }, column, 0);

        divider.Controls.Add(new Label
        {
            Text = "KẾT QUẢ ĐỀ XUẤT",
            Font = UITheme.FontMicro,
            ForeColor = UITheme.Neutral400,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(10, 0, 10, 0),
        }, 1, 0);
        return divider;
    }

    private Control BuildPreviewCard(int width)
    {
        var card = new BorderPanel
        {
            Size = new Size(width, 536),
            BackColor = UITheme.Neutral50,
            BorderColor = UITheme.Neutral400,
            CornerRadius = 12,
        };

        var header = new Label
        {
            Text = "Kết quả AI",
            Font = UITheme.FontBodyBold,
            ForeColor = UITheme.Ink,
            Location = new Point(16, 12),
            Size = new Size(width - 170, 22),
            TextAlign = ContentAlignment.MiddleLeft,
        };
        card.Controls.Add(header);

        _lblPreviewState.Text = "●  Chưa có đề xuất";
        _lblPreviewState.Font = UITheme.FontMicro;
        _lblPreviewState.ForeColor = UITheme.Neutral600;
        _lblPreviewState.TextAlign = ContentAlignment.MiddleRight;
        _lblPreviewState.Location = new Point(width - 150, 12);
        _lblPreviewState.Size = new Size(132, 22);
        card.Controls.Add(_lblPreviewState);

        int fieldWidth = width - 32;
        AddPreviewField(card, "Tiêu đề", _txtTitle, 42, 42, fieldWidth, singleLine: true);
        AddPreviewField(card, "Nội dung & Hashtag", _txtDescription, 126, 84, fieldWidth, singleLine: false);
        AddPreviewField(card, "Kịch bản", _txtScript, 248, 220, fieldWidth, singleLine: false);
        return card;
    }

    private void AddPreviewField(
        Control parent,
        string label,
        TextBox textBox,
        int top,
        int inputHeight,
        int width,
        bool singleLine)
    {
        const int copyButtonSize = 30;
        var fieldLabel = new Label
        {
            Text = label,
            Font = UITheme.FontLabelBold,
            ForeColor = UITheme.Neutral700,
            Location = new Point(16, top),
            Size = new Size(width - copyButtonSize - 10, 22),
            TextAlign = ContentAlignment.MiddleLeft,
        };
        parent.Controls.Add(fieldLabel);

        var copyButton = new CopyIconButton
        {
            Text = string.Empty,
            Style = RoundButtonStyle.Ghost,
            Size = new Size(copyButtonSize, copyButtonSize),
            Location = new Point(16 + width - copyButtonSize, top - 4),
            Padding = Padding.Empty,
            AccessibleName = $"Sao chép {label.ToLowerInvariant()}",
        };
        copyButton.Click += (_, _) => CopyField(textBox, label);
        _toolTip.SetToolTip(copyButton, $"Sao chép {label.ToLowerInvariant()}");
        parent.Controls.Add(copyButton);

        textBox.BorderStyle = BorderStyle.None;
        textBox.BackColor = UITheme.White;
        textBox.ForeColor = UITheme.Ink;
        textBox.Font = UITheme.FontBody;
        textBox.Multiline = !singleLine;
        textBox.ScrollBars = singleLine ? ScrollBars.None : ScrollBars.Vertical;
        textBox.AcceptsReturn = !singleLine;
        textBox.WordWrap = true;
        textBox.MaxLength = singleLine ? 200 : label == "Kịch bản" ? 200_000 : 2000;

        var inputFrame = new BorderPanel
        {
            Location = new Point(16, top + 28),
            Size = new Size(width, inputHeight),
            BackColor = UITheme.White,
            BorderColor = UITheme.Neutral400,
            CornerRadius = 8,
        };
        textBox.Location = new Point(10, 7);
        textBox.Size = new Size(width - 20, inputHeight - 14);
        inputFrame.Controls.Add(textBox);
        parent.Controls.Add(inputFrame);
    }

    private Control BuildFooter()
    {
        var footer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UITheme.White,
        };
        footer.Paint += (_, e) =>
        {
            using var pen = new Pen(UITheme.Neutral300);
            e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0);
        };

        _btnCancel.Text = "Hủy";
        _btnCancel.Style = RoundButtonStyle.Secondary;
        _btnCancel.Size = new Size(84, 44);
        _btnCancel.Click += (_, _) => Close();

        _btnRegenerate.Text = "Tạo lại bản khác";
        _btnRegenerate.Style = RoundButtonStyle.Ghost;
        _btnRegenerate.Size = new Size(174, 44);
        _btnRegenerate.Click += async (_, _) => await GenerateAsync();

        _btnCreate.Text = "Tạo Content mới";
        _btnCreate.Style = RoundButtonStyle.Primary;
        _btnCreate.ShowPlusIcon = true;
        _btnCreate.Size = new Size(190, 44);
        _btnCreate.Click += (_, _) => CreateContent();

        footer.Controls.Add(_btnCancel);
        footer.Controls.Add(_btnRegenerate);
        footer.Controls.Add(_btnCreate);
        footer.Resize += (_, _) => PositionFooterButtons(footer);
        PositionFooterButtons(footer);
        return footer;
    }

    private void PositionFooterButtons(Control footer)
    {
        int top = (footer.Height - _btnCancel.Height) / 2;
        _btnCancel.Location = new Point(18, top);
        _btnRegenerate.Location = new Point(_btnCancel.Right + 6, top);
        _btnCreate.Location = new Point(footer.ClientSize.Width - _btnCreate.Width - 18,
            (footer.Height - _btnCreate.Height) / 2);
    }

    private void LoadPlatforms()
    {
        if (_targetPlatforms.Count > 0)
            SelectPlatform(_targetPlatforms[0]);
        else
            SetStatus("Không có nền tảng đích nào khác với Content gốc đang được bật.", UITheme.Danger);
    }

    private void SelectPlatform(string platform)
    {
        if (!_targetPlatforms.Contains(platform, StringComparer.OrdinalIgnoreCase))
            return;

        if (string.Equals(_selectedPlatform, platform, StringComparison.OrdinalIgnoreCase))
            return;

        bool hadPreview = _hasPreview;
        _selectedPlatform = platform;
        foreach (ProjectActionButton tile in _platformTiles)
        {
            if (_sourcePlatformTiles.Contains(tile))
                continue;

            bool selected = string.Equals(tile.Tag as string, platform, StringComparison.OrdinalIgnoreCase);
            tile.Style = selected ? RoundButtonStyle.Primary : RoundButtonStyle.Ghost;
        }

        if (hadPreview)
        {
            ClearPreview();
            SetStatus("Bạn đã đổi nền tảng. Hãy tạo đề xuất mới cho nền tảng này.", UITheme.Warning);
        }
    }

    private void AppendInstruction(string instruction)
    {
        if (string.IsNullOrWhiteSpace(_txtInstruction.Text))
            _txtInstruction.Text = instruction;
        else if (!_txtInstruction.Text.Contains(instruction, StringComparison.OrdinalIgnoreCase))
            _txtInstruction.AppendText($"; {instruction}");

        _txtInstruction.Focus();
    }

    private async Task GenerateAsync()
    {
        if (_busy || _selectedPlatform is not string targetPlatform)
            return;

        if (HasEditedPreview() && MessageBox.Show(this,
                "Bạn đã sửa bản đề xuất. Tạo lại sẽ bỏ các chỉnh sửa này. Tiếp tục?",
                "Tạo lại đề xuất", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        SetBusy(true);
        SetStatus("Đang tạo bản đề xuất...", UITheme.Neutral600);

        _cts?.Cancel();
        _cts?.Dispose();
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
            // Đóng dialog hoặc hủy request không phải là lỗi cần hiện cho người dùng.
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

        _hasPreview = true;
        _lblPreviewState.Text = "●  Bản nháp";
        _lblPreviewState.ForeColor = UITheme.Info;
        if (response.IsStructured)
            SetStatus("Kiểm tra và chỉnh sửa bản nháp trước khi tạo Content mới.", UITheme.Success);
        else
            SetStatus("AI trả về nội dung chưa đúng cấu trúc; hãy kiểm tra và chỉnh sửa trước khi tạo.", UITheme.Warning);
    }

    private void ClearPreview()
    {
        _hasPreview = false;
        _txtTitle.Clear();
        _txtDescription.Clear();
        _txtScript.Clear();
        _lastTitle = string.Empty;
        _lastDescription = string.Empty;
        _lastScript = string.Empty;
        _lblPreviewState.Text = "●  Chưa có đề xuất";
        _lblPreviewState.ForeColor = UITheme.Neutral600;
        SetBusy(false);
    }

    private bool HasEditedPreview() =>
        _hasPreview &&
        (_txtTitle.Text != _lastTitle || _txtDescription.Text != _lastDescription || _txtScript.Text != _lastScript);

    private void CopyField(TextBox textBox, string label)
    {
        if (string.IsNullOrEmpty(textBox.Text))
            return;

        try
        {
            Clipboard.SetText(textBox.Text);
            SetStatus($"Đã sao chép {label.ToLowerInvariant()}.", UITheme.Success);
        }
        catch (Exception ex) when (ex is ExternalException or InvalidOperationException)
        {
            SetStatus("Không thể sao chép lúc này. Vui lòng thử lại.", UITheme.Danger);
        }
    }

    private sealed class CopyIconButton : RoundedButton
    {
        public CopyIconButton()
        {
            BackColor = UITheme.White;
            UseVisualStyleBackColor = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent?.BackColor ?? UITheme.White);
            base.OnPaint(e);
            Graphics graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(Enabled ? UITheme.Neutral700 : UITheme.Neutral400, 1.5f);
            var bounds = ClientRectangle;
            var back = new Rectangle(bounds.X + 10, bounds.Y + 5, 11, 13);
            var front = new Rectangle(bounds.X + 5, bounds.Y + 10, 11, 13);
            using var backPath = UITheme.CreateRoundedRectanglePath(back, 2);
            using var frontPath = UITheme.CreateRoundedRectanglePath(front, 2);
            graphics.DrawPath(pen, backPath);
            graphics.DrawPath(pen, frontPath);
        }
    }

    private void CreateContent()
    {
        if (_busy || !_hasPreview || _selectedPlatform is not string targetPlatform)
            return;

        string contentType = string.IsNullOrWhiteSpace(_detail?.ContentType)
            ? "Short video"
            : _detail.ContentType;

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

        if (MessageBox.Show(this,
                $"Tạo Content mới từ {_source.Code} cho nền tảng {targetPlatform}?",
                "Xác nhận tạo Content", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        SetBusy(true);
        try
        {
            CreatedContentId = _backend.Create(_source.ProjectId, ContentStatus.Idea, draft);
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
        catch (ApiBusinessException ex)
        {
            SetStatus(ex.Message, UITheme.Danger);
        }
        catch (ApiException ex)
        {
            SetStatus(ex.Message, UITheme.Danger);
        }
        catch (TimeoutException)
        {
            SetStatus("API phản hồi quá thời gian chờ. Hãy thử lại.", UITheme.Danger);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        foreach (ProjectActionButton tile in _platformTiles)
            tile.Enabled = !busy && !_sourcePlatformTiles.Contains(tile);
        foreach (ProjectActionButton chip in _instructionChips)
            chip.Enabled = !busy;

        _txtInstruction.Enabled = !busy;
        _txtTitle.Enabled = !busy;
        _txtDescription.Enabled = !busy;
        _txtScript.Enabled = !busy;
        _btnGenerate.Enabled = !busy && _ai.IsConfigured && _targetPlatforms.Count > 0;
        _btnRegenerate.Enabled = !busy && _hasPreview && _ai.IsConfigured;
        _btnCreate.Enabled = !busy && _hasPreview && _targetPlatforms.Count > 0;
        _btnCancel.Enabled = !busy;
        UseWaitCursor = busy;
    }

    private void SetStatus(string message, Color color)
    {
        _lblStatus.Text = message;
        _lblStatus.ForeColor = color;
    }

    private sealed class BorderPanel : Panel
    {
        public Color BorderColor { get; set; } = UITheme.Neutral400;

        public int CornerRadius { get; set; } = 10;

        public BorderPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (Width <= 1 || Height <= 1)
                return;

            Region? oldRegion = Region;
            using var path = UITheme.CreateRoundedRectanglePath(new Rectangle(0, 0, Width - 1, Height - 1), CornerRadius);
            Region = new Region(path);
            oldRegion?.Dispose();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle bounds = new(1, 1, Width - 3, Height - 3);
            using var path = UITheme.CreateRoundedRectanglePath(bounds, CornerRadius);
            using var pen = new Pen(BorderColor, 1.5f);
            e.Graphics.DrawPath(pen, path);
        }
    }
}
