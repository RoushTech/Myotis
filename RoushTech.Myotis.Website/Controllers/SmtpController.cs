namespace RoushTech.Myotis.Website.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoushTech.Myotis.Smtp;
using RoushTech.Myotis.Website.Models;

[ApiController]
[Authorize]
public class SmtpController(SmtpConnectionInfo connection) : ControllerBase
{
    [HttpGet("api/v0/smtp")]
    public SmtpConnectionResponse Get()
    {
        var described = connection.Describe();
        return new SmtpConnectionResponse(described.Host ?? Request.Host.Host, described.Ports.Select(p => new SmtpPortResponse(p.Port, p.Security)).ToList());
    }
}
