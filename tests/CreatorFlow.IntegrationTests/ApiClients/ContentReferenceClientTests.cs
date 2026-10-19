using System.Net;
using System.Net.Http.Json;
using CreatorFlow.ApiClients;
using CreatorFlow.Contracts.ContentDetail;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.ApiClients;

[TestClass]
[TestCategory("ApiFoundation")]
public sealed class ContentReferenceClientTests
{
    [TestMethod]
    public async Task References_GetAddDeleteUseContentScopedRoutes()
    {
        int call = 0;
        using var handler = new StubHandler(async (request, _) =>
        {
            Assert.AreEqual("Bearer", request.Headers.Authorization?.Scheme);
            call++;
            if (call == 1)
            {
                Assert.AreEqual(HttpMethod.Get, request.Method);
                Assert.AreEqual("https://example.test/api/contents/10/references", request.RequestUri!.AbsoluteUri);
                return JsonResponse("""{"contentId":10,"canManage":true,"references":[]}""");
            }
            if (call == 2)
            {
                Assert.AreEqual(HttpMethod.Post, request.Method);
                Assert.AreEqual("https://example.test/api/contents/10/references", request.RequestUri!.AbsoluteUri);
                string body = await request.Content!.ReadAsStringAsync();
                StringAssert.Contains(body, "\"displayName\":\"Brief\"");
                StringAssert.Contains(body, "\"resourceUrl\":\"https://example.test/brief.pdf\"");
                return JsonResponse("""{"referenceId":7,"contentId":10,"displayName":"Brief","resourceUrl":"https://example.test/brief.pdf","addedBy":3,"createdAt":"2026-10-19T07:00:00Z"}""");
            }

            Assert.AreEqual(HttpMethod.Delete, request.Method);
            Assert.AreEqual("https://example.test/api/contents/10/references/7", request.RequestUri!.AbsoluteUri);
            return JsonResponse("""{"contentId":10,"referenceId":7}""");
        });
        using var api = new ApiClient(new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") });

        ContentReferencesResponse list = await api.GetContentReferencesAsync(10, "token");
        ContentReferenceResponse added = await api.AddContentReferenceAsync(10,
            new CreateContentReferenceRequest("Brief", "https://example.test/brief.pdf"), "token");
        DeleteContentReferenceResponse deleted = await api.DeleteContentReferenceAsync(10, 7, "token");

        Assert.IsTrue(list.CanManage);
        Assert.AreEqual(7, added.ReferenceId);
        Assert.AreEqual(7, deleted.ReferenceId);
        Assert.AreEqual(3, call);
    }

    [TestMethod]
    public async Task References_SaveChangesUsesPutAndSendsPendingMutationsTogether()
    {
        using var handler = new StubHandler(async (request, _) =>
        {
            Assert.AreEqual(HttpMethod.Put, request.Method);
            Assert.AreEqual("https://example.test/api/contents/10/references", request.RequestUri!.AbsoluteUri);
            Assert.AreEqual("Bearer", request.Headers.Authorization?.Scheme);
            string body = await request.Content!.ReadAsStringAsync();
            StringAssert.Contains(body, "\"additions\":[{\"displayName\":\"Brief\",\"resourceUrl\":\"https://example.test/brief.pdf\"}]");
            StringAssert.Contains(body, "\"deletedReferenceIds\":[7]");
            return JsonResponse("""{"contentId":10,"canManage":true,"references":[]}""");
        });
        using var api = new ApiClient(new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") });

        ContentReferencesResponse saved = await api.SaveContentReferenceChangesAsync(10,
            new SaveContentReferenceChangesRequest(
                [new CreateContentReferenceRequest("Brief", "https://example.test/brief.pdf")], [7]), "token");

        Assert.AreEqual(10, saved.ContentId);
        Assert.IsTrue(saved.CanManage);
        Assert.AreEqual(0, saved.References.Count);
    }

    [TestMethod]
    public async Task References_SaveChangesPreservesMissingReferenceBusinessError()
    {
        using var handler = new StubHandler((request, _) =>
        {
            Assert.AreEqual(HttpMethod.Put, request.Method);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = JsonContent.Create(new { code = "reference_not_found", title = "Reference no longer exists." })
            });
        });
        using var api = new ApiClient(new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") });

        ApiBusinessException error = await Assert.ThrowsExactlyAsync<ApiBusinessException>(() =>
            api.SaveContentReferenceChangesAsync(10,
                new SaveContentReferenceChangesRequest([], [7]), "token"));

        Assert.AreEqual(HttpStatusCode.NotFound, error.StatusCode);
        Assert.AreEqual("reference_not_found", error.Code);
    }

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
