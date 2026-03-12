namespace AgroCDDotnet.Api.Todos;

public sealed record TodoItem(Guid Id, string Title, bool IsCompleted, DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);
