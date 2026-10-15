namespace CreatorFlow.Api.Models.Ideas;

public enum IdeaStatus { Draft, Backlog, Converted, Archived }

public sealed record Idea(
    long IdeaId,
    long ProjectId,
    string Title,
    string Description,
    string Note,
    IdeaStatus Status,
    IReadOnlyList<string> Tags,
    long CreatedByUserId,
    string CreatedByName,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    long? ConvertedContentId);

public sealed record IdeaDraft(string Title, string Description, string Note, IdeaStatus Status, IReadOnlyList<string> Tags);

public sealed record IdeaFilter(string SearchText, IdeaStatus? Status, string? Tag);
