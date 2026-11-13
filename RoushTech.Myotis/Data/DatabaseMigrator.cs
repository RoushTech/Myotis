namespace RoushTech.Myotis.Data;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RoushTech.Myotis.Configuration;

public class DatabaseMigrator(IServiceScopeFactory scopes, IOptions<MyotisOptions> options) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.MigrateOnStartup)
        {
            return;
        }

        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MyotisDbContext>();
        CreateDatabaseFolder(db.Database.GetConnectionString());
        await db.Database.MigrateAsync(cancellationToken);
        // WAL lets the web app keep reading while SMTP intake writes, and the setting is stored in the file.
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static void CreateDatabaseFolder(string? connectionString)
    {
        var folder = Path.GetDirectoryName(new SqliteConnectionStringBuilder(connectionString).DataSource);
        if (!string.IsNullOrEmpty(folder))
        {
            Directory.CreateDirectory(folder);
        }
    }
}
