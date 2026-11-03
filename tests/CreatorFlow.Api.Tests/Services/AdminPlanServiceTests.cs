using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Models.Auth;
using CreatorFlow.Api.Services.Admin;
using CreatorFlow.Contracts.Admin;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.Api.Tests.Services;

[TestClass]
[TestCategory("AdminApiUnit")]
public sealed class AdminPlanServiceTests
{
    [TestMethod]
    public async Task AnonymousAndNonAdmin_AreForbidden()
    {
        var repository = new AdminPlanRepositoryFake();
        Assert.AreEqual(403, (await Create(null, repository).GetPlansAsync()).Status);

        var service = Create((5, false), repository);
        Assert.AreEqual(403, (await service.GetPlansAsync()).Status);
        Assert.AreEqual(403, (await service.CreatePlanAsync(NewPlan("PRO"))).Status);
        Assert.AreEqual(403, (await service.UpdatePlanAsync(1, new UpdatePlanRequest(null, null, null, null, false))).Status);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("  ")]
    public async Task EmptyCode_IsRejected(string? code)
    {
        var result = await Create((1, true), new AdminPlanRepositoryFake()).CreatePlanAsync(NewPlan(code));
        Assert.AreEqual(400, result.Status);
        Assert.AreEqual("Code", result.ErrorField);
    }

    [TestMethod]
    public async Task EmptyName_IsRejected()
    {
        var result = await Create((1, true), new AdminPlanRepositoryFake())
            .CreatePlanAsync(new CreatePlanRequest("PRO", " ", null, 0, null, null, null));
        Assert.AreEqual(400, result.Status);
        Assert.AreEqual("Name", result.ErrorField);
    }

    [TestMethod]
    public async Task DuplicateCode_IsRejectedIgnoringCase()
    {
        var repository = new AdminPlanRepositoryFake();
        repository.Plans.Add(ExistingPlan("PRO"));

        var result = await Create((1, true), repository).CreatePlanAsync(NewPlan("pro"));
        Assert.AreEqual(409, result.Status);
        Assert.AreEqual("duplicate_code", result.ErrorCode);
    }

    [TestMethod]
    [DataRow(-1.0, 0)]
    [DataRow(0.0, -1)]
    public async Task NegativeNumbers_AreRejected(double price, int maxProjects)
    {
        var result = await Create((1, true), new AdminPlanRepositoryFake())
            .CreatePlanAsync(new CreatePlanRequest("PRO", "Pro", null, (decimal)price, maxProjects, null, null));
        Assert.AreEqual(400, result.Status);
        Assert.AreEqual("validation", result.ErrorCode);
    }

    [TestMethod]
    public async Task ValidCreate_TrimsAndUppercasesCode()
    {
        var result = await Create((1, true), new AdminPlanRepositoryFake())
            .CreatePlanAsync(new CreatePlanRequest(" pro ", " Gói Pro ", "Mô tả", 99_000m, 5, 10, 100));

        Assert.IsTrue(result.Succeeded);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(1, result.Value.PlanId);
        Assert.AreEqual("PRO", result.Value.Code);
        Assert.AreEqual("Gói Pro", result.Value.Name);
        Assert.AreEqual(99_000m, result.Value.PriceMonthly);
        Assert.IsTrue(result.Value.IsActive);
    }

    [TestMethod]
    public async Task Update_KeepsOmittedFieldsAndTogglesActive()
    {
        var repository = new AdminPlanRepositoryFake();
        repository.Plans.Add(ExistingPlan("PRO"));

        var result = await Create((1, true), repository)
            .UpdatePlanAsync(1, new UpdatePlanRequest(200_000m, null, null, null, false));

        Assert.IsTrue(result.Succeeded);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(200_000m, result.Value.PriceMonthly);
        Assert.AreEqual(5, result.Value.MaxProjects);
        Assert.IsFalse(result.Value.IsActive);
    }

    [TestMethod]
    public async Task Update_MissingPlan_Returns404()
    {
        var result = await Create((1, true), new AdminPlanRepositoryFake())
            .UpdatePlanAsync(999, new UpdatePlanRequest(null, null, null, null, false));
        Assert.AreEqual(404, result.Status);
    }

    [TestMethod]
    public async Task DatabaseFailure_MapsTo503()
    {
        var repository = new AdminPlanRepositoryFake { Failure = new TimeoutException("db down") };
        var service = Create((1, true), repository);

        Assert.AreEqual(503, (await service.GetPlansAsync()).Status);
        Assert.AreEqual(503, (await service.CreatePlanAsync(NewPlan("PRO"))).Status);
        Assert.AreEqual(503, (await service.UpdatePlanAsync(1, new UpdatePlanRequest(null, null, null, null, false))).Status);
    }

    private static CreatePlanRequest NewPlan(string? code) =>
        new(code, "Gói Pro", null, 100_000m, 5, 10, 100);

    private static CreatorFlow.Models.Plan ExistingPlan(string code) => new()
    {
        PlanId = 1,
        Code = code,
        Name = "Gói Pro",
        PriceMonthly = 100_000m,
        MaxProjects = 5,
        MaxMembers = 10,
        AiRequestLimit = 100,
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
    };

    private static AdminPlanService Create((long UserId, bool IsAdmin)? caller, AdminPlanRepositoryFake repository)
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

        return new AdminPlanService(repository, current, NullLogger<AdminPlanService>.Instance);
    }
}
