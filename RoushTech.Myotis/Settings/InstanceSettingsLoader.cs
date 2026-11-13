namespace RoushTech.Myotis.Settings;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RoushTech.Myotis.Data;

public class InstanceSettingsLoader(InstanceSettingsConfigurationSource source, IServiceScopeFactory scopes, IConfiguration configuration) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        source.Reader = Read;
        (configuration as IConfigurationRoot)?.Reload();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    // Synchronous because configuration providers load synchronously.
    private IDictionary<string, string?> Read()
    {
        using var scope = scopes.CreateScope();
        return scope.ServiceProvider.GetRequiredService<MyotisDbContext>().InstanceSettings
            .AsNoTracking()
            .ToDictionary(s => s.Key, s => (string?)s.Value, StringComparer.OrdinalIgnoreCase);
    }
}
