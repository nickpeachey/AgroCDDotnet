using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace AgroCDDotnet.Api.Tests;

public sealed class TodoEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TodoEndpointTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    [Trait("TestTarget", "InMemory")]
    public async Task TodoCrudFlow_WorksAgainstPostgres()
    {
        var createdResponse = await _client.PostAsJsonAsync("/todos", new
        {
            title = "Write integration test",
            isCompleted = false
        });

        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);

        var createdTodo = await createdResponse.Content.ReadFromJsonAsync<TodoContract>();
        Assert.NotNull(createdTodo);
        Assert.Equal("Write integration test", createdTodo.Title);
        Assert.False(createdTodo.IsCompleted);

        var listAfterCreate = await _client.GetFromJsonAsync<List<TodoContract>>("/todos");
        Assert.NotNull(listAfterCreate);
        Assert.Contains(listAfterCreate, todo => todo.Id == createdTodo.Id);

        var getByIdResponse = await _client.GetAsync($"/todos/{createdTodo.Id}");
        Assert.Equal(HttpStatusCode.OK, getByIdResponse.StatusCode);

        var updatedResponse = await _client.PutAsJsonAsync($"/todos/{createdTodo.Id}", new
        {
            title = "Write deployed integration test",
            isCompleted = true
        });

        Assert.Equal(HttpStatusCode.OK, updatedResponse.StatusCode);

        var updatedTodo = await updatedResponse.Content.ReadFromJsonAsync<TodoContract>();
        Assert.NotNull(updatedTodo);
        Assert.Equal("Write deployed integration test", updatedTodo.Title);
        Assert.True(updatedTodo.IsCompleted);
        Assert.NotNull(updatedTodo.UpdatedAtUtc);

        var deleteResponse = await _client.DeleteAsync($"/todos/{createdTodo.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var notFoundResponse = await _client.GetAsync($"/todos/{createdTodo.Id}");
        Assert.Equal(HttpStatusCode.NotFound, notFoundResponse.StatusCode);
    }

    [Fact]
    [Trait("TestTarget", "InMemory")]
    public async Task CreateTodo_WithBlankTitle_ReturnsValidationError()
    {
        var response = await _client.PostAsJsonAsync("/todos", new
        {
            title = "",
            isCompleted = false
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
