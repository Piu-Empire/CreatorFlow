using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Models.Auth;
using CreatorFlow.Api.Models.Tasks;
using CreatorFlow.Api.Repositories.Tasks;
using CreatorFlow.Api.Services.Tasks;
using CreatorFlow.Contracts.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.Api.Tests.Tasks;

/// <summary>
/// Quyền và quy tắc giao việc / My Tasks / progress / deadline được thực thi Ở BACKEND:
/// người gọi lấy từ JWT, kiểm tra thành viên Project, vai trò, validation, giao nhiều Creator mỗi người một deadline.
/// </summary>
[TestClass]
public sealed class TaskServiceTests
{
    private const long ProjectId = 1;
    private const long OwnerId = 1;
    private const long CreatorId = 2;
    private const long ManagerId = 3;
    private const long OtherCreatorId = 4;
    private const long OutsiderCreatorId = 9; // Creator của Project #2, không thuộc Project #1
    private const long ContentId = 10;

    private static readonly DateOnly Today = new(2026, 10, 8);

    private static AssigneeDeadlineRequest Req(long userId, DateOnly? deadline = null) => new(userId, deadline);

    // ================================================================== đọc

    [TestMethod]
    public async Task GetMyTasks_NotSignedIn_Returns401()
    {
        var f = new Fixture(actingUserId: null);

        var result = await f.Service.GetMyTasksAsync(ProjectId);

        Assert.AreEqual(401, result.Status);
        Assert.AreEqual("unauthorized", result.ErrorCode);
    }

    [TestMethod]
    public async Task GetMyTasks_NotProjectMember_Returns403()
    {
        var f = new Fixture(99);

        var result = await f.Service.GetMyTasksAsync(ProjectId);

        Assert.AreEqual(403, result.Status);
    }

    [TestMethod]
    public async Task GetMyTasks_ReturnsOnlyOwnTasksInThatProject()
    {
        var f = new Fixture(CreatorId);
        f.Add(Assignment(1, ContentId, CreatorId));
        f.Add(Assignment(2, ContentId, OtherCreatorId));          // của người khác
        f.Add(Assignment(3, 20, CreatorId, projectId: 2));       // Project khác
        f.Add(Assignment(4, 21, CreatorId, status: AssignmentStatus.Cancelled)); // đã huỷ

        var result = await f.Service.GetMyTasksAsync(ProjectId);

        Assert.IsTrue(result.Succeeded);
        CollectionAssert.AreEqual(new long[] { 1 }, result.Value!.Tasks.Select(t => t.AssignmentId).ToArray());
        Assert.AreEqual("InProgress", result.Value.Tasks[0].Status);
        Assert.AreEqual(2, result.Value.Tasks[0].TeamTotal);
    }

    [TestMethod]
    public async Task GetMyRole_ReturnsRoleOfSignedInUser_NonMemberGets403()
    {
        Assert.AreEqual("Manager", (await new Fixture(ManagerId).Service.GetMyRoleAsync(ProjectId)).Value!.Role);
        Assert.AreEqual("Creator", (await new Fixture(CreatorId).Service.GetMyRoleAsync(ProjectId)).Value!.Role);
        Assert.AreEqual(403, (await new Fixture(99).Service.GetMyRoleAsync(ProjectId)).Status);
        Assert.AreEqual(401, (await new Fixture(null).Service.GetMyRoleAsync(ProjectId)).Status);
    }

    [TestMethod]
    public async Task GetAssignableMembers_OwnerSeesAllCreators_CreatorSeesOnlySelf()
    {
        var owner = await new Fixture(OwnerId).Service.GetAssignableMembersAsync(ProjectId);
        var creator = await new Fixture(CreatorId).Service.GetAssignableMembersAsync(ProjectId);
        var outsider = await new Fixture(99).Service.GetAssignableMembersAsync(ProjectId);

        CollectionAssert.AreEquivalent(new long[] { CreatorId, OtherCreatorId }, owner.Value!.Members.Select(m => m.UserId).ToArray());
        CollectionAssert.AreEqual(new long[] { CreatorId }, creator.Value!.Members.Select(m => m.UserId).ToArray());
        Assert.AreEqual(403, outsider.Status);
    }

    // ================================================================== progress

