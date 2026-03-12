using System.Diagnostics;
using Npgsql;
using Xunit;

namespace AgroCDDotnet.Api.Tests;

public sealed class PostgresTestContainer : IAsyncLifetime
{
    private readonly string _containerName = $"agrocd-api-tests-postgres-{Guid.NewGuid():N}";
    private readonly string _database = "agrocdtodos";
    private readonly string _username = "postgres";
    private readonly string _password = "postgres";
    private readonly string _host = IsRunningInsideContainer() ? "host.docker.internal" : "127.0.0.1";
    private int _hostPort;

    public string ConnectionString =>
        $"Host={_host};Port={_hostPort};Database={_database};Username={_username};Password={_password}";

    public async Task InitializeAsync()
    {
        _hostPort = GetFreeTcpPort();

        await RunDockerCommandAsync([
            "run",
            "--detach",
            "--rm",
            "--name", _containerName,
            "-e", $"POSTGRES_DB={_database}",
            "-e", $"POSTGRES_USER={_username}",
            "-e", $"POSTGRES_PASSWORD={_password}",
            "-p", $"{_hostPort}:5432",
            "postgres:17-alpine"
        ]);

        const int maxAttempts = 30;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await using var connection = new NpgsqlConnection(ConnectionString);
                await connection.OpenAsync();
                return;
            }
            catch when (attempt < maxAttempts)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        throw new InvalidOperationException("The Postgres test container did not become ready in time.");
    }

    public async Task DisposeAsync()
    {
        try
        {
            await RunDockerCommandAsync(["rm", "-f", _containerName], ignoreExitCode: true);
        }
        catch
        {
        }
    }

    private static int GetFreeTcpPort()
    {
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static bool IsRunningInsideContainer() =>
        File.Exists("/.dockerenv");

    private static async Task RunDockerCommandAsync(IReadOnlyList<string> arguments, bool ignoreExitCode = false)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "docker",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start docker.");

        var standardError = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0 && !ignoreExitCode)
        {
            throw new InvalidOperationException($"Docker command failed: {standardError}");
        }
    }
}
