namespace RoushTech.Myotis.Tests.Integration;

using MailKit.Net.Smtp;
using MailKit.Security;
using RoushTech.Myotis.Website.Models;

public class SmtpTlsTests
{
    [Test]
    public async Task ImplicitTlsSignsInFromTheFirstByte()
    {
        using var certificate = new TestCertificate();
        await using var app = TlsApp(certificate, "Implicit", out var port);
        var password = await CredentialAsync(app);

        using var client = await ConnectAsync(app, port, SecureSocketOptions.SslOnConnect);
        await client.AuthenticateAsync("billing-staging", password);

        Assert.That(client.IsSecure, Is.True);
        Assert.That(client.IsAuthenticated, Is.True);
    }

    [Test]
    public async Task StartTlsOnlyOffersSignInAfterTheUpgrade()
    {
        using var certificate = new TestCertificate();
        await using var app = TlsApp(certificate, "StartTls", out var port);
        var password = await CredentialAsync(app);

        using var plain = await ConnectAsync(app, port, SecureSocketOptions.None);
        var authBeforeUpgrade = plain.Capabilities.HasFlag(SmtpCapabilities.Authentication);
        var offersStartTls = plain.Capabilities.HasFlag(SmtpCapabilities.StartTLS);
        using var upgraded = await ConnectAsync(app, port, SecureSocketOptions.StartTls);
        await upgraded.AuthenticateAsync("billing-staging", password);

        Assert.That(authBeforeUpgrade, Is.False);
        Assert.That(offersStartTls, Is.True);
        Assert.That(upgraded.IsAuthenticated, Is.True);
    }

    [Test]
    public async Task AListenerBehindAProxySignsInOverPlaintextEvenInProduction()
    {
        var port = MyotisApp.FreePort();
        await using var app = new MyotisApp(environment: "Production", settings: new Dictionary<string, string>
        {
            ["Myotis:Smtp:Listeners:0:Port"] = port.ToString(),
            ["Myotis:Smtp:Listeners:0:Tls"] = "Proxy",
        });
        var password = await CredentialAsync(app);

        using var client = await ConnectAsync(app, port, SecureSocketOptions.None);
        await client.AuthenticateAsync("billing-staging", password);

        Assert.That(client.IsAuthenticated, Is.True);
    }

    [Test]
    public void ATlsListenerWithoutACertificateStopsStartup()
    {
        using var app = new MyotisApp(settings: new Dictionary<string, string>
        {
            ["Myotis:Smtp:Listeners:0:Port"] = MyotisApp.FreePort().ToString(),
            ["Myotis:Smtp:Listeners:0:Tls"] = "Implicit",
        });

        var failure = Assert.Catch(() => app.CreateClient());

        Assert.That(failure?.ToString(), Does.Contain("needs CertificatePath and KeyPath"));
    }

    [Test]
    public void AListenerWithAValueThatWontBindStopsStartup()
    {
        using var app = new MyotisApp(settings: new Dictionary<string, string>
        {
            ["Myotis:Smtp:Listeners:0:Port"] = "#{myotis.smtp.port}",
        });

        var failure = Assert.Catch(() => app.CreateClient());

        Assert.That(failure?.ToString(), Does.Contain("has a value that isn't valid"));
    }

    [Test]
    public async Task ARenewedCertificateIsUsedWithoutARestart()
    {
        using var certificate = new TestCertificate();
        await using var app = TlsApp(certificate, "Implicit", out var port);
        await CredentialAsync(app);

        var before = await ServedThumbprintAsync(app, port);
        certificate.Write("localhost-renewed");
        var after = await ServedThumbprintAsync(app, port);

        Assert.That(after, Is.Not.EqualTo(before));
        Assert.That(after, Is.EqualTo(certificate.Thumbprint));
    }

    [Test]
    public async Task SignedInPeopleSeeWhereToSendMail()
    {
        var port = MyotisApp.FreePort();
        await using var app = new MyotisApp(environment: "Production", settings: new Dictionary<string, string>
        {
            ["Myotis:Smtp:PublicHost"] = "mail.example.test",
            ["Myotis:Smtp:Listeners:0:Port"] = port.ToString(),
            ["Myotis:Smtp:Listeners:0:Tls"] = "Proxy",
            ["Myotis:Smtp:Listeners:0:PublicPort"] = "465",
            ["Myotis:Smtp:Listeners:1:Port"] = MyotisApp.FreePort().ToString(),
            ["Myotis:Smtp:Listeners:1:Tls"] = "None",
        });
        using var admin = await app.SignInAsAdminAsync();

        var connection = await admin.GetAsync<SmtpConnectionResponse>("/api/v0/smtp");

        Assert.That(connection.Host, Is.EqualTo("mail.example.test"));
        Assert.That(connection.Ports, Is.EqualTo(new[] { new SmtpPortResponse(465, "TLS") }));
    }

    private static MyotisApp TlsApp(TestCertificate certificate, string tls, out int port)
    {
        port = MyotisApp.FreePort();
        var settings = certificate.Settings();
        settings["Myotis:Smtp:Listeners:0:Port"] = port.ToString();
        settings["Myotis:Smtp:Listeners:0:Tls"] = tls;
        return new MyotisApp(environment: "Production", settings: settings);
    }

    private static async Task<string> CredentialAsync(MyotisApp app)
    {
        using var admin = await app.SignInAsAdminAsync();
        var mailbox = await admin.CreateMailboxAsync("Billing staging");
        return (await admin.CreateCredentialAsync(mailbox.Id, "billing-staging")).Password;
    }

    private static async Task<SmtpClient> ConnectAsync(MyotisApp app, int port, SecureSocketOptions security)
    {
        _ = app.Services;
        var client = new SmtpClient { ServerCertificateValidationCallback = (_, _, _, _) => true };
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await client.ConnectAsync("localhost", port, security);
                return client;
            }
            catch (System.Net.Sockets.SocketException) when (attempt < 50)
            {
                await Task.Delay(100);
            }
        }
    }

    private static async Task<string> ServedThumbprintAsync(MyotisApp app, int port)
    {
        string? thumbprint = null;
        _ = app.Services;
        using var client = new SmtpClient
        {
            ServerCertificateValidationCallback = (_, cert, _, _) =>
            {
                thumbprint = cert?.GetCertHashString();
                return true;
            },
        };
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await client.ConnectAsync("localhost", port, SecureSocketOptions.SslOnConnect);
                break;
            }
            catch (System.Net.Sockets.SocketException) when (attempt < 50)
            {
                await Task.Delay(100);
            }
        }

        await client.DisconnectAsync(true);
        return thumbprint ?? "";
    }
}