    [TestMethod]
    [DataRow(0, "Assigned")]
    [DataRow(40, "InProgress")]
    [DataRow(100, "Completed")]
    public async Task UpdateProgress_Assignee_WritesPercentAndDerivedStatus(int percent, string expected)
    {
        var f = new Fixture(CreatorId);
        f.Add(Assignment(1, ContentId, CreatorId, progress: 20));

        var result = await f.Service.UpdateProgressAsync(ContentId, new UpdateProgressRequest(percent));

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(percent, result.Value!.ProgressPercent);
        Assert.AreEqual(expected, result.Value.Status);
        Assert.AreEqual(1, f.Repo.ProgressWrites.Count);
    }

    [TestMethod]
    [DataRow(-1)]
    [DataRow(101)]
    public async Task UpdateProgress_OutOfRange_Returns400AndWritesNothing(int percent)
    {
        var f = new Fixture(CreatorId);
        f.Add(Assignment(1, ContentId, CreatorId));

        var result = await f.Service.UpdateProgressAsync(ContentId, new UpdateProgressRequest(percent));

        Assert.AreEqual(400, result.Status);
        Assert.AreEqual(0, f.Repo.ProgressWrites.Count);
    }

    [TestMethod]
    public async Task UpdateProgress_UserNotAssigned_Returns403()
    {
        var f = new Fixture(ManagerId); // Manager là thành viên nhưng không được giao
        f.Add(Assignment(1, ContentId, CreatorId));

        var result = await f.Service.UpdateProgressAsync(ContentId, new UpdateProgressRequest(50));

        Assert.AreEqual(403, result.Status);
        Assert.AreEqual(0, f.Repo.ProgressWrites.Count);
    }

    [TestMethod]
    public async Task UpdateProgress_ContentPublished_Returns409()
    {
        var f = new Fixture(CreatorId);
        f.Add(Assignment(1, ContentId, CreatorId, stage: "Published", status: AssignmentStatus.Completed, progress: 100));

        var result = await f.Service.UpdateProgressAsync(ContentId, new UpdateProgressRequest(50));

        Assert.AreEqual(409, result.Status);
    }

    [TestMethod]
    public async Task UpdateProgress_NothingChanged_SkipsWrite()
    {
        var f = new Fixture(CreatorId);
        f.Add(Assignment(1, ContentId, CreatorId, progress: 40));

        await f.Service.UpdateProgressAsync(ContentId, new UpdateProgressRequest(40));

        Assert.AreEqual(0, f.Repo.ProgressWrites.Count);
    }

    [TestMethod]
    public async Task UpdateProgress_TwoCreators_EachUpdatesOnlyOwnAssignmentAndTeamCountsFollow()
    {
        var f = new Fixture(OtherCreatorId);
        f.Add(Assignment(11, ContentId, CreatorId, status: AssignmentStatus.Completed, progress: 100));
        f.Add(Assignment(12, ContentId, OtherCreatorId, status: AssignmentStatus.Assigned, progress: 0));

        var result = await f.Service.UpdateProgressAsync(ContentId, new UpdateProgressRequest(100));

        Assert.AreEqual(12L, f.Repo.ProgressWrites.Single().AssignmentId);   // chỉ assignment của chính người gọi
        Assert.AreEqual(2, result.Value!.TeamDone);                          // cả nhóm đã xong → Content hoàn thành
        Assert.AreEqual(2, result.Value.TeamTotal);
    }

    // ================================================================== deadline

    [TestMethod]
    public async Task ChangeDeadline_Creator_Returns403EvenForOwnTask()
    {
        var f = new Fixture(CreatorId);
        f.Add(Assignment(1, ContentId, CreatorId, deadline: Today.AddDays(3)));

        var result = await f.Service.ChangeDeadlineAsync(ContentId, CreatorId, new ChangeDeadlineRequest(Today.AddDays(10)));

        Assert.AreEqual(403, result.Status);
        Assert.AreEqual(0, f.Repo.DeadlineWrites.Count);
    }

