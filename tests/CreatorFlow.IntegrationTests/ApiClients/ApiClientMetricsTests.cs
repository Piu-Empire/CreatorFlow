using System.Net;
using CreatorFlow.ApiClients;
using CreatorFlow.Contracts.Metrics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.ApiClients;

[TestClass]
[TestCategory("ApiFoundation")]
public sealed class ApiClientMetricsTests
{
    private const string ListJson = """
        {"contentId":10,"platforms":[
          {"contentPlatformId":100,"platformId":5,"platformName":"TikTok","publicationStatus":"PUBLISHED",
           "canRecord":true,"latest":{"metricId":7,"views":100,"likes":5,"comments":2,"shares":1,
           "recordedAt":"2026-10-12T07:00:00Z","recordedBy":3,"recordedByName":"Manager A"},"history":null},
          {"contentPlatformId":101,"platformId":6,"platformName":"YouTube","publicationStatus":"PLANNED",
           "canRecord":false,"latest":null,"history":null}]}
        """;

    [TestMethod]
    public async Task GetContentMetrics_DeserializesLatestAndNull()
    {
        using var handler = new StubHandler((request, _) =>
        {
            Assert.AreEqual(HttpMethod.Get, request.Method);
            Assert.AreEqual("https://example.test/api/contents/10/metrics", request.RequestUri!.AbsoluteUri);
            Assert.AreEqual("Bearer", request.Headers.Authorization?.Scheme);
            return Task.FromResult(JsonResponse(ListJson));
        });
        using var api = new ApiClient(CreateHttpClient(handler));

        ContentMetricsResponse response = await api.GetContentMetricsAsync(10, "token");

        Assert.AreEqual(10, response.ContentId);
        Assert.AreEqual(2, response.Platforms.Count);
        Assert.AreEqual(100, response.Platforms[0].Latest!.Views);
        Assert.AreEqual("Manager A", response.Platforms[0].Latest?.RecordedByName);
        Assert.IsNull(response.Platforms[1].Latest); // chưa có số liệu vẫn hợp lệ
    }

    [TestMethod]
    public async Task GetPlatformMetrics_HistoryFlag_ControlsQuery()
    {
        string? seen = null;
        using var handler = new StubHandler((request, _) =>
        {
            seen = request.RequestUri!.AbsoluteUri;
            return Task.FromResult(JsonResponse(
                """{"contentPlatformId":100,"platformId":5,"platformName":"TikTok","publicationStatus":"PUBLISHED","canRecord":true,"latest":null,"history":[]}"""));
        });
        using var api = new ApiClient(CreateHttpClient(handler));

        await api.GetPlatformMetricsAsync(10, 5, true, "token");
        Assert.IsTrue(seen!.EndsWith("/api/contents/10/platforms/5/metrics?history=true"));
        await api.GetPlatformMetricsAsync(10, 5, false, "token");
        Assert.IsTrue(seen.EndsWith("/api/contents/10/platforms/5/metrics"));
    }

    [TestMethod]
    public async Task RecordMetric_PostsBodyAndReadsSnapshot()
    {
        string? body = null;
        using var handler = new StubHandler(async (request, _) =>
        {
            Assert.AreEqual(HttpMethod.Post, request.Method);
            body = await request.Content!.ReadAsStringAsync();
            return JsonResponse(
                """{"contentPlatformId":100,"platformId":5,"platformName":"TikTok","publicationStatus":"PUBLISHED","canRecord":true,"latest":{"metricId":8,"views":100,"likes":5,"comments":0,"shares":0,"recordedAt":"2026-10-12T08:00:00Z","recordedBy":3,"recordedByName":"Manager A"},"history":null}""");
        });
        using var api = new ApiClient(CreateHttpClient(handler));

        PlatformMetricsResponse response = await api.RecordMetricAsync(10, 5, new RecordMetricRequest(100, 5, null, null), "token");

        Assert.IsTrue(body!.Contains("\"views\":100"));
        Assert.AreEqual(8, response.Latest!.MetricId);
        Assert.AreEqual(100, response.Latest.Views);
    }

    [TestMethod]
    [DataRow(409, "content_locked")]
    [DataRow(403, "forbidden")]
    [DataRow(400, "validation")]
    public async Task RecordMetric_BusinessError_KeepsCode(int status, string code)
    {
        using var handler = new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage((HttpStatusCode)status)
        {
            Content = new StringContent("{\"code\":\"" + code + "\",\"title\":\"Lỗi nghiệp vụ.\"}",
                System.Text.Encoding.UTF8, "application/problem+json")
        }));
        using var api = new ApiClient(CreateHttpClient(handler));

        ApiBusinessException error = await Assert.ThrowsExactlyAsync<ApiBusinessException>(
            () => api.RecordMetricAsync(10, 5, new RecordMetricRequest(1, null, null, null), "token"));

        Assert.AreEqual(code, error.Code);
        Assert.AreEqual((HttpStatusCode)status, error.StatusCode);
        Assert.AreEqual("Lỗi nghiệp vụ.", error.Message);
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
