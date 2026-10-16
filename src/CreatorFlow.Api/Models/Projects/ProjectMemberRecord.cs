namespace CreatorFlow.Api.Models.Projects;

public sealed record ProjectMemberRecord(long ProjectId, long UserId, string DisplayName, string Email,
    string Role, DateTimeOffset JoinedAt, bool IsActive = true);

public sealed record ProjectMembersRecord(string ActorRole, IReadOnlyList<ProjectMemberRecord> Members);
