using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Repositories.Interfaces;
using CreatorFlow.Services;
using CreatorFlow.Services.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.Shared;

[TestClass]
public sealed class MyTaskServiceTests
{
    private static readonly DateTime Today = new(2026, 10, 4);

    private const long ProjectId = 1;
    private const long CreatorId = 2;
    private const long ManagerId = 3;
    private const long OwnerId = 1;
    private const long OtherCreatorId = 4;

    // ==========================================================
    // Đọc: lọc theo User / Project
    // ==========================================================

    [TestMethod]
    public void GetMyTasks_UserNotMemberOfProject_ReturnsEmptyAndSkipsRepository()
    {
        var taskRepo = new FakeTaskRepository(MakeTask(1, AssignmentStatus.Assigned));
        var service = Build(taskRepo);

        List<MyTaskItem> result = service.GetMyTasks(projectId: ProjectId, userId: 99);

        Assert.AreEqual(0, result.Count);
        Assert.AreEqual(0, taskRepo.CallCount);
    }

    [TestMethod]
    public void GetMyTasks_Member_QueriesRepositoryWithSameProjectAndUser()
    {
        var taskRepo = new FakeTaskRepository(MakeTask(1, AssignmentStatus.InProgress));
        var service = Build(taskRepo);

        List<MyTaskItem> result = service.GetMyTasks(ProjectId, CreatorId);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(ProjectId, taskRepo.LastProjectId);
        Assert.AreEqual(CreatorId, taskRepo.LastUserId);
    }

    [TestMethod]
    public void CountOpenTasks_CountsOnlyAssignedAndInProgress()
    {
        var taskRepo = new FakeTaskRepository(
            MakeTask(1, AssignmentStatus.Assigned),
            MakeTask(2, AssignmentStatus.InProgress),
            MakeTask(3, AssignmentStatus.Completed));

        Assert.AreEqual(2, Build(taskRepo).CountOpenTasks(ProjectId, CreatorId));
    }

    // ==========================================================
    // Filter trạng thái / deadline
    // ==========================================================

    [TestMethod]
    public void ApplyFilter_ByStatus_ReturnsOnlyThatStatus()
    {
        var tasks = new[]
        {
            MakeTask(1, AssignmentStatus.Assigned),
            MakeTask(2, AssignmentStatus.InProgress),
            MakeTask(3, AssignmentStatus.Completed),
        };

        var result = MyTaskService.ApplyFilter(tasks, new MyTaskFilter { Status = AssignmentStatus.InProgress }, Today);

        CollectionAssert.AreEqual(new long[] { 2 }, Ids(result));
    }

    [TestMethod]
    public void ApplyFilter_NoFilter_ReturnsAllTasks()
    {
        var tasks = new[] { MakeTask(1, AssignmentStatus.Assigned), MakeTask(2, AssignmentStatus.Completed) };

        Assert.AreEqual(2, MyTaskService.ApplyFilter(tasks, new MyTaskFilter(), Today).Count);
    }

    [TestMethod]
    public void ApplyFilter_Overdue_ExcludesCompletedAndFutureTasks()
    {
        var tasks = new[]
        {
            MakeTask(1, AssignmentStatus.InProgress, deadline: Today.AddDays(-1)),   // quá hạn
            MakeTask(2, AssignmentStatus.Completed, deadline: Today.AddDays(-3)),    // đã xong → không tính quá hạn
            MakeTask(3, AssignmentStatus.Assigned, deadline: Today),                 // hôm nay → chưa quá hạn
            MakeTask(4, AssignmentStatus.Assigned, deadline: Today.AddDays(2)),
            MakeTask(5, AssignmentStatus.Assigned, deadline: null),
        };

        var result = MyTaskService.ApplyFilter(tasks, new MyTaskFilter { Deadline = MyTaskDeadlineFilter.Overdue }, Today);

        CollectionAssert.AreEqual(new long[] { 1 }, Ids(result));
    }

    [TestMethod]
    public void ApplyFilter_Today_ReturnsOnlyTasksDueToday()
    {
        var tasks = new[]
        {
            MakeTask(1, AssignmentStatus.Assigned, deadline: Today.AddHours(15)),
            MakeTask(2, AssignmentStatus.Assigned, deadline: Today.AddDays(1)),
            MakeTask(3, AssignmentStatus.Assigned, deadline: Today.AddDays(-1)),
        };

        var result = MyTaskService.ApplyFilter(tasks, new MyTaskFilter { Deadline = MyTaskDeadlineFilter.Today }, Today);

        CollectionAssert.AreEqual(new long[] { 1 }, Ids(result));
    }

