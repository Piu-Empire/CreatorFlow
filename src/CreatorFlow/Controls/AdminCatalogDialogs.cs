using CreatorFlow.Contracts.Admin;
using CreatorFlow.Services;
using CreatorFlow.Theme;

namespace CreatorFlow.Controls;

/// <summary>Dialog tạo/sửa gói dịch vụ (SCRUM-23).</summary>
public sealed class PlanEditorDialog : Form
{
    private readonly AdminPlanResponse? _existing;
    private readonly TextBox _txtCode = NewText();
    private readonly TextBox _txtName = NewText();
    private readonly TextBox _txtDescription = NewText();
    private readonly NumericUpDown _numPrice = new();
    private readonly TextBox _txtMaxProjects = NewText();
    private readonly TextBox _txtMaxMembers = NewText();
    private readonly TextBox _txtAiLimit = NewText();
    private readonly CheckBox _chkActive = new();

    public PlanEditorDialog(AdminPlanResponse? existing)
    {
        _existing = existing;
        Text = existing is null ? "Tạo gói dịch vụ" : "Sửa gói dịch vụ";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UITheme.White;
        Font = UITheme.FontBody;
        ClientSize = new Size(440, existing is null ? 430 : 470);
        Padding = new Padding(20);

        _numPrice.DecimalPlaces = 0;
        _numPrice.Maximum = 1_000_000_000;
        _numPrice.ThousandsSeparator = true;
        _numPrice.Width = 180;
        _numPrice.Font = UITheme.FontBody;

        int y = 0;
        AddLabel("Mã gói", ref y);
        _txtCode.Location = new Point(0, y);
        _txtCode.Width = 380;
        Controls.Add(_txtCode);
        y += 34;

        AddLabel("Tên gói", ref y);
        _txtName.Location = new Point(0, y);
        _txtName.Width = 380;
        Controls.Add(_txtName);
        y += 34;

        AddLabel("Mô tả", ref y);
        _txtDescription.Location = new Point(0, y);
        _txtDescription.Width = 380;
        Controls.Add(_txtDescription);
        y += 34;

        AddLabel("Giá mỗi tháng (đ)", ref y);
        _numPrice.Location = new Point(0, y);
        Controls.Add(_numPrice);
        y += 34;

        AddLabel("Project tối đa (để trống = không giới hạn)", ref y);
        _txtMaxProjects.Location = new Point(0, y);
        _txtMaxProjects.Width = 120;
        Controls.Add(_txtMaxProjects);
        y += 34;

        AddLabel("Thành viên tối đa (để trống = không giới hạn)", ref y);
        _txtMaxMembers.Location = new Point(0, y);
        _txtMaxMembers.Width = 120;
        Controls.Add(_txtMaxMembers);
        y += 34;

        AddLabel("Lượt gọi AI (để trống = không giới hạn)", ref y);
        _txtAiLimit.Location = new Point(0, y);
        _txtAiLimit.Width = 120;
        Controls.Add(_txtAiLimit);
        y += 36;

        if (existing is not null)
        {
            _chkActive.Text = "Đang bật (cho phép gán gói này)";
            _chkActive.Font = UITheme.FontBody;
            _chkActive.AutoSize = true;
            _chkActive.Location = new Point(0, y);
            Controls.Add(_chkActive);
        }

        var ok = new RoundedButton
        {
            Text = "Lưu",
            Style = RoundButtonStyle.Primary,
            Size = new Size(110, 36),
            Location = new Point(ClientSize.Width - 200, ClientSize.Height - 60),
        };
        ok.Click += (_, _) => Confirm();
        var cancel = new RoundedButton
        {
            Text = "Hủy",
            Style = RoundButtonStyle.Secondary,
            Size = new Size(90, 36),
            Location = new Point(ClientSize.Width - 110, ClientSize.Height - 60),
        };
        cancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };
        Controls.Add(ok);
        Controls.Add(cancel);
        AcceptButton = null;
        CancelButton = null;

