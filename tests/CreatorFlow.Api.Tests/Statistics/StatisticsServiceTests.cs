using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Models.Auth;
using CreatorFlow.Api.Models.Statistics;
using CreatorFlow.Api.Models.Tasks;
using CreatorFlow.Api.Repositories.Statistics;
using CreatorFlow.Api.Repositories.Tasks;
using CreatorFlow.Api.Services.Statistics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.Api.Tests.Statistics;

/// <summary>
/// SCRUM-55: quyền đọc, phạm vi project và cách cộng dòng "Toàn Project".
/// Fake repository đại diện cho SQL: mọi dòng nó trả về đều đã thuộc đúng project được truyền vào.
/// </summary>
[TestClass]
public sealed class StatisticsServiceTests
{
    private const long ProjectId = 1;
    private const long OtherProjectId = 2;
    private const long OwnerId = 1;
    private const long CreatorAId = 2;
    private const long CreatorBId = 3;
    private const long OutsiderId = 99;

    [TestMethod]
    public async Task NotSignedIn_Returns401()
    {
        var f = new Fixture(null);

        var result = await f.Service.GetCreatorStatisticsAsync(ProjectId, null);

        Assert.AreEqual(401, result.Status);
        Assert.AreEqual(0, f.Statistics.Calls);
    }

    [TestMethod]
    public async Task OutsideProject_Returns403_AndDoesNotQuery()
    {
        var f = new Fixture(OutsiderId);

        var result = await f.Service.GetCreatorStatisticsAsync(ProjectId, null);

        Assert.AreEqual(403, result.Status);
        Assert.AreEqual(0, f.Statistics.Calls);
    }

    [TestMethod]
    public async Task CreatorRole_CanReadProjectStatistics()
    {
        var f = new Fixture(CreatorAId);

        var result = await f.Service.GetCreatorStatisticsAsync(ProjectId, null);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(ProjectId, f.Statistics.LastProjectId);
    }

    [TestMethod]
    public async Task MapsCountsAndEngagement()
    {
        var f = new Fixture(OwnerId);

        var result = await f.Service.GetCreatorStatisticsAsync(ProjectId, null);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(2, result.Value!.Creators.Count);
        var a = result.Value.Creators.Single(r => r.CreatorId == CreatorAId);
        Assert.AreEqual(12, a.Total);
        Assert.AreEqual(3, a.InProgress);
        Assert.AreEqual(8, a.Published);
        Assert.AreEqual(1, a.Overdue);
        Assert.AreEqual(120_000, a.Views);
        // (6000 + 400 + 300) / 120000 × 100 = 5.5833… → 5.58
        Assert.AreEqual(5.58, a.EngagementPercent, 0.0001);
    }

    [TestMethod]
    public async Task ProjectRow_IsSumOfAllCreators_NotAffectedByCreatorFilter()
    {
        var f = new Fixture(OwnerId);

        var result = await f.Service.GetCreatorStatisticsAsync(ProjectId, CreatorAId);
        var value = result.Value!;

        Assert.AreEqual(1, value.Creators.Count);
        Assert.AreEqual(CreatorAId, value.Creators[0].CreatorId);
        // Project luôn là tổng của A + B dù đang lọc A.
        Assert.AreEqual(21, value.Project.Total);
        Assert.AreEqual(13, value.Project.Published);
        Assert.AreEqual(2, value.Project.Overdue);
        Assert.AreEqual(200_000, value.Project.Views);
        // (9000 + 600 + 400) / 200000 × 100 = 5.00
        Assert.AreEqual(5d, value.Project.EngagementPercent, 0.0001);
    }

    [TestMethod]
    public async Task FilterUnknownCreator_ReturnsEmptyList_NoLeak()
    {
        var f = new Fixture(OwnerId);

        var result = await f.Service.GetCreatorStatisticsAsync(ProjectId, 12345);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(0, result.Value!.Creators.Count);
        Assert.AreEqual(21, result.Value.Project.Total);
    }

    [TestMethod]
    public async Task ZeroViews_EngagementIsZero()
    {
        var f = new Fixture(OwnerId);
        f.Statistics.Rows = [new CreatorStatisticsRecord(CreatorAId, "Minh", 4, 1, 0, 1, 0, 0, 0, 0)];

        var result = await f.Service.GetCreatorStatisticsAsync(ProjectId, null);

        Assert.AreEqual(0d, result.Value!.Creators[0].EngagementPercent);
        Assert.AreEqual(0d, result.Value.Project.EngagementPercent);
    }

