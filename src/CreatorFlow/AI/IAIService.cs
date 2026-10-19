using CreatorFlow.AI.Models;

namespace CreatorFlow.AI;

public interface IAIService
{
    Task<AiResult> AskAsync(
        string prompt,
        CancellationToken cancellationToken = default);
}