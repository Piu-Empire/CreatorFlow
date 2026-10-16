using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Models.Auth;
using CreatorFlow.Api.Models.Projects;
using CreatorFlow.Api.Repositories.Projects;
using CreatorFlow.Api.Services.Projects;
using CreatorFlow.Contracts.Projects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.Api.Tests.Services;

[TestClass]
[TestCategory("ProjectApiUnit")]
public sealed class ProjectServiceTests
{
    internal static CurrentAuthenticatedUser Actor(bool admin = false) => new() { User = new User
    { UserId = 42, Email = "project@example.test", DisplayName = "Fixture", PasswordHash = "unused", AccountStatus = AccountStatus.Active, EmailVerifiedAt = DateTimeOffset.UtcNow, IsSystemAdmin = admin } };

    [TestMethod]
    [DataRow("MANAGER", false, 403)]
    [DataRow("CREATOR", false, 403)]
    [DataRow("MANAGER", true, 403)]
    [DataRow("OWNER", true, 200)]
    public async Task Mutations_UseLockedMembershipAndNoAdminBypass(string role, bool admin, int expected)
    {
        var repo = new ProjectFake { Project = ProjectFake.Record(role) };
        var service = new ProjectService(repo, Actor(admin));
        Assert.AreEqual(expected, (await service.UpdateAsync(8, new("Renamed", "text"), default)).Status);
        Assert.AreEqual(expected, (await service.ArchiveAsync(8, default)).Status);
        Assert.AreEqual(expected, (await service.DeleteAsync(8, default)).Status);
        Assert.AreEqual(3, repo.LockCount);
        Assert.AreEqual(expected == 200 ? 3 : 0, repo.WriteCount);
    }
    [TestMethod]
    public async Task OwnerRoleFromProjectMembersControlsWrites()
    {
        var repo = new ProjectFake { Project = null }; var service = new ProjectService(repo, Actor());
        Assert.AreEqual(404, (await service.DeleteAsync(8, default)).Status);
        repo.Project = ProjectFake.Record("OWNER") with { OwnerId = 99 };
        Assert.AreEqual(200, (await service.DeleteAsync(8, default)).Status);
        Assert.AreEqual(1, repo.WriteCount);
    }
    [TestMethod]
    public async Task Create_TrimsNameAndUsesAuthenticatedUser()
    {
        var repo = new ProjectFake(); var service = new ProjectService(repo, Actor());
        var result = await service.CreateAsync(new("  Team  ", "\n formatted \n"), default);
        Assert.AreEqual(201, result.Status); Assert.AreEqual("Team", result.Value!.Name);
        Assert.AreEqual(42L, repo.CreatedUser); Assert.AreEqual("OWNER", result.Value.CurrentUserRole);
        Assert.AreEqual("\n formatted \n", result.Value.Description);
    }
    [TestMethod]
    public async Task NameValidation_CountsUnicodeScalarsAndRejectsEmptyOrTooLong()
    {
        var service = new ProjectService(new ProjectFake(), Actor());
        Assert.AreEqual(400, (await service.CreateAsync(new("  ", null), default)).Status);
        Assert.AreEqual(201, (await service.CreateAsync(new(string.Concat(Enumerable.Repeat("😀", 200)), null), default)).Status);
        Assert.AreEqual(400, (await service.CreateAsync(new(new string('a', 201), null), default)).Status);
        Assert.AreEqual(400, (await service.CreateAsync(new("a", "\0"), default)).Status);
    }
    [TestMethod]
    public async Task Archived_CanBeManagedButCannotBeSelected()
    {
        var repo = new ProjectFake { Project = ProjectFake.Record("OWNER") with { Status = "ARCHIVED" } };
        var service = new ProjectService(repo, Actor());
        Assert.AreEqual(200, (await service.GetAsync(8, default)).Status);
        Assert.AreEqual(409, (await service.AccessAsync(8, default)).Status);
        Assert.AreEqual(200, (await service.DeleteAsync(8, default)).Status);
    }

    [TestMethod]
    public async Task Restore_RequiresOwnerAndArchivedStatus()
    {
        var repo = new ProjectFake { Project = ProjectFake.Record("OWNER") with { Status = "ARCHIVED" } };
        var service = new ProjectService(repo, Actor());

        var restored = await service.RestoreAsync(8, default);
        Assert.AreEqual(200, restored.Status);
        Assert.AreEqual("ACTIVE", restored.Value!.Status);
        Assert.AreEqual("ACTIVE", repo.Project!.Status);
        Assert.AreEqual(1, repo.WriteCount);

        Assert.AreEqual(409, (await service.RestoreAsync(8, default)).Status);
        repo.Project = ProjectFake.Record("MANAGER") with { Status = "ARCHIVED" };
        Assert.AreEqual(403, (await service.RestoreAsync(8, default)).Status);
        Assert.AreEqual(1, repo.WriteCount);
    }

