namespace RoushTech.Myotis.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using RoushTech.Myotis.Settings;

public class SettingsTests
{
    private const string SizeKey = "Myotis:Limits:MaxMessageMegabytes";

    [Test]
    public async Task LimitsStartAtTheirDefaults()
    {
        await using var app = new MyotisApp();
        using var admin = await app.SignInAsAdminAsync();

        var settings = await admin.GetAsync<List<SettingView>>("/api/v0/settings");

        Assert.That(settings.ToDictionary(s => s.Key, s => s.Value), Is.EquivalentTo(new Dictionary<string, int>
        {
            [SizeKey] = 25,
            ["Myotis:Limits:KeepMessages"] = 1000,
            ["Myotis:Limits:KeepDays"] = 14,
            ["Myotis:Limits:MessagesPerMinute"] = 300,
            ["Myotis:Limits:AttemptsPerConnection"] = 3,
            ["Myotis:Limits:BlockAfterFailures"] = 10,
            ["Myotis:Limits:BlockWindowMinutes"] = 10,
            ["Myotis:Limits:BlockMinutes"] = 15,
        }));
    }

    [Test]
    public async Task AnAdministratorChangesALimitAndCanPutItBack()
    {
        await using var app = new MyotisApp();
        using var admin = await app.SignInAsAdminAsync();

        var changed = await Change(admin, new { key = "Myotis:Limits:KeepDays", value = "30" });
        var cleared = await Change(admin, new { key = "Myotis:Limits:KeepDays", clear = true });

        Assert.That(changed.Single(s => s.Key == "Myotis:Limits:KeepDays").Value, Is.EqualTo(30));
        Assert.That(cleared.Single(s => s.Key == "Myotis:Limits:KeepDays").Value, Is.EqualTo(14));
    }

    [Test]
    public async Task ABadValueSavesNothing()
    {
        await using var app = new MyotisApp();
        using var admin = await app.SignInAsAdminAsync();

        var response = await admin.PatchAsJsonAsync("/api/v0/settings", new
        {
            changes = new object[] { new { key = "Myotis:Limits:KeepDays", value = "30" }, new { key = SizeKey, value = "0" } },
        });
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        var after = await admin.GetAsync<List<SettingView>>("/api/v0/settings");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(problem?.Errors[SizeKey], Is.EqualTo(new[] { "Enter a whole number from 1 to 1,024." }));
        Assert.That(after.Single(s => s.Key == "Myotis:Limits:KeepDays").Value, Is.EqualTo(14));
    }

    [Test]
    public async Task OnlyAdministratorsSeeOrChangeLimits()
    {
        await using var app = new MyotisApp();
        using var admin = await app.SignInAsAdminAsync();
        await admin.CreateUserAsync("dana@example.test");
        using var dana = await app.SignInAsync("dana@example.test", "user password 1");

        var read = await dana.GetAsync("/api/v0/settings");
        var write = await dana.PatchAsJsonAsync("/api/v0/settings", new { changes = new[] { new { key = SizeKey, value = "500" } } });

        Assert.That(read.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        Assert.That(write.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task ANewSizeCapAppliesToTheNextMessageWithoutARestart()
    {
        await using var app = new MyotisApp(smtp: true);
        using var admin = await app.SignInAsAdminAsync();
        var mailbox = await admin.CreateMailboxAsync("Billing staging");
        var credential = await admin.CreateCredentialAsync(mailbox.Id, "billing-staging");

        var before = await SmtpClients.SendIgnoringSizeAsync(app, "billing-staging", credential.Password, 2 * 1024 * 1024);
        await Change(admin, new { key = SizeKey, value = "1" });
        var after = await SmtpClients.SendIgnoringSizeAsync(app, "billing-staging", credential.Password, 2 * 1024 * 1024);

        Assert.That(before, Does.StartWith("250"));
        Assert.That(after, Does.StartWith("552"));
    }

    private static async Task<List<SettingView>> Change(HttpClient admin, object change)
    {
        var response = await admin.PatchAsJsonAsync("/api/v0/settings", new { changes = new[] { change } });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<SettingView>>())!;
    }
}
