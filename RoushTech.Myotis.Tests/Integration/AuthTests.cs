namespace RoushTech.Myotis.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using RoushTech.Myotis.Website.Models;

public class AuthTests
{
    [Test]
    public async Task TheFirstPersonToSetUpBecomesAnAdministrator()
    {
        await using var app = new MyotisApp(seedAdministrator: false);
        using var browser = app.CreateClient();

        var before = await browser.GetAsync<AuthStateResponse>("/api/v0/auth/state");
        var setup = await browser.PostAsJsonAsync("/api/v0/auth/setup", new { email = "first@example.test", password = "first password 1" });
        var me = await browser.GetAsync<MeResponse>("/api/v0/auth/me");
        var after = await browser.GetAsync<AuthStateResponse>("/api/v0/auth/state");

        Assert.That(before.SetupRequired, Is.True);
        Assert.That(setup.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(me.Email, Is.EqualTo("first@example.test"));
        Assert.That(me.IsAdministrator, Is.True);
        Assert.That(after.SetupRequired, Is.False);
    }

    [Test]
    public async Task SetupCannotRunTwice()
    {
        await using var app = new MyotisApp();
        using var stranger = app.CreateClient();

        var response = await stranger.PostAsJsonAsync("/api/v0/auth/setup", new { email = "intruder@example.test", password = "intruder 1" });
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(problem?.Title, Is.EqualTo("Setup is already done"));
    }

    [Test]
    public async Task AWrongPasswordIsRefusedWithoutSayingWhichPartWasWrong()
    {
        await using var app = new MyotisApp();
        using var browser = app.CreateClient();

        var wrongPassword = await browser.PostAsJsonAsync("/api/v0/auth/login", new { email = MyotisApp.AdminEmail, password = "nope" });
        var unknownEmail = await browser.PostAsJsonAsync("/api/v0/auth/login", new { email = "nobody@example.test", password = "nope" });

        Assert.That(wrongPassword.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That((await wrongPassword.Content.ReadFromJsonAsync<ProblemDetails>())?.Title, Is.EqualTo("Wrong email or password"));
        Assert.That((await unknownEmail.Content.ReadFromJsonAsync<ProblemDetails>())?.Title, Is.EqualTo("Wrong email or password"));
    }
}
