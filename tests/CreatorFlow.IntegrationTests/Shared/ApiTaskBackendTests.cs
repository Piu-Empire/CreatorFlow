using System.Net;
using System.Text;
using CreatorFlow.ApiClients;
using CreatorFlow.Contracts.Auth;
using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Repositories.Implementations;
using CreatorFlow.Repositories.InMemory;
using CreatorFlow.Repositories.Interfaces;
using CreatorFlow.Services;
using CreatorFlow.Services.Backends;
using CreatorFlow.Services.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.Shared;

/// <summary>
/// Mô hình Client–Server: giao việc / My Tasks / progress / deadline chạy qua CreatorFlow.Api.
/// Kiểm tra client gửi đúng request (đường dẫn, Bearer, body), đọc đúng DTO, đổi lỗi API sang ngoại lệ nghiệp vụ,
/// và MyTaskService / ContentService / Board uỷ quyền cho Backend khi được cấu hình.
/// </summary>
[TestClass]
public sealed class ApiTaskBackendTests
{
    private const string TaskJson = """
        {"assignmentId":11,"contentId":10,"projectId":1,"assigneeUserId":2,"contentCode":"CNT-010","contentTitle":"Video A",
         "contentStage":"Script","status":"InProgress","priority":"High","deadline":"2026-10-20","progressPercent":60,
         "assignedByName":"Manager A","platforms":["TikTok","YouTube"],"teamTotal":3,"teamDone":1}
        """;

    // ==================================================================
    // ApiMyTaskBackend
    // ==================================================================

    [TestMethod]
    public void GetMyTasks_SendsBearerAndMapsDtoToModel()
    {
        var http = new FakeHandler().Respond(HttpStatusCode.OK, $"{{\"tasks\":[{TaskJson}]}}");
        var backend = new ApiMyTaskBackend(NewApi(http), SignedIn("tok-123"));

        var tasks = backend.GetMyTasks(1);

        Assert.AreEqual("GET", http.Last.Method);
        Assert.AreEqual("/api/projects/1/my-tasks", http.Last.Path);
        Assert.AreEqual("Bearer tok-123", http.Last.Authorization);
        var task = tasks.Single();
        Assert.AreEqual(11L, task.AssignmentId);
        Assert.AreEqual(ContentStatus.Script, task.ContentStage);
        Assert.AreEqual(AssignmentStatus.InProgress, task.Status);
        Assert.AreEqual(Priority.High, task.Priority);
        Assert.AreEqual(new DateTime(2026, 10, 20), task.Deadline);
        Assert.AreEqual(60, task.ProgressPercent);
        Assert.AreEqual(3, task.TeamTotal);
        Assert.AreEqual(1, task.TeamDone);
        CollectionAssert.AreEqual(new[] { "TikTok", "YouTube" }, task.Platforms.ToArray());
    }

    [TestMethod]
    public void UpdateProgress_PutsPercentAndReturnsUpdatedTask()
    {
        var http = new FakeHandler().Respond(HttpStatusCode.OK, TaskJson);
        var backend = new ApiMyTaskBackend(NewApi(http), SignedIn());

        var task = backend.UpdateProgress(10, 60);

        Assert.AreEqual("PUT", http.Last.Method);
        Assert.AreEqual("/api/contents/10/progress", http.Last.Path);
        Assert.AreEqual("{\"progressPercent\":60}", http.Last.Body);
        Assert.AreEqual(60, task.ProgressPercent);
    }

    [TestMethod]
    public void ChangeDeadline_SendsDateOnlyOrNull()
    {
        var http = new FakeHandler().Respond(HttpStatusCode.OK, TaskJson);
        var backend = new ApiMyTaskBackend(NewApi(http), SignedIn());

        backend.ChangeDeadline(10, 4, new DateTime(2026, 10, 20, 15, 30, 0));
        Assert.AreEqual("/api/contents/10/assignments/4/deadline", http.Last.Path);
        Assert.AreEqual("{\"deadline\":\"2026-10-20\"}", http.Last.Body);   // chỉ gửi NGÀY

        backend.ChangeDeadline(10, 4, null);
        Assert.AreEqual("{\"deadline\":null}", http.Last.Body);
    }

