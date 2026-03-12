using AgroCDDotnet.Api.Todos;

namespace AgroCDDotnet.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddAuthorization();
        builder.Services.AddOpenApi();
        builder.Services.AddSingleton<ITodoRepository>(_ =>
            new PostgresTodoRepository(builder.Configuration.GetConnectionString("TodosDatabase")));

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();

        app.MapGet("/hello", (string name) => Results.Ok(new GreetingResponse($"Hello, {name}!")))
            .WithName("GetGreeting");

        app.MapGet("/healthz", async (ITodoRepository repository, CancellationToken cancellationToken) =>
        {
            var isHealthy = await repository.CanConnectAsync(cancellationToken);
            return isHealthy
                ? Results.Ok(new { status = "ok" })
                : Results.Problem("Database connectivity check failed.", statusCode: StatusCodes.Status503ServiceUnavailable);
        });

        app.MapTodos();

        app.Run();
    }
}

public record GreetingResponse(string Message);
