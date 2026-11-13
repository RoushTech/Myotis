namespace RoushTech.Myotis.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using RoushTech.Myotis.Data.Models;
using RoushTech.Myotis.Health;
using RoushTech.Myotis.Messages;

public class DatabaseHealthTests
{
    [Test]
    public async Task AdministratorsSeeSizesUsageByMailboxAndTheLastRetentionRun()
    {
        await using var app = new MyotisApp(smtp: true);
        using var admin = await app.SignInAsAdminAsync();
        var staging = await admin.CreateMailboxAsync("Billing staging");
        await admin.CreateMailboxAsync("Billing hotfix");
        var credential = await admin.CreateCredentialAsync(staging.Id, "billing-staging");
        await SmtpClients.SendAsync(app, "billing-staging", credential.Password, SmtpClients.Message("One"));
        await SmtpClients.SendAsync(app, "billing-staging", credential.Password, SmtpClients.Message("Two"));
        await app.WithServiceAsync<RetentionService, RetentionRun>(r => r.RunAsync(CancellationToken.None));

        var health = await admin.GetAsync<DatabaseHealth>("/api/v0/database");

        Assert.That(health.FileBytes, Is.GreaterThan(0));
        Assert.That(health.MessageCount, Is.EqualTo(2));
        Assert.That(health.Mailboxes.Select(m => (m.Name, m.MessageCount)), Is.EqualTo(new[] { ("Billing staging", 2), ("Billing hotfix", 0) }));
        Assert.That(health.Mailboxes[0].Bytes, Is.GreaterThan(0));
        Assert.That(health.Mailboxes[1].OldestReceivedAt, Is.Null);
        Assert.That(health.LastRetention?.DeletedCount, Is.Zero);
        Assert.That(health.LastRetention?.NextRunAt, Is.EqualTo(health.LastRetention?.StartedAt + RetentionWorker.Interval));
    }

    [Test]
    public async Task AnIntegrityCheckReportsAHealthyFile()
    {
        await using var app = new MyotisApp();
        using var admin = await app.SignInAsAdminAsync();

        var response = await admin.PostAsync("/api/v0/database/check", null);
        var result = await response.Content.ReadFromJsonAsync<IntegrityResult>();

        Assert.That(result?.Ok, Is.True);
        Assert.That(result?.Problems, Is.Empty);
    }

    [Test]
    public async Task OnlyAdministratorsSeeDatabaseHealth()
    {
        await using var app = new MyotisApp();
        using var admin = await app.SignInAsAdminAsync();
        await admin.CreateUserAsync("dana@example.test");
        using var dana = await app.SignInAsync("dana@example.test", "user password 1");

        Assert.That((await dana.GetAsync("/api/v0/database")).StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        Assert.That((await dana.PostAsync("/api/v0/database/check", null)).StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }
}
