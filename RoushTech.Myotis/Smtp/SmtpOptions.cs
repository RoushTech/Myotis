namespace RoushTech.Myotis.Smtp;

public class SmtpOptions
{
    public const string Section = "Myotis:Smtp";

    // Until limits move to the admin settings, the 25 MB default from the brief is fixed.
    public const int MaxMessageBytes = 25 * 1024 * 1024;

    public string ServerName { get; set; } = "myotis";

    // The host apps connect to, shown with a new credential. The web address's host when empty.
    public string? PublicHost { get; set; }

    public CertificateOptions Certificate { get; set; } = new();

    public List<SmtpListenerOptions> Listeners { get; set; } = [];
}

public class CertificateOptions
{
    public string? CertificatePath { get; set; }

    public string? KeyPath { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(CertificatePath) && !string.IsNullOrWhiteSpace(KeyPath);
}

public class SmtpListenerOptions
{
    public int Port { get; set; }

    public SmtpTls Tls { get; set; } = SmtpTls.None;

    // The port senders use when a proxy or container maps this one, such as Traefik's 465 to 2525.
    public int? PublicPort { get; set; }

    public bool NeedsCertificate => Tls is SmtpTls.Implicit or SmtpTls.StartTls;
}

public enum SmtpTls
{
    None,
    Implicit,
    StartTls,
    // Plaintext here because a proxy in front already terminated TLS.
    Proxy,
}
