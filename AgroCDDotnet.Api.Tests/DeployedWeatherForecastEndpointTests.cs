using System.Net;
using System.Net.Http.Json;
using AgroCDDotnet.Api;
using Xunit;

namespace AgroCDDotnet.Api.Tests;

public sealed class DeployedWeatherForecastEndpointTests : IClassFixture<DeployedApiFixture>
{
    private readonly HttpClient _client;

    public DeployedWeatherForecastEndpointTests(DeployedApiFixture fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    [Trait("TestTarget", "Deployed")]
    public async Task GetWeatherForecast_ReturnsFiveItems()
    {
        var response = await _client.GetAsync("/weatherforecast");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<WeatherForecast[]>();

        Assert.NotNull(payload);
        Assert.Equal(5, payload.Length);
        Assert.All(payload, item =>
        {
            Assert.NotEqual(default, item.Date);
            Assert.InRange(item.TemperatureC, -20, 54);
        });
    }
}
