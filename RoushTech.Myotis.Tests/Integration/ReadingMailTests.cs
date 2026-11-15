namespace RoushTech.Myotis.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using RoushTech.Myotis.Messages;
using RoushTech.Myotis.Website.Models;

public class ReadingMailTests
{
    [Test]
    public async Task MessagesListNewestFirstAndSearchSubjectFromAndTo()
    {
        await using var app = new MyotisApp(smtp: true);
        var (admin, mailbox, password) = await SetUpAsync(app);
        await SmtpClients.SendAsync(app, "billing-staging", password, SmtpClients.Message("Reset your password", "casey@example.org"));
        await SmtpClients.SendAsync(app, "billing-staging", password, SmtpClients.Message("Weekly receivables report", "finance@example.org"));

        var all = await admin.GetAsync<List<MessageSummary>>($"/api/v0/mailboxes/{mailbox}/messages");
        var bySubject = await admin.GetAsync<List<MessageSummary>>($"/api/v0/mailboxes/{mailbox}/messages?search=RECEIVABLES");
        var byTo = await admin.GetAsync<List<MessageSummary>>($"/api/v0/mailboxes/{mailbox}/messages?search=casey");
        var byFrom = await admin.GetAsync<List<MessageSummary>>($"/api/v0/mailboxes/{mailbox}/messages?search=billing.example");
        var none = await admin.GetAsync<List<MessageSummary>>($"/api/v0/mailboxes/{mailbox}/messages?search=100%25");

        Assert.That(all.Select(m => m.Subject), Is.EqualTo(new[] { "Weekly receivables report", "Reset your password" }));
        Assert.That(bySubject.Single().Subject, Is.EqualTo("Weekly receivables report"));
        Assert.That(byTo.Single().Subject, Is.EqualTo("Reset your password"));
        Assert.That(byFrom, Has.Count.EqualTo(2));
        Assert.That(none, Is.Empty);
    }

    [Test]
    public async Task TheDetailShowsEnvelopeHeadersBodiesAndAttachments()
    {
        await using var app = new MyotisApp(smtp: true);
        var (admin, mailbox, password) = await SetUpAsync(app);
        var message = Mail.Invoice();
        message.Bcc.Add(MimeKit.MailboxAddress.Parse("archive@example.org"));
        await SmtpClients.SendAsync(app, "billing-staging", password, message);
        var id = (await admin.GetAsync<List<MessageSummary>>($"/api/v0/mailboxes/{mailbox}/messages")).Single().Id;

        var detail = await admin.GetAsync<MessageDetail>($"/api/v0/mailboxes/{mailbox}/messages/{id}");

        Assert.That(detail.EnvelopeTo, Is.EquivalentTo(new[] { "jordan@example.org", "archive@example.org" }));
        Assert.That(detail.Headers.Any(h => h.Key == "Subject" && h.Value == "Your invoice INV-20481 is ready"), Is.True);
        Assert.That(detail.Text, Does.Contain("Invoice INV-20481 is attached."));
        Assert.That(detail.HasHtml, Is.True);
        Assert.That(detail.RemoteImageCount, Is.EqualTo(1));
        Assert.That(detail.Attachments.Single().FileName, Is.EqualTo("INV-20481.pdf"));
        Assert.That(detail.Attachments.Single().Size, Is.EqualTo(Mail.Pdf.Length));
    }

    [Test]
    public async Task HtmlIsServedUnderAPolicyThatBlocksScriptsAndRemoteImagesUntilAsked()
    {
        await using var app = new MyotisApp(smtp: true);
        var (admin, mailbox, password) = await SetUpAsync(app);
        await SmtpClients.SendAsync(app, "billing-staging", password, Mail.Invoice());
        var id = (await admin.GetAsync<List<MessageSummary>>($"/api/v0/mailboxes/{mailbox}/messages")).Single().Id;

        var blocked = await admin.GetAsync($"/api/v0/mailboxes/{mailbox}/messages/{id}/html");
        var allowed = await admin.GetAsync($"/api/v0/mailboxes/{mailbox}/messages/{id}/html?remoteImages=true");
        var html = await blocked.Content.ReadAsStringAsync();
        var policy = string.Join(" ", blocked.Headers.GetValues("Content-Security-Policy"));

        Assert.That(policy, Does.StartWith("default-src 'none'"));
        Assert.That(policy, Does.Contain("img-src data:;"));
        Assert.That(policy, Does.EndWith("; sandbox allow-popups allow-popups-to-escape-sandbox"));
        Assert.That(policy, Does.Not.Contain("script-src"));
        Assert.That(string.Join(" ", allowed.Headers.GetValues("Content-Security-Policy")), Does.Contain("img-src data: https: http:;"));
        Assert.That(blocked.Headers.GetValues("X-Content-Type-Options").Single(), Is.EqualTo("nosniff"));
        Assert.That(html, Does.Contain("src=\"data:image/png;base64,"));
        Assert.That(html, Does.Not.Contain("cid:"));
    }

