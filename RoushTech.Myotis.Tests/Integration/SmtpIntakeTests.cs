namespace RoushTech.Myotis.Tests.Integration;

using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using RoushTech.Myotis.Data;
using RoushTech.Myotis.Data.Models;
using RoushTech.Myotis.Website.Models;

public class SmtpIntakeTests
{
    [Test]
    public async Task SignedInMailLandsInTheCredentialsMailboxWithItsWholeEnvelope()
    {
        await using var app = new MyotisApp(smtp: true);
        using var admin = await app.SignInAsAdminAsync();
        var staging = await admin.CreateMailboxAsync("Billing staging");
        var credential = await admin.CreateCredentialAsync(staging.Id, "billing-staging");
        var message = SmtpClients.Message("Your invoice INV-20481 is ready", "jordan@example.org");
        message.Cc.Add(MailboxAddress.Parse("controller@example.org"));
        message.Bcc.Add(MailboxAddress.Parse("archive@example.org"));
        var body = new BodyBuilder { TextBody = "Invoice attached." };
        body.Attachments.Add("INV-20481.pdf", new byte[] { 37, 80, 68, 70 });
        message.Body = body.ToMessageBody();

        await SmtpClients.SendAsync(app, "billing-staging", credential.Password, message);
        var stored = await StoredAsync(app);

        Assert.That(stored, Has.Count.EqualTo(1));
        var saved = stored[0];
        Assert.That(saved.MailboxId, Is.EqualTo(staging.Id));
        Assert.That(saved.Subject, Is.EqualTo("Your invoice INV-20481 is ready"));
        Assert.That(saved.EnvelopeFrom, Is.EqualTo("no-reply@billing.example"));
        Assert.That(saved.EnvelopeTo, Is.EquivalentTo(new[] { "jordan@example.org", "controller@example.org", "archive@example.org" }));
        Assert.That(saved.To, Is.EqualTo("jordan@example.org"));
        Assert.That(saved.Cc, Is.EqualTo("controller@example.org"));
        Assert.That(saved.AttachmentCount, Is.EqualTo(1));
        Assert.That(saved.Content.Raw, Has.Length.EqualTo(saved.Size));
        Assert.That(saved.Size, Is.GreaterThan(0));
    }

    [Test]
    public async Task TheCredentialDecidesTheMailboxWhoeverTheMailIsAddressedTo()
    {
        await using var app = new MyotisApp(smtp: true);
        using var admin = await app.SignInAsAdminAsync();
        var staging = await admin.CreateMailboxAsync("Billing staging");
        await admin.CreateMailboxAsync("Billing hotfix");
        var credential = await admin.CreateCredentialAsync(staging.Id, "billing-staging");

        await SmtpClients.SendAsync(app, "billing-staging", credential.Password, SmtpClients.Message("Misaddressed", "billing-hotfix@example.test"));
        var stored = await StoredAsync(app);
        var credentials = await admin.GetAsync<List<CredentialResponse>>($"/api/v0/mailboxes/{staging.Id}/credentials");

        Assert.That(stored.Single().MailboxId, Is.EqualTo(staging.Id));
        Assert.That(credentials.Single().LastUsedAt, Is.Not.Null);
    }

    [Test]
    public async Task MailWithoutSigningInIsRefusedAtMailFrom()
    {
        await using var app = new MyotisApp(smtp: true);
        using var client = await SmtpClients.ConnectAsync(app);

        var refused = Assert.ThrowsAsync<ServiceNotAuthenticatedException>(() => client.SendAsync(SmtpClients.Message("Anonymous")));

        Assert.That(refused, Is.Not.Null);
        Assert.That(await StoredAsync(app), Is.Empty);
    }

    [Test]
    public async Task AWrongPasswordIsRefused()
    {
        await using var app = new MyotisApp(smtp: true);
        using var admin = await app.SignInAsAdminAsync();
        var staging = await admin.CreateMailboxAsync("Billing staging");
        await admin.CreateCredentialAsync(staging.Id, "billing-staging");
        using var client = await SmtpClients.ConnectAsync(app);

        Assert.ThrowsAsync<AuthenticationException>(() => client.AuthenticateAsync("billing-staging", "not the password"));
    }

    [Test]
    public async Task OutsideDevelopmentNobodyCanSignInOverPlaintext()
    {
        await using var app = new MyotisApp(smtp: true, environment: "Production");
        using var client = await SmtpClients.ConnectAsync(app);

        Assert.That(client.Capabilities.HasFlag(SmtpCapabilities.Authentication), Is.False);
        Assert.ThrowsAsync<NotSupportedException>(() => client.AuthenticateAsync("anyone", "anything"));
    }

    [Test]
    public async Task AMessageOverTheSizeCapIsCutOffWhileItsBeingSent()
    {
        await using var app = new MyotisApp(smtp: true);
        using var admin = await app.SignInAsAdminAsync();
        var staging = await admin.CreateMailboxAsync("Billing staging");
        var credential = await admin.CreateCredentialAsync(staging.Id, "billing-staging");

        var reply = await SmtpClients.SendIgnoringSizeAsync(app, "billing-staging", credential.Password, 26 * 1024 * 1024);

        Assert.That(reply, Does.StartWith("552"));
        Assert.That(await StoredAsync(app), Is.Empty);
    }

    private static Task<List<Message>> StoredAsync(MyotisApp app) =>
        app.WithServiceAsync<MyotisDbContext, List<Message>>(db => db.Messages.Include(m => m.Content).ToListAsync());
}
