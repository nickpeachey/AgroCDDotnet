namespace AgroCDDotnet.Api.Todos;

public sealed record CreateTodoRequest(string Title, bool IsCompleted);

public sealed record UpdateTodoRequest(string Title, bool IsCompleted);

public sealed record DeleteTodoRequest(Guid Id);