using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Models.Tasks;
using CreatorFlow.Api.Models.Workflow;
using CreatorFlow.Api.Services.Workflow;
using CreatorFlow.Api.Tests.Services;
using CreatorFlow.Contracts.Workflow;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.Api.Tests.Workflow;

[TestClass]
[TestCategory("ProjectAuthorization")]
public sealed class WorkflowServiceTests
{
    [TestMethod]
    public async Task DecideReview_RequiresAuthenticatedOwnerOrManagerBeforeWriting()
    {
        foreach (ProjectRole? role in new ProjectRole?[] { ProjectRole.Owner, ProjectRole.Manager, ProjectRole.Creator, null })
        {
            var repo = new WorkflowRepositoryFake();
            if (role is { } value) repo.Roles[(8, 42)] = value;
            var service = new WorkflowService(repo, ProjectServiceTests.Actor());
            var result = await service.DecideReviewAsync(10, new(true));
            bool allowed = role is ProjectRole.Owner or ProjectRole.Manager;
            Assert.AreEqual(allowed ? 200 : 403, result.Status);
            Assert.AreEqual(allowed ? 1 : 0, repo.Writes.Count);
            if (allowed) Assert.AreEqual((10L, 42L), repo.Writes.Single());
        }
        var anonymous = new WorkflowRepositoryFake();
        Assert.AreEqual(401, (await new WorkflowService(anonymous, new CurrentAuthenticatedUser())
            .DecideReviewAsync(10, new(true))).Status);
        Assert.AreEqual(0, anonymous.RoleReads.Count);
        Assert.AreEqual(0, anonymous.Writes.Count);
    }

    [TestMethod]
    public async Task RoleIsScopedToResourceProject_AndRecheckedAfterDemotionAndRemoval()
    {
        var repo = new WorkflowRepositoryFake();
        repo.Roles[(8, 42)] = ProjectRole.Owner;
        repo.Roles[(9, 42)] = ProjectRole.Creator;
        var service = new WorkflowService(repo, ProjectServiceTests.Actor());
        Assert.AreEqual(403, (await service.DecideReviewAsync(20, new(true))).Status);
        Assert.AreEqual((9L, 42L), repo.RoleReads.Last());
        Assert.AreEqual(200, (await service.DecideReviewAsync(10, new(true))).Status);
        repo.Contents[10] = repo.Contents[10] with { Status = WorkflowContentStatus.Review };
        repo.Roles[(8, 42)] = ProjectRole.Creator;
        Assert.AreEqual(403, (await service.DecideReviewAsync(10, new(true))).Status);
        repo.Roles.Remove((8, 42)); // Không còn membership hoạt động.
        Assert.AreEqual(403, (await service.GetStatusHistoryAsync(10)).Status);
        Assert.AreEqual(1, repo.Writes.Count);
    }

    [TestMethod]
    public async Task CreatorCanSubmitAndResubmit_ButCannotDecideReview()
    {
        var repo = new WorkflowRepositoryFake();
        repo.Roles[(8, 42)] = ProjectRole.Creator;
        repo.Contents[10] = repo.Contents[10] with { Status = WorkflowContentStatus.Editing };
        var service = new WorkflowService(repo, ProjectServiceTests.Actor());
        Assert.AreEqual(200, (await service.SubmitReviewAsync(10, new())).Status);
        Assert.AreEqual(403, (await service.DecideReviewAsync(10, new(false, "Revise"))).Status);
        repo.Roles[(8, 42)] = ProjectRole.Manager;
        Assert.AreEqual(400, (await service.DecideReviewAsync(10, new(false))).Status);
        Assert.AreEqual(200, (await service.DecideReviewAsync(10, new(false, "Revise"))).Status);
        repo.Roles[(8, 42)] = ProjectRole.Creator;
        Assert.AreEqual(200, (await service.SubmitReviewAsync(10, new())).Status);
        Assert.AreEqual(3, repo.Writes.Count);
    }
}
