using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CreatorFlow.Api.Endpoints;
using CreatorFlow.Api.Services;
using CreatorFlow.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;

namespace CreatorFlow.Api.Tests.Api;

[TestClass]
[TestCategory("ApiReadiness")]
public sealed class ReadinessTests
{
    [TestMethod]
    public async Task ReadyEndpoint_ProbesEveryRequest_WhileLivenessDoesNotProbeDatabase()
    {
        bool unavailable = false;
        int probes = 0;
        var repository = new ProbeRepository(_ =>
        {
            probes++;
            return unavailable ? Task.FromException(new NpgsqlException("private DB details")) : Task.CompletedTask;
        });
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IDatabaseReadinessRepository>(repository);
        builder.Services.AddScoped<DatabaseReadinessService>();
        builder.Services.AddSingleton<HealthService>();
        await using var app = builder.Build();
        app.MapHealthEndpoints();
        await app.StartAsync();
        using var client = app.GetTestClient();

        using var live = await client.GetAsync("/api/health");
        Assert.AreEqual(HttpStatusCode.OK, live.StatusCode);
        Assert.AreEqual("ok", (await live.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());
        Assert.AreEqual(0, probes);
        using var ready = await client.GetAsync("/api/health/ready");
        Assert.AreEqual(HttpStatusCode.OK, ready.StatusCode);
        Assert.AreEqual("ready", (await ready.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());

        unavailable = true;
        using var failed = await client.GetAsync("/api/health/ready");
        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
        Assert.AreEqual("{\"status\":\"not_ready\"}", await failed.Content.ReadAsStringAsync());
        Assert.AreEqual(2, probes);
        using var stillAlive = await client.GetAsync("/api/health");
        Assert.AreEqual(HttpStatusCode.OK, stillAlive.StatusCode);
        Assert.AreEqual(2, probes);
    }

    [TestMethod]
    public async Task ProbeTimeout_IsBoundedAndCancelsDependency()
    {
        CancellationToken dependencyToken = default;
        var service = Service(new ProbeRepository(token =>
        {
            dependencyToken = token;
            return Task.Delay(Timeout.Infinite, token);
        }), new RecordingLogger());
        Assert.IsFalse(await service.IsReadyAsync().WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.IsTrue(dependencyToken.IsCancellationRequested);
    }

    [TestMethod]
    public async Task CallerCancellation_IsPropagatedInsteadOfReportedAsDatabaseFailure()
    {
        var logger = new RecordingLogger();
        using var caller = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = Service(new ProbeRepository(token =>
        {
            entered.SetResult();
            return Task.Delay(Timeout.Infinite, token);
        }), logger);
        Task<bool> pending = service.IsReadyAsync(caller.Token);
        await entered.Task;
        caller.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await pending);
        Assert.AreEqual(0, logger.Messages.Count);
    }

    [TestMethod]
    public async Task DatabaseFailure_LogsTypeWithoutRawExceptionOrConnectionDetails()
    {
        var logger = new RecordingLogger();
        var service = Service(new ProbeRepository(_ => Task.FromException(new NpgsqlException("Password=PRIVATE; SQL=private query"))), logger);
        Assert.IsFalse(await service.IsReadyAsync());
        Assert.AreEqual(1, logger.Messages.Count);
        StringAssert.Contains(logger.Messages[0], nameof(NpgsqlException));
        Assert.IsFalse(logger.Messages[0].Contains("PRIVATE", StringComparison.Ordinal));
        Assert.IsFalse(logger.Messages[0].Contains("private query", StringComparison.Ordinal));
        Assert.IsNull(logger.Exception);
    }

    private static DatabaseReadinessService Service(IDatabaseReadinessRepository repository, RecordingLogger logger) => new(repository, logger);

    private sealed class ProbeRepository(Func<CancellationToken, Task> probe) : IDatabaseReadinessRepository
    {
        public Task ProbeAsync(CancellationToken cancellationToken) => probe(cancellationToken);
    }

    private sealed class RecordingLogger : ILogger<DatabaseReadinessService>
    {
        public List<string> Messages { get; } = [];
        public Exception? Exception { get; private set; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
            Exception = exception;
        }
    }
}
