namespace CreatorFlow.Models;

public sealed record ContentReference(
    long ReferenceId,
    long ContentId,
    string DisplayName,
    string ResourceUrl,
    long? AddedBy,
    DateTime CreatedAt);

public sealed record ContentReferenceList(long ContentId, bool CanManage, IReadOnlyList<ContentReference> References);

public sealed record ContentReferenceAddition(string DisplayName, string ResourceUrl);
