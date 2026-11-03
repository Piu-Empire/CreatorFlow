using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Models.Admin;
using CreatorFlow.Api.Models.Auth;
using CreatorFlow.Api.Services.Admin;
using CreatorFlow.Contracts.Admin;
using CreatorFlow.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.Api.Tests.Services;

[TestClass]
[TestCategory("AdminApiUnit")]
public sealed class AdminSubscriptionServiceTests
{
    [TestMethod]
    public async Task AnonymousAndNonAdmin_AreForbidden()
    {
        var plans = new AdminPlanRepositoryFake();
        var subs = new AdminSubscriptionRepositoryFake(plans);
        Assert.AreEqual(403, (await Create(null, plans, subs).GetSubscriptionsAsync(null, null, null, null, null)).Status);

        var service = Create((5, false), plans, subs);
        Assert.AreEqual(403, (await service.GetSubscriptionsAsync(null, null, null, null, null)).Status);
        Assert.AreEqual(403, (await service.CreateSubscriptionAsync(new CreateSubscriptionRequest(2, 1, null))).Status);
        Assert.AreEqual(403, (await service.UpdateSubscriptionAsync(1, new UpdateSubscriptionRequest("CANCELLED", null))).Status);
    }

    [TestMethod]
    public async Task Search_RejectsUnknownStatus()
    {
        var plans = new AdminPlanRepositoryFake();
        var result = await Create((1, true), plans, new AdminSubscriptionRepositoryFake(plans))
            .GetSubscriptionsAsync("PAUSED", null, null, null, null);
        Assert.AreEqual(400, result.Status);
        Assert.AreEqual("Status", result.ErrorField);
    }

    [TestMethod]
    public async Task Create_MissingUser_Returns404()
    {
        var plans = new AdminPlanRepositoryFake();
        plans.Plans.Add(PlanRow("PRO"));

        var result = await Create((1, true), plans, new AdminSubscriptionRepositoryFake(plans))
            .CreateSubscriptionAsync(new CreateSubscriptionRequest(2, 1, null));
        Assert.AreEqual(404, result.Status);
    }

    [TestMethod]
    public async Task Create_MissingPlan_Returns404()
    {
        var plans = new AdminPlanRepositoryFake();
        var subs = new AdminSubscriptionRepositoryFake(plans) { UserIds = { 2 } };

        var result = await Create((1, true), plans, subs)
            .CreateSubscriptionAsync(new CreateSubscriptionRequest(2, 1, null));
        Assert.AreEqual(404, result.Status);
    }

    [TestMethod]
    public async Task Create_InactivePlan_Returns409()
    {
        var plans = new AdminPlanRepositoryFake();
        plans.Plans.Add(PlanRow("PRO", isActive: false));
        var subs = new AdminSubscriptionRepositoryFake(plans) { UserIds = { 2 } };

        var result = await Create((1, true), plans, subs)
            .CreateSubscriptionAsync(new CreateSubscriptionRequest(2, 1, null));
        Assert.AreEqual(409, result.Status);
        Assert.AreEqual("plan_inactive", result.ErrorCode);
    }

    [TestMethod]
    public async Task Create_WhenUserAlreadyActive_Returns409()
    {
        var plans = new AdminPlanRepositoryFake();
        plans.Plans.Add(PlanRow("PRO"));
        var subs = new AdminSubscriptionRepositoryFake(plans) { UserIds = { 2 } };
        subs.Rows.Add(SubscriptionRow(1, 2, 1, "ACTIVE"));

        var result = await Create((1, true), plans, subs)
            .CreateSubscriptionAsync(new CreateSubscriptionRequest(2, 1, null));
        Assert.AreEqual(409, result.Status);
        Assert.AreEqual("already_active", result.ErrorCode);
    }

    [TestMethod]
    public async Task Create_PastExpiry_IsRejected()
    {
        var result = await Create((1, true), new AdminPlanRepositoryFake(), new AdminSubscriptionRepositoryFake(new AdminPlanRepositoryFake()))
            .CreateSubscriptionAsync(new CreateSubscriptionRequest(2, 1, DateTimeOffset.UtcNow.AddMinutes(-1)));
        Assert.AreEqual(400, result.Status);
        Assert.AreEqual("ExpiresAt", result.ErrorField);
    }

    [TestMethod]
    public async Task Create_Valid_ReturnsActiveSubscription()
    {
        var plans = new AdminPlanRepositoryFake();
        plans.Plans.Add(PlanRow("PRO"));
        var subs = new AdminSubscriptionRepositoryFake(plans) { UserIds = { 2 } };

        var result = await Create((1, true), plans, subs)
            .CreateSubscriptionAsync(new CreateSubscriptionRequest(2, 1, null));

        Assert.IsTrue(result.Succeeded);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(1, result.Value.SubscriptionId);
        Assert.AreEqual(2, result.Value.UserId);
        Assert.AreEqual("PRO", result.Value.PlanCode);
        Assert.AreEqual("ACTIVE", result.Value.Status);
    }

