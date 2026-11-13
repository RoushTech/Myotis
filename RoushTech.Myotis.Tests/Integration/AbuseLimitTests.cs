namespace RoushTech.Myotis.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using RoushTech.Myotis.Settings;
using RoushTech.Myotis.Smtp;

public class AbuseLimitTests
{
    [Test]
    public async Task AConnectionClosesAfterTooManyWrongPasswords()
    {
        await using var app = new MyotisApp(smtp: true);
        await SetUpAsync(app);
        await using var smtp = await RawSmtp.ConnectAsync(app);

        var first = await smtp.SignInAsync("billing-staging", "wrong");
        var second = await smtp.SignInAsync("billing-staging", "wrong");
        var third = await smtp.SignInAsync("billing-staging", "wrong");

        Assert.That(first, Does.StartWith("535"));
        Assert.That(second, Does.StartWith("535"));
        Assert.That(third, Does.StartWith("421"));
    }

    [Test]
    public async Task AnAddressThatKeepsGuessingIsBlockedEvenWithTheRightPassword()
    {
        await using var app = new MyotisApp(smtp: true);
        var (admin, password) = await SetUpAsync(app);
        await LimitAsync(admin, "BlockAfterFailures", "3");

        for (var i = 0; i < 2; i++)
        {
            await using var guess = await RawSmtp.ConnectAsync(app);
            Assert.That(await guess.SignInAsync("billing-staging", "wrong"), Does.StartWith("535"));
        }

        await using (var last = await RawSmtp.ConnectAsync(app))
        {
            Assert.That(await last.SignInAsync("billing-staging", "wrong"), Does.StartWith("421"));
        }

        await using var honest = await RawSmtp.ConnectAsync(app);
        Assert.That(await honest.SignInAsync("billing-staging", password), Does.StartWith("421"));
    }

    [Test]
    public async Task BehindATrustedProxyEachSenderIsJudgedOnItsOwnAddress()
    {
        await using var app = new MyotisApp(smtp: true, settings: new Dictionary<string, string> { ["Myotis:Smtp:TrustedProxies"] = "127.0.0.0/8" });
        var (admin, password) = await SetUpAsync(app);
        await LimitAsync(admin, "BlockAfterFailures", "2");

        for (var i = 0; i < 2; i++)
        {
            await using var attacker = await RawSmtp.ConnectAsync(app, proxySource: "203.0.113.9");
            await attacker.SignInAsync("billing-staging", "wrong");
        }

        await using var blocked = await RawSmtp.ConnectAsync(app, proxySource: "203.0.113.9");
        await using var other = await RawSmtp.ConnectAsync(app, proxySource: "198.51.100.4");
        Assert.That(await blocked.SignInAsync("billing-staging", password), Does.StartWith("421"));
        Assert.That(await other.SignInAsync("billing-staging", password), Does.StartWith("235"));
    }

    [Test]
    public async Task APROXYHeaderFromAnUntrustedClientIsIgnored()
    {
        await using var app = new MyotisApp(smtp: true);
        var (admin, password) = await SetUpAsync(app);
        await LimitAsync(admin, "BlockAfterFailures", "2");

        for (var i = 0; i < 2; i++)
        {
            await using var attacker = await RawSmtp.ConnectAsync(app, proxySource: "203.0.113.9");
            await attacker.SignInAsync("billing-staging", "wrong");
        }

        await using var spoofed = await RawSmtp.ConnectAsync(app, proxySource: "198.51.100.4");
        Assert.That(await spoofed.SignInAsync("billing-staging", password), Does.StartWith("421"));
    }

    [Test]
    public async Task ACredentialOverItsSendingRateIsToldToRetryLater()
    {
        await using var app = new MyotisApp(smtp: true);
        var (admin, password) = await SetUpAsync(app);
        await LimitAsync(admin, "MessagesPerMinute", "2");
        using var client = await SmtpClients.ConnectAsync(app);
        await client.AuthenticateAsync("billing-staging", password);

        await client.SendAsync(SmtpClients.Message("One"));
        await client.SendAsync(SmtpClients.Message("Two"));
        var refused = Assert.ThrowsAsync<SmtpCommandException>(() => client.SendAsync(SmtpClients.Message("Three")));

        Assert.That((int?)refused?.StatusCode, Is.EqualTo(451));
    }

    [Test]
    public void ABlockLiftsOnceItsTimeIsUp()
    {
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var limits = new OptionsMonitorStub(new LimitsOptions { BlockAfterFailures = 2, BlockWindowMinutes = 10, BlockMinutes = 15 });
        var guard = new SignInGuard(limits, time);
        var address = IPAddress.Parse("203.0.113.9");

        guard.RecordFailure(address);
        time.Advance(TimeSpan.FromMinutes(11));
        var afterWindow = guard.RecordFailure(address);
        guard.RecordFailure(address);
        var blocked = guard.IsBlocked(address);
        time.Advance(TimeSpan.FromMinutes(16));

        Assert.That(afterWindow, Is.False, "the first failure fell outside the window");
        Assert.That(blocked, Is.True);
        Assert.That(guard.IsBlocked(address), Is.False);
    }

    private sealed class OptionsMonitorStub(LimitsOptions value) : IOptionsMonitor<LimitsOptions>
    {
        public LimitsOptions CurrentValue => value;

        public LimitsOptions Get(string? name) => value;

        public IDisposable? OnChange(Action<LimitsOptions, string?> listener) => null;
    }

    private static async Task LimitAsync(HttpClient admin, string name, string value)
    {
        var response = await admin.PatchAsJsonAsync("/api/v0/settings", new { changes = new[] { new { key = $"Myotis:Limits:{name}", value } } });
        response.EnsureSuccessStatusCode();
    }

    private static async Task<(HttpClient Admin, string Password)> SetUpAsync(MyotisApp app)
    {
        var admin = await app.SignInAsAdminAsync();
        var mailbox = await admin.CreateMailboxAsync("Billing staging");
        var credential = await admin.CreateCredentialAsync(mailbox.Id, "billing-staging");
        await LimitAsync(admin, "AttemptsPerConnection", "3");
        return (admin, credential.Password);
    }
}
