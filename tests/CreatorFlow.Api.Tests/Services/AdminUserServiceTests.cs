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
public sealed class AdminUserServiceTests
{
    [TestMethod]
    public async Task NonAdmin_IsForbiddenOnBothOperations()
    {
        var service = Create(isAdmin: false, userId: 1);
        var list = await service.GetUsersAsync(null, null, null, null);
        Assert.AreEqual(403, list.Status);
        Assert.AreEqual("forbidden", list.ErrorCode);
        var update = await service.UpdateUserStatusAsync(2, new UpdateUserStatusRequest("LOCKED"), "trace");
        Assert.AreEqual(403, update.Status);
        Assert.AreEqual("forbidden", update.ErrorCode);
    }

    [TestMethod]
    public async Task SelfLock_IsRejected()
    {
        var service = Create(isAdmin: true, userId: 7);
        var result = await service.UpdateUserStatusAsync(7, new UpdateUserStatusRequest("LOCKED"), "trace");
        Assert.AreEqual(400, result.Status);
        Assert.AreEqual("self_lock", result.ErrorCode);
    }

    [TestMethod]
    [DataRow("ABC")]
    [DataRow("1")]
    [DataRow("0")]
    [DataRow("Locked, Active")]
    [DataRow("DISABLED")]
    [DataRow(null)]
    [DataRow("  ")]
    public async Task LenientStatusValues_AreRejected(string? status)
    {
        var service = Create(isAdmin: true, userId: 1);
        var result = await service.UpdateUserStatusAsync(2, new UpdateUserStatusRequest(status), "trace");
        Assert.AreEqual(400, result.Status);
        Assert.AreEqual("Status", result.ErrorField);
    }

    [TestMethod]
    [DataRow("LOCKED", "Đã khóa tài khoản.")]
    [DataRow("locked", "Đã khóa tài khoản.")]
    [DataRow("ACTIVE", "Đã mở khóa tài khoản.")]
    public async Task ValidStatus_UpdatesAccount(string status, string message)
    {
        var repository = new AdminUserRepositoryFake();
        var service = Create(isAdmin: true, userId: 1, repository);
        var result = await service.UpdateUserStatusAsync(2, new UpdateUserStatusRequest(status), "trace");
        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(message, result.Value!.Message);
        Assert.AreEqual(status.ToUpperInvariant(), result.Value.Status);
    }

    [TestMethod]
    public async Task MissingTarget_Returns404()
    {
        var repository = new AdminUserRepositoryFake { UpdateResult = null };
        var service = Create(isAdmin: true, userId: 1, repository);
        var result = await service.UpdateUserStatusAsync(999, new UpdateUserStatusRequest("LOCKED"), "trace");
        Assert.AreEqual(404, result.Status);
    }

    [TestMethod]
    public async Task LastActiveAdmin_IsProtected()
    {
        var repository = new AdminUserRepositoryFake
        {
            UpdateResult = new AdminStatusChange(true, false, true, AccountStatus.Active, true, "boss@example.test"),
        };
        var service = Create(isAdmin: true, userId: 1, repository);
        var result = await service.UpdateUserStatusAsync(2, new UpdateUserStatusRequest("LOCKED"), "trace");
        Assert.AreEqual(400, result.Status);
        Assert.AreEqual("last_active_admin", result.ErrorCode);
    }

    [TestMethod]
    public async Task NoChange_ReturnsSuccessWithoutUpdate()
    {
        var repository = new AdminUserRepositoryFake
        {
            UpdateResult = new AdminStatusChange(true, false, false, AccountStatus.Active, false, "target@example.test"),
        };
        var service = Create(isAdmin: true, userId: 1, repository);
        var result = await service.UpdateUserStatusAsync(2, new UpdateUserStatusRequest("ACTIVE"), "trace");
        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("Trạng thái tài khoản không thay đổi.", result.Value!.Message);
    }

    [TestMethod]
    [DataRow("x", "ACTIVE", 20, 0)]
    [DataRow(null, null, 20, 0)]
    [DataRow(null, "disabled", 20, 0)]
    public async Task Search_NormalizesFilters(string? search, string? status, int limit, int offset)
    {
        var repository = new AdminUserRepositoryFake { TotalCount = 3 };
        var service = Create(isAdmin: true, userId: 1, repository);
        var result = await service.GetUsersAsync(search, status, limit, offset);
        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(3, result.Value!.TotalCount);
        Assert.AreEqual(status?.ToUpperInvariant(), repository.LastStatus);
    }

    [TestMethod]
    public async Task OversizedLimit_IsClamped()
    {
        var repository = new AdminUserRepositoryFake();
        var service = Create(isAdmin: true, userId: 1, repository);
        Assert.IsTrue((await service.GetUsersAsync(null, null, 1000000, null)).Succeeded);
        Assert.AreEqual(100, repository.LastLimit);
    }

    [TestMethod]
    [DataRow("BADS", null, null)]
    [DataRow(null, 0, null)]
    [DataRow(null, null, -1)]
    [DataRow(null, null, 10001)]
    public async Task InvalidPaging_IsRejected(string? status, int? limit, int? offset)
    {
        var service = Create(isAdmin: true, userId: 1);
        var result = await service.GetUsersAsync(null, status, limit, offset);
        Assert.AreEqual(400, result.Status);
    }

    [TestMethod]
    public async Task LongSearch_IsRejected()
    {
        var service = Create(isAdmin: true, userId: 1);
        var result = await service.GetUsersAsync(new string('a', 101), null, null, null);
        Assert.AreEqual(400, result.Status);
        Assert.AreEqual("Search", result.ErrorField);
    }

    [TestMethod]
    public async Task DatabaseFailure_MapsTo503()
    {
        var repository = new AdminUserRepositoryFake { Failure = new TimeoutException("db down") };
        var service = Create(isAdmin: true, userId: 1, repository);
        Assert.AreEqual(503, (await service.GetUsersAsync(null, null, null, null)).Status);
        Assert.AreEqual(503, (await service.UpdateUserStatusAsync(2, new UpdateUserStatusRequest("LOCKED"), "trace")).Status);
    }

    private static AdminUserService Create(bool isAdmin, long userId, AdminUserRepositoryFake? repository = null)
    {
        var current = new CurrentAuthenticatedUser
        {
            User = new User
            {
                UserId = userId, Email = "admin@example.test", DisplayName = "Admin",
                PasswordHash = "x", IsSystemAdmin = isAdmin,
            },
        };
        return new AdminUserService(repository ?? new AdminUserRepositoryFake(), current, NullLogger<AdminUserService>.Instance);
    }
}
