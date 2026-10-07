using CreatorFlow.Models.Enums;

namespace CreatorFlow.Models;

/// <summary>Một dòng của bảng content_platforms: Content này được đăng lên nền tảng nào và trạng thái đăng.</summary>
public sealed record ContentPlatform
{
    public long ContentPlatformId { get; init; }

    public long ContentId { get; init; }

    public long PlatformId { get; init; }

    public required string PlatformName { get; init; }

    public PublicationStatus PublicationStatus { get; init; } = PublicationStatus.Planned;

    /// <summary>Link bài đăng thực tế trên nền tảng (null khi chưa đăng).</summary>
    public string? PostUrl { get; init; }

    public DateTime? PlannedPublishAt { get; init; }

    public DateTime? PublishedAt { get; init; }
}