using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Repositories.InMemory;
using CreatorFlow.Repositories.Interfaces;
using CreatorFlow.Services;
using CreatorFlow.Services.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.Shared;

/// <summary>
/// Giao Content cho MỘT HOẶC NHIỀU Creator: đúng Project, đúng quyền, mỗi người deadline/tiến độ riêng,
/// Content hoàn thành khi tất cả hoàn thành, và task xuất hiện trong My Tasks.
/// </summary>
[TestClass]
public sealed class ContentAssignmentTests
{
    private const long ProjectId = 1;
    private const long OwnerId = 1;
    private const long CreatorId = 2;
    private const long ManagerId = 3;
    private const long OtherCreatorId = 4;     // Creator thứ hai của Project #1
    private const long OutsiderCreatorId = 9;  // Creator nhưng thuộc Project #2, KHÔNG thuộc Project #1
    private const long ContentId = 10;

    private static readonly DateTime Today = DateTime.Today;

    private static AssignmentRequest Req(long userId, DateTime? deadline = null) => new(userId, deadline);

    // ==========================================================
    // Không assign người ngoài Project
    // ==========================================================

    [TestMethod]
    public void AssignContent_AssigneeOutsideProject_ThrowsAndWritesNothing()
    {
        var f = new Fixture();

        Assert.ThrowsExactly<ContentValidationException>(
            () => f.Service.AssignContent(ContentId, new[] { Req(CreatorId), Req(OutsiderCreatorId) }, OwnerId));
        Assert.AreEqual(0, f.Tasks.Added.Count);   // kiểm tra hết trước khi ghi: không giao dở dang cho người hợp lệ
    }

    [TestMethod]
    public void AssignContent_UnknownUser_Throws()
    {
        var f = new Fixture();

        Assert.ThrowsExactly<ContentValidationException>(
            () => f.Service.AssignContent(ContentId, new[] { Req(99) }, OwnerId));
        Assert.AreEqual(0, f.Tasks.Added.Count);
    }

    [TestMethod]
    public void AssignContent_AssigneeIsNotCreatorRole_Throws()
    {
        var f = new Fixture();

        Assert.ThrowsExactly<ContentValidationException>(
            () => f.Service.AssignContent(ContentId, new[] { Req(ManagerId) }, OwnerId));
        Assert.AreEqual(0, f.Tasks.Added.Count);
    }

    [TestMethod]
    public void AssignContent_EmptyList_Throws()
    {
        var f = new Fixture();

        Assert.ThrowsExactly<ContentValidationException>(
            () => f.Service.AssignContent(ContentId, Array.Empty<AssignmentRequest>(), OwnerId));
    }

    [TestMethod]
    public void AssignContent_DuplicateCreator_Throws()
    {
        var f = new Fixture();

        Assert.ThrowsExactly<ContentValidationException>(
            () => f.Service.AssignContent(ContentId, new[] { Req(CreatorId), Req(CreatorId) }, OwnerId));
    }

    [TestMethod]
    public void Create_AssigneeOutsideProject_Throws()
    {
        var f = new Fixture();

        Assert.ThrowsExactly<ContentValidationException>(
            () => f.Service.Create(ProjectId, ContentStatus.Idea, Draft(OutsiderCreatorId), OwnerId));
        Assert.AreEqual(0, f.Details.CreateCount);
    }

    // ==========================================================
    // Phân quyền giao việc
    // ==========================================================

    [TestMethod]
    public void AssignContent_CreatorActor_ThrowsUnauthorized()
    {
        var f = new Fixture();

        Assert.ThrowsExactly<UnauthorizedWorkflowActionException>(
            () => f.Service.AssignContent(ContentId, new[] { Req(OtherCreatorId) }, CreatorId));
        Assert.AreEqual(0, f.Tasks.Added.Count);
    }

    [TestMethod]
    public void AssignContent_ActorOutsideProject_ThrowsUnauthorized()
    {
        var f = new Fixture();

        Assert.ThrowsExactly<UnauthorizedWorkflowActionException>(
            () => f.Service.AssignContent(ContentId, new[] { Req(CreatorId) }, 99));
    }

    [TestMethod]
    public void Create_CreatorAssignsToAnotherPerson_ThrowsUnauthorized()
    {
        var f = new Fixture();

        Assert.ThrowsExactly<UnauthorizedWorkflowActionException>(
            () => f.Service.Create(ProjectId, ContentStatus.Idea, Draft(OtherCreatorId), CreatorId));
        Assert.AreEqual(0, f.Details.CreateCount);
    }

