using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace CreatorFlow.Api.Services.Auth;

public sealed class EmailOtpCodeProtector
{
    public const string VerificationPurpose = "EMAIL_VERIFICATION";
    public const string ResetPurpose = "PASSWORD_RESET";
    private readonly byte[] _key;

    public EmailOtpCodeProtector(byte[] key)
    {
        if (key.Length != 32) throw new ArgumentException("OTP key must contain 32 bytes.", nameof(key));
        _key = (byte[])key.Clone();
    }

    public static string GenerateCode() => RandomNumberGenerator.GetInt32(0, 1_000_000)
        .ToString("D6", CultureInfo.InvariantCulture);
    public static bool IsCodeFormatValid(string code) => code.Length == 6 &&
        code.All(c => c is >= '0' and <= '9');

    public byte[] Protect(Guid requestId, long? userId, string email, string purpose, string code)
    {
        if (purpose is not (VerificationPurpose or ResetPurpose)) throw new ArgumentException("Invalid OTP purpose.");
        using var payload = new MemoryStream();
        using (var writer = new BinaryWriter(payload, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write("CreatorFlow/EmailOtp/v2");
            writer.Write(requestId.ToByteArray());
            writer.Write(userId.HasValue);
            if (userId.HasValue) writer.Write(userId.Value);
            writer.Write(email);
            writer.Write(purpose);
            writer.Write(code);
        }
        return HMACSHA256.HashData(_key, payload.ToArray());
    }

    public bool Verify(Guid requestId, long? userId, string email, string purpose, string code, byte[] verifier) =>
        verifier.Length == 32 && CryptographicOperations.FixedTimeEquals(
            Protect(requestId, userId, email, purpose, code), verifier);
}