    [TestMethod]
    [DataRow(OwnerId)]
    [DataRow(ManagerId)]
    public async Task ChangeDeadline_OwnerOrManager_ChangesOnlyTheChosenCreator(long actor)
    {
        var f = new Fixture(actor);
        f.Add(Assignment(11, ContentId, CreatorId, deadline: Today.AddDays(3)));
        f.Add(Assignment(12, ContentId, OtherCreatorId, deadline: Today.AddDays(3)));

        var result = await f.Service.ChangeDeadlineAsync(ContentId, OtherCreatorId, new ChangeDeadlineRequest(Today.AddDays(9)));

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(Today.AddDays(9), result.Value!.Deadline);
        Assert.AreEqual((12L, (DateOnly?)Today.AddDays(9)), f.Repo.DeadlineWrites.Single());
    }

    [TestMethod]
    public async Task ChangeDeadline_PastDate_Returns400()
    {
        var f = new Fixture(ManagerId);
        f.Add(Assignment(1, ContentId, CreatorId, deadline: Today.AddDays(3)));

        var result = await f.Service.ChangeDeadlineAsync(ContentId, CreatorId, new ChangeDeadlineRequest(Today.AddDays(-1)));

        Assert.AreEqual(400, result.Status);
        Assert.AreEqual(0, f.Repo.DeadlineWrites.Count);
    }

    [TestMethod]
    public async Task ChangeDeadline_ExtendingOverdueTask_IsAllowed()
    {
        var f = new Fixture(ManagerId);
        f.Add(Assignment(1, ContentId, CreatorId, deadline: Today.AddDays(-4)));

        var result = await f.Service.ChangeDeadlineAsync(ContentId, CreatorId, new ChangeDeadlineRequest(Today.AddDays(2)));

        Assert.IsTrue(result.Succeeded);
    }

    [TestMethod]
    public async Task ChangeDeadline_NullClearsAndSameDateSkipsWrite()
    {
        var f = new Fixture(OwnerId);
        f.Add(Assignment(1, ContentId, CreatorId, deadline: Today.AddDays(3)));

        await f.Service.ChangeDeadlineAsync(ContentId, CreatorId, new ChangeDeadlineRequest(Today.AddDays(3))); // trùng → không ghi
        Assert.AreEqual(0, f.Repo.DeadlineWrites.Count);

        await f.Service.ChangeDeadlineAsync(ContentId, CreatorId, new ChangeDeadlineRequest(null));
        Assert.AreEqual((1L, (DateOnly?)null), f.Repo.DeadlineWrites.Single());
    }

    [TestMethod]
    public async Task ChangeDeadline_UnknownAssignment_Returns404_PublishedReturns409()
    {
        var f = new Fixture(OwnerId);
        Assert.AreEqual(404, (await f.Service.ChangeDeadlineAsync(ContentId, CreatorId, new ChangeDeadlineRequest(Today.AddDays(1)))).Status);

        f.Add(Assignment(1, ContentId, CreatorId, stage: "Published", status: AssignmentStatus.Completed));
        Assert.AreEqual(409, (await f.Service.ChangeDeadlineAsync(ContentId, CreatorId, new ChangeDeadlineRequest(Today.AddDays(1)))).Status);
    }

    // ================================================================== giao việc

    [TestMethod]
    public async Task AssignContent_AssigneeOutsideProject_Returns400AndWritesNothing()
    {
        var f = new Fixture(OwnerId);

        var result = await f.Service.AssignContentAsync(ContentId,
            new AssignContentRequest(new[] { Req(CreatorId), Req(OutsiderCreatorId) }));

        Assert.AreEqual(400, result.Status);
        Assert.AreEqual(0, f.Repo.ApplyCalls);
    }

    [TestMethod]
    public async Task AssignContent_AssigneeIsManagerOrUnknown_Returns400()
    {
        var f = new Fixture(OwnerId);

        Assert.AreEqual(400, (await f.Service.AssignContentAsync(ContentId, new AssignContentRequest(new[] { Req(ManagerId) }))).Status);
        Assert.AreEqual(400, (await f.Service.AssignContentAsync(ContentId, new AssignContentRequest(new[] { Req(99) }))).Status);
        Assert.AreEqual(0, f.Repo.ApplyCalls);
    }

