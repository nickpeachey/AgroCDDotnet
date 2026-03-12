using Npgsql;

namespace AgroCDDotnet.Api.Todos;

public sealed class PostgresTodoRepository : ITodoRepository
{
    private readonly string _connectionString;

    public PostgresTodoRepository(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'TodosDatabase' is not configured.");
        }

        _connectionString = connectionString;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS todos (
                id UUID PRIMARY KEY,
                title TEXT NOT NULL,
                is_completed BOOLEAN NOT NULL,
                created_at_utc TIMESTAMPTZ NOT NULL,
                updated_at_utc TIMESTAMPTZ NULL
            );
            """;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> CanConnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT to_regclass('public.todos') IS NOT NULL;";
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<TodoItem>> GetAllAsync(CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, title, is_completed, created_at_utc, updated_at_utc
            FROM todos
            ORDER BY created_at_utc ASC;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var todos = new List<TodoItem>();
        while (await reader.ReadAsync(cancellationToken))
        {
            todos.Add(ReadTodo(reader));
        }

        return todos;
    }

    public async Task<TodoItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, title, is_completed, created_at_utc, updated_at_utc
            FROM todos
            WHERE id = @id;
            """;
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadTodo(reader) : null;
    }

    public async Task<TodoItem> CreateAsync(CreateTodoRequest request, CancellationToken cancellationToken)
    {
        ValidateTitle(request.Title);

        var todo = new TodoItem(
            Guid.NewGuid(),
            request.Title.Trim(),
            request.IsCompleted,
            DateTimeOffset.UtcNow,
            null);

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO todos (id, title, is_completed, created_at_utc, updated_at_utc)
            VALUES (@id, @title, @isCompleted, @createdAtUtc, @updatedAtUtc);
            """;
        command.Parameters.AddWithValue("id", todo.Id);
        command.Parameters.AddWithValue("title", todo.Title);
        command.Parameters.AddWithValue("isCompleted", todo.IsCompleted);
        command.Parameters.AddWithValue("createdAtUtc", todo.CreatedAtUtc);
        command.Parameters.AddWithValue("updatedAtUtc", DBNull.Value);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return todo;
    }

    public async Task<TodoItem?> UpdateAsync(Guid id, UpdateTodoRequest request, CancellationToken cancellationToken)
    {
        ValidateTitle(request.Title);

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE todos
            SET title = @title,
                is_completed = @isCompleted,
                updated_at_utc = @updatedAtUtc
            WHERE id = @id
            RETURNING id, title, is_completed, created_at_utc, updated_at_utc;
            """;
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("title", request.Title.Trim());
        command.Parameters.AddWithValue("isCompleted", request.IsCompleted);
        command.Parameters.AddWithValue("updatedAtUtc", DateTimeOffset.UtcNow);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadTodo(reader) : null;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM todos
            WHERE id = @id;
            """;
        command.Parameters.AddWithValue("id", id);

        var deleted = await command.ExecuteNonQueryAsync(cancellationToken);
        return deleted > 0;
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static TodoItem ReadTodo(NpgsqlDataReader reader)
    {
        return new TodoItem(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.GetBoolean(2),
            reader.GetFieldValue<DateTimeOffset>(3),
            reader.IsDBNull(4) ? null : reader.GetFieldValue<DateTimeOffset>(4));
    }

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Todo title is required.", nameof(title));
        }

        if (title.Trim().Length > 200)
        {
            throw new ArgumentException("Todo title must be 200 characters or fewer.", nameof(title));
        }
    }
}
