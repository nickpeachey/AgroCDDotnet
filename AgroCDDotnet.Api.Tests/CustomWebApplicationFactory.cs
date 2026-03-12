using AgroCDDotnet.Api;
using AgroCDDotnet.Database;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;

namespace AgroCDDotnet.Api.Tests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgresTestContainer _postgres = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:TodosDatabase"] = _postgres.ConnectionString
            });
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.InitializeAsync();
        await MigrationRunner.MigrateAsync(_postgres.ConnectionString);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        Dispose();
        await _postgres.DisposeAsync();
    }
}
