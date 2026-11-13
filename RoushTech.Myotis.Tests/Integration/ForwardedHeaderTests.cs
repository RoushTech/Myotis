namespace RoushTech.Myotis.Tests.Integration;

using System.Net.Http.Json;

public class ForwardedHeaderTests
{
    [Test]
    public async Task AForwardedProtoFromAnUntrustedClientDoesNotMakeTheCookieSecure()
    {
        await using var app = new MyotisApp();
        using var browser = app.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { HandleCookies = false });
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v0/auth/login")
        {
            Content = JsonContent.Create(new { email = MyotisApp.AdminEmail, password = MyotisApp.AdminPassword }),
        };
        request.Headers.Add("X-Forwarded-Proto", "https");

        var response = await browser.SendAsync(request);
        var cookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("myotis=", StringComparison.Ordinal));

        Assert.That(cookie, Does.Not.Contain("; secure").IgnoreCase);
    }
}
