namespace RoushTech.Myotis.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using RoushTech.Myotis.Website.Models;

public class MailboxTests
{
    [Test]
    public async Task AnAdministratorCreatesAndListsMailboxes()
    {
        await using var app = new MyotisApp();
        using var admin = await app.SignInAsAdminAsync();

        var staging = await admin.CreateMailboxAsync("Billing staging");
        await admin.CreateMailboxAsync("Billing hotfix");
        var list = await admin.GetAsync<List<MailboxResponse>>("/api/v0/mailboxes");
        var single = await admin.GetAsync<MailboxResponse>($"/api/v0/mailboxes/{staging.Id}");

        Assert.That(list.Select(m => m.Name), Is.EqualTo(new[] { "Billing hotfix", "Billing staging" }));
        Assert.That(single.Name, Is.EqualTo("Billing staging"));
    }

    [Test]
    public async Task MailboxNamesAreUniqueWhateverTheirCase()
    {
        await using var app = new MyotisApp();
        using var admin = await app.SignInAsAdminAsync();
        await admin.CreateMailboxAsync("Billing staging");

        var response = await admin.PostAsJsonAsync("/api/v0/mailboxes", new { name = "BILLING STAGING" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That((await response.Content.ReadFromJsonAsync<ProblemDetails>())?.Title, Is.EqualTo("There's already a mailbox called BILLING STAGING"));
    }

    [Test]
    public async Task SomeoneWithoutAccessCannotTellAMailboxExists()
    {
        await using var app = new MyotisApp();
        using var admin = await app.SignInAsAdminAsync();
        using var user = await app.SignInAsNewUserAsync();
        var mailbox = await admin.CreateMailboxAsync("Billing staging");

        var list = await user.GetAsync<List<MailboxResponse>>("/api/v0/mailboxes");
        var existing = await user.GetAsync($"/api/v0/mailboxes/{mailbox.Id}");
        var missing = await user.GetAsync($"/api/v0/mailboxes/{Guid.NewGuid()}");

        Assert.That(list, Is.Empty);
        Assert.That(existing.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(missing.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task OnlyAdministratorsManageMailboxes()
    {
        await using var app = new MyotisApp();
        using var admin = await app.SignInAsAdminAsync();
        using var user = await app.SignInAsNewUserAsync();
        var mailbox = await admin.CreateMailboxAsync("Billing staging");

        var create = await user.PostAsJsonAsync("/api/v0/mailboxes", new { name = "Mine" });
        var delete = await user.DeleteAsync($"/api/v0/mailboxes/{mailbox.Id}");
        var credentials = await user.GetAsync($"/api/v0/mailboxes/{mailbox.Id}/credentials");

        Assert.That(create.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        Assert.That(delete.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        Assert.That(credentials.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task DeletingAMailboxRevokesItsCredentials()
    {
        await using var app = new MyotisApp();
        using var admin = await app.SignInAsAdminAsync();
        var mailbox = await admin.CreateMailboxAsync("Billing staging");
        var created = await admin.CreateCredentialAsync(mailbox.Id, "billing-staging");

        var delete = await admin.DeleteAsync($"/api/v0/mailboxes/{mailbox.Id}");
        var after = await admin.GetAsync($"/api/v0/mailboxes/{mailbox.Id}");
        var verified = await app.VerifyAsync("billing-staging", created.Password);

        Assert.That(delete.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(after.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(verified, Is.Null);
    }
}
