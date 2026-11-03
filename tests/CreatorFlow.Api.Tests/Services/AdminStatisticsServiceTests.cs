using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Models.Admin;
using CreatorFlow.Api.Models.Auth;
using CreatorFlow.Api.Services.Admin;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.Api.Tests.Services;

[TestClass]
[TestCategory("AdminApiUnit")]
public sealed class AdminStatisticsServiceTests
{
    [TestMethod]
    public async Task AnonymousAndNonAdmin_AreForbidden()
    {
        var repository = new AdminStatisticsRepositoryFake();
        Assert.AreEqual(403, (await Create(null, repository).GetStatisticsAsync()).Status);
        Assert.AreEqual(403, (await Create((5, false), repository).GetStatisticsAsync()).Status);
    }

    [TestMethod]
    public async Task MapsAllFiveGroups()
    {
        var repository = new AdminStatisticsRepositoryFake
        {
            Result = new AdminStatisticsRecord
            {
                UsersTotal = 10,
                UsersActive = 6,
                UsersLocked = 2,
                UsersDisabled = 2,
                SystemAdmins = 1,
                ProjectsTotal = 4,
                ProjectsActiveLast30Days = 3,
                ContentsByStatus = [("IDEA", 5), ("PUBLISHED", 2)],
                SubscriptionsByPlan = [new AdminPlanUsageRecord(1, "PRO", "Gói Pro", 3, 2)],
                ReportsByStatus = [("OPEN", 1)],
            },
        };

        var result = await Create((1, true), repository).GetStatisticsAsync();

        Assert.IsTrue(result.Succeeded);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(new CreatorFlow.Contracts.Admin.AdminUserStats(10, 6, 2, 2, 1), result.Value.Users);
        Assert.AreEqual(new CreatorFlow.Contracts.Admin.AdminProjectStats(4, 3), result.Value.Projects);
        Assert.AreEqual(2, result.Value.ContentsByStatus.Count);
        Assert.AreEqual("IDEA", result.Value.ContentsByStatus[0].Key);
        Assert.AreEqual(5, result.Value.ContentsByStatus[0].Count);
        Assert.AreEqual(1, result.Value.SubscriptionsByPlan.Count);
        Assert.AreEqual(3, result.Value.SubscriptionsByPlan[0].Total);
        Assert.AreEqual(2, result.Value.SubscriptionsByPlan[0].Active);
        Assert.AreEqual(1, result.Value.ReportsByStatus.Count);
        Assert.AreEqual("OPEN", result.Value.ReportsByStatus[0].Key);
        Assert.AreNotEqual(default, result.Value.GeneratedAt);
    }

    [TestMethod]
    public async Task EmptyData_ReturnsZeroesAndEmptyLists()
    {
        var result = await Create((1, true), new AdminStatisticsRepositoryFake()).GetStatisticsAsync();

        Assert.IsTrue(result.Succeeded);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(0, result.Value.Users.Total);
        Assert.AreEqual(0, result.Value.Projects.Total);
        Assert.AreEqual(0, result.Value.ContentsByStatus.Count);
        Assert.AreEqual(0, result.Value.SubscriptionsByPlan.Count);
        Assert.AreEqual(0, result.Value.ReportsByStatus.Count);
    }

    [TestMethod]
    public async Task DatabaseFailure_MapsTo503()
    {
        var repository = new AdminStatisticsRepositoryFake { Failure = new TimeoutException("db down") };
        Assert.AreEqual(503, (await Create((1, true), repository).GetStatisticsAsync()).Status);
    }

    private static AdminStatisticsService Create((long UserId, bool IsAdmin)? caller,
        AdminStatisticsRepositoryFake repository)
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

        return new AdminStatisticsService(repository, current, NullLogger<AdminStatisticsService>.Instance);
    }
}
