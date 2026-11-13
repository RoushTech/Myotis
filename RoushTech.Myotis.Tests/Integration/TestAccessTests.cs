namespace RoushTech.Myotis.Tests.Integration;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using RoushTech.Myotis.Website.Models;
using RoushTech.Myotis.Website.Models.V1;

public class TestAccessTests
{
    [Test]
    public async Task ATestSuiteReadsTheResetEmailAnAppJustSentAndFindsItsLink()
    {
        await using var app = new MyotisApp(smtp: true);
        var (admin, mailbox, password) = await SetUpAsync(app);
        using var suite = TokenClient(app, await TokenAsync(admin));
        var before = DateTime.UtcNow.AddSeconds(-1);
        await SmtpClients.SendAsync(app, "billing-staging", password, ResetEmail());

        var mailboxes = await suite.GetAsync<List<MailboxV1>>("/api/v1/mailboxes");
        var listed = await suite.GetAsync<List<MessageSummaryV1>>($"/api/v1/mailboxes/{mailbox}/messages?since={before:O}&search=casey");
        var message = await suite.GetAsync<MessageV1>($"/api/v1/mailboxes/{mailbox}/messages/{listed.Single().Id}");
        var empty = await suite.DeleteAsync($"/api/v1/mailboxes/{mailbox}/messages");
        var after = await suite.GetAsync<List<MessageSummaryV1>>($"/api/v1/mailboxes/{mailbox}/messages");

        Assert.That(mailboxes.Single().Name, Is.EqualTo("Billing staging"));
        Assert.That(message.Subject, Is.EqualTo("Reset your password"));
        Assert.That(message.Links, Is.EqualTo(new[] { "https://billing.example/reset?token=abc&user=1", "https://billing.example/help" }));
        Assert.That(empty.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(after, Is.Empty);
    }

    [Test]
    public async Task TokensOnlyWorkOnTheTestApiAndTheBrowserCookieNeverDoes()
    {
        await using var app = new MyotisApp();
        using var admin = await app.SignInAsAdminAsync();
        using var token = TokenClient(app, await TokenAsync(admin));

        var cookieOnV1 = await admin.GetAsync("/api/v1/mailboxes");
        var cookieOnMcp = await admin.PostAsJsonAsync("/mcp", new { jsonrpc = "2.0", id = 1, method = "tools/list" });
        var tokenOnV0 = await token.GetAsync("/api/v0/mailboxes");
        var tokenOnV1 = await token.GetAsync("/api/v1/mailboxes");

        Assert.That(cookieOnV1.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(cookieOnMcp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(tokenOnV0.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(tokenOnV1.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task ATokenReachesOnlyItsOwnersMailboxesAndStopsWhenRevoked()
    {
        await using var app = new MyotisApp(smtp: true);
        var (admin, mailbox, _) = await SetUpAsync(app);
        var other = await admin.CreateMailboxAsync("Billing hotfix");
        await admin.CreateUserAsync("dana@example.test", other.Id);
        using var dana = await app.SignInAsync("dana@example.test", "user password 1");
        var created = await dana.PostAsync<CreatedTokenResponse>("/api/v0/tokens", new { name = "Playwright suite" });
        using var token = TokenClient(app, created.PlainText);

        var visible = await token.GetAsync<List<MailboxV1>>("/api/v1/mailboxes");
        var hidden = await token.GetAsync($"/api/v1/mailboxes/{mailbox}/messages");
        var listedTokens = await dana.GetStringAsync("/api/v0/tokens");
        await dana.DeleteAsync($"/api/v0/tokens/{created.Token.Id}");
        var revoked = await token.GetAsync("/api/v1/mailboxes");

        Assert.That(created.PlainText, Does.StartWith("myotis_pat_"));
        Assert.That(visible.Select(m => m.Name), Is.EqualTo(new[] { "Billing hotfix" }));
        Assert.That(hidden.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(listedTokens, Does.Not.Contain(created.PlainText));
        Assert.That(revoked.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task AnAgentFindsTheEmailThroughMcpButOnlyInMailboxesItsOwnerCanRead()
    {
        await using var app = new MyotisApp(smtp: true);
        var (admin, _, password) = await SetUpAsync(app);
        var other = await admin.CreateMailboxAsync("Billing hotfix");
        await admin.CreateUserAsync("dana@example.test", other.Id);
        using var dana = await app.SignInAsync("dana@example.test", "user password 1");
        await SmtpClients.SendAsync(app, "billing-staging", password, ResetEmail());

        await using var adminAgent = await ConnectAsync(app, await TokenAsync(admin));
        await using var danaAgent = await ConnectAsync(app, (await dana.PostAsync<CreatedTokenResponse>("/api/v0/tokens", new { name = "agent" })).PlainText);
        var tools = await adminAgent.ListToolsAsync();
        var listed = await CallAsync(adminAgent, "list_messages", new() { ["mailbox"] = "billing STAGING" });
        var id = listed[0].GetProperty("id").GetGuid();
        var message = await CallAsync(adminAgent, "read_message", new() { ["mailbox"] = "Billing staging", ["message"] = id });
        var danaMailboxes = await CallAsync(danaAgent, "list_mailboxes", []);
        var danaTry = await danaAgent.CallToolAsync("read_message", new Dictionary<string, object?> { ["mailbox"] = "Billing staging", ["message"] = id });
        var crossed = await adminAgent.CallToolAsync("read_message", new Dictionary<string, object?> { ["mailbox"] = "Billing hotfix", ["message"] = id });

        Assert.That(tools.Select(t => t.Name), Is.EquivalentTo(new[] { "list_mailboxes", "list_messages", "read_message", "empty_mailbox" }));
        Assert.That(message.GetProperty("links")[0].GetString(), Is.EqualTo("https://billing.example/reset?token=abc&user=1"));
        Assert.That(danaMailboxes.EnumerateArray().Select(m => m.GetProperty("name").GetString()), Is.EqualTo(new[] { "Billing hotfix" }));
        Assert.That(danaTry.IsError, Is.True);
        Assert.That(Text(danaTry), Does.Contain("No mailbox called Billing staging"));
        Assert.That(crossed.IsError, Is.True);
    }

    private static MimeKit.MimeMessage ResetEmail()
    {
        var message = SmtpClients.Message("Reset your password", "casey@example.org");
        message.Body = new MimeKit.BodyBuilder
        {
            HtmlBody = """<p><a href="https://billing.example/reset?token=abc&amp;user=1">Reset password</a></p>""",
            TextBody = "Reset: https://billing.example/reset?token=abc&user=1\nHelp: https://billing.example/help.",
        }.ToMessageBody();
        return message;
    }

    private static async Task<string> TokenAsync(HttpClient owner) =>
        (await owner.PostAsync<CreatedTokenResponse>("/api/v0/tokens", new { name = "tests" })).PlainText;

    private static HttpClient TokenClient(MyotisApp app, string token)
    {
        var client = app.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { HandleCookies = false });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static Task<McpClient> ConnectAsync(MyotisApp app, string token) =>
        McpClient.CreateAsync(new HttpClientTransport(
            new HttpClientTransportOptions { Endpoint = new Uri("http://localhost/mcp"), TransportMode = HttpTransportMode.StreamableHttp },
            TokenClient(app, token),
            loggerFactory: null,
            ownsHttpClient: true));

    private static async Task<JsonElement> CallAsync(McpClient mcp, string tool, Dictionary<string, object?> arguments)
    {
        var result = await mcp.CallToolAsync(tool, arguments);
        Assert.That(result.IsError, Is.Not.True, Text(result));
        return JsonDocument.Parse(Text(result)).RootElement;
    }

    private static string Text(CallToolResult result) => string.Join("\n", result.Content.OfType<TextContentBlock>().Select(c => c.Text));

    private static async Task<(HttpClient Admin, Guid Mailbox, string Password)> SetUpAsync(MyotisApp app)
    {
        var admin = await app.SignInAsAdminAsync();
        var mailbox = await admin.CreateMailboxAsync("Billing staging");
        var credential = await admin.CreateCredentialAsync(mailbox.Id, "billing-staging");
        return (admin, mailbox.Id, credential.Password);
    }
}
