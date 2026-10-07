namespace CreatorFlow.Services;

internal static class AuthUiHints
{
    public const int MaximumAvatarBytes = 2 * 1024 * 1024;
    public const int ResendSeconds = 60;
    public const string PasswordPolicyDescription = "Ít nhất 8 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt. Tối đa 72 byte UTF-8.";
}