    [Test]
    public async Task EveryLinkInTheHtmlOpensInANewWindowWhileTheStoredHtmlStaysAsSent()
    {
        await using var app = new MyotisApp(smtp: true);
        var (admin, mailbox, password) = await SetUpAsync(app);
        var sent = """
            <!-- template v2 --><!DOCTYPE html><html><head><base target="_self"></head><body>
            <a href="https://app.example/invite" target="_top" title="opens target=elsewhere">Accept</a>
            <AREA TARGET='_self' href="https://app.example/map"><a data-target="keep" target=_parent href="https://app.example/b">B</a>
            </body></html>
            """;
        var message = SmtpClients.Message("You're invited");
        message.Body = new MimeKit.BodyBuilder { HtmlBody = sent }.ToMessageBody();
        await SmtpClients.SendAsync(app, "billing-staging", password, message);
        var id = (await admin.GetAsync<List<MessageSummary>>($"/api/v0/mailboxes/{mailbox}/messages")).Single().Id;

        var html = await (await admin.GetAsync($"/api/v0/mailboxes/{mailbox}/messages/{id}/html")).Content.ReadAsStringAsync();
        var detail = await admin.GetAsync<MessageDetail>($"/api/v0/mailboxes/{mailbox}/messages/{id}");

        Assert.That(html, Does.StartWith("""<!-- template v2 --><!DOCTYPE html><base target="_blank"><html><head><base target="_self">"""));
        Assert.That(html, Does.Contain("""<a href="https://app.example/invite" title="opens target=elsewhere">"""));
        Assert.That(html, Does.Contain("""<AREA href="https://app.example/map">"""));
        Assert.That(html, Does.Contain("""<a data-target="keep" href="https://app.example/b">"""));
        Assert.That(detail.Html, Does.Contain("target=\"_top\"").And.Not.Contain("_blank"));
    }

