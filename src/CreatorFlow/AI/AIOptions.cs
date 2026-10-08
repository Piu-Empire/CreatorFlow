namespace CreatorFlow.AI;

public sealed class AIOptions
{
    public string ApiKey { get; init; } = string.Empty;

    public string Endpoint { get; init; } = string.Empty;

    public string Model { get; init; } = string.Empty;

    public int TimeoutSeconds { get; init; } = 30;

    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            return "Chưa cấu hình AI API key.";
        }

        if (string.IsNullOrWhiteSpace(Endpoint))
        {
            return "Chưa cấu hình AI endpoint.";
        }

        if (!Uri.TryCreate(
                Endpoint,
                UriKind.Absolute,
                out _))
        {
            return "AI endpoint không phải URL hợp lệ.";
        }

        if (string.IsNullOrWhiteSpace(Model))
        {
            return "Chưa cấu hình AI model.";
        }

        if (TimeoutSeconds <= 0)
        {
            return "TimeoutSeconds phải > 0.";
        }

        return null;
    }
}