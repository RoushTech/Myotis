namespace RoushTech.Myotis.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using RoushTech.Myotis.Website.Models;

public class UserTests
{
    private const string Password = "user password 1";

    [Test]
    public async Task APersonSeesOnlyTheMailboxesTheyreAddedTo()
    {
        await using var app = new MyotisApp();
        using var admin = await app.SignInAsAdminAsync();
        var staging = await admin.CreateMailboxAsync("Billing staging");
        var hotfix = await admin.CreateMailboxAsync("Billing hotfix");
        await admin.CreateUserAsync("dana@example.test", staging.Id);
        using var dana = await app.SignInAsync("dana@example.test", Password);

        var list = await dana.GetAsync<List<MailboxResponse>>("/api/v0/mailboxes");
        var granted = await dana.GetAsync($"/api/v0/mailboxes/{staging.Id}");
        var other = await dana.GetAsync($"/api/v0/mailboxes/{hotfix.Id}");

        Assert.That(list.Select(m => m.Name), Is.EqualTo(new[] { "Billing staging" }));
        Assert.That(granted.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(other.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task RemovingSomeoneFromAMailboxHidesItFromThem()
    {
        await using var app = new MyotisApp();
        using var admin = await app.SignInAsAdminAsync();
        var staging = await admin.CreateMailboxAsync("Billing staging");
        var dana = await admin.CreateUserAsync("dana@example.test", staging.Id);
        using var browser = await app.SignInAsync("dana@example.test", Password);

        var remove = await admin.DeleteAsync($"/api/v0/mailboxes/{staging.Id}/people/{dana.Id}");
        using var again = await app.SignInAsync("dana@example.test", Password);
        var after = await again.GetAsync($"/api/v0/mailboxes/{staging.Id}");

        Assert.That(remove.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(after.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task AdministratorsAddPeopleFromTheMailboxToo()
    {
        await using var app = new MyotisApp();
        using var admin = await app.SignInAsAdminAsync();
        var staging = await admin.CreateMailboxAsync("Billing staging");
        var lee = await admin.CreateUserAsync("lee@example.test");

        var people = await admin.PostAsync<List<PersonResponse>>($"/api/v0/mailboxes/{staging.Id}/people", new { userId = lee.Id });
        using var browser = await app.SignInAsync("lee@example.test", Password);
        var list = await browser.GetAsync<List<MailboxResponse>>("/api/v0/mailboxes");

        Assert.That(people.Select(p => (p.Email, p.IsAdministrator)), Is.EquivalentTo(new[] { (MyotisApp.AdminEmail, true), ("lee@example.test", false) }));
        Assert.That(list.Single().Id, Is.EqualTo(staging.Id));
    }

    [Test]
    public async Task OnlyAdministratorsManageUsers()
    {
        await using var app = new MyotisApp();
        using var admin = await app.SignInAsAdminAsync();
        await admin.CreateUserAsync("dana@example.test");
        using var dana = await app.SignInAsync("dana@example.test", Password);

        var list = await dana.GetAsync("/api/v0/users");
        var create = await dana.PostAsJsonAsync("/api/v0/users", new { email = "sneaky@example.test", password = Password });

        Assert.That(list.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        Assert.That(create.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task SigningInIsRecorded()
    {
        await using var app = new MyotisApp();
        using var admin = await app.SignInAsAdminAsync();
        await admin.CreateUserAsync("dana@example.test");

        var before = (await admin.GetAsync<List<UserResponse>>("/api/v0/users")).Single(u => u.Email == "dana@example.test");
        using var dana = await app.SignInAsync("dana@example.test", Password);
        var after = (await admin.GetAsync<List<UserResponse>>("/api/v0/users")).Single(u => u.Email == "dana@example.test");

        Assert.That(before.LastSignInAt, Is.Null);
        Assert.That(after.LastSignInAt, Is.Not.Null);
    }

    [Test]
    public async Task AnAdministratorCanSetSomeonesPassword()
    {
        await using var app = new MyotisApp();
        using var admin = await app.SignInAsAdminAsync();
        var dana = await admin.CreateUserAsync("dana@example.test");

        var weak = await admin.PatchAsJsonAsync($"/api/v0/users/{dana.Id}", new { password = "short" });
        var reset = await admin.PatchAsJsonAsync($"/api/v0/users/{dana.Id}", new { password = "brand new 2" });
        using var browser = app.CreateClient();
        var old = await browser.PostAsJsonAsync("/api/v0/auth/login", new { email = "dana@example.test", password = Password });
        var current = await browser.PostAsJsonAsync("/api/v0/auth/login", new { email = "dana@example.test", password = "brand new 2" });

        Assert.That(weak.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(reset.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(old.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(current.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task AdministratorsCannotLockThemselvesOut()
    {
        await using var app = new MyotisApp();
        using var admin = await app.SignInAsAdminAsync();
        var me = await admin.GetAsync<MeResponse>("/api/v0/auth/me");

        var demote = await admin.PatchAsJsonAsync($"/api/v0/users/{me.Id}", new { isAdministrator = false });
        var delete = await admin.DeleteAsync($"/api/v0/users/{me.Id}");

        Assert.That((await demote.Content.ReadFromJsonAsync<ProblemDetails>())?.Title, Is.EqualTo("You can't remove your own administrator role"));
        Assert.That((await delete.Content.ReadFromJsonAsync<ProblemDetails>())?.Title, Is.EqualTo("You can't delete your own account"));
    }

    [Test]
    public async Task ADeletedUserCannotSignIn()
    {
        await using var app = new MyotisApp();
        using var admin = await app.SignInAsAdminAsync();
        var staging = await admin.CreateMailboxAsync("Billing staging");
        var dana = await admin.CreateUserAsync("dana@example.test", staging.Id);

        var delete = await admin.DeleteAsync($"/api/v0/users/{dana.Id}");
        using var browser = app.CreateClient();
        var login = await browser.PostAsJsonAsync("/api/v0/auth/login", new { email = "dana@example.test", password = Password });
        var people = await admin.GetAsync<List<PersonResponse>>($"/api/v0/mailboxes/{staging.Id}/people");

        Assert.That(delete.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(login.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(people.Select(p => p.Email), Is.EqualTo(new[] { MyotisApp.AdminEmail }));
    }

    [Test]
    public async Task APromotedUserSeesEverythingAndADemotedOneOnlyTheirOwn()
    {
        await using var app = new MyotisApp();
        using var admin = await app.SignInAsAdminAsync();
        var staging = await admin.CreateMailboxAsync("Billing staging");
        await admin.CreateMailboxAsync("Billing hotfix");
        var dana = await admin.CreateUserAsync("dana@example.test", staging.Id);

        await admin.PatchAsJsonAsync($"/api/v0/users/{dana.Id}", new { isAdministrator = true });
        using (var promoted = await app.SignInAsync("dana@example.test", Password))
        {
            Assert.That(await promoted.GetAsync<List<MailboxResponse>>("/api/v0/mailboxes"), Has.Count.EqualTo(2));
        }

        await admin.PatchAsJsonAsync($"/api/v0/users/{dana.Id}", new { isAdministrator = false });
        using var demoted = await app.SignInAsync("dana@example.test", Password);
        Assert.That(await demoted.GetAsync<List<MailboxResponse>>("/api/v0/mailboxes"), Has.Count.EqualTo(1));
    }
}
