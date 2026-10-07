using CreatorFlow.Api.Services.Auth;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.Api.Tests.Services;

[TestClass]
[TestCategory("AuthUnit")]
public sealed class PasswordPolicyTests
{
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("Ab1!xyz")]
    [DataRow("abcdef1!")]
    [DataRow("ABCDEF1!")]
    [DataRow("Abcdefg!")]
    [DataRow("Abcdefg1")]
    [DataRow("Abcdef1 ")]
    public void InvalidPassword_IsRejected(string? password) => Assert.IsNotNull(PasswordPolicy.Validate(password));

    [TestMethod]
    public void Boundary_UsesUtf8BytesAndDoesNotTrim()
    {
        Assert.IsNull(PasswordPolicy.Validate("Abcdef1!"));
        Assert.IsNull(PasswordPolicy.Validate("Ab1!" + new string('a', 68)));
        Assert.IsNotNull(PasswordPolicy.Validate("Ab1!" + new string('a', 69)));
        Assert.IsNull(PasswordPolicy.Validate("Ab1!" + new string('é', 34)));
        Assert.IsNotNull(PasswordPolicy.Validate("Ab1!" + new string('é', 35)));
        Assert.IsNull(PasswordPolicy.Validate(" Abcdef1! "));
    }
}
