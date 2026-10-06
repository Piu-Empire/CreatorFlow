using Microsoft.Extensions.Configuration;

namespace CreatorFlow.ApiClients;

public sealed class ApiClientConfiguration
{
    public Uri BaseUrl { get; }
    public TimeSpan Timeout { get; }

    public ApiClientConfiguration(string baseUrl, double timeoutSeconds = 30)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri? uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || !string.IsNullOrEmpty(uri.UserInfo)
            || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
        {
            throw new InvalidOperationException("Api:BaseUrl must be an absolute HTTP/HTTPS URL without credentials, query or fragment.");
        }

        if (!double.IsFinite(timeoutSeconds) || timeoutSeconds <= 0
            || timeoutSeconds > int.MaxValue / 1000d)
        {
            throw new InvalidOperationException("Api:TimeoutSeconds must be a positive supported timeout.");
        }

        BaseUrl = new Uri(uri.AbsoluteUri.TrimEnd('/') + "/");
        Timeout = TimeSpan.FromSeconds(timeoutSeconds);
    }

    public static ApiClientConfiguration Load()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
        return FromConfiguration(configuration);
    }

    public static ApiClientConfiguration FromConfiguration(IConfiguration configuration)
    {
        string? baseUrl = configuration["Api:BaseUrl"];
        string? timeoutValue = configuration["Api:TimeoutSeconds"];
        double timeoutSeconds = 30;
        if (timeoutValue is not null && !double.TryParse(timeoutValue,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out timeoutSeconds))
        {
            throw new InvalidOperationException("Api:TimeoutSeconds is invalid.");
        }
        return new ApiClientConfiguration(baseUrl ?? string.Empty, timeoutSeconds);
    }
}