    [Test]
    public async Task AttachmentsAndTheRawMessageDownloadAsFilesTheBrowserWontSniff()
    {
        await using var app = new MyotisApp(smtp: true);
        var (admin, mailbox, password) = await SetUpAsync(app);
        await SmtpClients.SendAsync(app, "billing-staging", password, Mail.Invoice());
        var id = (await admin.GetAsync<List<MessageSummary>>($"/api/v0/mailboxes/{mailbox}/messages")).Single().Id;

        var attachment = await admin.GetAsync($"/api/v0/mailboxes/{mailbox}/messages/{id}/attachments/0");
        var eml = await admin.GetAsync($"/api/v0/mailboxes/{mailbox}/messages/{id}/raw?download=true");
        var missing = await admin.GetAsync($"/api/v0/mailboxes/{mailbox}/messages/{id}/attachments/5");

        Assert.That(attachment.Content.Headers.ContentDisposition?.DispositionType, Is.EqualTo("attachment"));
        Assert.That(attachment.Content.Headers.ContentDisposition?.FileName?.Trim('"'), Is.EqualTo("INV-20481.pdf"));
        Assert.That(attachment.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/octet-stream"));
        Assert.That(attachment.Headers.GetValues("X-Content-Type-Options").Single(), Is.EqualTo("nosniff"));
        Assert.That(await attachment.Content.ReadAsByteArrayAsync(), Is.EqualTo(Mail.Pdf));
        Assert.That(eml.Content.Headers.ContentDisposition?.DispositionType, Is.EqualTo("attachment"));
        Assert.That(missing.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task OpeningAMessageMarksItReadForEveryone()
    {
        await using var app = new MyotisApp(smtp: true);
        var (admin, mailbox, password) = await SetUpAsync(app);
        await SmtpClients.SendAsync(app, "billing-staging", password, SmtpClients.Message("Reset your password"));
        var id = (await admin.GetAsync<List<MessageSummary>>($"/api/v0/mailboxes/{mailbox}/messages")).Single().Id;

        var before = await admin.GetAsync<MailboxResponse>($"/api/v0/mailboxes/{mailbox}");
        await admin.PatchAsJsonAsync($"/api/v0/mailboxes/{mailbox}/messages/{id}", new { isRead = true });
        var after = await admin.GetAsync<MailboxResponse>($"/api/v0/mailboxes/{mailbox}");

        Assert.That((before.MessageCount, before.UnreadCount), Is.EqualTo((1, 1)));
        Assert.That((after.MessageCount, after.UnreadCount), Is.EqualTo((1, 0)));
        Assert.That(after.LastReceivedAt, Is.Not.Null);
    }

    [Test]
    public async Task MessagesAreInvisibleWithoutAccessAndCantBeReachedThroughAnotherMailbox()
    {
        await using var app = new MyotisApp(smtp: true);
        var (admin, mailbox, password) = await SetUpAsync(app);
        var other = await admin.CreateMailboxAsync("Billing hotfix");
        await admin.CreateUserAsync("lee@example.test", other.Id);
        using var lee = await app.SignInAsync("lee@example.test", "user password 1");
        await SmtpClients.SendAsync(app, "billing-staging", password, Mail.Invoice());
        var id = (await admin.GetAsync<List<MessageSummary>>($"/api/v0/mailboxes/{mailbox}/messages")).Single().Id;

        var paths = new[] { "messages", $"messages/{id}", $"messages/{id}/html", $"messages/{id}/raw", $"messages/{id}/attachments/0" };
        foreach (var path in paths)
        {
            Assert.That((await lee.GetAsync($"/api/v0/mailboxes/{mailbox}/{path}")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound), path);
        }

        var crossed = await admin.GetAsync($"/api/v0/mailboxes/{other.Id}/messages/{id}");
        Assert.That(crossed.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task LiveMailReachesOnlyPeopleWhoCanReadTheMailbox()
    {
        await using var app = new MyotisApp(smtp: true);
        var (admin, mailbox, password) = await SetUpAsync(app);
        var other = await admin.CreateMailboxAsync("Billing hotfix");
        await admin.CreateUserAsync("dana@example.test", mailbox);
        await admin.CreateUserAsync("lee@example.test", other.Id);

        var adminHears = await ListenAsync(app, MyotisApp.AdminEmail, MyotisApp.AdminPassword);
        var danaHears = await ListenAsync(app, "dana@example.test", "user password 1");
        var leeHears = await ListenAsync(app, "lee@example.test", "user password 1");
        await SmtpClients.SendAsync(app, "billing-staging", password, SmtpClients.Message("Reset your password"));

        Assert.That((await adminHears.Received.Task.WaitAsync(TimeSpan.FromSeconds(10))).Subject, Is.EqualTo("Reset your password"));
        Assert.That((await danaHears.Received.Task.WaitAsync(TimeSpan.FromSeconds(10))).MailboxId, Is.EqualTo(mailbox));
        await Task.Delay(500);
        Assert.That(leeHears.Received.Task.IsCompleted, Is.False);
    }

    private sealed record Listener(HubConnection Connection, TaskCompletionSource<MessageSummary> Received);

    private static async Task<Listener> ListenAsync(MyotisApp app, string email, string password)
    {
        using var browser = app.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var login = await browser.PostAsJsonAsync("/api/v0/auth/login", new { email, password });
        var cookie = string.Join("; ", login.Headers.GetValues("Set-Cookie").Select(c => c.Split(';')[0]));
        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(app.Server.BaseAddress, "hubs/mail"), o =>
            {
                o.Transports = HttpTransportType.LongPolling;
                o.HttpMessageHandlerFactory = _ => app.Server.CreateHandler();
                o.Headers["Cookie"] = cookie;
            })
            .Build();
        var received = new TaskCompletionSource<MessageSummary>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<MessageSummary>("MessageReceived", m => received.TrySetResult(m));
        await connection.StartAsync();
        return new Listener(connection, received);
    }

    private static async Task<(HttpClient Admin, Guid Mailbox, string Password)> SetUpAsync(MyotisApp app)
    {
        var admin = await app.SignInAsAdminAsync();
        var mailbox = await admin.CreateMailboxAsync("Billing staging");
        var credential = await admin.CreateCredentialAsync(mailbox.Id, "billing-staging");
        return (admin, mailbox.Id, credential.Password);
    }
}
