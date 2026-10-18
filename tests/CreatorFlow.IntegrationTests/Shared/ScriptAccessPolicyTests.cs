using CreatorFlow.Models.Enums;
using CreatorFlow.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.Shared;

[TestClass]
public sealed class ScriptAccessPolicyTests
{
    [TestMethod]
    [DataRow(ProjectRole.Owner, ContentStatus.Script, false, false)]
    [DataRow(ProjectRole.Manager, ContentStatus.Editing, false, false)]
    [DataRow(ProjectRole.Creator, ContentStatus.Script, true, false)]  // người tạo
    [DataRow(ProjectRole.Creator, ContentStatus.Script, false, true)]  // người được giao
    public void Evaluate_AllowedCases_CanEdit(ProjectRole role, ContentStatus status, bool isCreator, bool isAssignee)
    {
        var access = ScriptAccessPolicy.Evaluate(role, status, isCreator, isAssignee);

        Assert.IsTrue(access.CanEdit);
        Assert.IsNull(access.ReadOnlyReason);
    }

    [TestMethod]
    public void Evaluate_CreatorNeitherAuthorNorAssignee_IsDeniedNotLocked()
    {
        var access = ScriptAccessPolicy.Evaluate(ProjectRole.Creator, ContentStatus.Script, false, false);

        Assert.IsFalse(access.CanEdit);
        Assert.IsFalse(access.IsContentLocked);
        Assert.IsNotNull(access.ReadOnlyReason);
    }

    [TestMethod]
    [DataRow(ProjectRole.Owner, ContentStatus.Published)]
    [DataRow(ProjectRole.Manager, ContentStatus.Published)]
    [DataRow(ProjectRole.Creator, ContentStatus.Archived)]
    [DataRow(ProjectRole.Owner, ContentStatus.Archived)]
    public void Evaluate_PublishedOrArchived_IsLockedForEveryone(ProjectRole role, ContentStatus status)
    {
        var access = ScriptAccessPolicy.Evaluate(role, status, true, true);

        Assert.IsFalse(access.CanEdit);
        Assert.IsTrue(access.IsContentLocked);
    }
}
