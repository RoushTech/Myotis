namespace RoushTech.Myotis.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RoushTech.Myotis.Data;
using RoushTech.Myotis.Data.Models;
using RoushTech.Myotis.Messages;

public class RetentionTests
{
    [Test]
    public async Task AMailboxKeepsOnlyItsNewestMessagesAsMailArrives()
    {
        await using var app = new MyotisApp(smtp: true);
        var (admin, mailbox, password) = await SetUpAsync(app);
        await admin.PatchAsJsonAsync("/api/v0/settings", new { changes = new[] { new { key = "Myotis:Limits:KeepMessages", value = "2" } } });

        foreach (var subject in new[] { "First", "Second", "Third" })
        {
            await SmtpClients.SendAsync(app, "billing-staging", password, SmtpClients.Message(subject));
        }

        var kept = await admin.GetAsync<List<MessageSummary>>($"/api/v0/mailboxes/{mailbox}/messages");
        Assert.That(kept.Select(m => m.Subject), Is.EqualTo(new[] { "Third", "Second" }));
        Assert.That(await CountAsync(app, db => db.MessageContents.CountAsync()), Is.EqualTo(2));
    }

    [Test]
    public async Task TheHourlyRunDropsMessagesPastTheirAgeAndRecordsWhatItDid()
    {
        await using var app = new MyotisApp(smtp: true);
        var (admin, mailbox, password) = await SetUpAsync(app);
        await SmtpClients.SendAsync(app, "billing-staging", password, SmtpClients.Message("Old"));
        await SmtpClients.SendAsync(app, "billing-staging", password, SmtpClients.Message("Recent"));
        await CountAsync(app, db => db.Messages.Where(m => m.Subject == "Old")
            .ExecuteUpdateAsync(m => m.SetProperty(x => x.ReceivedAt, DateTime.UtcNow.AddDays(-15))));

        var run = await app.WithServiceAsync<RetentionService, RetentionRun>(r => r.RunAsync(CancellationToken.None));
        var kept = await admin.GetAsync<List<MessageSummary>>($"/api/v0/mailboxes/{mailbox}/messages");

        Assert.That(run.DeletedCount, Is.EqualTo(1));
        Assert.That(run.FinishedAt, Is.Not.Null);
        Assert.That(run.Error, Is.Null);
        Assert.That(kept.Select(m => m.Subject), Is.EqualTo(new[] { "Recent" }));
    }

    [Test]
    public async Task PeopleWithAccessDeleteAMessageOrEmptyTheMailbox()
    {
        await using var app = new MyotisApp(smtp: true);
        var (admin, mailbox, password) = await SetUpAsync(app);
        await admin.CreateUserAsync("dana@example.test", mailbox);
        using var dana = await app.SignInAsync("dana@example.test", "user password 1");
        foreach (var subject in new[] { "One", "Two", "Three" })
        {
            await SmtpClients.SendAsync(app, "billing-staging", password, SmtpClients.Message(subject));
        }

        var one = (await dana.GetAsync<List<MessageSummary>>($"/api/v0/mailboxes/{mailbox}/messages")).Single(m => m.Subject == "One");
        var delete = await dana.DeleteAsync($"/api/v0/mailboxes/{mailbox}/messages/{one.Id}");
        var afterDelete = await dana.GetAsync<List<MessageSummary>>($"/api/v0/mailboxes/{mailbox}/messages");
        var empty = await dana.DeleteAsync($"/api/v0/mailboxes/{mailbox}/messages");
        var afterEmpty = await dana.GetAsync<List<MessageSummary>>($"/api/v0/mailboxes/{mailbox}/messages");

        Assert.That(delete.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(afterDelete, Has.Count.EqualTo(2));
        Assert.That(empty.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(afterEmpty, Is.Empty);
        Assert.That(await CountAsync(app, db => db.MessageContents.CountAsync()), Is.Zero);
    }

    [Test]
    public async Task SomeoneWithoutAccessCannotDeleteAnything()
    {
        await using var app = new MyotisApp(smtp: true);
        var (admin, mailbox, password) = await SetUpAsync(app);
        await admin.CreateUserAsync("lee@example.test");
        using var lee = await app.SignInAsync("lee@example.test", "user password 1");
        await SmtpClients.SendAsync(app, "billing-staging", password, SmtpClients.Message("Keep me"));
        var id = (await admin.GetAsync<List<MessageSummary>>($"/api/v0/mailboxes/{mailbox}/messages")).Single().Id;

        var delete = await lee.DeleteAsync($"/api/v0/mailboxes/{mailbox}/messages/{id}");
        var empty = await lee.DeleteAsync($"/api/v0/mailboxes/{mailbox}/messages");

        Assert.That(delete.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(empty.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(await admin.GetAsync<List<MessageSummary>>($"/api/v0/mailboxes/{mailbox}/messages"), Has.Count.EqualTo(1));
    }

    private static Task<int> CountAsync(MyotisApp app, Func<MyotisDbContext, Task<int>> query) =>
        app.WithServiceAsync<MyotisDbContext, int>(query);

    private static async Task<(HttpClient Admin, Guid Mailbox, string Password)> SetUpAsync(MyotisApp app)
    {
        var admin = await app.SignInAsAdminAsync();
        var mailbox = await admin.CreateMailboxAsync("Billing staging");
        var credential = await admin.CreateCredentialAsync(mailbox.Id, "billing-staging");
        return (admin, mailbox.Id, credential.Password);
    }
}
