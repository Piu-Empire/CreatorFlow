using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Services.Auth;
using CreatorFlow.Contracts.Admin;
using CreatorFlow.Models;
using CreatorFlow.Repositories;

namespace CreatorFlow.Api.Services.Admin;

/// <summary>
/// SCRUM-23: quản trị gói dịch vụ. Chỉ System Admin; không dùng ProjectRole.
/// </summary>
public sealed class AdminPlanService(
    IPlanRepository repository,
    CurrentAuthenticatedUser currentUser,
    ILogger<AdminPlanService> logger)
{
    private const int MaxCodeLength = 50;
    private const int MaxNameLength = 100;
    private const int MaxDescriptionLength = 1000;

    public async Task<AuthOperationResult<AdminPlansListResponse>> GetPlansAsync(
        CancellationToken cancellationToken = default)
    {
        if (currentUser.User is not { IsSystemAdmin: true })
            return AuthOperationResult<AdminPlansListResponse>.Fail("forbidden", "Bạn không có quyền quản trị hệ thống.", status: 403);

        try
        {
            IReadOnlyList<Plan> plans = await repository.GetAllAsync(cancellationToken);
            return new(new AdminPlansListResponse([.. plans.Select(ToResponse)]));
        }
        catch (Exception exception) when (AuthRules.IsDatabaseError(exception, cancellationToken))
        {
            return AuthOperationResult<AdminPlansListResponse>.Fail("service_unavailable", "Không thể tải danh sách gói lúc này.", status: 503);
        }
    }

    public async Task<AuthOperationResult<AdminPlanResponse>> CreatePlanAsync(CreatePlanRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.User is not { IsSystemAdmin: true })
            return AuthOperationResult<AdminPlanResponse>.Fail("forbidden", "Bạn không có quyền quản trị hệ thống.", status: 403);

        string? codeError = ValidateCode(request.Code);
        if (codeError is not null)
            return AuthOperationResult<AdminPlanResponse>.Fail("validation", codeError, "Code");
        string? nameError = ValidateName(request.Name);
        if (nameError is not null)
            return AuthOperationResult<AdminPlanResponse>.Fail("validation", nameError, "Name");
        if (request.Description is not null && request.Description.Trim().Length > MaxDescriptionLength)
            return AuthOperationResult<AdminPlanResponse>.Fail("validation", "Mô tả tối đa 1000 ký tự.", "Description");
        string? priceError = ValidateNumbers(request.PriceMonthly, request.MaxProjects, request.MaxMembers, request.AiRequestLimit);
        if (priceError is not null)
            return AuthOperationResult<AdminPlanResponse>.Fail("validation", priceError);

        string code = request.Code!.Trim().ToUpperInvariant();
        try
        {
            if (await repository.GetByCodeAsync(code, cancellationToken) is not null)
                return AuthOperationResult<AdminPlanResponse>.Fail("duplicate_code", "Mã gói đã tồn tại.", "Code", status: 409);

            Plan created = await repository.AddAsync(new Plan
            {
                Code = code,
                Name = request.Name!.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                PriceMonthly = request.PriceMonthly,
                MaxProjects = request.MaxProjects,
                MaxMembers = request.MaxMembers,
                AiRequestLimit = request.AiRequestLimit,
                IsActive = true,
            }, cancellationToken);
            logger.LogInformation("Admin created plan. PlanId: {PlanId}; Code: {Code}", created.PlanId, created.Code);
            return new(ToResponse(created));
        }
        catch (Exception exception) when (AuthRules.IsDatabaseError(exception, cancellationToken))
        {
            return AuthOperationResult<AdminPlanResponse>.Fail("service_unavailable", "Không thể tạo gói lúc này.", status: 503);
        }
    }

    public async Task<AuthOperationResult<AdminPlanResponse>> UpdatePlanAsync(long planId, UpdatePlanRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.User is not { IsSystemAdmin: true })
            return AuthOperationResult<AdminPlanResponse>.Fail("forbidden", "Bạn không có quyền quản trị hệ thống.", status: 403);

        try
        {
            Plan? existing = await repository.GetByIdAsync(planId, cancellationToken);
            if (existing is null)
                return AuthOperationResult<AdminPlanResponse>.Fail("not_found", "Không tìm thấy gói dịch vụ.", status: 404);

            // Field null = giữ nguyên giá trị hiện tại.
            Plan updated = existing with
            {
                PriceMonthly = request.PriceMonthly ?? existing.PriceMonthly,
                MaxProjects = request.MaxProjects ?? existing.MaxProjects,
                MaxMembers = request.MaxMembers ?? existing.MaxMembers,
                AiRequestLimit = request.AiRequestLimit ?? existing.AiRequestLimit,
                IsActive = request.IsActive ?? existing.IsActive,
            };

            string? numberError = ValidateNumbers(updated.PriceMonthly, updated.MaxProjects,
                updated.MaxMembers, updated.AiRequestLimit);
            if (numberError is not null)
                return AuthOperationResult<AdminPlanResponse>.Fail("validation", numberError);

            Plan? saved = await repository.UpdateAsync(updated, cancellationToken);
            if (saved is null)
                return AuthOperationResult<AdminPlanResponse>.Fail("not_found", "Không tìm thấy gói dịch vụ.", status: 404);
            logger.LogInformation("Admin updated plan. PlanId: {PlanId}; IsActive: {IsActive}", saved.PlanId, saved.IsActive);
            return new(ToResponse(saved));
        }
        catch (Exception exception) when (AuthRules.IsDatabaseError(exception, cancellationToken))
        {
            return AuthOperationResult<AdminPlanResponse>.Fail("service_unavailable", "Không thể cập nhật gói lúc này.", status: 503);
        }
    }

    private static string? ValidateCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return "Mã gói là bắt buộc.";
        string trimmed = code.Trim();
        if (trimmed.Length > MaxCodeLength) return "Mã gói tối đa 50 ký tự.";
        if (!trimmed.All(c => char.IsLetterOrDigit(c) || c is '_' or '-' or '.'))
            return "Mã gói chỉ gồm chữ, số và các ký tự _ - .";
        return null;
    }

    private static string? ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Tên gói là bắt buộc.";
        return name.Trim().Length > MaxNameLength ? "Tên gói tối đa 100 ký tự." : null;
    }

    private static string? ValidateNumbers(decimal price, int? maxProjects, int? maxMembers, int? aiRequestLimit)
    {
        if (price < 0) return "Giá gói không được âm.";
        if (maxProjects is < 0) return "Số project tối đa không được âm.";
        if (maxMembers is < 0) return "Số thành viên tối đa không được âm.";
        if (aiRequestLimit is < 0) return "Giới hạn lượt AI không được âm.";
        return null;
    }

    private static AdminPlanResponse ToResponse(Plan plan) => new(
        plan.PlanId, plan.Code, plan.Name, plan.Description, plan.PriceMonthly,
        plan.MaxProjects, plan.MaxMembers, plan.AiRequestLimit, plan.IsActive, plan.CreatedAt);
}
