using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Models.Auth;
using CreatorFlow.Api.Models.Metrics;
using CreatorFlow.Api.Models.Tasks;
using CreatorFlow.Api.Repositories.Metrics;
using CreatorFlow.Api.Repositories.Tasks;
using CreatorFlow.Api.Services.Metrics;
using CreatorFlow.Contracts.Metrics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.Api.Tests.Metrics;

/// <summary>Quyền và luồng Metrics ở backend, fake đúng ngữ nghĩa database (merge null từ snapshot trước).</summary>
[TestClass]
public sealed class MetricServiceTests
{
    private const long ProjectId = 1;
    private const long OwnerId = 1;
    private const long ManagerId = 3;
    private const long CreatorId = 2;
    private const long ContentId = 10;
    private const long TikTokId = 5;
    private const long YouTubeId = 6;

    [TestMethod]
    public async Task Record_NotSignedIn_Returns401()
    {
        var f = new Fixture(null);

        var result = await f.Service.RecordAsync(ContentId, TikTokId, new RecordMetricRequest(1, null, null, null));

        Assert.AreEqual(401, result.Status);
    }

    [TestMethod]
    public async Task Record_OutsideProject_Returns403()
    {
        var f = new Fixture(99);

        var result = await f.Service.RecordAsync(ContentId, TikTokId, new RecordMetricRequest(1, null, null, null));

        Assert.AreEqual(403, result.Status);
    }

    [TestMethod]
    public async Task Record_Creator_Returns403()
    {
        var f = new Fixture(CreatorId);

        var result = await f.Service.RecordAsync(ContentId, TikTokId, new RecordMetricRequest(1, null, null, null));

        Assert.AreEqual(403, result.Status);
    }

    [TestMethod]
    public async Task Record_UnknownContent_Returns404()
    {
        var f = new Fixture(OwnerId);

        var result = await f.Service.RecordAsync(999, TikTokId, new RecordMetricRequest(1, null, null, null));

        Assert.AreEqual(404, result.Status);
    }

    [TestMethod]
    public async Task Record_PlatformNotInContent_Returns404()
    {
        var f = new Fixture(OwnerId);

        var result = await f.Service.RecordAsync(ContentId, 777, new RecordMetricRequest(1, null, null, null));

        Assert.AreEqual(404, result.Status);
        Assert.AreEqual("Content này không đăng lên platform đó.", result.Message);
    }

    [TestMethod]
    public async Task Record_PlannedPlatform_Returns409()
    {
        var f = new Fixture(OwnerId); // YouTube PLANNED, Content READY

        var result = await f.Service.RecordAsync(ContentId, YouTubeId, new RecordMetricRequest(1, 2, 3, 4));

        Assert.AreEqual(409, result.Status);
        Assert.AreEqual("content_locked", result.ErrorCode);
    }

    [TestMethod]
    public async Task Record_EmptyAndNegative_Returns400()
    {
        var f = new Fixture(ManagerId);

        Assert.AreEqual(400, (await f.Service.RecordAsync(ContentId, TikTokId, new RecordMetricRequest(null, null, null, null))).Status);
        Assert.AreEqual(400, (await f.Service.RecordAsync(ContentId, TikTokId, new RecordMetricRequest(-1, null, null, null))).Status);
        Assert.AreEqual(0, f.Metrics.AddCalls);
    }

    [TestMethod]
    public async Task Record_PartialMerge_KeepsPreviousValues()
    {
        var f = new Fixture(ManagerId);

        var first = await f.Service.RecordAsync(ContentId, TikTokId, new RecordMetricRequest(100, null, null, null));
        var second = await f.Service.RecordAsync(ContentId, TikTokId, new RecordMetricRequest(null, 5, null, null));

        Assert.IsTrue(first.Succeeded);
        Assert.IsTrue(second.Succeeded);
        Assert.AreEqual(100, second.Value!.Latest!.Views); // giữ từ snapshot trước
        Assert.AreEqual(5, second.Value.Latest.Likes);
        Assert.AreEqual(ManagerId, second.Value.Latest.RecordedBy);
        Assert.AreEqual($"User {ManagerId}", second.Value.Latest.RecordedByName);
        Assert.IsTrue(second.Value.Latest.RecordedAt.Offset == TimeSpan.Zero);
    }

    [TestMethod]
    public async Task GetPlatformMetrics_HistoryFlag_ControlsHistory()
    {
        var f = new Fixture(ManagerId);
        await f.Service.RecordAsync(ContentId, TikTokId, new RecordMetricRequest(100, null, null, null));
        await f.Service.RecordAsync(ContentId, TikTokId, new RecordMetricRequest(null, 5, null, null));

        var without = await f.Service.GetPlatformMetricsAsync(ContentId, TikTokId, false);
        var with = await f.Service.GetPlatformMetricsAsync(ContentId, TikTokId, true);

        Assert.IsTrue(without.Succeeded);
        Assert.IsNull(without.Value!.History);
        Assert.AreEqual(5, without.Value.Latest!.Likes);
        Assert.IsTrue(with.Succeeded);
        Assert.AreEqual(2, with.Value!.History!.Count);
        Assert.IsTrue(with.Value.History[0].MetricId > with.Value.History[1].MetricId); // mới → cũ
    }

