namespace RoushTech.Myotis.Smtp;

using System.Security.Authentication;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RoushTech.Myotis.Settings;
using SmtpServer;
using SmtpServer.ComponentModel;

public class SmtpListener(
    IOptions<SmtpOptions> options,
    IOptionsMonitor<LimitsOptions> limits,
    IHostEnvironment environment,
    CredentialAuthenticator authenticator,
    MailboxMessageStore store,
    PemCertificateFactory certificates,
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

        if (listeners.Any(l => l.NeedsCertificate))
        {
            // Load once up front so a bad path or key fails at startup, not on the first sender.
            certificates.Current();
        }

        var builder = new SmtpServerOptionsBuilder()
            .ServerName(options.Value.ServerName)
            .MaxMessageSize(limits.CurrentValue.MaxMessageBytes, MaxMessageSizeHandling.Strict);
        foreach (var listener in listeners)
        {
            builder.Endpoint(endpoint =>
            {
                endpoint
                    .Port(listener.Port, isSecure: listener.Tls == SmtpTls.Implicit)
                    .AuthenticationRequired()
                    .AllowUnsecureAuthentication(listener.Tls == SmtpTls.Proxy || (listener.Tls == SmtpTls.None && environment.IsDevelopment()))
                    .SupportedSslProtocols(SslProtocols.Tls12 | SslProtocols.Tls13);
                if (listener.NeedsCertificate)
                {
                    endpoint.Certificate(certificates);
                }
            });
        }

        var services = new ServiceProvider();
        services.Add(authenticator);
        services.Add(store);

        var serverOptions = builder.Build();
        // The library reads this object on every EHLO, MAIL and DATA, so changing it applies a new cap to the next message.
        var sizeCap = (MaxMessageSizeOptions)serverOptions.MaxMessageSizeOptions;
        using var _ = limits.OnChange(l => sizeCap.Length = l.MaxMessageBytes);

        var server = new SmtpServer(serverOptions, services);
        server.SessionFaulted += (_, e) => logger.LogWarning(e.Exception, "SMTP session faulted");
        logger.LogInformation("Listening for SMTP on {Listeners}", string.Join(", ", listeners.Select(l => $"{l.Port} ({l.Tls})")));
        try
        {
            await server.StartAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
