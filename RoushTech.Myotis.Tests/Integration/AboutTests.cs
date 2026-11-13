namespace RoushTech.Myotis.Tests.Integration;

using System.Net;
using RoushTech.Myotis.Website.Models;

public class AboutTests
{
    [Test]
    public async Task PeopleWhoAreSignedInSeeTheShortVersion()
    {
        await using var app = new MyotisApp();
        using var admin = await app.SignInAsAdminAsync();
        using var anonymous = app.CreateClient();

        var about = await admin.GetAsync<AboutResponse>("/api/v0/about");
        var nobody = await anonymous.GetAsync("/api/v0/about");

        Assert.That(about.Version, Does.Match(@"^\d+\.\d+\.\d+(\.\d+)?(\+[0-9a-f]{7})?$"));
        Assert.That(nobody.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}