    [TestMethod]
    public async Task GetContentMetrics_UnrecordedPlatform_LatestNull()
    {
        var f = new Fixture(CreatorId);
        await new Fixture(ManagerId, f.Metrics).Service.RecordAsync(ContentId, TikTokId, new RecordMetricRequest(10, 1, 2, 3));

        var result = await f.Service.GetContentMetricsAsync(ContentId);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(2, result.Value!.Platforms.Count);
        var youtube = result.Value.Platforms.Single(p => p.PlatformId == YouTubeId);
        Assert.IsNull(youtube.Latest); // AC2: chưa có số liệu vẫn hợp lệ
        Assert.IsFalse(youtube.CanRecord); // Creator + PLANNED
        var tiktok = result.Value.Platforms.Single(p => p.PlatformId == TikTokId);
        Assert.AreEqual(10, tiktok.Latest!.Views);
        Assert.IsFalse(tiktok.CanRecord); // Creator không được ghi
    }

    [TestMethod]
    public async Task GetContentMetrics_NotMember_Returns403()
    {
        var f = new Fixture(99);

        var result = await f.Service.GetContentMetricsAsync(ContentId);

        Assert.AreEqual(403, result.Status);
    }

    // ================================================================== helpers

    private sealed class Fixture
    {
        private readonly long? _actingUserId;

        public Fixture(long? actingUserId, FakeMetricRepository? shared = null)
        {
            _actingUserId = actingUserId;
            Metrics = shared ?? new FakeMetricRepository();
        }

        public FakeMetricRepository Metrics { get; }

        public MetricService Service => new(Metrics, new FakeTaskRepository(), new CurrentAuthenticatedUser
        {
            User = _actingUserId is long id
                ? new User { UserId = id, Email = $"u{id}@x.vn", DisplayName = $"User {id}", PasswordHash = "x" }
                : null,
        });
    }

    private sealed class FakeTaskRepository : ITaskRepository
    {
        private static readonly Dictionary<(long Project, long User), ProjectRole> Roles = new()
        {
            [(ProjectId, OwnerId)] = ProjectRole.Owner,
            [(ProjectId, ManagerId)] = ProjectRole.Manager,
            [(ProjectId, CreatorId)] = ProjectRole.Creator,
        };

        public Task<ProjectRole?> GetRoleAsync(long projectId, long userId, CancellationToken ct = default) =>
            Task.FromResult<ProjectRole?>(Roles.TryGetValue((projectId, userId), out var role) ? role : null);

        public Task<ContentInfo?> GetContentAsync(long contentId, CancellationToken ct = default) =>
            Task.FromResult<ContentInfo?>(contentId == ContentId
                ? new ContentInfo(contentId, ProjectId, "Ready", OwnerId, null)
                : null);

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

    /// <summary>Fake đúng ngữ nghĩa SQL: null gộp từ snapshot trước (chưa có thì 0), history mới → cũ.</summary>
    private sealed class FakeMetricRepository : IMetricRepository
    {
        private long _nextId;
        private readonly List<MetricSnapshot> _rows = new();
        public int AddCalls { get; private set; }

        private static readonly List<PlatformTarget> Platforms = new()
        {
            new PlatformTarget(100, ContentId, ProjectId, TikTokId, "TikTok", "PUBLISHED", "READY"),
            new PlatformTarget(101, ContentId, ProjectId, YouTubeId, "YouTube", "PLANNED", "READY"),
        };

        public Task<IReadOnlyList<PlatformTarget>> GetPlatformsAsync(long contentId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<PlatformTarget>>(contentId == ContentId ? Platforms : Array.Empty<PlatformTarget>());

        public Task<IReadOnlyList<MetricSnapshot>> GetLatestAsync(long contentId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<MetricSnapshot>>(_rows
                .GroupBy(r => r.ContentPlatformId).Select(g => g.OrderByDescending(r => r.MetricId).First()).ToList());

        public Task<IReadOnlyList<MetricSnapshot>> GetHistoryAsync(long contentPlatformId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<MetricSnapshot>>(_rows
                .Where(r => r.ContentPlatformId == contentPlatformId).OrderByDescending(r => r.MetricId).ToList());

        public Task<MetricSnapshot> AddSnapshotAsync(NewMetric metric, CancellationToken ct = default)
        {
            AddCalls++;
            MetricSnapshot? prev = _rows.Where(r => r.ContentPlatformId == metric.ContentPlatformId)
                .OrderByDescending(r => r.MetricId).FirstOrDefault();
            var saved = new MetricSnapshot(++_nextId, metric.ContentPlatformId,
                metric.Views ?? prev?.Views ?? 0, metric.Likes ?? prev?.Likes ?? 0,
                metric.Comments ?? prev?.Comments ?? 0, metric.Shares ?? prev?.Shares ?? 0,
                metric.RecordedBy, $"User {metric.RecordedBy}", DateTimeOffset.UtcNow);
            _rows.Add(saved);
            return Task.FromResult(saved);
        }
    }
}