    [TestMethod]
    public async Task AssignContent_EmptyOrDuplicateList_Returns400()
    {
        var f = new Fixture(OwnerId);

        Assert.AreEqual(400, (await f.Service.AssignContentAsync(ContentId, new AssignContentRequest(Array.Empty<AssigneeDeadlineRequest>()))).Status);
        Assert.AreEqual(400, (await f.Service.AssignContentAsync(ContentId, new AssignContentRequest(new[] { Req(CreatorId), Req(CreatorId) }))).Status);
    }

    [TestMethod]
    public async Task AssignContent_CreatorAssigningOthers_Returns403()
    {
        var f = new Fixture(CreatorId) { ContentCreatedBy = OwnerId };

        var result = await f.Service.AssignContentAsync(ContentId, new AssignContentRequest(new[] { Req(OtherCreatorId) }));

        Assert.AreEqual(403, result.Status);
        Assert.AreEqual(0, f.Repo.ApplyCalls);
    }

    [TestMethod]
    public async Task AssignContent_CreatorSelfAssignsOwnContent_Succeeds()
    {
        var f = new Fixture(CreatorId) { ContentCreatedBy = CreatorId };

        var result = await f.Service.AssignContentAsync(ContentId, new AssignContentRequest(new[] { Req(CreatorId) }));

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(1, f.Repo.ApplyCalls);
    }

    [TestMethod]
    public async Task AssignContent_CreatorSelfAssignsSomeoneElsesContent_Returns403()
    {
        var f = new Fixture(CreatorId) { ContentCreatedBy = OwnerId };

        var result = await f.Service.AssignContentAsync(ContentId, new AssignContentRequest(new[] { Req(CreatorId) }));

        Assert.AreEqual(403, result.Status);
    }

    [TestMethod]
    public async Task AssignContent_CreatorSelfAssignWhenOthersAlreadyAssigned_Returns403()
    {
        var f = new Fixture(CreatorId) { ContentCreatedBy = CreatorId };
        f.Add(Assignment(12, ContentId, OtherCreatorId));

        var result = await f.Service.AssignContentAsync(ContentId, new AssignContentRequest(new[] { Req(CreatorId) }));

        Assert.AreEqual(403, result.Status);
    }

    [TestMethod]
    public async Task AssignContent_ActorOutsideProject_Returns403_ContentMissingReturns404()
    {
        Assert.AreEqual(403, (await new Fixture(99).Service.AssignContentAsync(ContentId, new AssignContentRequest(new[] { Req(CreatorId) }))).Status);
        Assert.AreEqual(404, (await new Fixture(OwnerId).Service.AssignContentAsync(999, new AssignContentRequest(new[] { Req(CreatorId) }))).Status);
    }

    [TestMethod]
    public async Task AssignContent_ContentPublished_Returns409()
    {
        var f = new Fixture(OwnerId) { ContentStage = "Published" };

        var result = await f.Service.AssignContentAsync(ContentId, new AssignContentRequest(new[] { Req(CreatorId) }));

        Assert.AreEqual(409, result.Status);
    }

    [TestMethod]
    [DataRow(OwnerId)]
    [DataRow(ManagerId)]
    public async Task AssignContent_OwnerOrManager_AssignsSeveralCreatorsWithOwnDeadlines(long actor)
    {
        var f = new Fixture(actor);

        var result = await f.Service.AssignContentAsync(ContentId, new AssignContentRequest(
            new[] { Req(CreatorId, Today.AddDays(3)), Req(OtherCreatorId, Today.AddDays(9)) }));

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(1, f.Repo.ApplyCalls);                        // một transaction duy nhất
        var added = f.Repo.LastAdditions;
        Assert.AreEqual(2, added.Count);
        Assert.AreEqual((CreatorId, (DateOnly?)Today.AddDays(3)), (added[0].UserId, added[0].Deadline));
        Assert.AreEqual((OtherCreatorId, (DateOnly?)Today.AddDays(9)), (added[1].UserId, added[1].Deadline));
        Assert.AreEqual(2, result.Value!.Assignments.Count);          // trả về danh sách sau khi giao
    }

    [TestMethod]
    public async Task AssignContent_PastDeadlineForOnePerson_Returns400AndWritesNothing()
    {
        var f = new Fixture(OwnerId);

        var result = await f.Service.AssignContentAsync(ContentId, new AssignContentRequest(
            new[] { Req(CreatorId, Today.AddDays(2)), Req(OtherCreatorId, Today.AddDays(-1)) }));

        Assert.AreEqual(400, result.Status);
        Assert.AreEqual(0, f.Repo.ApplyCalls);
    }

