namespace RoushTech.Myotis.Smtp;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmtpServer;
using SmtpServer.ComponentModel;

public class SmtpListener(
    IOptions<SmtpOptions> options,
    IHostEnvironment environment,
    CredentialAuthenticator authenticator,
    MailboxMessageStore store,
    ILogger<SmtpListener> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var listeners = options.Value.Listeners;
        if (listeners.Count == 0)
        {
            logger.LogWarning("No SMTP listeners are configured, so Myotis won't receive mail");
            return;
        }

        var builder = new SmtpServerOptionsBuilder()
            .ServerName(options.Value.ServerName)
            .MaxMessageSize(SmtpOptions.MaxMessageBytes, MaxMessageSizeHandling.Strict);
        foreach (var listener in listeners)
        {
            builder.Endpoint(endpoint => endpoint
                .Port(listener.Port)
                .AuthenticationRequired()
                .AllowUnsecureAuthentication(environment.IsDevelopment()));
        }

        var services = new ServiceProvider();
        services.Add(authenticator);
        services.Add(store);

        var server = new SmtpServer(builder.Build(), services);
        server.SessionFaulted += (_, e) => logger.LogWarning(e.Exception, "SMTP session faulted");
        logger.LogInformation("Listening for SMTP on {Ports}", string.Join(", ", listeners.Select(l => l.Port)));
        try
        {
            await server.StartAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
