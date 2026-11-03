using CreatorFlow.Contracts.Admin;

namespace CreatorFlow.Services;

/// <summary>Kết quả tải danh sách gói dịch vụ cho màn quản trị.</summary>
public sealed record AdminPlansResult(
    bool Succeeded, string? ErrorMessage, AdminPlansListResponse? Response = null)
{
    public static AdminPlansResult Success(AdminPlansListResponse response) => new(true, null, response);
    public static AdminPlansResult Failure(string message) => new(false, message);
}

/// <summary>Kết quả tạo/sửa gói dịch vụ.</summary>
public sealed record AdminPlanSaveResult(
    bool Succeeded, string? ErrorMessage, AdminPlanResponse? Response = null)
{
    public static AdminPlanSaveResult Success(AdminPlanResponse response) => new(true, null, response);
    public static AdminPlanSaveResult Failure(string message) => new(false, message);
}

/// <summary>Kết quả tải danh sách đăng ký gói.</summary>
public sealed record AdminSubscriptionsResult(
    bool Succeeded, string? ErrorMessage, AdminSubscriptionsListResponse? Response = null)
{
    public static AdminSubscriptionsResult Success(AdminSubscriptionsListResponse response) => new(true, null, response);
    public static AdminSubscriptionsResult Failure(string message) => new(false, message);
}

/// <summary>Kết quả gán/sửa đăng ký gói.</summary>
public sealed record AdminSubscriptionSaveResult(
    bool Succeeded, string? ErrorMessage, AdminSubscriptionResponse? Response = null)
{
    public static AdminSubscriptionSaveResult Success(AdminSubscriptionResponse response) => new(true, null, response);
    public static AdminSubscriptionSaveResult Failure(string message) => new(false, message);
}

/// <summary>Kết quả tải thống kê hệ thống.</summary>
public sealed record AdminStatisticsResult(
    bool Succeeded, string? ErrorMessage, AdminStatisticsResponse? Response = null)
{
    public static AdminStatisticsResult Success(AdminStatisticsResponse response) => new(true, null, response);
    public static AdminStatisticsResult Failure(string message) => new(false, message);
}
