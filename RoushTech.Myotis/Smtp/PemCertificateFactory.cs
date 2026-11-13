namespace RoushTech.Myotis.Smtp;

using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using SmtpServer;

// Reloads when either file changes, so a renewed certificate is used without a restart.
public sealed class PemCertificateFactory(IOptions<SmtpOptions> options) : ICertificateFactory, IDisposable
{
    private readonly Lock gate = new();
    private X509Certificate2? certificate;
    private (DateTime Certificate, DateTime Key) loadedVersion;

    public X509Certificate GetServerCertificate(ISessionContext sessionContext) => Current();

    public X509Certificate2 Current()
    {
        var paths = options.Value.Certificate;
        if (!paths.IsConfigured)
        {
            throw new InvalidOperationException("No SMTP certificate is configured");
        }

        var version = (File.GetLastWriteTimeUtc(paths.CertificatePath!), File.GetLastWriteTimeUtc(paths.KeyPath!));
        lock (gate)
        {
            if (certificate is null || version != loadedVersion)
            {
                var previous = certificate;
                certificate = X509Certificate2.CreateFromPemFile(paths.CertificatePath!, paths.KeyPath);
                loadedVersion = version;
                previous?.Dispose();
            }

            return certificate;
        }
    }

    public void Dispose() => certificate?.Dispose();
}
