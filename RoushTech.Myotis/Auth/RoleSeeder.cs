namespace RoushTech.Myotis.Auth;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RoushTech.Myotis.Data.Models;

public class RoleSeeder(IServiceScopeFactory scopes) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        foreach (var name in MyotisRoles.All)
        {
            if (await roles.FindByNameAsync(name) is null)
            {
                await roles.CreateAsync(new Role { Id = Guid.CreateVersion7(), Name = name });
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
