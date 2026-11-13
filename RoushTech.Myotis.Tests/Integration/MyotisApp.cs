namespace RoushTech.Myotis.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using RoushTech.Myotis.Data.Models;

public sealed class MyotisApp(
    bool seedAdministrator = true,
    bool smtp = false,
    string environment = "Development",
    IReadOnlyDictionary<string, string>? settings = null) : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@example.test";

    public const string AdminPassword = "Password1";

    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"myotis-{Guid.NewGuid():N}.db");

    public int SmtpPort { get; } = smtp ? FreePort() : 0;

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
        // The website's production appsettings.json carries #{tokens}; tests run on their own copy in the output folder.
        builder.UseContentRoot(AppContext.BaseDirectory);
        builder.UseEnvironment(environment);
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={databasePath}");
        if (smtp)
        {
            builder.UseSetting("Myotis:Smtp:Listeners:0:Port", SmtpPort.ToString());
        }

        foreach (var (key, value) in settings ?? new Dictionary<string, string>())
        {
            builder.UseSetting(key, value);
        }
        if (seedAdministrator)
        {
            builder.UseSetting("Myotis:Seed:AdminEmail", AdminEmail);
            builder.UseSetting("Myotis:Seed:AdminPassword", AdminPassword);
        }
    }

    public static int FreePort()
    {
        using var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        return ((IPEndPoint)probe.LocalEndpoint).Port;
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