    [TestMethod]
    public async Task AssignContent_AddSecondCreator_KeepsFirstAndUpdatesOnlyChangedDeadline()
    {
        var f = new Fixture(OwnerId);
        f.Add(Assignment(11, ContentId, CreatorId, progress: 40, deadline: Today.AddDays(3)));

        await f.Service.AssignContentAsync(ContentId, new AssignContentRequest(
            new[] { Req(CreatorId, Today.AddDays(8)), Req(OtherCreatorId, Today.AddDays(5)) }));

        Assert.AreEqual(0, f.Repo.LastCancelled.Count);
        Assert.AreEqual((11L, (DateOnly?)Today.AddDays(8)), (f.Repo.LastDeadlineChanges.Single().AssignmentId, f.Repo.LastDeadlineChanges.Single().Deadline));
        Assert.AreEqual(OtherCreatorId, f.Repo.LastAdditions.Single().UserId);
        Assert.AreEqual(40, f.Repo.Find(11).ProgressPercent);          // tiến độ người cũ giữ nguyên
    }

    [TestMethod]
    public async Task AssignContent_RemovingUnfinishedCreator_CancelsTheirAssignment()
    {
        var f = new Fixture(OwnerId);
        f.Add(Assignment(11, ContentId, CreatorId, progress: 40));
        f.Add(Assignment(12, ContentId, OtherCreatorId, status: AssignmentStatus.Assigned));

        await f.Service.AssignContentAsync(ContentId, new AssignContentRequest(new[] { Req(CreatorId) }));

        CollectionAssert.AreEqual(new long[] { 12 }, f.Repo.LastCancelled.ToArray());
    }

    [TestMethod]
    public async Task AssignContent_RemovingCompletedCreator_Returns400()
    {
        var f = new Fixture(OwnerId);
        f.Add(Assignment(11, ContentId, CreatorId, status: AssignmentStatus.Completed, progress: 100));
        f.Add(Assignment(12, ContentId, OtherCreatorId, progress: 20));

        var result = await f.Service.AssignContentAsync(ContentId, new AssignContentRequest(new[] { Req(OtherCreatorId) }));

        Assert.AreEqual(400, result.Status);
        Assert.AreEqual(0, f.Repo.ApplyCalls);
    }

    [TestMethod]
    public async Task AssignContent_KeepExistingWithNullDeadline_ChangesNothingForThem()
    {
        var f = new Fixture(OwnerId);
        f.Add(Assignment(11, ContentId, CreatorId, deadline: Today.AddDays(3)));

        await f.Service.AssignContentAsync(ContentId, new AssignContentRequest(new[] { Req(CreatorId) }));

        Assert.AreEqual(0, f.Repo.LastAdditions.Count);
        Assert.AreEqual(0, f.Repo.LastDeadlineChanges.Count);
        Assert.AreEqual(0, f.Repo.LastCancelled.Count);
    }

    // ================================================================== Board

    [TestMethod]
    public async Task GetBoardAssignees_MemberGetsEveryCreatorOfEveryContent_NonMemberGets403()
    {
        var f = new Fixture(ManagerId);
        f.Add(Assignment(11, ContentId, CreatorId));
        f.Add(Assignment(12, ContentId, OtherCreatorId));
        f.Add(Assignment(13, 11, CreatorId));

        var ok = await f.Service.GetBoardAssigneesAsync(ProjectId);
        var denied = await new Fixture(99).Service.GetBoardAssigneesAsync(ProjectId);

        Assert.AreEqual(3, ok.Value!.Assignees.Count);
        Assert.AreEqual(403, denied.Status);
    }

    // ================================================================== quy tắc thuần