    [TestMethod]
    public void Create_CreatorSelfAssigns_Succeeds()
    {
        var f = new Fixture();

        f.Service.Create(ProjectId, ContentStatus.Idea, Draft(CreatorId), CreatorId);

        Assert.AreEqual(1, f.Details.CreateCount);
    }

    [TestMethod]
    public void Create_OwnerAssignsCreator_Succeeds()
    {
        var f = new Fixture();

        f.Service.Create(ProjectId, ContentStatus.Idea, Draft(CreatorId), OwnerId);

        Assert.AreEqual(1, f.Details.CreateCount);
    }

    [TestMethod]
    public void Create_OwnerAssignsManager_Throws()
    {
        var f = new Fixture();

        Assert.ThrowsExactly<ContentValidationException>(
            () => f.Service.Create(ProjectId, ContentStatus.Idea, Draft(ManagerId), OwnerId));
    }

    [TestMethod]
    public void GetAssignableMembers_DependsOnRole()
    {
        var f = new Fixture();

        CollectionAssert.AreEquivalent(new[] { CreatorId, OtherCreatorId },
            f.Service.GetAssignableMembers(ProjectId, OwnerId).Select(m => m.UserId).ToArray());
        CollectionAssert.AreEquivalent(new[] { CreatorId, OtherCreatorId },
            f.Service.GetAssignableMembers(ProjectId, ManagerId).Select(m => m.UserId).ToArray());
        CollectionAssert.AreEqual(new[] { CreatorId },
            f.Service.GetAssignableMembers(ProjectId, CreatorId).Select(m => m.UserId).ToArray());
        Assert.AreEqual(0, f.Service.GetAssignableMembers(ProjectId, 99).Count);
    }

    // ==========================================================
    // Giao nhiều Creator, mỗi người một deadline
    // ==========================================================

    [TestMethod]
    [DataRow(OwnerId)]
    [DataRow(ManagerId)]
    public void AssignContent_OwnerOrManager_AssignsSeveralCreatorsWithOwnDeadlines(long actorId)
    {
        var f = new Fixture();

        f.Service.AssignContent(ContentId,
            new[] { Req(CreatorId, Today.AddDays(3).AddHours(14)), Req(OtherCreatorId, Today.AddDays(9)) }, actorId);

        Assert.AreEqual(2, f.Tasks.Added.Count);
        Assert.AreEqual((ContentId, CreatorId, actorId, (DateTime?)Today.AddDays(3)), f.Tasks.Added[0]);          // deadline chuẩn hoá về ngày
        Assert.AreEqual((ContentId, OtherCreatorId, actorId, (DateTime?)Today.AddDays(9)), f.Tasks.Added[1]);     // deadline riêng người thứ hai
        Assert.AreEqual(1, f.Uow.CommitCount);
    }

    [TestMethod]
    public void AssignContent_NullDeadline_AddsAssignmentWithoutPersonalDeadline()
    {
        var f = new Fixture();

        f.Service.AssignContent(ContentId, new[] { Req(CreatorId) }, OwnerId);

        Assert.AreEqual((ContentId, CreatorId, OwnerId, (DateTime?)null), f.Tasks.Added[0]);
    }

    [TestMethod]
    public void AssignContent_PastDeadlineForOnePerson_ThrowsAndWritesNothing()
    {
        var f = new Fixture();

        Assert.ThrowsExactly<ContentValidationException>(() => f.Service.AssignContent(ContentId,
            new[] { Req(CreatorId, Today.AddDays(2)), Req(OtherCreatorId, Today.AddDays(-1)) }, OwnerId));
        Assert.AreEqual(0, f.Tasks.Added.Count);
    }

    [TestMethod]
    public void AssignContent_ContentPublished_Throws()
    {
        var f = new Fixture { ContentStatus = ContentStatus.Published };

        Assert.ThrowsExactly<ContentValidationException>(
            () => f.Service.AssignContent(ContentId, new[] { Req(CreatorId) }, OwnerId));
    }

