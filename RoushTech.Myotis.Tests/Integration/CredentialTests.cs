namespace RoushTech.Myotis.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using RoushTech.Myotis.Mailboxes;

public static class CredentialVerification
{
    public static Task<Guid?> VerifyAsync(this MyotisApp app, string username, string password) =>
        app.WithServiceAsync<SmtpCredentialService, Guid?>(s => s.VerifyAsync(username, password, CancellationToken.None));
}

public class CredentialTests
{
    [Test]
    public async Task ThePasswordIsShownOnceAndNeverReturnedAgain()
    {
        await using var app = new MyotisApp();
        using var admin = await app.SignInAsAdminAsync();
        var mailbox = await admin.CreateMailboxAsync("Billing staging");

        var created = await admin.CreateCredentialAsync(mailbox.Id, "billing-staging");
        var listed = await admin.GetStringAsync($"/api/v0/mailboxes/{mailbox.Id}/credentials");

        Assert.That(created.Password, Has.Length.EqualTo(24));
        Assert.That(listed, Does.Contain("billing-staging"));
        Assert.That(listed, Does.Not.Contain(created.Password));
        Assert.That(listed, Does.Not.Contain("hash").IgnoreCase);
    }

    [Test]
    public async Task TheRightPasswordReachesTheCredentialsMailboxAndNothingElseDoes()
    {
        await using var app = new MyotisApp();
        using var admin = await app.SignInAsAdminAsync();
        var staging = await admin.CreateMailboxAsync("Billing staging");
        var hotfix = await admin.CreateMailboxAsync("Billing hotfix");
        var stagingCredential = await admin.CreateCredentialAsync(staging.Id, "billing-staging");
        var hotfixCredential = await admin.CreateCredentialAsync(hotfix.Id, "billing-hotfix");

        Assert.That(await app.VerifyAsync("billing-staging", stagingCredential.Password), Is.EqualTo(staging.Id));
        Assert.That(await app.VerifyAsync("BILLING-HOTFIX", hotfixCredential.Password), Is.EqualTo(hotfix.Id));
        Assert.That(await app.VerifyAsync("billing-staging", hotfixCredential.Password), Is.Null);
        Assert.That(await app.VerifyAsync("nobody", stagingCredential.Password), Is.Null);
    }

    [Test]
    public async Task ARevokedCredentialStopsWorking()
    {
        await using var app = new MyotisApp();
        using var admin = await app.SignInAsAdminAsync();
        var mailbox = await admin.CreateMailboxAsync("Billing staging");
        var created = await admin.CreateCredentialAsync(mailbox.Id, "billing-staging");

        var revoke = await admin.DeleteAsync($"/api/v0/mailboxes/{mailbox.Id}/credentials/{created.Credential.Id}");

        Assert.That(revoke.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(await app.VerifyAsync("billing-staging", created.Password), Is.Null);
    }

    [Test]
    public async Task UsernamesAreUniqueAcrossMailboxes()
    {
        await using var app = new MyotisApp();
        using var admin = await app.SignInAsAdminAsync();
        var staging = await admin.CreateMailboxAsync("Billing staging");
        var hotfix = await admin.CreateMailboxAsync("Billing hotfix");
        await admin.CreateCredentialAsync(staging.Id, "shared-name");

        var response = await admin.PostAsJsonAsync($"/api/v0/mailboxes/{hotfix.Id}/credentials", new { username = "Shared-Name" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task ACredentialCannotBeReachedThroughAnotherMailbox()
    {
        await using var app = new MyotisApp();
        using var admin = await app.SignInAsAdminAsync();
        var staging = await admin.CreateMailboxAsync("Billing staging");
        var hotfix = await admin.CreateMailboxAsync("Billing hotfix");
        var created = await admin.CreateCredentialAsync(staging.Id, "billing-staging");

        var response = await admin.DeleteAsync($"/api/v0/mailboxes/{hotfix.Id}/credentials/{created.Credential.Id}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(await app.VerifyAsync("billing-staging", created.Password), Is.EqualTo(staging.Id));
    }
}
