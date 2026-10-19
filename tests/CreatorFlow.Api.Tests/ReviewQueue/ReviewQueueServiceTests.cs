using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Models.Auth;
using CreatorFlow.Api.Models.ReviewQueue;
using CreatorFlow.Api.Models.Tasks;
using CreatorFlow.Api.Repositories.ReviewQueue;
using CreatorFlow.Api.Services.ReviewQueue;
using CreatorFlow.Api.Tests.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.Api.Tests.ReviewQueue;

/// <summary>
/// SCRUM-46 follow-up: Review Queue nối API. Quyền được kiểm tra Ở BACKEND:
/// phải đăng nhập, phải là thành viên Project, chỉ Owner/Manager mới xem danh sách chờ duyệt.
/// </summary>
[TestClass]
public sealed class ReviewQueueServiceTests
{
    private const long ProjectId = 1;
    private const long OwnerId = 1;
    private const long ManagerId = 3;
    private const long CreatorId = 2;

    [TestMethod]
    public async Task GetPending_NotSignedIn_401()
    {
        var f = new Fixture(actingUserId: null);

        var result = await f.Service.GetPendingAsync(ProjectId);

        Assert.AreEqual(401, result.Status);
        Assert.AreEqual(0, f.Repository.Queried.Count);
    }

    [TestMethod]
    public async Task GetPending_NotMember_403()
    {
        var f = new Fixture(actingUserId: 999);

        var result = await f.Service.GetPendingAsync(ProjectId);

        Assert.AreEqual(403, result.Status);
        Assert.AreEqual(0, f.Repository.Queried.Count);
    }

    [TestMethod]
    public async Task GetPending_Creator_403()
    {
        var f = new Fixture(CreatorId);

        var result = await f.Service.GetPendingAsync(ProjectId);

        Assert.AreEqual(403, result.Status);
        Assert.AreEqual(0, f.Repository.Queried.Count);
    }

    [TestMethod]
    public async Task GetPending_Owner_ReturnsPendingItemsWithContentCode()
    {
        var f = new Fixture(OwnerId);
        var submittedAt = new DateTime(2026, 10, 19, 8, 30, 0, DateTimeKind.Utc);
        f.Repository.Pending[ProjectId] =
        [
            new PendingReviewRecord(12, ProjectId, "Reel khẩn", "Creator A", submittedAt, 2),
        ];

        var result = await f.Service.GetPendingAsync(ProjectId);

        Assert.IsTrue(result.Succeeded, result.Message);
        Assert.AreEqual(ProjectId, result.Value!.ProjectId);
        Assert.AreEqual(1, result.Value.Items.Count);

        var item = result.Value.Items[0];
        Assert.AreEqual(12, item.ContentId);
        Assert.AreEqual("CNT-012", item.ContentCode);
        Assert.AreEqual("Reel khẩn", item.ContentTitle);
        Assert.AreEqual("Creator A", item.SubmittedByName);
        Assert.AreEqual(submittedAt, item.SubmittedAt);
        Assert.AreEqual(2, item.ReviewNo);
        CollectionAssert.AreEqual(new[] { ProjectId }, f.Repository.Queried);
    }

    [TestMethod]
    public async Task GetPending_Manager_Allowed_AndEmptyQueueIsEmptyList()
    {
        var f = new Fixture(ManagerId);

        var result = await f.Service.GetPendingAsync(ProjectId);

        Assert.IsTrue(result.Succeeded, result.Message);
        Assert.AreEqual(0, result.Value!.Items.Count);
    }

    private sealed class Fixture
    {
        public Fixture(long? actingUserId)
        {
            CurrentAuthenticatedUser current = new();
            if (actingUserId.HasValue)
            {
                current.User = new User
                {
                    UserId = actingUserId.Value,
                    Email = "actor@x.vn",
                    DisplayName = "Actor",
                    PasswordHash = "x",
                };
            }

            Roles.Roles[(ProjectId, OwnerId)] = ProjectRole.Owner;
            Roles.Roles[(ProjectId, ManagerId)] = ProjectRole.Manager;
            Roles.Roles[(ProjectId, CreatorId)] = ProjectRole.Creator;

            Service = new ReviewQueueService(Repository, Roles, current);
        }

        public ReviewQueueRepositoryFake Repository { get; } = new();
        public MemberRolesFake Roles { get; } = new();
        public ReviewQueueService Service { get; }
    }
}

/// <summary>IReviewQueueRepository giả: chỉ trả dữ liệu đã seed và ghi lại Project đã hỏi.</summary>
internal sealed class ReviewQueueRepositoryFake : IReviewQueueRepository
{
    public Dictionary<long, List<PendingReviewRecord>> Pending { get; } = new();
    public List<long> Queried { get; } = new();

    public Task<IReadOnlyList<PendingReviewRecord>> GetPendingAsync(
        long projectId, CancellationToken cancellationToken = default)
    {
        Queried.Add(projectId);
        return Task.FromResult<IReadOnlyList<PendingReviewRecord>>(
            Pending.GetValueOrDefault(projectId) ?? new List<PendingReviewRecord>());
    }
}
