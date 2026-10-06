using System.Net;
using System.Net.Http;
using CreatorFlow.ApiClients;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.ApiClients;

[TestClass]
[TestCategory("ApiFoundation")]
public sealed class ApiClientTests
{
    [TestMethod]
    public async Task GetHealthAsync_PreservesBasePathAndReadsWebJson()
    {
        using var handler = new StubHandler((request, _) =>
        {
            Assert.AreEqual("https://example.test/backend/api/health", request.RequestUri!.AbsoluteUri);
            return Task.FromResult(JsonResponse("{\"status\":\"ok\"}"));
        });
        var configuration = new ApiClientConfiguration("https://example.test/backend");
        using var http = new HttpClient(handler) { BaseAddress = configuration.BaseUrl };
        using var api = new ApiClient(http);
        Assert.AreEqual("ok", (await api.GetHealthAsync()).Status);
    }

    [TestMethod]
    [DataRow(400)]
    [DataRow(401)]
    [DataRow(403)]
    [DataRow(404)]
    [DataRow(409)]
    [DataRow(500)]
    public async Task GetHealthAsync_HttpErrorKeepsStatusWithoutExposingBody(int statusCode)
    {
        using var handler = new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage((HttpStatusCode)statusCode)
        {
            Content = new StringContent("private-server-error")
        }));
        using var http = CreateHttpClient(handler);
        using var api = new ApiClient(http);
        ApiException error = await Assert.ThrowsExactlyAsync<ApiException>(() => api.GetHealthAsync());
        Assert.AreEqual(ApiErrorKind.Http, error.Kind);
        Assert.AreEqual((HttpStatusCode)statusCode, error.StatusCode);
        Assert.IsFalse(error.Message.Contains("private-server-error"));
    }

    [TestMethod]
    [DataRow("not-json")]
    [DataRow("null")]
    [DataRow("{}")]
    [DataRow("{\"status\":\"unhealthy\"}")]
    public async Task GetHealthAsync_InvalidResponseIsClassified(string json)
    {
        using var handler = new StubHandler((_, _) => Task.FromResult(JsonResponse(json)));
        using var http = CreateHttpClient(handler);
        using var api = new ApiClient(http);
        ApiException error = await Assert.ThrowsExactlyAsync<ApiException>(() => api.GetHealthAsync());
        Assert.AreEqual(ApiErrorKind.InvalidResponse, error.Kind);
    }

    [TestMethod]
    public async Task GetHealthAsync_NetworkFailureDoesNotExposeExceptionMessage()
    {
        using var handler = new StubHandler((_, _) => throw new HttpRequestException("private-network-detail"));
        using var http = CreateHttpClient(handler);
        using var api = new ApiClient(http);
        ApiException error = await Assert.ThrowsExactlyAsync<ApiException>(() => api.GetHealthAsync());
        Assert.AreEqual(ApiErrorKind.Network, error.Kind);
        Assert.IsFalse(error.Message.Contains("private-network-detail"));
    }

    [TestMethod]
    public async Task GetHealthAsync_CallerCancellationIsNotTimeout()
    {
        using var cancellation = new CancellationTokenSource();
        using var handler = new StubHandler(async (_, token) =>
        {
            cancellation.Cancel();
            await Task.Delay(Timeout.Infinite, token);
            return JsonResponse("{}");
        });
        using var http = CreateHttpClient(handler);
        using var api = new ApiClient(http);
        try
        {
            await api.GetHealthAsync(cancellation.Token);
            Assert.Fail("Expected caller cancellation.");
        }
        catch (OperationCanceledException)
        {
            Assert.IsTrue(cancellation.IsCancellationRequested);
        }
    }

    [TestMethod]
    public async Task GetHealthAsync_HttpTimeoutIsClassifiedSeparately()
    {
        using var handler = new StubHandler(async (_, token) =>
        {
            await Task.Delay(Timeout.Infinite, token);
            return JsonResponse("{}");
        });
        using var http = CreateHttpClient(handler);
        http.Timeout = TimeSpan.FromMilliseconds(50);
        using var api = new ApiClient(http);
        ApiException error = await Assert.ThrowsExactlyAsync<ApiException>(() => api.GetHealthAsync());
        Assert.AreEqual(ApiErrorKind.Timeout, error.Kind);
    }

    [TestMethod]
    public async Task Dispose_DoesNotDisposeInjectedHttpClient()
    {
        using var handler = new StubHandler((_, _) => Task.FromResult(JsonResponse("{\"status\":\"ok\"}")));
        using var http = CreateHttpClient(handler);
        var api = new ApiClient(http);
        api.Dispose();
        using HttpResponseMessage response = await http.GetAsync("api/health");
        Assert.IsTrue(response.IsSuccessStatusCode);
    }

    private static HttpClient CreateHttpClient(HttpMessageHandler handler) =>
        new(handler) { BaseAddress = new Uri("https://example.test/") };

    private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
    };

    private sealed class StubHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            send(request, cancellationToken);
    }
}