    [TestMethod]
    public void CanChangeDeadline_TrueForOwnerAndManagerOnly()
    {
        Assert.IsTrue(Role("Owner").CanChangeDeadline(1));
        Assert.IsTrue(Role("Manager").CanChangeDeadline(1));
        Assert.IsFalse(Role("Creator").CanChangeDeadline(1));

        static ApiMyTaskBackend Role(string role) =>
            new(NewApi(new FakeHandler().Respond(HttpStatusCode.OK, $"{{\"role\":\"{role}\"}}")), SignedIn());
    }

    [TestMethod]
    public void CanChangeDeadline_FalseWhenNotMemberOrApiDown()
    {
        var forbidden = new ApiMyTaskBackend(NewApi(new FakeHandler().Respond(HttpStatusCode.Forbidden,
            Problem("forbidden", "Bạn không thuộc Project này."))), SignedIn());
        var down = new ApiMyTaskBackend(NewApi(new FakeHandler { Throw = new HttpRequestException("boom") }), SignedIn());

        Assert.IsFalse(forbidden.CanChangeDeadline(1));
        Assert.IsFalse(down.CanChangeDeadline(1));
    }

    // ==================================================================
    // Đổi lỗi API sang ngoại lệ nghiệp vụ mà giao diện đã xử lý
    // ==================================================================

    [TestMethod]
    public void ApiValidationError_BecomesContentValidationExceptionWithBackendMessage()
    {
        var http = new FakeHandler().Respond(HttpStatusCode.BadRequest, Problem("validation", "Tiến độ phải từ 0 đến 100%."));
        var backend = new ApiMyTaskBackend(NewApi(http), SignedIn());

        var ex = Assert.ThrowsExactly<ContentValidationException>(() => backend.UpdateProgress(10, 150));

        Assert.AreEqual("Tiến độ phải từ 0 đến 100%.", ex.Message);
    }

    [TestMethod]
    public void ApiForbidden_BecomesUnauthorizedWorkflowActionExceptionWithBackendMessage()
    {
        var http = new FakeHandler().Respond(HttpStatusCode.Forbidden, Problem("forbidden", "Chỉ Owner/Manager mới được đổi deadline."));
        var backend = new ApiMyTaskBackend(NewApi(http), SignedIn());

        var ex = Assert.ThrowsExactly<UnauthorizedWorkflowActionException>(() => backend.ChangeDeadline(10, 2, new DateTime(2026, 10, 20)));

        Assert.AreEqual("Chỉ Owner/Manager mới được đổi deadline.", ex.Message);
    }

    [TestMethod]
    public void ApiConflict_BecomesContentValidationException_NotFound_BecomesInvalidOperation()
    {
        var locked = new ApiMyTaskBackend(NewApi(new FakeHandler().Respond(HttpStatusCode.Conflict,
            Problem("content_locked", "Nội dung đã Published nên không thể đổi tiến độ."))), SignedIn());
        var missing = new ApiMyTaskBackend(NewApi(new FakeHandler().Respond(HttpStatusCode.NotFound,
            Problem("not_found", "Không tìm thấy công việc được giao."))), SignedIn());

        Assert.ThrowsExactly<ContentValidationException>(() => locked.UpdateProgress(10, 50));
        Assert.ThrowsExactly<InvalidOperationException>(() => missing.ChangeDeadline(10, 2, null));
    }

    [TestMethod]
    public void NetworkFailure_BecomesContentValidationException()
    {
        var backend = new ApiMyTaskBackend(NewApi(new FakeHandler { Throw = new HttpRequestException("boom") }), SignedIn());

        var ex = Assert.ThrowsExactly<ContentValidationException>(() => backend.GetMyTasks(1));

        Assert.IsTrue(ex.Message.Contains("API"));
    }