    [TestMethod]
    public void AssignContent_AddingSecondCreator_KeepsFirstAndUpdatesOnlyChangedDeadline()
    {
        var f = new Fixture
        {
            Existing = { ExistingAssignment(CreatorId, 11, AssignmentStatus.InProgress, 40, Today.AddDays(3)) },
        };

        f.Service.AssignContent(ContentId,
            new[] { Req(CreatorId, Today.AddDays(8)), Req(OtherCreatorId, Today.AddDays(5)) }, OwnerId);

        Assert.AreEqual(0, f.Tasks.Cancelled.Count);
        Assert.AreEqual(1, f.Tasks.DeadlineWrites.Count);                                    // Creator #2 đổi deadline, giữ tiến độ
        Assert.AreEqual((11L, (DateTime?)Today.AddDays(8)), f.Tasks.DeadlineWrites[0]);
        Assert.AreEqual(1, f.Tasks.Added.Count);                                             // chỉ Creator #4 là người mới
        Assert.AreEqual(OtherCreatorId, f.Tasks.Added[0].Assignee);
    }

    [TestMethod]
    public void AssignContent_KeepExistingWithNullDeadline_WritesNothingForThem()
    {
        var f = new Fixture
        {
            Existing = { ExistingAssignment(CreatorId, 11, AssignmentStatus.InProgress, 40, Today.AddDays(3)) },
        };

        f.Service.AssignContent(ContentId, new[] { Req(CreatorId) }, OwnerId);

        Assert.AreEqual(0, f.Tasks.Added.Count);
        Assert.AreEqual(0, f.Tasks.DeadlineWrites.Count);
        Assert.AreEqual(0, f.Tasks.Cancelled.Count);
    }

    [TestMethod]
    public void AssignContent_RemovingUnfinishedCreator_CancelsTheirAssignment()
    {
        var f = new Fixture
        {
            Existing =
            {
                ExistingAssignment(CreatorId, 11, AssignmentStatus.InProgress, 40, null),
                ExistingAssignment(OtherCreatorId, 12, AssignmentStatus.Assigned, 0, null),
            },
        };

        f.Service.AssignContent(ContentId, new[] { Req(CreatorId) }, OwnerId);

        CollectionAssert.AreEqual(new long[] { 12 }, f.Tasks.Cancelled);
    }

    [TestMethod]
    public void AssignContent_RemovingCompletedCreator_Throws()
    {
        var f = new Fixture
        {
            Existing =
            {
                ExistingAssignment(CreatorId, 11, AssignmentStatus.Completed, 100, null),
                ExistingAssignment(OtherCreatorId, 12, AssignmentStatus.InProgress, 20, null),
            },
        };

        Assert.ThrowsExactly<ContentValidationException>(
            () => f.Service.AssignContent(ContentId, new[] { Req(OtherCreatorId) }, OwnerId));
        Assert.AreEqual(0, f.Tasks.Cancelled.Count);
    }

    [TestMethod]
    public void AssignContent_RepositoryFails_RollsBack()
    {
        var f = new Fixture { ThrowOnWrite = true };

        Assert.ThrowsExactly<InvalidOperationException>(
            () => f.Service.AssignContent(ContentId, new[] { Req(CreatorId) }, OwnerId));
        Assert.AreEqual(1, f.Uow.RollbackCount);
        Assert.AreEqual(0, f.Uow.CommitCount);
    }

    // ==========================================================
    // Task xuất hiện trong My Tasks + Content hoàn thành khi tất cả hoàn thành
    // (dùng các repository in-memory thật)
    // ==========================================================

