namespace RoushTech.Myotis.Tests.Integration;

using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using RoushTech.Myotis.Data.Models;

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

    // Adding users through the app arrives with user management, so tests create them directly.
    public async Task<HttpClient> SignInAsNewUserAsync()
    {
        var email = $"user-{Guid.NewGuid():N}@example.test";
        const string password = "user password 1";
        await using (var scope = Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var result = await users.CreateAsync(new User { Id = Guid.CreateVersion7(), UserName = email, Email = email }, password);
            Assert.That(result.Succeeded, Is.True);
        }

        return await SignInAsync(email, password);
    }

    public async Task<TResult> WithServiceAsync<TService, TResult>(Func<TService, Task<TResult>> use) where TService : notnull
    {
        await using var scope = Services.CreateAsyncScope();
        return await use(scope.ServiceProvider.GetRequiredService<TService>());
    }

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