    [TestMethod]
    public void TaskRules_Basics()
    {
        Assert.IsTrue(TaskRules.CanAssign(ProjectRole.Owner));
        Assert.IsTrue(TaskRules.CanAssign(ProjectRole.Manager));
        Assert.IsFalse(TaskRules.CanAssign(ProjectRole.Creator));
        Assert.IsTrue(TaskRules.CanBeAssignee(ProjectRole.Creator));
        Assert.IsFalse(TaskRules.CanBeAssignee(ProjectRole.Manager));
        Assert.IsNull(TaskRules.ValidateProgress(0));
        Assert.IsNull(TaskRules.ValidateProgress(100));
        Assert.IsNotNull(TaskRules.ValidateProgress(101));
        Assert.IsNull(TaskRules.ValidateNewDeadline(Today.AddDays(-5), Today.AddDays(-5), Today)); // giữ nguyên deadline cũ đã quá hạn
        Assert.IsNotNull(TaskRules.ValidateNewDeadline(Today.AddDays(-1), null, Today));
        Assert.IsTrue(TaskRules.IsContentCompleted(new[] { AssignmentStatus.Completed, AssignmentStatus.Completed }));
        Assert.IsFalse(TaskRules.IsContentCompleted(new[] { AssignmentStatus.Completed, AssignmentStatus.InProgress }));
        Assert.IsFalse(TaskRules.IsContentCompleted(Array.Empty<AssignmentStatus>()));
    }

    // ================================================================== helpers

    private static TaskAssignment Assignment(
        long id, long contentId, long assignee, long projectId = ProjectId,
        AssignmentStatus status = AssignmentStatus.InProgress, int progress = 30,
        DateOnly? deadline = null, string stage = "Script") =>
        new(id, contentId, projectId, assignee, $"CNT-{contentId:000}", $"Content {contentId}", stage, "Medium",
            status, progress, deadline, "Manager A", Array.Empty<string>(), 0, 0);

    private sealed class Fixture
    {
        public long ContentCreatedBy { get; init; } = OwnerId;
        public string ContentStage { get; init; } = "Script";

        private readonly long? _actingUserId;
        private TaskService? _service;
        private FakeTaskRepository? _repo;

        public Fixture(long? actingUserId) { _actingUserId = actingUserId; }

        public FakeTaskRepository Repo => _repo ??= new FakeTaskRepository(ContentCreatedBy, ContentStage);

        public TaskService Service => _service ??= new TaskService(
            Repo,
            new CurrentAuthenticatedUser
            {
                User = _actingUserId is long id
                    ? new User { UserId = id, Email = $"u{id}@x.vn", DisplayName = $"User {id}", PasswordHash = "x" }
                    : null,
            },
            new FixedTimeProvider(Today));

        public void Add(TaskAssignment assignment) => Repo.Assignments.Add(assignment);
    }

