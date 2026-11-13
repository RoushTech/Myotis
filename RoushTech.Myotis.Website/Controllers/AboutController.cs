namespace RoushTech.Myotis.Website.Controllers;

using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoushTech.Myotis.Website.Models;

[ApiController]
[Authorize]
public class AboutController : ControllerBase
{
    private static readonly string Version = ShortVersion(
        typeof(AboutController).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown");

    [HttpGet("api/v0/about")]
    public AboutResponse Get() => new(Version);

    // 0.1.0+d14cbe45e2aad39... becomes 0.1.0+d14cbe4.
    private static string ShortVersion(string version)
    {
        var plus = version.IndexOf('+');
        return plus >= 0 && version.Length > plus + 8 ? version[..(plus + 8)] : version;
    }
}