    [TestMethod]
    public void NotSignedIn_ThrowsUnauthorizedAndSendsNothing()
    {
        var http = new FakeHandler().Respond(HttpStatusCode.OK, "{\"tasks\":[]}");
        var backend = new ApiMyTaskBackend(NewApi(http), new UserSession());

        Assert.ThrowsExactly<UnauthorizedWorkflowActionException>(() => backend.GetMyTasks(1));
        Assert.AreEqual(0, http.Requests.Count);
    }

    // ==================================================================
    // ApiAssignmentBackend
    // ==================================================================

    [TestMethod]
    public void GetAssignableMembers_MapsMembers()
    {
        var http = new FakeHandler().Respond(HttpStatusCode.OK,
            "{\"members\":[{\"userId\":2,\"displayName\":\"Creator A\",\"role\":\"Creator\"},{\"userId\":4,\"displayName\":\"Creator B\",\"role\":\"Creator\"}]}");
        var backend = new ApiAssignmentBackend(NewApi(http), SignedIn());

        var members = backend.GetAssignableMembers(1);

        Assert.AreEqual("/api/projects/1/assignable-members", http.Last.Path);
        CollectionAssert.AreEqual(new long[] { 2, 4 }, members.Select(m => m.UserId).ToArray());
        Assert.AreEqual("Creator B", members[1].Name);
        Assert.AreEqual(ProjectRole.Creator, members[1].Role);
    }

    [TestMethod]
    public void AssignContent_SendsEveryCreatorWithOwnDeadline()
    {
        var http = new FakeHandler().Respond(HttpStatusCode.OK, $"{{\"assignments\":[{TaskJson}]}}");
        var backend = new ApiAssignmentBackend(NewApi(http), SignedIn());

        backend.AssignContent(10, new[]
        {
            new AssignmentRequest(2, new DateTime(2026, 10, 20)),
            new AssignmentRequest(4, null),
        });

        Assert.AreEqual("PUT", http.Last.Method);
        Assert.AreEqual("/api/contents/10/assignments", http.Last.Path);
        Assert.AreEqual("{\"assignments\":[{\"userId\":2,\"deadline\":\"2026-10-20\"},{\"userId\":4,\"deadline\":null}]}", http.Last.Body);
    }

    [TestMethod]
    public void GetAssignments_MapsRows()
    {
        var http = new FakeHandler().Respond(HttpStatusCode.OK, $"{{\"assignments\":[{TaskJson}]}}");

        var rows = new ApiAssignmentBackend(NewApi(http), SignedIn()).GetAssignments(10);

        Assert.AreEqual("/api/contents/10/assignments", http.Last.Path);
        Assert.AreEqual(2L, rows.Single().AssigneeUserId);
    }

    // ==================================================================
    // MyTaskService / ContentService uỷ quyền cho Backend
    // ==================================================================

    [TestMethod]
    public void MyTaskService_Remote_DelegatesEveryOperationToBackend()
    {
        var backend = new FakeMyTaskBackend();
        var service = MyTaskService.CreateRemote(backend);

        var tasks = service.GetMyTasks(1, 2);
        service.UpdateProgress(10, 50, 2);
        service.ChangeDeadline(10, 2, new DateTime(2026, 10, 20), 3);

        Assert.AreEqual(2, tasks.Count);
        Assert.AreEqual(1, service.CountOpenTasks(1, 2));                     // một task Assigned + một Completed → 1 task chưa xong
        Assert.IsTrue(service.CanChangeDeadline(1, 2));
        CollectionAssert.AreEqual(new[] { "progress:10:50", "deadline:10:2:2026-10-20" }, backend.Calls.Where(c => !c.StartsWith("tasks") && !c.StartsWith("can")).ToArray());
    }

    [TestMethod]
    public void MyTaskService_Remote_StillRejectsBadProgressBeforeCallingApi()
    {
        var backend = new FakeMyTaskBackend();

        Assert.ThrowsExactly<ContentValidationException>(() => MyTaskService.CreateRemote(backend).UpdateProgress(10, 101, 2));
        Assert.AreEqual(0, backend.Calls.Count);
    }