        if (existing is not null)
        {
            _txtCode.Text = existing.Code;
            _txtCode.ReadOnly = true;
            _txtCode.BackColor = UITheme.Neutral100;
            _txtName.Text = existing.Name;
            _txtName.ReadOnly = true;
            _txtName.BackColor = UITheme.Neutral100;
            _txtDescription.Text = existing.Description ?? string.Empty;
            _numPrice.Value = Math.Clamp(existing.PriceMonthly, 0, _numPrice.Maximum);
            _txtMaxProjects.Text = existing.MaxProjects?.ToString() ?? string.Empty;
            _txtMaxMembers.Text = existing.MaxMembers?.ToString() ?? string.Empty;
            _txtAiLimit.Text = existing.AiRequestLimit?.ToString() ?? string.Empty;
            _chkActive.Checked = existing.IsActive;
        }
        else
        {
            _chkActive.Checked = true;
        }
    }

    public string Code => _txtCode.Text.Trim();

    public string PlanName => _txtName.Text.Trim();

    public string? Description => string.IsNullOrWhiteSpace(_txtDescription.Text) ? null : _txtDescription.Text.Trim();

    public decimal Price => _numPrice.Value;

    public int? MaxProjects => ParseOptional(_txtMaxProjects.Text);

    public int? MaxMembers => ParseOptional(_txtMaxMembers.Text);

    public int? AiRequestLimit => ParseOptional(_txtAiLimit.Text);

    public bool IsActive => _chkActive.Checked;

    private static TextBox NewText() => new() { Font = UITheme.FontBody, BorderStyle = BorderStyle.FixedSingle };

    private void AddLabel(string text, ref int y)
    {
        var label = new Label
        {
            Text = text,
            Font = UITheme.FontLabel,
            ForeColor = UITheme.Neutral600,
            AutoSize = true,
            Location = new Point(0, y),
        };
        Controls.Add(label);
        y += 20;
    }

    private static int? ParseOptional(string text) =>
        int.TryParse(text.Trim(), out int value) ? value : null;

    private void Confirm()
    {
        if (string.IsNullOrWhiteSpace(_txtCode.Text))
        {
            Warn("Mã gói là bắt buộc.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_txtName.Text))
        {
            Warn("Tên gói là bắt buộc.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(_txtMaxProjects.Text) && MaxProjects is null)
        {
            Warn("Số project tối đa phải là số nguyên.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(_txtMaxMembers.Text) && MaxMembers is null)
        {
            Warn("Số thành viên tối đa phải là số nguyên.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(_txtAiLimit.Text) && AiRequestLimit is null)
        {
            Warn("Giới hạn lượt AI phải là số nguyên.");
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private void Warn(string message) =>
        MessageBox.Show(this, message, "Gói dịch vụ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
}

/// <summary>Dialog gán gói cho một người dùng (SCRUM-23).</summary>
public sealed class SubscriptionDialog : Form
{
    private readonly AuthApiFacade _auth;
    private readonly CancellationToken _token;
    private readonly TextBox _txtEmail = new()
    {
        Font = UITheme.FontBody,
        BorderStyle = BorderStyle.FixedSingle,
        Width = 300,
    };
    private readonly ListBox _lstUsers = new()
    {
        Font = UITheme.FontBody,
        Width = 380,
        Height = 110,
        IntegralHeight = false,
    };
    private readonly ComboBox _cboPlan = new()
    {
        Font = UITheme.FontBody,
        Width = 380,
        DropDownStyle = ComboBoxStyle.DropDownList,
    };
    private readonly CheckBox _chkExpires = new()
    {
        Text = "Có hạn dùng",
        Font = UITheme.FontBody,
        AutoSize = true,
    };
    private readonly DateTimePicker _dtExpires = new()
    {
        Font = UITheme.FontBody,
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "dd/MM/yyyy",
        Width = 160,
    };
    private readonly Label _hint = new()
    {
        Font = UITheme.FontLabel,
        ForeColor = UITheme.Neutral600,
        AutoSize = false,
        Width = 380,
        Height = 36,
    };
    private List<AdminUserSummaryResponse> _users = [];
    private readonly RoundedButton _okButton = new();

    public SubscriptionDialog(AuthApiFacade auth, CancellationToken token, IReadOnlyList<AdminPlanResponse> plans)
    {
        _auth = auth;
        _token = token;
        Text = "Gán gói dịch vụ";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UITheme.White;
        Font = UITheme.FontBody;
        ClientSize = new Size(440, 470);
        Padding = new Padding(20);

        int y = 0;
        AddLabel("Tìm người dùng theo email", ref y);
        _txtEmail.Location = new Point(0, y);
        Controls.Add(_txtEmail);

        var btnFind = new RoundedButton
        {
            Text = "Tìm",
            Style = RoundButtonStyle.Secondary,
            Size = new Size(70, 26),
            Location = new Point(310, y - 3),
        };
        btnFind.Click += async (_, _) => await SearchUsersAsync();
        Controls.Add(btnFind);
        y += 34;

        _lstUsers.Location = new Point(0, y);
        _lstUsers.SelectedIndexChanged += (_, _) => UpdateState();
        Controls.Add(_lstUsers);
        y += 118;

        AddLabel("Gói dịch vụ", ref y);
        _cboPlan.Location = new Point(0, y);
        _cboPlan.Items.AddRange([.. plans.Where(p => p.IsActive).Select(p => new PlanItem(p)).ToArray()]);
        _cboPlan.SelectedIndexChanged += (_, _) => UpdateState();
        if (_cboPlan.Items.Count > 0) _cboPlan.SelectedIndex = 0;
        Controls.Add(_cboPlan);
        y += 34;

        _chkExpires.Location = new Point(0, y);
        _chkExpires.Checked = false;
        _chkExpires.CheckedChanged += (_, _) => _dtExpires.Enabled = _chkExpires.Checked;
        Controls.Add(_chkExpires);

        _dtExpires.Location = new Point(140, y - 3);
        _dtExpires.Enabled = false;
        _dtExpires.Value = DateTime.Today.AddMonths(1);
        Controls.Add(_dtExpires);
        y += 34;

        _hint.Location = new Point(0, y);
        _hint.Text = "Chọn đúng một người dùng trong danh sách kết quả.";
        _hint.ForeColor = UITheme.Neutral600;
        Controls.Add(_hint);

        var ok = _okButton;
        ok.Text = "Gán gói";
        ok.Style = RoundButtonStyle.Primary;
        ok.Size = new Size(120, 36);
        ok.Location = new Point(ClientSize.Width - 210, ClientSize.Height - 60);
        ok.Enabled = false;
        ok.Click += (_, _) =>
        {
            DialogResult = DialogResult.OK;
            Close();
        };
        var cancel = new RoundedButton
        {
            Text = "Hủy",
            Style = RoundButtonStyle.Secondary,
            Size = new Size(80, 36),
            Location = new Point(ClientSize.Width - 90, ClientSize.Height - 60),
        };
        cancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };
        Controls.Add(ok);
        Controls.Add(cancel);
    }

    public long? SelectedUserId =>
        _lstUsers.SelectedIndex >= 0 && _lstUsers.SelectedIndex < _users.Count
            ? _users[_lstUsers.SelectedIndex].UserId
            : null;

    public long? SelectedPlanId => (_cboPlan.SelectedItem as PlanItem)?.Plan.PlanId;

    public DateTimeOffset? ExpiresAt => _chkExpires.Checked
        ? new DateTimeOffset(_dtExpires.Value.Date.AddDays(1).AddSeconds(-1))
        : null;

    private void AddLabel(string text, ref int y)
    {
        var label = new Label
        {
            Text = text,
            Font = UITheme.FontLabel,
            ForeColor = UITheme.Neutral600,
            AutoSize = true,
            Location = new Point(0, y),
        };
        Controls.Add(label);
        y += 20;
    }

    private async Task SearchUsersAsync()
    {
        string? search = string.IsNullOrWhiteSpace(_txtEmail.Text) ? null : _txtEmail.Text.Trim();
        AdminUsersResult result = await _auth.GetAdminUsersAsync(search, null, 20, 0, _token);
        if (IsDisposed || _token.IsCancellationRequested) return;

        _lstUsers.Items.Clear();
        _users = [];
        if (!result.Succeeded || result.Response is null)
        {
            _hint.Text = result.ErrorMessage ?? "Không tìm được người dùng.";
            UpdateState();
            return;
        }

        _users = [.. result.Response.Items];
        foreach (AdminUserSummaryResponse user in _users)
        {
            _lstUsers.Items.Add($"{user.Email} — {user.DisplayName}");
        }

        _hint.Text = _users.Count == 0
            ? "Không tìm thấy người dùng phù hợp."
            : "Chọn đúng một người dùng trong danh sách kết quả.";
        if (_users.Count > 0) _lstUsers.SelectedIndex = 0;
        UpdateState();
    }

    private void UpdateState()
    {
        _okButton.Enabled = SelectedUserId is not null && SelectedPlanId is not null;
    }

    private sealed record PlanItem(AdminPlanResponse Plan)
    {
        public override string ToString() => $"{Plan.Code} — {Plan.Name}";
    }
}

/// <summary>Dialog đổi trạng thái/gia hạn một đăng ký gói (SCRUM-23).</summary>
public sealed class SubscriptionStatusDialog : Form
{
    private readonly ComboBox _cboStatus = new()
    {
        Font = UITheme.FontBody,
        Width = 200,
        DropDownStyle = ComboBoxStyle.DropDownList,
    };
    private readonly CheckBox _chkExpires = new()
    {
        Text = "Gia hạn đến",
        Font = UITheme.FontBody,
        AutoSize = true,
    };
    private readonly DateTimePicker _dtExpires = new()
    {
        Font = UITheme.FontBody,
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "dd/MM/yyyy",
        Width = 160,
        Enabled = false,
    };

    public SubscriptionStatusDialog(AdminSubscriptionResponse subscription)
    {
        Text = "Cập nhật đăng ký gói";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UITheme.White;
        Font = UITheme.FontBody;
        ClientSize = new Size(400, 240);

        var info = new Label
        {
            Text = $"{subscription.UserEmail}\n{subscription.PlanCode} — {subscription.PlanName}",
            Font = UITheme.FontBody,
            ForeColor = UITheme.Ink,
            AutoSize = false,
            Width = 360,
            Height = 44,
            Location = new Point(20, 16),
        };

        var statusLabel = new Label
        {
            Text = "Trạng thái",
            Font = UITheme.FontLabel,
            ForeColor = UITheme.Neutral600,
            AutoSize = true,
            Location = new Point(20, 70),
        };

        _cboStatus.Location = new Point(20, 92);
        _cboStatus.Items.AddRange(["ACTIVE", "EXPIRED", "CANCELLED"]);
        _cboStatus.SelectedItem = subscription.Status;
        if (_cboStatus.SelectedIndex < 0) _cboStatus.SelectedIndex = 0;

        _chkExpires.Location = new Point(20, 136);
        _chkExpires.CheckedChanged += (_, _) => _dtExpires.Enabled = _chkExpires.Checked;

        _dtExpires.Location = new Point(160, 133);
        _dtExpires.Value = subscription.ExpiresAt?.LocalDateTime ?? DateTime.Today.AddMonths(1);

        var ok = new RoundedButton
        {
            Text = "Lưu",
            Style = RoundButtonStyle.Primary,
            Size = new Size(100, 36),
            Location = new Point(ClientSize.Width - 190, ClientSize.Height - 56),
        };
        ok.Click += (_, _) =>
        {
            DialogResult = DialogResult.OK;
            Close();
        };
        var cancel = new RoundedButton
        {
            Text = "Hủy",
            Style = RoundButtonStyle.Secondary,
            Size = new Size(80, 36),
            Location = new Point(ClientSize.Width - 86, ClientSize.Height - 56),
        };
        cancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        Controls.Add(info);
        Controls.Add(statusLabel);
        Controls.Add(_cboStatus);
        Controls.Add(_chkExpires);
        Controls.Add(_dtExpires);
        Controls.Add(ok);
        Controls.Add(cancel);
    }

    public string Status => _cboStatus.SelectedItem?.ToString() ?? "ACTIVE";

    public DateTimeOffset? ExpiresAt => _chkExpires.Checked
        ? new DateTimeOffset(_dtExpires.Value.Date.AddDays(1).AddSeconds(-1))
        : null;
}
