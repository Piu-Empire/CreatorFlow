namespace CreatorFlow.AI.Models;

public sealed class AiResult
{
    public bool Success { get; init; }

    public string? Content { get; init; }

    public string? ErrorMessage { get; init; }

    public int? StatusCode { get; init; }
}