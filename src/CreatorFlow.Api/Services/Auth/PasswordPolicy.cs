using System.Text;

namespace CreatorFlow.Api.Services.Auth;

public static class PasswordPolicy
{
    public const string Description =
        "Ít nhất 8 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt. Tối đa 72 byte UTF-8.";

    public static string? Validate(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return "Vui lòng nhập mật khẩu.";
        }

        int characterCount = 0;
        bool hasUpper = false, hasLower = false, hasDigit = false, hasSpecial = false;
        foreach (Rune character in password.EnumerateRunes())
        {
            characterCount++;
            hasUpper |= Rune.IsUpper(character);
            hasLower |= Rune.IsLower(character);
            hasDigit |= Rune.IsDigit(character);
            hasSpecial |= Rune.IsPunctuation(character) || Rune.IsSymbol(character);
        }

        if (characterCount < 8)
        {
            return "Mật khẩu cần ít nhất 8 ký tự.";
        }
        if (Encoding.UTF8.GetByteCount(password) > 72)
        {
            return "Mật khẩu không được vượt quá 72 byte UTF-8.";
        }
        if (!hasUpper || !hasLower || !hasDigit || !hasSpecial)
        {
            return "Mật khẩu cần chữ hoa, chữ thường, số và ký tự đặc biệt.";
        }
        return null;
    }
}
