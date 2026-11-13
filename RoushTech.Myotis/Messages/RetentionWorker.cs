namespace RoushTech.Myotis.Messages;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class RetentionWorker(IServiceScopeFactory scopes, TimeProvider time, ILogger<RetentionWorker> logger) : BackgroundService
{
    public static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, time);
        do
        {
            await using var scope = scopes.CreateAsyncScope();
            var run = await scope.ServiceProvider.GetRequiredService<RetentionService>().RunAsync(stoppingToken);
            if (run.Error is null)
            {
                logger.LogInformation("Retention deleted {Count} messages", run.DeletedCount);
            }
            else
            {
                logger.LogError("Retention failed: {Error}", run.Error);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
