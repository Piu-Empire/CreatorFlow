namespace CreatorFlow.AI;

public static class AIFactory
{
    public static AIOptions FromEnvironment(
        int timeoutSeconds = 30)
    {
        DotEnv.Load();

        return new AIOptions
        {
            ApiKey =
                Environment.GetEnvironmentVariable("AI_API_KEY")
                ?? string.Empty,
            Endpoint =
                Environment.GetEnvironmentVariable("AI_ENDPOINT")
                ?? string.Empty,
            Model =
                Environment.GetEnvironmentVariable("AI_MODEL")
                ?? string.Empty,
            TimeoutSeconds = timeoutSeconds
        };
    }

    public static IAIService Create(
        HttpClient? httpClient = null,
        int timeoutSeconds = 30)
    {
        return new AIService(
            httpClient ?? new HttpClient(),
            FromEnvironment(timeoutSeconds));
    }
}
