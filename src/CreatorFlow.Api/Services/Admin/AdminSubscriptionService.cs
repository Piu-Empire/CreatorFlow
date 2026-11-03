using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Models.Admin;
using CreatorFlow.Api.Repositories.Admin;
using CreatorFlow.Api.Services.Auth;
using CreatorFlow.Contracts.Admin;
using CreatorFlow.Models;
using CreatorFlow.Repositories;

namespace CreatorFlow.Api.Services.Admin;

/// <summary>
/// SCRUM-23: gán và quản lý đăng ký gói. Chỉ System Admin; không dùng ProjectRole.
/// Bất biến: mỗi người dùng tối đa một đăng ký ACTIVE.
/// </summary>
public sealed class AdminSubscriptionService(
    ISubscriptionRepository subscriptions,
    IPlanRepository plans,
    CurrentAuthenticatedUser currentUser,
    ILogger<AdminSubscriptionService> logger)
{
    private const int DefaultLimit = 20;
    private const int MaxLimit = 100;
    private const int MaxOffset = 10_000;
    private const int MaxSearchLength = 100;

    private static readonly string[] AllowedStatuses = ["ACTIVE", "EXPIRED", "CANCELLED"];

    public async Task<AuthOperationResult<AdminSubscriptionsListResponse>> GetSubscriptionsAsync(
        string? status, long? planId, string? search, int? limit, int? offset,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.User is not { IsSystemAdmin: true })
            return AuthOperationResult<AdminSubscriptionsListResponse>.Fail("forbidden", "Bạn không có quyền quản trị hệ thống.", status: 403);

        if (search is not null && search.Trim().Length > MaxSearchLength)
            return AuthOperationResult<AdminSubscriptionsListResponse>.Fail("validation", "Từ khóa tìm kiếm không được vượt quá 100 ký tự.", "Search");

        string? normalizedStatus = NormalizeStatus(status);
        if (normalizedStatus is not null && !AllowedStatuses.Contains(normalizedStatus))
            return AuthOperationResult<AdminSubscriptionsListResponse>.Fail("validation", "Trạng thái lọc không hợp lệ.", "Status");
        if (normalizedStatus == "ALL")
            normalizedStatus = null;
        if (planId is <= 0)
            return AuthOperationResult<AdminSubscriptionsListResponse>.Fail("validation", "Gói lọc không hợp lệ.", "PlanId");

        int take = limit ?? DefaultLimit;
        if (limit is < 1)
            return AuthOperationResult<AdminSubscriptionsListResponse>.Fail("validation", "Limit phải lớn hơn 0.", "Limit");
        if (take > MaxLimit)
            take = MaxLimit;
        int skip = offset ?? 0;
        if (skip is < 0 or > MaxOffset)
            return AuthOperationResult<AdminSubscriptionsListResponse>.Fail("validation", "Offset không hợp lệ.", "Offset");

        try
        {
            string? keyword = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
            IReadOnlyList<AdminSubscriptionRecord> items =
                await subscriptions.SearchAsync(normalizedStatus, planId, keyword, take, skip, cancellationToken);
            int total = await subscriptions.CountAsync(normalizedStatus, planId, keyword, cancellationToken);
            return new(new AdminSubscriptionsListResponse([.. items.Select(ToResponse)], total));
        }
        catch (Exception exception) when (AuthRules.IsDatabaseError(exception, cancellationToken))
        {
            return AuthOperationResult<AdminSubscriptionsListResponse>.Fail("service_unavailable", "Không thể tải danh sách đăng ký gói lúc này.", status: 503);
        }
    }

    public async Task<AuthOperationResult<AdminSubscriptionResponse>> CreateSubscriptionAsync(
        CreateSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        if (currentUser.User is not { IsSystemAdmin: true })
            return AuthOperationResult<AdminSubscriptionResponse>.Fail("forbidden", "Bạn không có quyền quản trị hệ thống.", status: 403);
        if (request.UserId <= 0)
            return AuthOperationResult<AdminSubscriptionResponse>.Fail("validation", "Người dùng không hợp lệ.", "UserId");
        if (request.PlanId <= 0)
            return AuthOperationResult<AdminSubscriptionResponse>.Fail("validation", "Gói dịch vụ không hợp lệ.", "PlanId");
        if (request.ExpiresAt is { } expiresAt && expiresAt <= DateTimeOffset.UtcNow)
            return AuthOperationResult<AdminSubscriptionResponse>.Fail("validation", "Hạn dùng phải ở tương lai.", "ExpiresAt");

        try
        {
            if (!await subscriptions.UserExistsAsync(request.UserId, cancellationToken))
                return AuthOperationResult<AdminSubscriptionResponse>.Fail("not_found", "Không tìm thấy người dùng.", status: 404);

            Plan? plan = await plans.GetByIdAsync(request.PlanId, cancellationToken);
            if (plan is null)
                return AuthOperationResult<AdminSubscriptionResponse>.Fail("not_found", "Không tìm thấy gói dịch vụ.", status: 404);
            if (!plan.IsActive)
                return AuthOperationResult<AdminSubscriptionResponse>.Fail("plan_inactive", "Gói này đã tắt, không thể gán mới.", "PlanId", status: 409);

            if (await subscriptions.GetActiveByUserAsync(request.UserId, cancellationToken) is not null)
                return AuthOperationResult<AdminSubscriptionResponse>.Fail("already_active", "Người dùng này đang có gói còn hiệu lực.", status: 409);

            AdminSubscriptionRecord? created = await subscriptions.CreateAsync(request.UserId, request.PlanId,
                request.ExpiresAt, cancellationToken);
            if (created is null)
                return AuthOperationResult<AdminSubscriptionResponse>.Fail("not_found", "Không tìm thấy người dùng hoặc gói dịch vụ.", status: 404);
            logger.LogInformation("Admin assigned plan. SubscriptionId: {SubscriptionId}; UserId: {UserId}; PlanId: {PlanId}",
                created.SubscriptionId, created.UserId, created.PlanId);
            return new(ToResponse(created));
        }
        catch (Exception exception) when (AuthRules.IsDatabaseError(exception, cancellationToken))
        {
            return AuthOperationResult<AdminSubscriptionResponse>.Fail("service_unavailable", "Không thể gán gói lúc này.", status: 503);
        }
    }

    public async Task<AuthOperationResult<AdminSubscriptionResponse>> UpdateSubscriptionAsync(long subscriptionId,
        UpdateSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        if (currentUser.User is not { IsSystemAdmin: true })
            return AuthOperationResult<AdminSubscriptionResponse>.Fail("forbidden", "Bạn không có quyền quản trị hệ thống.", status: 403);

        string? normalizedStatus = NormalizeStatus(request.Status);
        if (normalizedStatus is not null && !AllowedStatuses.Contains(normalizedStatus))
            return AuthOperationResult<AdminSubscriptionResponse>.Fail("validation",
                "Trạng thái không hợp lệ. Chỉ nhận ACTIVE, EXPIRED hoặc CANCELLED.", "Status");
        if (normalizedStatus is null && request.ExpiresAt is null)
            return AuthOperationResult<AdminSubscriptionResponse>.Fail("validation", "Không có thay đổi nào để lưu.");

        try
        {
            AdminSubscriptionRecord? existing = await subscriptions.GetByIdAsync(subscriptionId, cancellationToken);
            if (existing is null)
                return AuthOperationResult<AdminSubscriptionResponse>.Fail("not_found", "Không tìm thấy đăng ký gói.", status: 404);

            if (request.ExpiresAt is { } expiresAt && expiresAt <= existing.StartedAt)
                return AuthOperationResult<AdminSubscriptionResponse>.Fail("validation", "Hạn dùng phải sau ngày bắt đầu.", "ExpiresAt");

            if (normalizedStatus == "ACTIVE" && existing.Status != "ACTIVE")
            {
                if (await subscriptions.GetActiveByUserAsync(existing.UserId, cancellationToken) is not null)
                    return AuthOperationResult<AdminSubscriptionResponse>.Fail("already_active",
                        "Người dùng này đang có gói còn hiệu lực khác.", status: 409);
            }

            AdminSubscriptionRecord? saved = await subscriptions.UpdateAsync(subscriptionId,
                normalizedStatus ?? existing.Status, request.ExpiresAt, cancellationToken);
            if (saved is null)
                return AuthOperationResult<AdminSubscriptionResponse>.Fail("not_found", "Không tìm thấy đăng ký gói.", status: 404);
            logger.LogInformation("Admin updated subscription. SubscriptionId: {SubscriptionId}; Status: {Status}",
                saved.SubscriptionId, saved.Status);
            return new(ToResponse(saved));
        }
        catch (Exception exception) when (AuthRules.IsDatabaseError(exception, cancellationToken))
        {
            return AuthOperationResult<AdminSubscriptionResponse>.Fail("service_unavailable", "Không thể cập nhật đăng ký gói lúc này.", status: 503);
        }
    }

    private static string? NormalizeStatus(string? status) =>
        string.IsNullOrWhiteSpace(status) ? null : status.Trim().ToUpperInvariant();

    private static AdminSubscriptionResponse ToResponse(AdminSubscriptionRecord record) => new(
        record.SubscriptionId, record.UserId, record.UserEmail, record.PlanId,
        record.PlanCode, record.PlanName, record.Status, record.StartedAt, record.ExpiresAt);
}