    [TestMethod]
    public async Task Update_NoChanges_IsRejected()
    {
        var plans = new AdminPlanRepositoryFake();
        var result = await Create((1, true), plans, new AdminSubscriptionRepositoryFake(plans))
            .UpdateSubscriptionAsync(1, new UpdateSubscriptionRequest(null, null));
        Assert.AreEqual(400, result.Status);
    }

    [TestMethod]
    public async Task Update_InvalidStatus_IsRejected()
    {
        var plans = new AdminPlanRepositoryFake();
        var result = await Create((1, true), plans, new AdminSubscriptionRepositoryFake(plans))
            .UpdateSubscriptionAsync(1, new UpdateSubscriptionRequest("PAUSED", null));
        Assert.AreEqual(400, result.Status);
        Assert.AreEqual("Status", result.ErrorField);
    }

    [TestMethod]
    public async Task Update_ExpiryBeforeStart_IsRejected()
    {
        var plans = new AdminPlanRepositoryFake();
        var subs = new AdminSubscriptionRepositoryFake(plans);
        AdminSubscriptionRecord row = SubscriptionRow(1, 2, 1, "EXPIRED");
        subs.Rows.Add(row);

        var result = await Create((1, true), plans, subs)
            .UpdateSubscriptionAsync(1, new UpdateSubscriptionRequest(null, row.StartedAt.AddDays(-1)));
        Assert.AreEqual(400, result.Status);
        Assert.AreEqual("ExpiresAt", result.ErrorField);
    }

    [TestMethod]
    public async Task Update_ReactivateWhenAnotherActive_Returns409()
    {
        var plans = new AdminPlanRepositoryFake();
        var subs = new AdminSubscriptionRepositoryFake(plans);
        subs.Rows.Add(SubscriptionRow(1, 2, 1, "CANCELLED"));
        subs.Rows.Add(SubscriptionRow(2, 2, 1, "ACTIVE"));

        var result = await Create((1, true), plans, subs)
            .UpdateSubscriptionAsync(1, new UpdateSubscriptionRequest("ACTIVE", null));
        Assert.AreEqual(409, result.Status);
        Assert.AreEqual("already_active", result.ErrorCode);
    }

    [TestMethod]
    public async Task Update_StatusChange_Succeeds()
    {
        var plans = new AdminPlanRepositoryFake();
        var subs = new AdminSubscriptionRepositoryFake(plans);
        subs.Rows.Add(SubscriptionRow(1, 2, 1, "ACTIVE"));

        var result = await Create((1, true), plans, subs)
            .UpdateSubscriptionAsync(1, new UpdateSubscriptionRequest("cancelled", null));

        Assert.IsTrue(result.Succeeded);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual("CANCELLED", result.Value.Status);
        Assert.AreEqual("CANCELLED", subs.Rows[0].Status);
    }

    [TestMethod]
    public async Task Update_MissingSubscription_Returns404()
    {
        var plans = new AdminPlanRepositoryFake();
        var result = await Create((1, true), plans, new AdminSubscriptionRepositoryFake(plans))
            .UpdateSubscriptionAsync(999, new UpdateSubscriptionRequest("CANCELLED", null));
        Assert.AreEqual(404, result.Status);
    }

    [TestMethod]
    public async Task DatabaseFailure_MapsTo503()
    {
        var plans = new AdminPlanRepositoryFake();
        var subs = new AdminSubscriptionRepositoryFake(plans) { Failure = new TimeoutException("db down") };
        var service = Create((1, true), plans, subs);

        Assert.AreEqual(503, (await service.GetSubscriptionsAsync(null, null, null, null, null)).Status);
        Assert.AreEqual(503, (await service.CreateSubscriptionAsync(new CreateSubscriptionRequest(2, 1, null))).Status);
        Assert.AreEqual(503, (await service.UpdateSubscriptionAsync(1, new UpdateSubscriptionRequest("CANCELLED", null))).Status);
    }

    private static Plan PlanRow(string code, bool isActive = true) => new()
    {
        PlanId = 1,
        Code = code,
        Name = "Gói Pro",
        PriceMonthly = 100_000m,
        IsActive = isActive,
        CreatedAt = DateTime.UtcNow,
    };

    private static AdminSubscriptionRecord SubscriptionRow(long id, long userId, long planId, string status) => new()
    {
        SubscriptionId = id,
        UserId = userId,
        UserEmail = $"user{userId}@example.test",
        PlanId = planId,
        PlanCode = "PRO",
        PlanName = "Gói Pro",
        Status = status,
        StartedAt = DateTimeOffset.UtcNow.AddDays(-2),
    };

    private static AdminSubscriptionService Create((long UserId, bool IsAdmin)? caller,
        AdminPlanRepositoryFake plans, AdminSubscriptionRepositoryFake subs)
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

        return new AdminSubscriptionService(subs, plans, current, NullLogger<AdminSubscriptionService>.Instance);
    }
}