    [TestMethod]
    public void ContentService_WithBackend_AssignmentCallsGoToBackendNotLocalRepository()
    {
        var backend = new FakeAssignmentBackend();
        var service = BuildContentService(backend, out _, out _);

        var members = service.GetAssignableMembers(1, 2);
        var rows = service.GetAssignments(10);
        service.AssignContent(10, new[] { new AssignmentRequest(2, null) }, 3);

        Assert.AreEqual(1, members.Count);
        Assert.AreEqual(0, rows.Count);
        CollectionAssert.AreEqual(new[] { "members:1", "assignments:10", "assign:10:2" }, backend.Calls.ToArray());
    }

    [TestMethod]
    public void ContentService_WithBackend_CreateAssignsAfterCommitSoBackendSeesTheContent()
    {
        var order = new List<string>();
        var backend = new FakeAssignmentBackend { Log = order };
        var service = BuildContentService(backend, out var uow, out var details);
        uow.Log = order;
        details.Log = order;

        long id = service.Create(1, ContentStatus.Idea,
            new ContentDraft { Title = "Tiêu đề", Description = "Mô tả", AssigneeUserId = 2 }, 1);

        Assert.AreEqual(555L, id);
        CollectionAssert.AreEqual(new[] { "create-content", "commit", "assign:555:2" }, order.ToArray());
    }

    [TestMethod]
    public void ContentService_WithBackend_CreateWithoutAssigneeDoesNotCallBackend()
    {
        var backend = new FakeAssignmentBackend();
        var service = BuildContentService(backend, out _, out _);

        service.Create(1, ContentStatus.Idea, new ContentDraft { Title = "Tiêu đề", Description = "Mô tả" }, 1);

        Assert.AreEqual(0, backend.Calls.Count);
    }

    // ==================================================================
    // Board: Creator được giao lấy từ API
    // ==================================================================

    [TestMethod]
    public void ApiAssigneeBoardRepository_AttachesCreatorsFromApiToEachCard()
    {
        var http = new FakeHandler().Respond(HttpStatusCode.OK, """
            {"assignees":[
              {"contentId":10,"userId":2,"displayName":"Creator A","status":"Completed","progressPercent":100,"deadline":"2026-10-20"},
              {"contentId":10,"userId":4,"displayName":"Creator B","status":"InProgress","progressPercent":40,"deadline":null}]}
            """);
        var inner = new FakeBoardRepository(new ContentBoardCard { ContentId = 10, Title = "A" }, new ContentBoardCard { ContentId = 11, Title = "B" });
        var repo = new ApiAssigneeBoardRepository(inner, NewApi(http), SignedIn());

        var cards = repo.GetBoardCards(1);

        Assert.AreEqual("/api/projects/1/board/assignees", http.Last.Path);
        var first = cards.Single(c => c.ContentId == 10);
        Assert.AreEqual(2, first.TeamTotal);
        Assert.AreEqual(1, first.TeamDone);
        Assert.IsFalse(first.IsTeamCompleted);
        Assert.AreEqual(2L, first.AssigneeUserId);
        Assert.AreEqual("Creator A", first.AssigneeName);
        Assert.AreEqual(0, cards.Single(c => c.ContentId == 11).Assignees.Count);
    }

    // ==================================================================
    // Helpers
    // ==================================================================

