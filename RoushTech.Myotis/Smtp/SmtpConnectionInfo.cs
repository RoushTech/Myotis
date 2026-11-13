namespace RoushTech.Myotis.Smtp;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

public record SmtpPort(int Port, string Security);

public record SmtpConnection(string? Host, IReadOnlyList<SmtpPort> Ports);

public class SmtpConnectionInfo(IOptions<SmtpOptions> options, IHostEnvironment environment)
{
    public SmtpConnection Describe() => new(
        string.IsNullOrWhiteSpace(options.Value.PublicHost) ? null : options.Value.PublicHost,
        options.Value.Listeners
            .Where(l => l.Tls != SmtpTls.None || environment.IsDevelopment())
            .Select(l => new SmtpPort(l.PublicPort ?? l.Port, l.Tls switch
            {
                SmtpTls.StartTls => "STARTTLS",
                SmtpTls.None => "None (development only)",
                // Senders see the proxy's TLS; Traefik terminates implicit TLS and can't do STARTTLS.
                _ => "TLS",
            }))
            .ToList());
}
