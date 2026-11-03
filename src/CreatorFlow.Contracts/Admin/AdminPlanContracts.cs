namespace CreatorFlow.Contracts.Admin;

/// <summary>Một gói dịch vụ hiển thị cho System Admin.</summary>
public sealed record AdminPlanResponse(
    long PlanId,
    string Code,
    string Name,
    string? Description,
    decimal PriceMonthly,
    int? MaxProjects,
    int? MaxMembers,
    int? AiRequestLimit,
    bool IsActive,
    DateTimeOffset CreatedAt);

/// <summary>Danh sách gói dịch vụ (kể cả gói đã tắt).</summary>
public sealed record AdminPlansListResponse(IReadOnlyList<AdminPlanResponse> Items);

/// <summary>Tạo gói mới; Max* null = không giới hạn.</summary>
public sealed record CreatePlanRequest(
    string? Code,
    string? Name,
    string? Description,
    decimal PriceMonthly,
    int? MaxProjects,
    int? MaxMembers,
    int? AiRequestLimit);

/// <summary>Sửa gói; field null = giữ nguyên giá trị hiện tại.</summary>
public sealed record UpdatePlanRequest(
    decimal? PriceMonthly,
    int? MaxProjects,
    int? MaxMembers,
    int? AiRequestLimit,
    bool? IsActive);