    private static ApiClient NewApi(HttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5080/") });

    private static UserSession SignedIn(string token = "tok")
    {
        var session = new UserSession();
        bool ok = session.TrySetLogin(
            new LoginResponse(token, DateTimeOffset.UtcNow.AddHours(1), new CurrentUserResponse(2, "creator@x.vn", "Creator A", false)),
            session.Generation);
        Assert.IsTrue(ok);
        return session;
    }

    private static string Problem(string code, string title) =>
        $"{{\"title\":\"{title}\",\"status\":400,\"code\":\"{code}\"}}";

    private static ContentService BuildContentService(
        IAssignmentBackend backend, out FakeUnitOfWork uow, out FakeDetailsRepository details)
    {
        uow = new FakeUnitOfWork();
        details = new FakeDetailsRepository();
        return new ContentService(
            new InMemoryContentRepository(), details, new InMemoryContentStatusHistoryRepository(),
            new InMemoryProjectMemberRepository(), new InMemoryPlatformRepository(), uow,
            taskRepo: null, assignmentBackend: backend);
    }

    private sealed class FakeHandler : HttpMessageHandler
    {
        private HttpStatusCode _status = HttpStatusCode.OK;
        private string _body = "{}";
        public Exception? Throw { get; init; }
        public List<RecordedRequest> Requests { get; } = new();
        public RecordedRequest Last => Requests[^1];

        public FakeHandler Respond(HttpStatusCode status, string body)
        {
            _status = status;
            _body = body;
            return this;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(new RecordedRequest(
                request.Method.Method,
                request.RequestUri!.AbsolutePath,
                request.Headers.Authorization?.ToString(),
                request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken)));
            if (Throw is not null) throw Throw;
            return new HttpResponseMessage(_status) { Content = new StringContent(_body, Encoding.UTF8, "application/json") };
        }
    }

    private sealed record RecordedRequest(string Method, string Path, string? Authorization, string? Body);

    private sealed class FakeMyTaskBackend : IMyTaskBackend
    {
        public List<string> Calls { get; } = new();

        public List<MyTaskItem> GetMyTasks(long projectId)
        {
            Calls.Add($"tasks:{projectId}");
            return new List<MyTaskItem>
            {
                new() { ContentId = 10, Status = AssignmentStatus.Assigned },
                new() { ContentId = 11, Status = AssignmentStatus.Completed },
            };
        }

        public MyTaskItem UpdateProgress(long contentId, int percent)
        {
            Calls.Add($"progress:{contentId}:{percent}");
            return new MyTaskItem { ContentId = contentId, ProgressPercent = percent };
        }

        public MyTaskItem ChangeDeadline(long contentId, long assigneeUserId, DateTime? deadline)
        {
            Calls.Add($"deadline:{contentId}:{assigneeUserId}:{deadline:yyyy-MM-dd}");
            return new MyTaskItem { ContentId = contentId, Deadline = deadline };
        }

        public bool CanChangeDeadline(long projectId)
        {
            Calls.Add($"can:{projectId}");
            return true;
        }
    }

    private sealed class FakeAssignmentBackend : IAssignmentBackend
    {
        public List<string> Calls { get; } = new();
        public List<string>? Log { get; init; }

        public List<ProjectMemberInfo> GetAssignableMembers(long projectId)
        {
            Calls.Add($"members:{projectId}");
            return new List<ProjectMemberInfo> { new() { UserId = 2, Name = "Creator A", Role = ProjectRole.Creator } };
        }

        public List<MyTaskItem> GetAssignments(long contentId)
        {
            Calls.Add($"assignments:{contentId}");
            return new List<MyTaskItem>();
        }

        public void AssignContent(long contentId, IReadOnlyList<AssignmentRequest> assignments)
        {
            string entry = $"assign:{contentId}:{string.Join(",", assignments.Select(a => a.UserId))}";
            Calls.Add(entry);
            Log?.Add(entry);
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public List<string>? Log { get; set; }
        public void Begin() { }
        public void Commit() => Log?.Add("commit");
        public void Rollback() => Log?.Add("rollback");
        public void Dispose() { }
    }

    private sealed class FakeDetailsRepository : IContentDetailsRepository
    {
        public List<string>? Log { get; set; }

        public long Create(long projectId, ContentStatus status, ContentDraft draft, long createdByUserId)
        {
            Log?.Add("create-content");
            return 555;
        }

        public void Update(long contentId, ContentDraft draft) { }
    }

    private sealed class FakeBoardRepository(params ContentBoardCard[] cards) : IBoardRepository
    {
        public List<ContentBoardCard> GetBoardCards(long projectId) => cards.ToList();
    }
}