    [TestMethod]
    public void AssignContent_InMemory_EachCreatorSeesOwnTaskAndContentCompletesWhenAllDone()
    {
        const long newId = 9001;
        InMemoryDataStore.Contents.Add(new InMemoryContentRecord
        {
            Id = newId,
            ProjectId = ProjectId,
            Title = "Content nhiều Creator",
            Status = ContentStatus.Script,
            CreatedByUserId = OwnerId,
        });

        try
        {
            var taskRepo = new InMemoryMyTaskRepository();
            var members = new InMemoryProjectMemberRepository();
            var uow = new InMemoryUnitOfWork();
            var contentService = new ContentService(
                new InMemoryContentRepository(),
                new InMemoryContentDetailsRepository(),
                new InMemoryContentStatusHistoryRepository(),
                members, new InMemoryPlatformRepository(), uow, taskRepo);
            var taskService = new MyTaskService(taskRepo, members, uow);

            contentService.AssignContent(newId,
                new[] { Req(CreatorId, Today.AddDays(4)), Req(OtherCreatorId, Today.AddDays(10)) }, OwnerId);

            // Mỗi Creator thấy task của MÌNH với deadline RIÊNG; nhóm 0/2.
            var first = taskService.GetMyTasks(ProjectId, CreatorId).Single(t => t.ContentId == newId);
            var second = taskService.GetMyTasks(ProjectId, OtherCreatorId).Single(t => t.ContentId == newId);
            Assert.AreEqual(Today.AddDays(4), first.Deadline);
            Assert.AreEqual(Today.AddDays(10), second.Deadline);
            Assert.AreEqual(AssignmentStatus.Assigned, first.Status);
            Assert.AreEqual(2, first.TeamTotal);
            Assert.AreEqual(0, first.TeamDone);

            // Board thấy cả hai người.
            var card = new InMemoryBoardRepository().GetBoardCards(ProjectId).Single(c => c.ContentId == newId);
            CollectionAssert.AreEquivalent(new[] { CreatorId, OtherCreatorId }, card.Assignees.Select(a => a.UserId).ToArray());
            Assert.IsFalse(card.IsTeamCompleted);

            // Một người xong → chưa hoàn thành; cả hai xong → hoàn thành.
            taskService.UpdateProgress(newId, 100, CreatorId);
            Assert.IsFalse(new InMemoryBoardRepository().GetBoardCards(ProjectId).Single(c => c.ContentId == newId).IsTeamCompleted);
            Assert.AreEqual(1, taskService.GetMyTasks(ProjectId, OtherCreatorId).Single(t => t.ContentId == newId).TeamDone);

            taskService.UpdateProgress(newId, 100, OtherCreatorId);
            Assert.IsTrue(new InMemoryBoardRepository().GetBoardCards(ProjectId).Single(c => c.ContentId == newId).IsTeamCompleted);
        }
        finally
        {
            InMemoryDataStore.Contents.RemoveAll(c => c.Id == newId);
            InMemoryDataStore.Assignments.RemoveAll(a => a.ContentId == newId);
        }
    }

    [TestMethod]
    public void AssignContent_InMemory_RemovedCreatorLosesTheTask()
    {
        const long newId = 9002;
        InMemoryDataStore.Contents.Add(new InMemoryContentRecord
        {
            Id = newId,
            ProjectId = ProjectId,
            Title = "Content đổi người",
            Status = ContentStatus.Production,
            CreatedByUserId = OwnerId,
        });

        try
        {
            var taskRepo = new InMemoryMyTaskRepository();
            var members = new InMemoryProjectMemberRepository();
            var contentService = new ContentService(
                new InMemoryContentRepository(),
                new InMemoryContentDetailsRepository(),
                new InMemoryContentStatusHistoryRepository(),
                members, new InMemoryPlatformRepository(), new InMemoryUnitOfWork(), taskRepo);

            contentService.AssignContent(newId, new[] { Req(CreatorId), Req(OtherCreatorId) }, OwnerId);
            contentService.AssignContent(newId, new[] { Req(OtherCreatorId) }, OwnerId); // bỏ Creator #2

            Assert.IsFalse(taskRepo.GetMyTasks(ProjectId, CreatorId).Any(t => t.ContentId == newId));
            Assert.IsTrue(taskRepo.GetMyTasks(ProjectId, OtherCreatorId).Any(t => t.ContentId == newId));
        }
        finally
        {
            InMemoryDataStore.Contents.RemoveAll(c => c.Id == newId);
            InMemoryDataStore.Assignments.RemoveAll(a => a.ContentId == newId);
        }
    }

    // ==========================================================
    // Helpers
    // ==========================================================

    private static ContentDraft Draft(long? assigneeUserId) => new()
    {
        Title = "Tiêu đề",
        Description = "Mô tả",
        AssigneeUserId = assigneeUserId,
    };

    private static MyTaskItem ExistingAssignment(long assignee, long assignmentId, AssignmentStatus status, int progress, DateTime? deadline) => new()
    {
        AssignmentId = assignmentId,
        ContentId = ContentId,
        ProjectId = ProjectId,
        AssigneeUserId = assignee,
        Status = status,
        ProgressPercent = progress,
        Deadline = deadline,
    };

    private sealed class Fixture
    {
        public ContentStatus ContentStatus { get; init; } = ContentStatus.Script;
        public List<MyTaskItem> Existing { get; } = new();
        public bool ThrowOnWrite { get; init; }

        private readonly Lazy<ContentService> _service;
        private readonly Lazy<FakeTaskRepository> _tasks;
        private readonly Lazy<FakeDetailsRepository> _details = new(() => new FakeDetailsRepository());

