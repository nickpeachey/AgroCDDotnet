namespace AgroCDDotnet.Database;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var connectionString =
            args.FirstOrDefault()
            ?? Environment.GetEnvironmentVariable("TODOS_DATABASE_CONNECTION_STRING")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__TodosDatabase");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("A Postgres connection string is required.");
        }

        await MigrationRunner.MigrateAsync(connectionString, Console.WriteLine, CancellationToken.None);
    }
}