    private sealed class FixedTimeProvider(DateOnly today) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(today.ToDateTime(new TimeOnly(9, 30)), TimeSpan.Zero);
    }

    /// <summary>Repository giả lập đúng ngữ nghĩa database: mỗi dòng một assignment, số liệu nhóm tính lại khi đọc.</summary>
    private sealed class FakeTaskRepository(long contentCreatedBy, string contentStage) : ITaskRepository
    {
        public List<TaskAssignment> Assignments { get; } = new();
        public List<(long AssignmentId, int Percent, AssignmentStatus Status)> ProgressWrites { get; } = new();
        public List<(long AssignmentId, DateOnly? Deadline)> DeadlineWrites { get; } = new();
        public int ApplyCalls { get; private set; }
        public List<long> LastCancelled { get; private set; } = new();
        public List<DeadlineChange> LastDeadlineChanges { get; private set; } = new();
        public List<NewAssignment> LastAdditions { get; private set; } = new();

        private static readonly Dictionary<(long Project, long User), ProjectRole> Roles = new()
        {
            [(ProjectId, OwnerId)] = ProjectRole.Owner,
            [(ProjectId, CreatorId)] = ProjectRole.Creator,
            [(ProjectId, ManagerId)] = ProjectRole.Manager,
            [(ProjectId, OtherCreatorId)] = ProjectRole.Creator,
            [(2, OutsiderCreatorId)] = ProjectRole.Creator,
        };

        public TaskAssignment Find(long assignmentId) => Assignments.Single(a => a.AssignmentId == assignmentId);

        public Task<ProjectRole?> GetRoleAsync(long projectId, long userId, CancellationToken ct = default) =>
            Task.FromResult<ProjectRole?>(Roles.TryGetValue((projectId, userId), out var role) ? role : null);

        public Task<IReadOnlyList<ProjectMember>> GetMembersAsync(long projectId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ProjectMember>>(Roles.Where(r => r.Key.Project == projectId)
                .Select(r => new ProjectMember(r.Key.User, $"User {r.Key.User}", r.Value)).ToList());

        public Task<ContentInfo?> GetContentAsync(long contentId, CancellationToken ct = default) =>
            Task.FromResult<ContentInfo?>(contentId == ContentId || contentId == 11
                ? new ContentInfo(contentId, ProjectId, contentStage, contentCreatedBy, null)
                : null);

        public Task<IReadOnlyList<TaskAssignment>> GetMyTasksAsync(long projectId, long userId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<TaskAssignment>>(Active().Where(a => a.AssigneeUserId == userId && a.ProjectId == projectId).ToList());

        public Task<TaskAssignment?> GetAssignmentAsync(long contentId, long userId, CancellationToken ct = default) =>
            Task.FromResult(Active().FirstOrDefault(a => a.ContentId == contentId && a.AssigneeUserId == userId));

        public Task<IReadOnlyList<TaskAssignment>> GetAssignmentsAsync(long contentId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<TaskAssignment>>(Active().Where(a => a.ContentId == contentId).ToList());

        public Task<IReadOnlyList<BoardAssignee>> GetBoardAssigneesAsync(long projectId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<BoardAssignee>>(Active().Where(a => a.ProjectId == projectId)
                .Select(a => new BoardAssignee(a.ContentId, a.AssigneeUserId, $"User {a.AssigneeUserId}", a.Status, a.ProgressPercent, a.Deadline)).ToList());

        public Task UpdateProgressAsync(long assignmentId, int percent, AssignmentStatus status, CancellationToken ct = default)
        {
            ProgressWrites.Add((assignmentId, percent, status));
            Replace(assignmentId, a => a with { ProgressPercent = percent, Status = status });
            return Task.CompletedTask;
        }

        public Task UpdateAssignmentDeadlineAsync(long assignmentId, DateOnly? deadline, CancellationToken ct = default)
        {
            DeadlineWrites.Add((assignmentId, deadline));
            Replace(assignmentId, a => a with { Deadline = deadline });
            return Task.CompletedTask;
        }

        public Task ApplyAssignmentsAsync(long contentId, long actorUserId, IReadOnlyList<long> cancelAssignmentIds,
            IReadOnlyList<DeadlineChange> deadlineChanges, IReadOnlyList<NewAssignment> additions, CancellationToken ct = default)
        {
            ApplyCalls++;
            LastCancelled = cancelAssignmentIds.ToList();
            LastDeadlineChanges = deadlineChanges.ToList();
            LastAdditions = additions.ToList();

            foreach (long id in cancelAssignmentIds) Replace(id, a => a.Status == AssignmentStatus.Completed ? a : a with { Status = AssignmentStatus.Cancelled });
            foreach (var change in deadlineChanges) Replace(change.AssignmentId, a => a with { Deadline = change.Deadline });
            long next = Assignments.Count == 0 ? 100 : Assignments.Max(a => a.AssignmentId) + 1;
            foreach (var add in additions)
                Assignments.Add(new TaskAssignment(next++, contentId, ProjectId, add.UserId, $"CNT-{contentId:000}", $"Content {contentId}",
                    contentStage, "Medium", AssignmentStatus.Assigned, 0, add.Deadline, "Owner", Array.Empty<string>(), 0, 0));
            return Task.CompletedTask;
        }

        private void Replace(long assignmentId, Func<TaskAssignment, TaskAssignment> change)
        {
            int index = Assignments.FindIndex(a => a.AssignmentId == assignmentId);
            Assignments[index] = change(Assignments[index]);
        }

        /// <summary>Assignment chưa huỷ, kèm TeamTotal/TeamDone tính lại theo từng Content.</summary>
        private List<TaskAssignment> Active()
        {
            var live = Assignments.Where(a => a.Status != AssignmentStatus.Cancelled).ToList();
            return live.Select(a => a with
            {
                TeamTotal = live.Count(x => x.ContentId == a.ContentId),
                TeamDone = live.Count(x => x.ContentId == a.ContentId && x.Status == AssignmentStatus.Completed),
            }).ToList();
        }
    }
}
