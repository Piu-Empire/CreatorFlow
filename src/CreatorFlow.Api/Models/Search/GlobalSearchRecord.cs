namespace CreatorFlow.Api.Models.Search;

public sealed record GlobalSearchRecord(
    string Type,
    long EntityId,
    long ProjectId,
    string ProjectName,
    string Title,
    string Subtitle,
    string Status);
