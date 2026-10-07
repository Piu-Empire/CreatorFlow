using System.Net;

namespace CreatorFlow.ApiClients;

public enum ApiErrorKind
{
    Http,
    Network,
    Timeout,
    InvalidResponse
}

public sealed class ApiException(
    ApiErrorKind kind, string message, HttpStatusCode? statusCode = null) : Exception(message)
{
    public ApiErrorKind Kind { get; } = kind;
    public HttpStatusCode? StatusCode { get; } = statusCode;
}
