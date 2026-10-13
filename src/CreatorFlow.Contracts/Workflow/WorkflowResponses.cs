namespace CreatorFlow.Contracts.Workflow;

/// <summary>Trạng thái Content sau khi chuyển.</summary>
public sealed record ContentStatusResponse(long ContentId, string Status, DateTime UpdatedAt);

/// <summary>Một dòng lịch sử đổi trạng thái (mới nhất trước).</summary>
public sealed record StatusHistoryEntryResponse(
    long HistoryId,
    string? FromStatus,
    string ToStatus,
    long? ChangedBy,
    string? ChangedByName,
    string? Note,
    DateTime ChangedAt);

/// <summary>Lịch sử đổi trạng thái của một Content.</summary>
public sealed record StatusHistoryResponse(long ContentId, IReadOnlyList<StatusHistoryEntryResponse> Entries);
