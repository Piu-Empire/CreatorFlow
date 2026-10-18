using CreatorFlow.Api.Services.Projects;
using CreatorFlow.Contracts.Projects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.Api.Tests.Services;

[TestClass]
[TestCategory("ProjectApiUnit")]
public sealed class ProjectMembersServiceTests
{
    [TestMethod]
    public async Task ListMembers_IsOwnerOnlyAndReturnsActiveProjectMembers()
    {
        var repo = new ProjectFake();
        repo.Members.Add(new(8, 56, "Inactive", "inactive@example.test", "CREATOR", DateTimeOffset.UtcNow, false));
        var service = new ProjectMembersService(repo, ProjectServiceTests.Actor());

        var result = await service.ListAsync(8, default);

        Assert.AreEqual(200, result.Status);
        Assert.AreEqual(2, result.Value!.Members.Count);
        Assert.IsTrue(result.Value.Members.Any(member => member.UserId == 42 && member.Email == "project@example.test"));
        repo.Project = ProjectFake.Record("MANAGER");
        Assert.AreEqual(403, (await service.ListAsync(8, default)).Status);
        repo.Project = null;
        Assert.AreEqual(404, (await service.ListAsync(8, default)).Status);
        Assert.AreEqual(401, (await new ProjectMembersService(repo, new()).ListAsync(8, default)).Status);
    }

    [TestMethod]
    public async Task UpdateRole_ValidatesRoleAndChangesOnlyRequestedMembership()
    {
        var repo = new ProjectFake();
        var service = new ProjectMembersService(repo, ProjectServiceTests.Actor());

        Assert.AreEqual(400, (await service.UpdateRoleAsync(8, 55, new("Viewer"), default)).Status);
        var result = await service.UpdateRoleAsync(8, 55, new(" manager "), default);

        Assert.AreEqual(200, result.Status);
        Assert.AreEqual("MANAGER", result.Value!.Role);
        Assert.AreEqual("MANAGER", repo.Members.Single(member => member.UserId == 55).Role);
        Assert.AreEqual("OWNER", repo.Members.Single(member => member.UserId == 42).Role);
    }

    [TestMethod]
    public async Task LastActiveOwner_CannotBeDemotedOrRemoved()
    {
        var repo = new ProjectFake();
        var service = new ProjectMembersService(repo, ProjectServiceTests.Actor());

        var demote = await service.UpdateRoleAsync(8, 42, new("MANAGER"), default);
        var remove = await service.RemoveAsync(8, 42, default);

        Assert.AreEqual(409, demote.Status);
        Assert.AreEqual("last_owner_required", demote.Code);
        Assert.AreEqual(409, remove.Status);
        Assert.AreEqual("last_owner_required", remove.Code);
        Assert.AreEqual("OWNER", repo.Members.Single(member => member.UserId == 42).Role);
        Assert.IsTrue(repo.Members.Single(member => member.UserId == 42).IsActive);
        Assert.AreEqual(0, repo.WriteCount);
    }

    [TestMethod]
    public async Task OwnerCanDemoteOrRemoveSelfWhenAnotherOwnerRemains()
    {
        var repo = new ProjectFake();
        repo.Members.Add(new(8, 43, "Second Owner", "second@example.test", "OWNER", DateTimeOffset.UtcNow));
        var service = new ProjectMembersService(repo, ProjectServiceTests.Actor());

        var demote = await service.UpdateRoleAsync(8, 42, new("CREATOR"), default);
        Assert.AreEqual(200, demote.Status);
        Assert.AreEqual(43L, repo.Project!.OwnerId);
        Assert.AreEqual("CREATOR", repo.Members.Single(member => member.UserId == 42).Role);

        repo.Members[0] = repo.Members[0] with { Role = "OWNER", IsActive = true };
        repo.Project = ProjectFake.Record("OWNER") with { OwnerId = 42 };
        var remove = await service.RemoveAsync(8, 42, default);
        Assert.AreEqual(200, remove.Status);
        Assert.IsTrue(remove.Value!.Removed);
        Assert.AreEqual(43L, repo.Project!.OwnerId);
        Assert.IsFalse(repo.Members.Single(member => member.UserId == 42).IsActive);
        Assert.AreEqual(43L, repo.Members.Single(member => member.UserId == 43 && member.IsActive && member.Role == "OWNER").UserId);
    }

    [TestMethod]
    public async Task NonOwnerCannotChangeOrRemoveMembersAndMissingMemberIsNotFound()
    {
        var repo = new ProjectFake { Project = ProjectFake.Record("CREATOR") };
        var service = new ProjectMembersService(repo, ProjectServiceTests.Actor());
        Assert.AreEqual(403, (await service.UpdateRoleAsync(8, 55, new("MANAGER"), default)).Status);
        Assert.AreEqual(403, (await service.RemoveAsync(8, 55, default)).Status);
        repo.Project = ProjectFake.Record("OWNER");
        Assert.AreEqual(404, (await service.UpdateRoleAsync(8, 999, new("MANAGER"), default)).Status);
        Assert.AreEqual(404, (await service.RemoveAsync(8, 999, default)).Status);
        Assert.AreEqual(0, repo.WriteCount);
    }
}
