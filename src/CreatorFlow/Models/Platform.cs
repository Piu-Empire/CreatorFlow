namespace CreatorFlow.Models;

/// <summary>Một dòng của bảng platforms — nguồn thật của danh sách nền tảng đăng bài.</summary>
public sealed record Platform
{
    public long PlatformId { get; init; }

    public required string Code { get; init; }

    public required string Name { get; init; }

    public bool IsActive { get; init; } = true;
}
