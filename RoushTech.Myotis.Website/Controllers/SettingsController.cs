namespace RoushTech.Myotis.Website.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoushTech.Myotis.Auth;
using RoushTech.Myotis.Settings;
using RoushTech.Myotis.Website.Models;

[ApiController]
[Authorize(Roles = MyotisRoles.Administrator)]
public class SettingsController(SettingsService settings) : ControllerBase
{
    [HttpGet("api/v0/settings")]
    public List<SettingView> List() => settings.GetAll();

    [HttpPatch("api/v0/settings")]
    public async Task<ActionResult<List<SettingView>>> Update(SettingsRequest request, CancellationToken cancellationToken)
    {
        var errors = await settings.SaveAsync(request.Changes.Select(c => new SettingChange(c.Key, c.Value, c.Clear)).ToList(), cancellationToken);
        if (errors.Count > 0)
        {
            return ValidationProblem(new ValidationProblemDetails(errors.ToDictionary(e => e.Key, e => new[] { e.Value })));
        }

        return settings.GetAll();
    }
}
