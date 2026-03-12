namespace AgroCDDotnet.Api.Todos;

public interface ITodoRepository
{
    Task InitializeAsync(CancellationToken cancellationToken);
    Task<bool> CanConnectAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<TodoItem>> GetAllAsync(CancellationToken cancellationToken);
    Task<TodoItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<TodoItem> CreateAsync(CreateTodoRequest request, CancellationToken cancellationToken);
    Task<TodoItem?> UpdateAsync(Guid id, UpdateTodoRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
