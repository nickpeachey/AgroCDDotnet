using System.Reflection;
using Npgsql;

namespace AgroCDDotnet.Database;

public static class MigrationRunner
{
    public static async Task MigrateAsync(string connectionString, Action<string>? log = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        await EnsureMigrationsTableAsync(connectionString, cancellationToken);

        foreach (var script in GetScripts())
        {
            var alreadyApplied = await IsAppliedAsync(connectionString, script.Name, cancellationToken);
            if (alreadyApplied)
            {
                log?.Invoke($"Skipping {script.Name} (already applied).");
                continue;
            }

            log?.Invoke($"Applying {script.Name}...");
            await ExecuteScriptAsync(connectionString, script.Sql, cancellationToken);
            await MarkAppliedAsync(connectionString, script.Name, cancellationToken);
        }
    }

    private static async Task EnsureMigrationsTableAsync(string connectionString, CancellationToken cancellationToken)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS schema_migrations (
                script_name TEXT PRIMARY KEY,
                applied_at_utc TIMESTAMPTZ NOT NULL
            );
            """;

        await ExecuteScriptAsync(connectionString, sql, cancellationToken);
    }

    private static async Task<bool> IsAppliedAsync(string connectionString, string scriptName, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM schema_migrations WHERE script_name = @scriptName;";
        command.Parameters.AddWithValue("scriptName", scriptName);

        var result = (long)(await command.ExecuteScalarAsync(cancellationToken) ?? 0L);
        return result > 0;
    }

    private static async Task MarkAppliedAsync(string connectionString, string scriptName, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO schema_migrations (script_name, applied_at_utc)
            VALUES (@scriptName, @appliedAtUtc);
            """;
        command.Parameters.AddWithValue("scriptName", scriptName);
        command.Parameters.AddWithValue("appliedAtUtc", DateTimeOffset.UtcNow);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task ExecuteScriptAsync(string connectionString, string sql, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static IReadOnlyList<MigrationScript> GetScripts()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(name => name.Contains(".Scripts.", StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        var scripts = new List<MigrationScript>(resourceNames.Length);
        foreach (var resourceName in resourceNames)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' was not found.");
            using var reader = new StreamReader(stream);
            scripts.Add(new MigrationScript(
                resourceName.Split(".Scripts.", StringSplitOptions.None)[1],
                reader.ReadToEnd()));
        }

        return scripts;
    }

    private sealed record MigrationScript(string Name, string Sql);
}
