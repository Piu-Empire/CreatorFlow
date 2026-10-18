namespace CreatorFlow.Contracts.Ideas;

/// <summary>Thêm / sửa Idea. Status là tên enum PascalCase: "Draft", "Backlog", "Archived" ("Converted" chỉ do API đặt khi chuyển Idea thành Content).</summary>
public sealed record IdeaRequest(string Title, string? Description, string? Note, string Status, IReadOnlyList<string>? Tags);

/// <summary>Một Idea trong kho ý tưởng của Project. Status là tên enum PascalCase.</summary>
public sealed record IdeaResponse(
    long IdeaId,
    long ProjectId,
    string Title,
    string Description,
    string Note,
    string Status,
    IReadOnlyList<string> Tags,
    long CreatedByUserId,
    string CreatedByName,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    long? ConvertedContentId);

/// <summary>Danh sách Idea đã lọc kèm vai trò của người gọi trong Project (client dùng để bật/tắt nút).</summary>
public sealed record IdeasResponse(IReadOnlyList<IdeaResponse> Ideas, string MyRole);

public sealed record IdeaTagsResponse(IReadOnlyList<string> Tags);

public sealed record ConvertIdeaResponse(long IdeaId, long ContentId);

public sealed record DeleteIdeaResponse(long IdeaId);
