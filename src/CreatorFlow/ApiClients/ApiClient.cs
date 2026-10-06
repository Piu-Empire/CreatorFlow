using System.Net;
using System.Net.Http;
using System.Text.Json;
using CreatorFlow.Contracts.Health;

namespace CreatorFlow.ApiClients;

public sealed class ApiClient : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient httpClient;
    private readonly bool ownsHttpClient;

    // An injected HttpClient remains owned by the caller.
    public ApiClient(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        this.httpClient = httpClient;
    }

    private ApiClient(HttpClient httpClient, bool ownsHttpClient) : this(httpClient)
    {
        this.ownsHttpClient = ownsHttpClient;
    }

    // Create once per application lifecycle, then dispose when that lifecycle ends.
    public static ApiClient Create(ApiClientConfiguration configuration)
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            AllowAutoRedirect = false
        };
        var client = new HttpClient(handler)
        {
            BaseAddress = configuration.BaseUrl,
            Timeout = configuration.Timeout
        };
        return new ApiClient(client, ownsHttpClient: true);
    }

    public async Task<HealthResponse> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using HttpResponseMessage response = await httpClient.GetAsync("api/health", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new ApiException(ApiErrorKind.Http, GetErrorMessage(response.StatusCode), response.StatusCode);
            }

            byte[] json = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            HealthResponse? health = JsonSerializer.Deserialize<HealthResponse>(json, JsonOptions);
            if (health is null || health.Status != "ok")
            {
                throw new ApiException(ApiErrorKind.InvalidResponse, "The API returned an invalid health response.");
            }
            return health;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw new ApiException(ApiErrorKind.Timeout, "The API request timed out.");
        }
        catch (HttpRequestException)
        {
            throw new ApiException(ApiErrorKind.Network, "The API could not be reached.");
        }
        catch (JsonException)
        {
            throw new ApiException(ApiErrorKind.InvalidResponse, "The API returned an invalid JSON response.");
        }
    }

    private static string GetErrorMessage(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.BadRequest => "The API rejected the request.",
        HttpStatusCode.Unauthorized => "Please sign in to continue.",
        HttpStatusCode.Forbidden => "You do not have permission for this operation.",
        HttpStatusCode.NotFound => "The requested API resource was not found.",
        HttpStatusCode.Conflict => "The operation conflicts with the current state.",
        _ => "The API request could not be completed."
    };

    public void Dispose()
    {
        if (ownsHttpClient)
        {
            httpClient.Dispose();
        }
    }
}
