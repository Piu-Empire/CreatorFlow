using CreatorFlow.Api.ErrorHandling;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.Api;

[TestClass]
[TestCategory("ApiFoundation")]
public sealed class ApiExceptionHandlerTests
{
    [TestMethod]
    public async Task ServerError_WritesSafeProblemDetailsEvenWithoutMatchingWriter()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        using ServiceProvider provider = services.BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = provider, TraceIdentifier = "test-trace" };
        using var body = new MemoryStream();
        context.Response.Body = body;
        var handler = new ApiExceptionHandler(new NoWriter(), NullLogger<ApiExceptionHandler>.Instance);

        Assert.IsTrue(await handler.TryHandleAsync(context, new Exception("private-server-secret"), CancellationToken.None));
        Assert.AreEqual(500, context.Response.StatusCode);
        body.Position = 0;
        using var reader = new StreamReader(body);
        string json = await reader.ReadToEndAsync();
        Assert.IsTrue(json.Contains("test-trace"));
        Assert.IsFalse(json.Contains("private-server-secret"));
        Assert.IsFalse(json.Contains("stackTrace"));
    }

    [TestMethod]
    public async Task AbortedRequest_DoesNotWriteServerError()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var context = new DefaultHttpContext { RequestAborted = cancellation.Token };
        var handler = new ApiExceptionHandler(new NoWriter(), NullLogger<ApiExceptionHandler>.Instance);
        Assert.IsTrue(await handler.TryHandleAsync(context, new OperationCanceledException(), cancellation.Token));
        Assert.AreNotEqual(500, context.Response.StatusCode);
    }

    private sealed class NoWriter : IProblemDetailsService
    {
        public ValueTask WriteAsync(ProblemDetailsContext context) => ValueTask.CompletedTask;
        public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context) => ValueTask.FromResult(false);
    }
}