        public FakeUnitOfWork Uow { get; } = new();
        public ContentService Service => _service.Value;
        public FakeTaskRepository Tasks => _tasks.Value;
        public FakeDetailsRepository Details => _details.Value;

        public Fixture()
        {
            _tasks = new Lazy<FakeTaskRepository>(() => new FakeTaskRepository(Existing, ThrowOnWrite));
            _service = new Lazy<ContentService>(() => new ContentService(
                new FakeContentRepository(ContentStatus),
                Details,
                new FakeHistoryRepository(),
                new FakeMemberRepository(),
                new InMemoryPlatformRepository(),
                Uow,
                Tasks));
        }
    }

    private sealed class FakeContentRepository(ContentStatus status) : IContentRepository
    {
        public Content GetById(long contentId) => new()
        {
            Id = contentId,
            ProjectId = ProjectId,
            Title = "Content",
            Status = status,
            CreatedByUserId = OwnerId,
        };

        public void UpdateStatus(long contentId, ContentStatus newStatus) { }
    }

    private sealed class FakeTaskRepository(List<MyTaskItem> existing, bool throwOnWrite) : IMyTaskRepository
    {
        public List<(long ContentId, long Assignee, long By, DateTime? Deadline)> Added { get; } = new();
        public List<long> Cancelled { get; } = new();
        public List<(long AssignmentId, DateTime? Deadline)> DeadlineWrites { get; } = new();

        public List<MyTaskItem> GetMyTasks(long projectId, long assigneeUserId) => new();

        public MyTaskItem? GetAssignment(long contentId, long assigneeUserId) =>
            existing.FirstOrDefault(e => e.AssigneeUserId == assigneeUserId);

        public List<MyTaskItem> GetAssignments(long contentId) => existing.ToList();

        public void UpdateProgress(long assignmentId, int percent, AssignmentStatus status) { }

        public void UpdateAssignmentDeadline(long assignmentId, DateTime? deadline)
        {
            if (throwOnWrite) throw new InvalidOperationException("DB down");
            DeadlineWrites.Add((assignmentId, deadline));
        }

        public void AddAssignment(long contentId, long assigneeUserId, long assignedByUserId, DateTime? deadline)
        {
            if (throwOnWrite) throw new InvalidOperationException("DB down");
            Added.Add((contentId, assigneeUserId, assignedByUserId, deadline));
        }

        public void CancelAssignment(long assignmentId)
        {
            if (throwOnWrite) throw new InvalidOperationException("DB down");
            Cancelled.Add(assignmentId);
        }
    }

    private sealed class FakeDetailsRepository : IContentDetailsRepository
    {
        public int CreateCount { get; private set; }

        public long Create(long projectId, ContentStatus status, ContentDraft draft, long createdByUserId)
        {
            CreateCount++;
            return 100;
        }

        public void Update(long contentId, ContentDraft draft) { }

        public Content? GetDetail(long contentId) => null;
    }

    private sealed class FakeHistoryRepository : IContentStatusHistoryRepository
    {
        public void Add(ContentStatusHistory history) { }
    }

    /// <summary>Project #1: Owner(1), Creator(2), Manager(3), Creator(4). User 9 là Creator của Project #2.</summary>
    private sealed class FakeMemberRepository : IProjectMemberRepository
    {
        private static readonly (long ProjectId, long UserId, ProjectRole Role)[] Members =
        {
            (ProjectId, OwnerId, ProjectRole.Owner),
            (ProjectId, CreatorId, ProjectRole.Creator),
            (ProjectId, ManagerId, ProjectRole.Manager),
            (ProjectId, OtherCreatorId, ProjectRole.Creator),
            (2, OutsiderCreatorId, ProjectRole.Creator),
        };

        public ProjectRole? GetRole(long projectId, long userId)
        {
            foreach (var m in Members)
                if (m.ProjectId == projectId && m.UserId == userId)
                    return m.Role;
            return null;
        }

        public List<ProjectMemberInfo> GetMembers(long projectId) =>
            Members.Where(m => m.ProjectId == projectId)
                   .Select(m => new ProjectMemberInfo { UserId = m.UserId, Name = $"User {m.UserId}", Role = m.Role })
                   .ToList();
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int CommitCount { get; private set; }
        public int RollbackCount { get; private set; }

        public void Begin() { }
        public void Commit() => CommitCount++;
        public void Rollback() => RollbackCount++;
        public void Dispose() { }
    }
}