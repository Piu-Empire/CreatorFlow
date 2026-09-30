using CreatorFlow.Models.Enums;

namespace CreatorFlow.Models;

public sealed record ContentFilter
{
    public ContentStatus? Status { get; init; }
}