    [TestMethod]
    public async Task RevokedBetweenReadAndMutation_IsRechecked()
    {
        var repo = new ProjectFake(); var service = new ProjectService(repo, Actor());
        Assert.AreEqual(200, (await service.AccessAsync(8, default)).Status);
        repo.Project = null;
        Assert.AreEqual(404, (await service.UpdateAsync(8, new("Changed", null), default)).Status);
        Assert.AreEqual(0, repo.WriteCount);
        Assert.AreEqual(401, (await new ProjectService(repo, new()).ListAsync(default)).Status);
    }
}

internal sealed class ProjectFake : IProjectRepository, IProjectTransaction
{
    public ProjectRecord? Project { get; set; } = Record("OWNER");
    public List<ProjectMemberRecord> Members { get; } =
    [
        new(8, 42, "Fixture", "project@example.test", "OWNER", DateTimeOffset.UtcNow),
        new(8, 55, "Member", "member@example.test", "CREATOR", DateTimeOffset.UtcNow)
    ];
    public long CreatedUser { get; private set; }
    public int LockCount { get; private set; }
    public int WriteCount { get; private set; }
    public static ProjectRecord Record(string role) => new(8, "Team", null, 42, "Fixture", role, "ACTIVE", DateTimeOffset.UtcNow);
    public Task<IReadOnlyList<ProjectRecord>> ListAsync(long userId, CancellationToken token) => Task.FromResult<IReadOnlyList<ProjectRecord>>(userId == 42 && Project is not null ? [Project] : []);
    public Task<ProjectRecord?> FindAsync(long id, long userId, CancellationToken token) => Task.FromResult(id == 8 && userId == 42 ? Project : null);
    public Task<ProjectMembersRecord?> ListMembersAsync(long id, long actorUserId, CancellationToken token) =>
        Task.FromResult<ProjectMembersRecord?>(id == 8 && actorUserId == 42 && Project is not null
            ? new ProjectMembersRecord(Project.Role, Members.Where(member => member.IsActive).ToArray()) : null);
    public Task<T> InTransactionAsync<T>(Func<IProjectTransaction, CancellationToken, Task<T>> action, CancellationToken token) => action(this, token);
    public Task<ProjectRecord?> LockAsync(long id, long userId, CancellationToken token) { LockCount++; return FindAsync(id, userId, token); }
    public Task<ProjectMemberRecord?> LockMemberAsync(long projectId, long memberUserId, CancellationToken token) =>
        Task.FromResult(Members.FirstOrDefault(member => member.ProjectId == projectId && member.UserId == memberUserId && member.IsActive));
    public Task<int> CountActiveOwnersAsync(long projectId, CancellationToken token) =>
        Task.FromResult(Members.Count(member => member.ProjectId == projectId && member.IsActive && member.Role == "OWNER"));
    public Task<long?> FindActiveOwnerAsync(long projectId, long exceptUserId, CancellationToken token) =>
        Task.FromResult(Members.FirstOrDefault(member => member.ProjectId == projectId && member.UserId != exceptUserId && member.IsActive && member.Role == "OWNER")?.UserId);
    public Task SetProjectOwnerAsync(long projectId, long userId, CancellationToken token)
    { if (Project is not null) Project = Project with { OwnerId = userId }; WriteCount++; return Task.CompletedTask; }
    public Task UpdateMemberRoleAsync(long projectId, long memberUserId, string role, CancellationToken token)
    {
        int index = Members.FindIndex(member => member.ProjectId == projectId && member.UserId == memberUserId && member.IsActive);
        if (index >= 0) Members[index] = Members[index] with { Role = role };
        WriteCount++; return Task.CompletedTask;
    }
    public Task RemoveMemberAsync(long projectId, long memberUserId, CancellationToken token)
    {
        int index = Members.FindIndex(member => member.ProjectId == projectId && member.UserId == memberUserId && member.IsActive);
        if (index >= 0) Members[index] = Members[index] with { IsActive = false };
        WriteCount++; return Task.CompletedTask;
    }
    public Task<ProjectRecord> CreateAsync(long userId, string name, string? description, CancellationToken token)
    { CreatedUser = userId; WriteCount++; Project = Record("OWNER") with { Name = name, Description = description, OwnerId = userId }; return Task.FromResult(Project); }
    public Task<ProjectRecord> UpdateAsync(long id, long userId, string name, string? description, CancellationToken token)
    { WriteCount++; Project = Project! with { Name = name, Description = description }; return Task.FromResult(Project); }
    public Task<ProjectRecord> ArchiveAsync(long id, long userId, CancellationToken token)
    { WriteCount++; Project = Project! with { Status = "ARCHIVED" }; return Task.FromResult(Project); }
    public Task<ProjectRecord> RestoreAsync(long id, long userId, CancellationToken token)
    { WriteCount++; Project = Project! with { Status = "ACTIVE" }; return Task.FromResult(Project); }
    public Task DeleteAsync(long id, CancellationToken token) { WriteCount++; Project = null; return Task.CompletedTask; }
}
