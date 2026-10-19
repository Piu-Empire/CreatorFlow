namespace CreatorFlow.Contracts.Search;

/// <summary>Một kết quả tìm kiếm có thể mở được trong workspace CreatorFlow.</summary>
public sealed record GlobalSearchItemResponse(
    string Type,
    long EntityId,
    long ProjectId,
    string ProjectName,
    string Title,
    string Subtitle,
    string Status);

public sealed record GlobalSearchResponse(IReadOnlyList<GlobalSearchItemResponse> Items);