    [TestMethod]
    public void ApplyFilter_Next7Days_IncludesTodayThroughDay6()
    {
        var tasks = new[]
        {
            MakeTask(1, AssignmentStatus.Assigned, deadline: Today),
            MakeTask(2, AssignmentStatus.Assigned, deadline: Today.AddDays(6)),
            MakeTask(3, AssignmentStatus.Assigned, deadline: Today.AddDays(7)),
            MakeTask(4, AssignmentStatus.Assigned, deadline: Today.AddDays(-1)),
            MakeTask(5, AssignmentStatus.Assigned, deadline: null),
        };

        var result = MyTaskService.ApplyFilter(tasks, new MyTaskFilter { Deadline = MyTaskDeadlineFilter.Next7Days }, Today);

        CollectionAssert.AreEqual(new long[] { 1, 2 }, Ids(result));
    }

    [TestMethod]
    public void ApplyFilter_NoDeadline_ReturnsOnlyTasksWithoutDeadline()
    {
        var tasks = new[]
        {
            MakeTask(1, AssignmentStatus.Assigned, deadline: null),
            MakeTask(2, AssignmentStatus.Assigned, deadline: Today.AddDays(1)),
        };

        var result = MyTaskService.ApplyFilter(tasks, new MyTaskFilter { Deadline = MyTaskDeadlineFilter.NoDeadline }, Today);

        CollectionAssert.AreEqual(new long[] { 1 }, Ids(result));
    }

    [TestMethod]
    public void ApplyFilter_StatusAndDeadlineCombined_AppliesBoth()
    {
        var tasks = new[]
        {
            MakeTask(1, AssignmentStatus.InProgress, deadline: Today.AddDays(-1)),
            MakeTask(2, AssignmentStatus.Assigned, deadline: Today.AddDays(-1)),
        };

        var filter = new MyTaskFilter { Status = AssignmentStatus.Assigned, Deadline = MyTaskDeadlineFilter.Overdue };

        CollectionAssert.AreEqual(new long[] { 2 }, Ids(MyTaskService.ApplyFilter(tasks, filter, Today)));
    }

    [TestMethod]
    public void ApplyFilter_Sorts_OpenFirst_ThenEarliestDeadline_ThenHigherPriority()
    {
        var tasks = new[]
        {
            MakeTask(1, AssignmentStatus.Completed, deadline: Today.AddDays(-9)),
            MakeTask(2, AssignmentStatus.Assigned, deadline: null),
            MakeTask(3, AssignmentStatus.Assigned, deadline: Today.AddDays(3), priority: Priority.Low),
            MakeTask(4, AssignmentStatus.InProgress, deadline: Today.AddDays(3), priority: Priority.Urgent),
            MakeTask(5, AssignmentStatus.Assigned, deadline: Today.AddDays(1)),
        };

        var result = MyTaskService.ApplyFilter(tasks, new MyTaskFilter(), Today);

        // 5 (deadline sớm nhất) → 4 (cùng deadline với 3 nhưng ưu tiên cao hơn) → 3 → 2 (không deadline) → 1 (đã xong)
        CollectionAssert.AreEqual(new long[] { 5, 4, 3, 2, 1 }, Ids(result));
    }

    // ==========================================================
    // Phát hiện overdue
    // ==========================================================

    [TestMethod]
    public void IsOverdueOn_CompletedOrCancelledOrToday_IsFalse()
    {
        Assert.IsFalse(MakeTask(1, AssignmentStatus.Completed, deadline: Today.AddDays(-3)).IsOverdueOn(Today));
        Assert.IsFalse(MakeTask(2, AssignmentStatus.Cancelled, deadline: Today.AddDays(-3)).IsOverdueOn(Today));
        Assert.IsFalse(MakeTask(3, AssignmentStatus.InProgress, deadline: Today).IsOverdueOn(Today));
        Assert.IsFalse(MakeTask(4, AssignmentStatus.InProgress, deadline: null).IsOverdueOn(Today));
        Assert.IsTrue(MakeTask(5, AssignmentStatus.Assigned, deadline: Today.AddDays(-1)).IsOverdueOn(Today));
    }

    [TestMethod]
    public void CountOverdueTasks_CountsOnlyUnfinishedPastDeadline()
    {
        var taskRepo = new FakeTaskRepository(
            MakeTask(1, AssignmentStatus.InProgress, deadline: Today.AddDays(-2)),
            MakeTask(2, AssignmentStatus.Completed, deadline: Today.AddDays(-2)),
            MakeTask(3, AssignmentStatus.Assigned, deadline: Today.AddDays(1)));

        Assert.AreEqual(1, Build(taskRepo).CountOverdueTasks(ProjectId, CreatorId));
    }

