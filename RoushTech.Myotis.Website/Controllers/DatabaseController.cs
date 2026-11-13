namespace RoushTech.Myotis.Website.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoushTech.Myotis.Auth;
using RoushTech.Myotis.Health;

[ApiController]
[Authorize(Roles = MyotisRoles.Administrator)]
public class DatabaseController(DatabaseHealthService health) : ControllerBase
{
    [HttpGet("api/v0/database")]
    public Task<DatabaseHealth> Get(CancellationToken cancellationToken) => health.ReportAsync(cancellationToken);

    [HttpPost("api/v0/database/check")]
    public Task<IntegrityResult> Check(CancellationToken cancellationToken) => health.CheckAsync(cancellationToken);
}
