namespace AgroCDDotnet.Api.Tests;

public sealed class DeployedApiFixture : IDisposable
{
    private const string BaseUrlVariable = "INTEGRATION_TESTS_BASE_URL";
    private const string HostHeaderVariable = "INTEGRATION_TESTS_HOST_HEADER";

    public HttpClient Client { get; }

    public DeployedApiFixture()
    {
        var baseUrl = Environment.GetEnvironmentVariable(BaseUrlVariable);
        var hostHeader = Environment.GetEnvironmentVariable(HostHeaderVariable);

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException(
                $"Environment variable '{BaseUrlVariable}' must be set for deployed integration tests.");
        }

        if (string.IsNullOrWhiteSpace(hostHeader))
        {
            throw new InvalidOperationException(
                $"Environment variable '{HostHeaderVariable}' must be set for deployed integration tests.");
        }

        if (!baseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            baseUrl = $"http://{baseUrl}";
        }

        Client = new HttpClient
        {
            BaseAddress = new Uri(baseUrl, UriKind.Absolute)
        };
        Client.DefaultRequestHeaders.Host = hostHeader;
    }

    public void Dispose()
    {
        Client.Dispose();
    }
}