    // ==========================================================
    // Cập nhật progress
    // ==========================================================

    [TestMethod]
    [DataRow(0, AssignmentStatus.Assigned)]
    [DataRow(40, AssignmentStatus.InProgress)]
    [DataRow(100, AssignmentStatus.Completed)]
    public void UpdateProgress_Assignee_WritesPercentAndDerivedStatus(int percent, AssignmentStatus expected)
    {
        var taskRepo = new FakeTaskRepository(MakeTask(1, AssignmentStatus.InProgress, progress: 20));
        var uow = new FakeUnitOfWork();
        var service = Build(taskRepo, uow);

        MyTaskItem result = service.UpdateProgress(1, percent, CreatorId);

        Assert.AreEqual(percent, result.ProgressPercent);
        Assert.AreEqual(expected, result.Status);
        Assert.AreEqual(1, taskRepo.ProgressWrites.Count);
        Assert.AreEqual((1L, percent, expected), taskRepo.ProgressWrites[0]);
        Assert.AreEqual(1, uow.CommitCount);
    }

    [TestMethod]
    [DataRow(-1)]
    [DataRow(101)]
    public void UpdateProgress_OutOfRange_ThrowsAndWritesNothing(int percent)
    {
        var taskRepo = new FakeTaskRepository(MakeTask(1, AssignmentStatus.InProgress));
        var service = Build(taskRepo);

        Assert.ThrowsExactly<ContentValidationException>(() => service.UpdateProgress(1, percent, CreatorId));
        Assert.AreEqual(0, taskRepo.ProgressWrites.Count);
    }

    [TestMethod]
    public void UpdateProgress_UserIsNotAssignee_Throws()
    {
        var taskRepo = new FakeTaskRepository(MakeTask(1, AssignmentStatus.InProgress)); // giao cho Creator #2
        var service = Build(taskRepo);

        Assert.ThrowsExactly<UnauthorizedWorkflowActionException>(() => service.UpdateProgress(1, 50, ManagerId));
        Assert.AreEqual(0, taskRepo.ProgressWrites.Count);
    }

    [TestMethod]
    public void UpdateProgress_UserNotInProject_Throws()
    {
        var taskRepo = new FakeTaskRepository(MakeTask(1, AssignmentStatus.InProgress));
        var service = Build(taskRepo);

        Assert.ThrowsExactly<UnauthorizedWorkflowActionException>(() => service.UpdateProgress(1, 50, 99));
    }

    [TestMethod]
    public void UpdateProgress_ContentPublished_Throws()
    {
        var task = MakeTask(1, AssignmentStatus.Completed, progress: 100);
        task.ContentStage = ContentStatus.Published;
        var taskRepo = new FakeTaskRepository(task);

        Assert.ThrowsExactly<ContentValidationException>(() => Build(taskRepo).UpdateProgress(1, 50, CreatorId));
        Assert.AreEqual(0, taskRepo.ProgressWrites.Count);
    }

    [TestMethod]
    public void UpdateProgress_NothingChanged_SkipsWrite()
    {
        var taskRepo = new FakeTaskRepository(MakeTask(1, AssignmentStatus.InProgress, progress: 40));

        Build(taskRepo).UpdateProgress(1, 40, CreatorId);

        Assert.AreEqual(0, taskRepo.ProgressWrites.Count);
    }

    [TestMethod]
    public void UpdateProgress_RepositoryFails_RollsBack()
    {
        var taskRepo = new FakeTaskRepository(MakeTask(1, AssignmentStatus.InProgress)) { ThrowOnWrite = true };
        var uow = new FakeUnitOfWork();

        Assert.ThrowsExactly<InvalidOperationException>(() => Build(taskRepo, uow).UpdateProgress(1, 60, CreatorId));
        Assert.AreEqual(1, uow.RollbackCount);
        Assert.AreEqual(0, uow.CommitCount);
    }

    [TestMethod]
    public void UpdateProgress_TwoCreatorsOnSameContent_EachUpdatesOnlyOwnAssignment()
    {
        var taskRepo = new FakeTaskRepository(
            MakeTask(1, AssignmentStatus.InProgress, progress: 30, assignee: CreatorId, assignmentId: 11),
            MakeTask(1, AssignmentStatus.Assigned, progress: 0, assignee: OtherCreatorId, assignmentId: 12));
        var service = Build(taskRepo);

        service.UpdateProgress(1, 80, OtherCreatorId);

        Assert.AreEqual(1, taskRepo.ProgressWrites.Count);
        Assert.AreEqual((12L, 80, AssignmentStatus.InProgress), taskRepo.ProgressWrites[0]); // chỉ assignment của Creator #4
    }

