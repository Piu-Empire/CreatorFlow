using CreatorFlow.ApiClients;
using CreatorFlow.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.Api;

[TestClass]
[TestCategory("ApiFoundation")]
public sealed class FoundationConfigurationTests
{
    [TestMethod]
    public void DatabaseConfiguration_MissingConnectionStringFailsClearly()
    {
        IConfiguration config = new ConfigurationBuilder().Build();
        InvalidOperationException error = Assert.ThrowsExactly<InvalidOperationException>(
            () => DatabaseConfiguration.GetConnectionString(config));
        Assert.IsTrue(error.Message.Contains("ConnectionStrings__CreatorFlow"));
    }

    [TestMethod]
    public void DatabaseConfiguration_MalformedConnectionStringDoesNotLeakValue()
    {
        IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:CreatorFlow"] = "UnsupportedSetting=private-config-value"
        }).Build();
        InvalidOperationException error = Assert.ThrowsExactly<InvalidOperationException>(
            () => DatabaseConfiguration.GetConnectionString(config));
        Assert.IsFalse(error.Message.Contains("private-config-value"));
        Assert.IsNull(error.InnerException);
    }

    [TestMethod]
    public void DatabaseConfiguration_UsesHostConfigurationPrecedenceWithoutConnecting()
    {
        const string expected = "Host=localhost;Database=foundation_test";
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:CreatorFlow"] = "" })
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:CreatorFlow"] = expected })
            .Build();
        Assert.AreEqual(expected, DatabaseConfiguration.GetConnectionString(config));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("relative/url")]
    [DataRow("file:///tmp/api")]
    [DataRow("https://user:private@example.test/")]
    [DataRow("https://example.test/?query=value")]
    public void ApiConfiguration_RejectsInvalidBaseUrlWithoutEchoingIt(string baseUrl)
    {
        InvalidOperationException error = Assert.ThrowsExactly<InvalidOperationException>(
            () => new ApiClientConfiguration(baseUrl));
        Assert.IsFalse(error.Message.Contains("private"));
    }

    [TestMethod]
    [DataRow("zero")]
    [DataRow("0")]
    [DataRow("-1")]
    [DataRow("NaN")]
    [DataRow("Infinity")]
    public void ApiConfiguration_RejectsInvalidTimeout(string timeout)
    {
        IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Api:BaseUrl"] = "https://example.test/backend",
            ["Api:TimeoutSeconds"] = timeout
        }).Build();
        Assert.ThrowsExactly<InvalidOperationException>(() => ApiClientConfiguration.FromConfiguration(config));
    }

    [TestMethod]
    public void ApiConfiguration_DefaultTimeoutAndTrailingSlashAreUsable()
    {
        IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Api:BaseUrl"] = "https://example.test/backend"
        }).Build();
        var actual = ApiClientConfiguration.FromConfiguration(config);
        Assert.AreEqual("https://example.test/backend/", actual.BaseUrl.AbsoluteUri);
        Assert.AreEqual(TimeSpan.FromSeconds(30), actual.Timeout);
    }
}
