using System.Net;

namespace CreatorFlow.ApiClients;

public sealed class ApiBusinessException(HttpStatusCode status, string code, string message,
    string? field = null, Guid? requestId = null) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = status;
    public string Code { get; } = code;
    public string? ErrorField { get; } = field;
    public Guid? RequestId { get; } = requestId;
}