    [TestMethod]
    public void ChangeDeadline_TwoCreators_OnlyTheChosenOneChanges()
    {
        var taskRepo = new FakeTaskRepository(
            MakeTask(1, AssignmentStatus.InProgress, deadline: Today.AddDays(3), assignee: CreatorId, assignmentId: 11),
            MakeTask(1, AssignmentStatus.InProgress, deadline: Today.AddDays(3), assignee: OtherCreatorId, assignmentId: 12));

        Build(taskRepo).ChangeDeadline(1, OtherCreatorId, Today.AddDays(9), ManagerId);

        Assert.AreEqual(1, taskRepo.DeadlineWrites.Count);
        Assert.AreEqual((12L, (DateTime?)Today.AddDays(9)), taskRepo.DeadlineWrites[0]);
    }

    // ==========================================================
    // Phân quyền + validation deadline
    // ==========================================================

    [TestMethod]
    public void CanChangeDeadline_ByRole()
    {
        var service = Build(new FakeTaskRepository());

        Assert.IsTrue(service.CanChangeDeadline(ProjectId, OwnerId));
        Assert.IsTrue(service.CanChangeDeadline(ProjectId, ManagerId));
        Assert.IsFalse(service.CanChangeDeadline(ProjectId, CreatorId));
        Assert.IsFalse(service.CanChangeDeadline(ProjectId, 99));
    }

    [TestMethod]
    public void ChangeDeadline_Creator_ThrowsEvenForOwnTask()
    {
        var taskRepo = new FakeTaskRepository(MakeTask(1, AssignmentStatus.InProgress, deadline: Today.AddDays(3)));

        Assert.ThrowsExactly<UnauthorizedWorkflowActionException>(
            () => Build(taskRepo).ChangeDeadline(1, CreatorId, Today.AddDays(10), CreatorId));
        Assert.AreEqual(0, taskRepo.DeadlineWrites.Count);
    }

    [TestMethod]
    [DataRow(OwnerId)]
    [DataRow(ManagerId)]
    public void ChangeDeadline_OwnerOrManager_WritesNormalizedDate(long userId)
    {
        var taskRepo = new FakeTaskRepository(MakeTask(1, AssignmentStatus.InProgress, deadline: Today.AddDays(3)));
        var uow = new FakeUnitOfWork();

        MyTaskItem result = Build(taskRepo, uow).ChangeDeadline(1, CreatorId, Today.AddDays(10).AddHours(15), userId);

        Assert.AreEqual(Today.AddDays(10), result.Deadline);
        Assert.AreEqual(1, taskRepo.DeadlineWrites.Count);
        Assert.AreEqual((1L, (DateTime?)Today.AddDays(10)), taskRepo.DeadlineWrites[0]);
        Assert.AreEqual(1, uow.CommitCount);
    }

    [TestMethod]
    public void ChangeDeadline_PastDate_ThrowsValidation()
    {
        var taskRepo = new FakeTaskRepository(MakeTask(1, AssignmentStatus.InProgress, deadline: Today.AddDays(3)));

        Assert.ThrowsExactly<ContentValidationException>(
            () => Build(taskRepo).ChangeDeadline(1, CreatorId, Today.AddDays(-1), ManagerId));
        Assert.AreEqual(0, taskRepo.DeadlineWrites.Count);
    }

    [TestMethod]
    public void ChangeDeadline_ExtendingAnOverdueTask_IsAllowed()
    {
        // Task đang quá hạn: Manager dời deadline sang tương lai để gỡ trạng thái overdue.
        var taskRepo = new FakeTaskRepository(MakeTask(1, AssignmentStatus.InProgress, deadline: Today.AddDays(-4)));

        MyTaskItem result = Build(taskRepo).ChangeDeadline(1, CreatorId, Today.AddDays(2), ManagerId);

        Assert.IsFalse(result.IsOverdueOn(Today));
    }

    [TestMethod]
    public void ChangeDeadline_Null_ClearsDeadline()
    {
        var taskRepo = new FakeTaskRepository(MakeTask(1, AssignmentStatus.InProgress, deadline: Today.AddDays(3)));

        MyTaskItem result = Build(taskRepo).ChangeDeadline(1, CreatorId, null, OwnerId);

        Assert.IsNull(result.Deadline);
        Assert.AreEqual((1L, (DateTime?)null), taskRepo.DeadlineWrites[0]);
    }

