namespace RoushTech.Myotis.Tests.Integration;

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

public sealed class TestCertificate : IDisposable
{
    private readonly string folder = Directory.CreateTempSubdirectory("myotis-cert-").FullName;

    public TestCertificate() => Write("localhost");

    public string CertificatePath => Path.Combine(folder, "cert.pem");

    public string KeyPath => Path.Combine(folder, "key.pem");

    public string Thumbprint { get; private set; } = "";

    public void Write(string subject)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest($"CN={subject}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var names = new SubjectAlternativeNameBuilder();
        names.AddDnsName("localhost");
        request.CertificateExtensions.Add(names.Build());
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(1));
        File.WriteAllText(CertificatePath, certificate.ExportCertificatePem());
        File.WriteAllText(KeyPath, key.ExportPkcs8PrivateKeyPem());
        // Renewals land with a new timestamp; make sure this one differs even within the same second.
        var stamp = DateTime.UtcNow.AddSeconds(Thumbprint.Length == 0 ? 0 : 5);
        File.SetLastWriteTimeUtc(CertificatePath, stamp);
        File.SetLastWriteTimeUtc(KeyPath, stamp);
        Thumbprint = certificate.Thumbprint;
    }

    public Dictionary<string, string> Settings() => new()
    {
        ["Myotis:Smtp:Certificate:CertificatePath"] = CertificatePath,
        ["Myotis:Smtp:Certificate:KeyPath"] = KeyPath,
    };

    public void Dispose() => Directory.Delete(folder, true);
}
