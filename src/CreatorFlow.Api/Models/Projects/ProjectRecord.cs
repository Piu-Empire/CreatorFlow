namespace CreatorFlow.Api.Models.Projects;

public sealed record ProjectRecord(long ProjectId, string Name, string? Description, long OwnerId,
    string OwnerName, string Role, string Status, DateTimeOffset CreatedAt);