    [TestMethod]
    public void ChangeDeadline_SameDate_SkipsWrite()
    {
        var taskRepo = new FakeTaskRepository(MakeTask(1, AssignmentStatus.InProgress, deadline: Today.AddDays(3)));

        Build(taskRepo).ChangeDeadline(1, CreatorId, Today.AddDays(3).AddHours(9), OwnerId);

        Assert.AreEqual(0, taskRepo.DeadlineWrites.Count);
    }

    [TestMethod]
    public void ChangeDeadline_ContentPublished_Throws()
    {
        var task = MakeTask(1, AssignmentStatus.Completed, deadline: Today.AddDays(-2), progress: 100);
        task.ContentStage = ContentStatus.Published;
        var taskRepo = new FakeTaskRepository(task);

        Assert.ThrowsExactly<ContentValidationException>(
            () => Build(taskRepo).ChangeDeadline(1, CreatorId, Today.AddDays(5), OwnerId));
    }

    // ==========================================================
    // Helpers
    // ==========================================================

    private static MyTaskService Build(FakeTaskRepository taskRepo, FakeUnitOfWork? uow = null) =>
        new(taskRepo,
            new FakeMemberRepository(
                (ProjectId, OwnerId, ProjectRole.Owner),
                (ProjectId, CreatorId, ProjectRole.Creator),
                (ProjectId, ManagerId, ProjectRole.Manager),
                (ProjectId, OtherCreatorId, ProjectRole.Creator)),
            uow ?? new FakeUnitOfWork(),
            () => Today);

    private static long[] Ids(IEnumerable<MyTaskItem> tasks) => tasks.Select(t => t.ContentId).ToArray();

    private static MyTaskItem MakeTask(
        long id,
        AssignmentStatus status,
        DateTime? deadline = null,
        Priority priority = Priority.Medium,
        int progress = 0,
        long assignee = CreatorId,
        long? assignmentId = null) =>
        new()
        {
            AssignmentId = assignmentId ?? id,
            ContentId = id,
            ProjectId = ProjectId,
            AssigneeUserId = assignee,
            ContentCode = $"CNT-{id:000}",
            ContentTitle = $"Task {id}",
            ContentStage = ContentStatus.Script,
            Status = status,
            Priority = priority,
            Deadline = deadline,
            ProgressPercent = progress,
        };

    private sealed class FakeTaskRepository(params MyTaskItem[] items) : IMyTaskRepository
    {
        private readonly List<MyTaskItem> _items = items.ToList();

        public int CallCount { get; private set; }
        public long LastProjectId { get; private set; }
        public long LastUserId { get; private set; }
        public bool ThrowOnWrite { get; init; }

        public List<(long AssignmentId, int Percent, AssignmentStatus Status)> ProgressWrites { get; } = new();
        public List<(long AssignmentId, DateTime? Deadline)> DeadlineWrites { get; } = new();

        public List<MyTaskItem> GetMyTasks(long projectId, long assigneeUserId)
        {
            CallCount++;
            LastProjectId = projectId;
            LastUserId = assigneeUserId;
            return _items.ToList();
        }

        public MyTaskItem? GetAssignment(long contentId, long assigneeUserId) =>
            _items.FirstOrDefault(i => i.ContentId == contentId && i.AssigneeUserId == assigneeUserId);

        public List<MyTaskItem> GetAssignments(long contentId) => _items.Where(i => i.ContentId == contentId).ToList();

        public void UpdateProgress(long assignmentId, int percent, AssignmentStatus status)
        {
            if (ThrowOnWrite) throw new InvalidOperationException("DB down");
            ProgressWrites.Add((assignmentId, percent, status));
        }

        public void UpdateAssignmentDeadline(long assignmentId, DateTime? deadline)
        {
            if (ThrowOnWrite) throw new InvalidOperationException("DB down");
            DeadlineWrites.Add((assignmentId, deadline));
        }

        public void AddAssignment(long contentId, long assigneeUserId, long assignedByUserId, DateTime? deadline) =>
            throw new NotSupportedException("MyTaskService không giao việc.");

        public void CancelAssignment(long assignmentId) =>
            throw new NotSupportedException("MyTaskService không huỷ giao việc.");
    }

    private sealed class FakeMemberRepository(params (long ProjectId, long UserId, ProjectRole Role)[] members)
        : IProjectMemberRepository
    {
        public ProjectRole? GetRole(long projectId, long userId)
        {
            foreach (var m in members)
                if (m.ProjectId == projectId && m.UserId == userId)
                    return m.Role;
            return null;
        }

        public List<ProjectMemberInfo> GetMembers(long projectId) => new();
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