    [TestMethod]
    public async Task CreatorIdZeroOrNegative_TreatedAsAll()
    {
        var f = new Fixture(OwnerId);

        var result = await f.Service.GetCreatorStatisticsAsync(ProjectId, 0);

        Assert.AreEqual(2, result.Value!.Creators.Count);
    }

    // ================================================================== helpers

    private sealed class Fixture
    {
        private readonly long? _actingUserId;

        public Fixture(long? actingUserId)
        {
            _actingUserId = actingUserId;
        }

        public FakeStatisticsRepository Statistics { get; } = new();

        public StatisticsService Service => new(Statistics, new FakeTaskRepository(), new CurrentAuthenticatedUser
        {
            User = _actingUserId is long id
                ? new User { UserId = id, Email = $"u{id}@x.vn", DisplayName = $"User {id}", PasswordHash = "x" }
                : null,
        });
    }

    private sealed class FakeStatisticsRepository : IStatisticsRepository
    {
        public int Calls { get; private set; }
        public long? LastProjectId { get; private set; }

        public List<CreatorStatisticsRecord> Rows { get; set; } =
        [
            new CreatorStatisticsRecord(CreatorAId, "Minh", 12, 3, 8, 1, 120_000, 6_000, 400, 300),
            new CreatorStatisticsRecord(CreatorBId, "Lan", 9, 3, 5, 1, 80_000, 3_000, 200, 100),
        ];

        public Task<IReadOnlyList<CreatorStatisticsRecord>> GetCreatorStatisticsAsync(
            long projectId, CancellationToken ct = default)
        {
            Calls++;
            LastProjectId = projectId;
            return Task.FromResult<IReadOnlyList<CreatorStatisticsRecord>>(Rows);
        }
    }

    private sealed class FakeTaskRepository : ITaskRepository
    {
        private static readonly Dictionary<(long Project, long User), ProjectRole> Roles = new()
        {
            [(ProjectId, OwnerId)] = ProjectRole.Owner,
            [(ProjectId, CreatorAId)] = ProjectRole.Creator,
            [(ProjectId, CreatorBId)] = ProjectRole.Creator,
        };

        public Task<ProjectRole?> GetRoleAsync(long projectId, long userId, CancellationToken ct = default) =>
            Task.FromResult<ProjectRole?>(Roles.TryGetValue((projectId, userId), out ProjectRole role) ? role : null);

        public Task<ContentInfo?> GetContentAsync(long contentId, CancellationToken ct = default) =>
            Task.FromResult<ContentInfo?>(null);
        public Task<IReadOnlyList<ProjectMember>> GetMembersAsync(long projectId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ProjectMember>>(Array.Empty<ProjectMember>());
        public Task<IReadOnlyList<TaskAssignment>> GetMyTasksAsync(long projectId, long userId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<TaskAssignment>>(Array.Empty<TaskAssignment>());
        public Task<TaskAssignment?> GetAssignmentAsync(long contentId, long userId, CancellationToken ct = default) =>
            Task.FromResult<TaskAssignment?>(null);
        public Task<IReadOnlyList<TaskAssignment>> GetAssignmentsAsync(long contentId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<TaskAssignment>>(Array.Empty<TaskAssignment>());
        public Task<IReadOnlyList<BoardAssignee>> GetBoardAssigneesAsync(long projectId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<BoardAssignee>>(Array.Empty<BoardAssignee>());
        public Task UpdateProgressAsync(long assignmentId, int percent, AssignmentStatus status, CancellationToken ct = default) =>
            Task.CompletedTask;
        public Task UpdateAssignmentDeadlineAsync(long assignmentId, DateOnly? deadline, CancellationToken ct = default) =>
            Task.CompletedTask;
        public Task ApplyAssignmentsAsync(long contentId, long actorUserId, IReadOnlyList<long> cancelAssignmentIds,
            IReadOnlyList<DeadlineChange> deadlineChanges, IReadOnlyList<NewAssignment> additions, CancellationToken ct = default) =>
            Task.CompletedTask;
    }
}
