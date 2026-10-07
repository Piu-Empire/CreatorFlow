using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Repositories.InMemory;
using CreatorFlow.Repositories.Interfaces;
using CreatorFlow.Services;
using CreatorFlow.Services.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.Shared;

/// <summary>Đường sửa Content (drawer) cũng phải tuân thủ phân quyền deadline.</summary>
[TestClass]
public sealed class ContentServiceDeadlineTests
{
    private const long ProjectId = 1;
    private const long CreatorId = 2;
    private const long ManagerId = 3;

    private static readonly DateTime OldDeadline = DateTime.Today.AddDays(5);

    [TestMethod]
    public void Update_CreatorChangesDeadline_Throws()
    {
        var (service, details) = Build(CreatorId, ProjectRole.Creator);

        Assert.ThrowsExactly<UnauthorizedWorkflowActionException>(
            () => service.Update(10, Draft(OldDeadline.AddDays(2)), CreatorId));
        Assert.AreEqual(0, details.UpdateCount);
    }

    [TestMethod]
    public void Update_CreatorKeepsSameDeadline_Saves()
    {
        var (service, details) = Build(CreatorId, ProjectRole.Creator);

        service.Update(10, Draft(OldDeadline.AddHours(10)), CreatorId); // cùng ngày → không tính là đổi

        Assert.AreEqual(1, details.UpdateCount);
    }

    [TestMethod]
    public void Update_ManagerChangesDeadline_Saves()
    {
        var (service, details) = Build(ManagerId, ProjectRole.Manager);

        service.Update(10, Draft(OldDeadline.AddDays(2)), ManagerId);

        Assert.AreEqual(1, details.UpdateCount);
    }

    [TestMethod]
    public void Update_ManagerSetsPastDeadline_ThrowsValidation()
    {
        var (service, details) = Build(ManagerId, ProjectRole.Manager);

        Assert.ThrowsExactly<ContentValidationException>(
            () => service.Update(10, Draft(DateTime.Today.AddDays(-1)), ManagerId));
        Assert.AreEqual(0, details.UpdateCount);
    }

    // ---------- Helpers ----------

    private static ContentDraft Draft(DateTime? deadline) => new()
    {
        Title = "Tiêu đề",
        Description = "Mô tả",
        Deadline = deadline,
    };

    private static (ContentService Service, FakeDetailsRepository Details) Build(long userId, ProjectRole role)
    {
        var details = new FakeDetailsRepository();
        var service = new ContentService(
            new FakeContentRepository(),
            details,
            new FakeHistoryRepository(),
            new FakeMemberRepository(userId, role),
            new FakeUnitOfWork(),
            new InMemoryMyTaskRepository());
        return (service, details);
    }

    private sealed class FakeContentRepository : IContentRepository
    {
        public Content GetById(long contentId) => new()
        {
            Id = contentId,
            ProjectId = ProjectId,
            Title = "Tiêu đề cũ",
            Status = ContentStatus.Script,
            CreatedByUserId = CreatorId, // Creator #2 là người tạo
            Deadline = OldDeadline,
        };

        public void UpdateStatus(long contentId, ContentStatus newStatus) { }
    }

    private sealed class FakeDetailsRepository : IContentDetailsRepository
    {
        public int UpdateCount { get; private set; }
        public long Create(long projectId, ContentStatus status, ContentDraft draft, long createdByUserId) => 1;
        public void Update(long contentId, ContentDraft draft) => UpdateCount++;
    }

    private sealed class FakeHistoryRepository : IContentStatusHistoryRepository
    {
        public void Add(ContentStatusHistory history) { }
    }

    private sealed class FakeMemberRepository(long userId, ProjectRole role) : IProjectMemberRepository
    {
        public ProjectRole? GetRole(long projectId, long id) => id == userId ? role : null;
        public List<ProjectMemberInfo> GetMembers(long projectId) => new();
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public void Begin() { }
        public void Commit() { }
        public void Rollback() { }
        public void Dispose() { }
    }
}
