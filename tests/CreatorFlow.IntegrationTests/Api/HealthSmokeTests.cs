using System.Net;
using System.Net.Http;
using CreatorFlow.ApiClients;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.Api;

[TestClass]
[TestCategory("ApiLive")]
public sealed class HealthSmokeTests
{
    [TestMethod]
    public async Task WinFormsApiClient_CallsLiveHealthEndpoint()
    {
        using var api = ApiClient.Create(GetConfiguration());
        Assert.AreEqual("ok", (await api.GetHealthAsync()).Status);
    }

    [TestMethod]
    public async Task UnknownApiRoute_ReturnsNotFound()
    {
        var configuration = GetConfiguration();
        using var handler = new SocketsHttpHandler { AllowAutoRedirect = false };
        using var http = new HttpClient(handler)
        {
            BaseAddress = configuration.BaseUrl,
            Timeout = configuration.Timeout
        };
        using HttpResponseMessage response = await http.GetAsync("api/foundation-route-that-does-not-exist");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static ApiClientConfiguration GetConfiguration()
    {
        string? url = Environment.GetEnvironmentVariable("CREATORFLOW_TEST_API_BASE_URL");
        if (string.IsNullOrWhiteSpace(url))
        {
            Assert.Inconclusive("Set CREATORFLOW_TEST_API_BASE_URL to an approved running local API for live smoke tests.");
        }
        var configuration = new ApiClientConfiguration(url!);
        if (!configuration.BaseUrl.IsLoopback)
        {
            Assert.Inconclusive("Foundation live smoke tests require a local loopback API.");
        }
        return configuration;
    }
}
