using CreatorFlow.Contracts.Health;

namespace CreatorFlow.Api.Services;

public sealed class HealthService
{
    public HealthResponse GetHealth() => new("ok");
}
