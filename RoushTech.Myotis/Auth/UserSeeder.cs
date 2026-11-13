namespace RoushTech.Myotis.Auth;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RoushTech.Myotis.Configuration;
using RoushTech.Myotis.Users;

/// <summary>Creates the configured administrator on an empty database, so local development skips the setup screen.</summary>
public class UserSeeder(IServiceScopeFactory scopes, IOptions<MyotisOptions> options, ILogger<UserSeeder> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var seed = options.Value.Seed;
        if (string.IsNullOrEmpty(seed.AdminEmail) || string.IsNullOrEmpty(seed.AdminPassword))
        {
            return;
        }

        await using var scope = scopes.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserService>();
        if (await users.SetupRequiredAsync(cancellationToken))
        {
            await users.CreateFirstAdministratorAsync(seed.AdminEmail, seed.AdminPassword, cancellationToken);
            logger.LogInformation("Seeded administrator {Email}", seed.AdminEmail);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
