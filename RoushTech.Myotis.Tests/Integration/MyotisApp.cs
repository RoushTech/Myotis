namespace RoushTech.Myotis.Tests.Integration;

using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

public sealed class MyotisApp(bool seedAdministrator = true) : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@example.test";

    public const string AdminPassword = "Password1";

    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"myotis-{Guid.NewGuid():N}.db");

    public async Task<HttpClient> SignInAsync(string email, string password)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/v0/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        return client;
    }

    public Task<HttpClient> SignInAsAdminAsync() => SignInAsync(AdminEmail, AdminPassword);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={databasePath}");
        if (seedAdministrator)
        {
            builder.UseSetting("Myotis:Seed:AdminEmail", AdminEmail);
            builder.UseSetting("Myotis:Seed:AdminPassword", AdminPassword);
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        foreach (var file in new[] { databasePath, databasePath + "-wal", databasePath + "-shm" })
        {
            File.Delete(file);
        }
    }
}
