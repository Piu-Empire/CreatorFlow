using CreatorFlow.Api.Services.Auth;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.Cryptography;

namespace CreatorFlow.Api.Tests.Services;

[TestClass]
[TestCategory("PasswordResetUnit")]
public sealed class EmailOtpCodeProtectorTests
{
    [TestMethod]
    public void Verifier_IsBoundToKeyRequestAndExactCode()
    {
        var protector = new EmailOtpCodeProtector(RandomNumberGenerator.GetBytes(32));
        Guid requestId = Guid.NewGuid();
        byte[] verifier = protector.Protect(requestId, 42, "person@example.test", EmailOtpCodeProtector.ResetPurpose, "012345");
        Assert.AreEqual(32, verifier.Length);
        Assert.IsFalse(protector.Verify(requestId, 43, "person@example.test", EmailOtpCodeProtector.ResetPurpose, "012345", verifier));
        Assert.IsFalse(protector.Verify(requestId, 42, "other@example.test", EmailOtpCodeProtector.ResetPurpose, "012345", verifier));
        Assert.IsFalse(protector.Verify(requestId, 42, "person@example.test", EmailOtpCodeProtector.VerificationPurpose, "012345", verifier));
        Assert.IsTrue(protector.Verify(requestId, 42, "person@example.test", EmailOtpCodeProtector.ResetPurpose, "012345", verifier));
        Assert.IsFalse(protector.Verify(requestId, 42, "person@example.test", EmailOtpCodeProtector.ResetPurpose, "12345", verifier));
        Assert.IsFalse(protector.Verify(Guid.NewGuid(), 42, "person@example.test", EmailOtpCodeProtector.ResetPurpose, "012345", verifier));
        Assert.IsFalse(new EmailOtpCodeProtector(RandomNumberGenerator.GetBytes(32)).Verify(requestId, 42, "person@example.test", EmailOtpCodeProtector.ResetPurpose, "012345", verifier));
        Assert.IsFalse(protector.Verify(requestId, 42, "person@example.test", EmailOtpCodeProtector.ResetPurpose, "012345", new byte[0]));
        Assert.IsFalse(CryptographicOperations.FixedTimeEquals(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("012345")), verifier));
    }

    [TestMethod]
    public void Generator_ProducesSixAsciiDigits_AndRejectsNonAsciiFormats()
    {
        for (int index = 0; index < 100; index++)
            Assert.IsTrue(EmailOtpCodeProtector.IsCodeFormatValid(EmailOtpCodeProtector.GenerateCode()));
        Assert.IsTrue(EmailOtpCodeProtector.IsCodeFormatValid("000001"));
        Assert.IsFalse(EmailOtpCodeProtector.IsCodeFormatValid("１２３４５６"));
        Assert.IsFalse(EmailOtpCodeProtector.IsCodeFormatValid("12345 "));
        Assert.ThrowsExactly<ArgumentException>(() => new EmailOtpCodeProtector(new byte[31]));
    }
}
