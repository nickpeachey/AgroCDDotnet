using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace AgroCDDotnet.Api.Tests;

public sealed class DeployedTodoEndpointTests : IClassFixture<DeployedApiFixture>
{
    private readonly HttpClient _client;

    public DeployedTodoEndpointTests(DeployedApiFixture fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    [Trait("TestTarget", "Deployed")]
    public async Task TodoCrudFlow_WorksAgainstDeployedEnvironment()
    {
        var healthResponse = await _client.GetAsync("/healthz");
        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);

        var createdResponse = await _client.PostAsJsonAsync("/todos", new
        {
            title = $"Deployed todo {Guid.NewGuid():N}",
            isCompleted = false
        });

        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);

        var createdTodo = await createdResponse.Content.ReadFromJsonAsync<TodoContract>();
        Assert.NotNull(createdTodo);

        var fetchedTodo = await _client.GetFromJsonAsync<TodoContract>($"/todos/{createdTodo.Id}");
        Assert.NotNull(fetchedTodo);
        Assert.Equal(createdTodo.Id, fetchedTodo.Id);

        var updatedResponse = await _client.PutAsJsonAsync($"/todos/{createdTodo.Id}", new
        {
            title = $"{createdTodo.Title} updated",
            isCompleted = true
        });

        Assert.Equal(HttpStatusCode.OK, updatedResponse.StatusCode);

        var deleteResponse = await _client.DeleteAsync($"/todos/{createdTodo.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }
}
