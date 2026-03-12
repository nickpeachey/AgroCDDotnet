using System.Net;
using System.Net.Http.Json;
using AgroCDDotnet.Api;
using Xunit;

namespace AgroCDDotnet.Api.Tests;

public sealed class HelloEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HelloEndpointTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    [Trait("TestTarget", "InMemory")]
    public async Task GetHello_WithName_ReturnsGreeting()
    {
        var response = await _client.GetAsync("/hello?name=Nick");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<GreetingResponse>();

        Assert.NotNull(payload);
        Assert.Equal("Hello, Nick!", payload.Message);
    }
}
