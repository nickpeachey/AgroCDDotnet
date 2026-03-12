namespace AgroCDDotnet.Api.Todos;

public static class TodoEndpoints
{
    public static IEndpointRouteBuilder MapTodos(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/todos")
            .WithTags("Todos");

        group.MapGet("/", async (ITodoRepository repository, CancellationToken cancellationToken) =>
            Results.Ok(await repository.GetAllAsync(cancellationToken)));

        group.MapGet("/{id:guid}", async (Guid id, ITodoRepository repository, CancellationToken cancellationToken) =>
        {
            var todo = await repository.GetByIdAsync(id, cancellationToken);
            return todo is null ? Results.NotFound() : Results.Ok(todo);
        });

        group.MapPost("/", async (CreateTodoRequest request, ITodoRepository repository, CancellationToken cancellationToken) =>
        {
            try
            {
                var todo = await repository.CreateAsync(request, cancellationToken);
                return Results.Created($"/todos/{todo.Id}", todo);
            }
            catch (ArgumentException exception)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["title"] = [exception.Message]
                });
            }
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateTodoRequest request, ITodoRepository repository, CancellationToken cancellationToken) =>
        {
            try
            {
                var todo = await repository.UpdateAsync(id, request, cancellationToken);
                return todo is null ? Results.NotFound() : Results.Ok(todo);
            }
            catch (ArgumentException exception)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["title"] = [exception.Message]
                });
            }
        });

        group.MapDelete("/{id:guid}", async (Guid id, ITodoRepository repository, CancellationToken cancellationToken) =>
        {
            var deleted = await repository.DeleteAsync(id, cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        return app;
    }
}
