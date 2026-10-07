using CreatorFlow.Helpers;
using CreatorFlow.Models;
using CreatorFlow.Services;
using System.Globalization;

namespace CreatorFlow.Controls;

public partial class ProfileControl : UserControl
{
    private readonly AuthApiFacade _authService;
    private readonly CancellationTokenSource _lifetime = new();
    private CancellationTokenSource? _avatarRequest;
    private UserProfile? _loaded;
    private bool _busy;
    private CreatorFlow.Models.AvatarChange _avatarChange = CreatorFlow.Models.AvatarChange.Keep;
    private byte[]? _pendingAvatar;
    public event Action<UserProfile>? ProfileSaved;
    public event EventHandler? PasswordChanged;
    public event EventHandler? LogoutRequested;

    public ProfileControl(AuthApiFacade authService)
    {
        _authService = authService;
        InitializeComponent();
    }

    public async Task LoadProfileAsync()
    {
        SetBusy(true);
        messageLabel.Text = "Đang tải hồ sơ…";
        try
        {
            UserOperationResult result = await _authService.GetCurrentProfileAsync(_lifetime.Token);
            if (IsDisposed || _lifetime.IsCancellationRequested) return;
            if (result.Succeeded && result.Profile is { } profile)
            {
                _loaded = profile;
                RenderProfile();
                messageLabel.Text = string.Empty;
            }
            else messageLabel.Text = result.ErrorMessage;
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        finally { if (!IsDisposed) SetBusy(false); }
    }

    private void RenderProfile()
    {
        if (_loaded is not { } profile) return;
        _avatarChange = CreatorFlow.Models.AvatarChange.Keep;
        _pendingAvatar = null;
        nameTextBox.Text = profile.DisplayName;
        avatarTextBox.Text = profile.AvatarUrl ?? string.Empty;
        _avatarChange = CreatorFlow.Models.AvatarChange.Keep;
        emailTextBox.Text = profile.Email;
        accountLabel.Text = $"Trạng thái: {profile.AccountStatus} · {(profile.IsSystemAdmin ? "System Admin" : "Tài khoản cá nhân")}";
        string[] parts = profile.DisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        initialsLabel.Text = parts.Length == 0 ? "?" :
            (StringInfo.GetNextTextElement(parts[0]) +
                (parts.Length > 1 ? StringInfo.GetNextTextElement(parts[^1]) : string.Empty)).ToUpperInvariant();
        RefreshAvatar(profile.AvatarUrl);
    }

    private async void RefreshAvatar(string? url)
    {
        _avatarRequest?.Cancel();
        var request = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        _avatarRequest = request;
        avatarPicture.Visible = false;
        Image? previous = avatarPicture.Image;
        avatarPicture.Image = null;
        previous?.Dispose();
        try
        {
            CreatorFlow.Models.UserAvatar? stored = await _authService.GetCurrentAvatarAsync(request.Token);
            Image? image;
            if (stored is not null)
            {
                image = AvatarImageLoader.Decode(stored.ImageData);
            }
            else image = await AvatarImageLoader.LoadAsync(url, request.Token);
            if (IsDisposed || request.IsCancellationRequested || _avatarRequest != request)
            {
                image?.Dispose();
                return;
            }
            avatarPicture.Image = image;
            avatarPicture.Visible = image is not null;
        }
        catch (OperationCanceledException) when (request.IsCancellationRequested) { }
        finally
        {
            if (_avatarRequest == request) _avatarRequest = null;
            request.Dispose();
        }
    }

    private async void ChooseAvatarButton_Click(object? sender, EventArgs e)
    {
        if (_busy || _loaded is null) return;
        using var dialog = new OpenFileDialog { Filter = "Ảnh JPEG/PNG|*.jpg;*.jpeg;*.png", Multiselect = false };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        SetBusy(true);
        try
        {
            await using var stream = new FileStream(dialog.FileName, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, true);
            // Bound transport memory; Service owns image validation/normalization.
            byte[] buffer = new byte[AuthUiHints.MaximumAvatarBytes + 1];
            int total = 0, count;
            while (total < buffer.Length && (count = await stream.ReadAsync(buffer.AsMemory(total), _lifetime.Token)) > 0)
                total += count;
            byte[] input = buffer[..total];
            AvatarPreviewResult result = await _authService.PreviewAvatarAsync(input, _lifetime.Token);
            if (IsDisposed || _lifetime.IsCancellationRequested) return;
            if (result.Avatar is not { } avatar) { messageLabel.Text = result.ErrorMessage; return; }
            _pendingAvatar = avatar.ImageData;
            _avatarRequest?.Cancel();
            _avatarChange = CreatorFlow.Models.AvatarChange.Upload;
            avatarTextBox.Text = string.Empty;
            using var previewStream = new MemoryStream(avatar.ImageData);
            using Image decoded = Image.FromStream(previewStream);
            Image? previous = avatarPicture.Image;
            avatarPicture.Image = new Bitmap(decoded);
            previous?.Dispose();
            avatarPicture.Visible = true;
            messageLabel.Text = "Ảnh đang xem trước. Bấm Lưu thay đổi để lưu.";
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        { if (!IsDisposed) messageLabel.Text = "Không thể đọc ảnh đã chọn."; }
        finally { if (!IsDisposed) SetBusy(false); }
    }

    private void RemoveAvatarButton_Click(object? sender, EventArgs e)
    {
        if (_busy) return;
        _avatarRequest?.Cancel();
        _pendingAvatar = null;
        avatarTextBox.Text = string.Empty;
        _avatarChange = CreatorFlow.Models.AvatarChange.Remove;
        Image? previous = avatarPicture.Image;
        avatarPicture.Image = null;
        previous?.Dispose();
        avatarPicture.Visible = false;
        messageLabel.Text = "Bấm Lưu thay đổi để xóa ảnh đại diện.";
    }

    private async void SaveButton_Click(object? sender, EventArgs e)
    {
        if (_busy || _loaded is null) return;
        SetBusy(true);
        ClearFieldErrors();
        messageLabel.Text = "Đang lưu…";
        try
        {
            UserOperationResult result = await _authService.SaveCurrentProfileAvatarAsync(nameTextBox.Text, avatarTextBox.Text, _avatarChange, _pendingAvatar, _lifetime.Token);
            if (IsDisposed || _lifetime.IsCancellationRequested) return;
            if (result.Succeeded && result.Profile is { } profile)
            {
                _loaded = profile;
                RenderProfile();
                messageLabel.Text = "Đã lưu hồ sơ.";
                ProfileSaved?.Invoke(profile);
            }
            else ShowError(result);
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        finally { if (!IsDisposed) SetBusy(false); }
    }

    private async void ChangeButton_Click(object? sender, EventArgs e)
    {
        if (_busy || _loaded is null) return;
        SetBusy(true);
        ClearFieldErrors();
        messageLabel.Text = "Đang đổi mật khẩu…";
        try
        {
            UserOperationResult result = await _authService.ChangePasswordAsync(
                currentPassword.Text, newPassword.Text, confirmPassword.Text, _lifetime.Token);
            if (IsDisposed || _lifetime.IsCancellationRequested) return;
            ClearPasswords();
            if (result.Succeeded) PasswordChanged?.Invoke(this, EventArgs.Empty);
            else ShowError(result);
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        finally { if (!IsDisposed) SetBusy(false); }
    }

    private void ShowError(UserOperationResult result)
    {
        messageLabel.Text = result.ErrorMessage;
        if (result.ErrorField is { } key && fields.TryGetValue(key, out Panel? field))
            UiTheme.SetFieldError(field, result.ErrorMessage);
    }

    private void ClearFieldErrors()
    {
        foreach (Panel field in fields.Values) UiTheme.SetFieldError(field, null);
    }

    private void ClearPasswords()
    {
        currentPassword.Clear();
        newPassword.Clear();
        confirmPassword.Clear();
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        foreach (TextBox field in new[] { nameTextBox, avatarTextBox, currentPassword, newPassword, confirmPassword })
            field.Enabled = !busy && _loaded is not null;
        chooseAvatarButton.Enabled = removeAvatarButton.Enabled = !busy && _loaded is not null;
        saveButton.Enabled = changeButton.Enabled = cancelButton.Enabled = showPassword.Enabled = !busy && _loaded is not null;
        logoutButton.Enabled = !busy;
        UseWaitCursor = busy;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !IsDisposed)
        {
            _lifetime.Cancel();
            _avatarRequest?.Cancel();
            ClearPasswords();
            avatarPicture.Image?.Dispose();
            avatarPicture.Image = null;
            _lifetime.Dispose();
        }
        base.Dispose(disposing);
    }
}
