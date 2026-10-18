using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Models.Auth;
using CreatorFlow.Api.Repositories.Admin;
using CreatorFlow.Api.Services.Admin;
using CreatorFlow.Contracts.Admin;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.Api.Tests.Services;

[TestClass]
[TestCategory("AdminApiUnit")]
public sealed class AdminReportServiceTests
{
    [TestMethod]
    public async Task AnonymousCreate_IsUnauthorized()
    {
        var service = Create(caller: null, repository: new AdminReportRepositoryFake());
        var result = await service.CreateReportAsync(new CreateReportRequest(2, "Spam", null));
        Assert.AreEqual(401, result.Status);
    }

    [TestMethod]
    public async Task SelfReport_IsRejected()
    {
        var service = Create(caller: (5, false));
        var result = await service.CreateReportAsync(new CreateReportRequest(5, "Spam", null));
        Assert.AreEqual(400, result.Status);
        Assert.AreEqual("self_report", result.ErrorCode);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("  ")]
    public async Task EmptyReason_IsRejected(string? reason)
    {
        var service = Create(caller: (5, false));
        var result = await service.CreateReportAsync(new CreateReportRequest(6, reason, null));
        Assert.AreEqual(400, result.Status);
        Assert.AreEqual("Reason", result.ErrorField);
    }

    [TestMethod]
    public async Task ValidCreate_ReturnsOpen()
    {
        var service = Create(caller: (5, false));
        var result = await service.CreateReportAsync(new CreateReportRequest(6, "Spam content", "Details here"));
        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(7, result.Value!.ReportId);
        Assert.AreEqual("OPEN", result.Value.Status);
    }

    [TestMethod]
    public async Task MissingTarget_Returns404()
    {
        var repository = new AdminReportRepositoryFake { CreateResult = null };
        var service = Create(caller: (5, false), repository);
        var result = await service.CreateReportAsync(new CreateReportRequest(999, "Spam", null));
        Assert.AreEqual(404, result.Status);
    }

    [TestMethod]
    public async Task DuplicateOpenPair_Returns409()
    {
        var repository = new AdminReportRepositoryFake { CreateResult = new AdminReportCreateResult(true, 0) };
        var service = Create(caller: (5, false), repository);
        var result = await service.CreateReportAsync(new CreateReportRequest(6, "Spam", null));
        Assert.AreEqual(409, result.Status);
        Assert.AreEqual("duplicate_report", result.ErrorCode);
    }

    [TestMethod]
    public async Task NonAdminReportAdminAreas_AreForbidden()
    {
        var service = Create(caller: (5, false));
        Assert.AreEqual(403, (await service.GetReportsAsync(null, null, null, null)).Status);
        Assert.AreEqual(403, (await service.HandleReportAsync(1, new HandleReportRequest("RESOLVED", null), "t")).Status);
    }

    [TestMethod]
    [DataRow("OPEN")]
    [DataRow("open")]
    [DataRow(null)]
    public async Task Handle_RejectsOpenAndNull(string? status)
    {
        var service = Create(caller: (1, true));
        var result = await service.HandleReportAsync(1, new HandleReportRequest(status, null), "t");
        Assert.AreEqual(400, result.Status);
        Assert.AreEqual("Status", result.ErrorField);
    }

    [TestMethod]
    [DataRow("IN_REVIEW", "Đã tiếp nhận báo cáo để xem xét.")]
    [DataRow("RESOLVED", "Đã xử lý xong báo cáo.")]
    [DataRow("REJECTED", "Đã bác bỏ báo cáo.")]
    public async Task Handle_TransitionsSucceed(string status, string message)
    {
        var service = Create(caller: (1, true));
        var result = await service.HandleReportAsync(1, new HandleReportRequest(status, "Ghi chú"), "t");
        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(status, result.Value!.Status);
        Assert.AreEqual(message, result.Value.Message);
    }

    [TestMethod]
    public async Task Handle_MissingReport_Returns404()
    {
        var repository = new AdminReportRepositoryFake { HandleResult = null };
        var service = Create(caller: (1, true), repository);
        var result = await service.HandleReportAsync(999, new HandleReportRequest("RESOLVED", null), "t");
        Assert.AreEqual(404, result.Status);
    }

    [TestMethod]
    public async Task DatabaseFailure_MapsTo503()
    {
        var repository = new AdminReportRepositoryFake { Failure = new TimeoutException("db down") };
        var service = Create(caller: (1, true), repository);
        Assert.AreEqual(503, (await service.GetReportsAsync(null, null, null, null)).Status);
        Assert.AreEqual(503, (await service.HandleReportAsync(1, new HandleReportRequest("RESOLVED", null), "t")).Status);
        var creator = Create(caller: (5, false), repository);
        Assert.AreEqual(503, (await creator.CreateReportAsync(new CreateReportRequest(6, "Spam", null))).Status);
    }

    private static AdminReportService Create((long UserId, bool IsAdmin)? caller, AdminReportRepositoryFake? repository = null)
    {
        var current = new CurrentAuthenticatedUser();
        if (caller is { } c)
        {
            current.User = new User
            {
                UserId = c.UserId, Email = "caller@example.test", DisplayName = "Caller",
                PasswordHash = "x", IsSystemAdmin = c.IsAdmin,
            };
        }

        return new AdminReportService(repository ?? new AdminReportRepositoryFake(), current,
            NullLogger<AdminReportService>.Instance);
    }
}
