namespace CreatorFlow.Contracts.Projects;

public sealed record CreateProjectRequest(string? Name, string? Description);
public sealed record UpdateProjectRequest(string? Name, string? Description);
public sealed record ProjectListItem(long ProjectId, string Name, string CurrentUserRole, string Status);
public sealed record ProjectListResponse(IReadOnlyList<ProjectListItem> Items);
public sealed record ProjectAccessResponse(long ProjectId, string Name, string CurrentUserRole, string Status);
public sealed record ProjectResponse(long ProjectId, string Name, string? Description, string OwnerName,
    string CurrentUserRole, string Status, DateTimeOffset CreatedAt);
public sealed record ProjectMutationResponse(long ProjectId, bool Deleted);
public sealed record ProjectMembersResponse(IReadOnlyList<ProjectMemberResponse> Members);
public sealed record ProjectMemberResponse(long UserId, string DisplayName, string Email, string Role, DateTimeOffset JoinedAt);
public sealed record UpdateProjectMemberRoleRequest(string? Role);
public sealed record ProjectMemberMutationResponse(long ProjectId, long UserId, string? Role, bool Removed);
