namespace RoushTech.Myotis.Website.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoushTech.Myotis.Auth;
using RoushTech.Myotis.Data.Models;
using RoushTech.Myotis.Mailboxes;
using RoushTech.Myotis.Website.Models;

[ApiController]
[Authorize(Roles = MyotisRoles.Administrator)]
public class CredentialsController(SmtpCredentialService credentials) : ControllerBase
{
    [HttpGet("api/v0/mailboxes/{mailbox}/credentials")]
    public async Task<IEnumerable<CredentialResponse>> List([FromRoute] Mailbox mailbox, CancellationToken cancellationToken) =>
        (await credentials.ListAsync(mailbox, cancellationToken)).Select(CredentialResponse.From);

    [HttpPost("api/v0/mailboxes/{mailbox}/credentials")]
    public async Task<CreatedCredentialResponse> Create([FromRoute] Mailbox mailbox, CredentialRequest request, CancellationToken cancellationToken)
    {
        var created = await credentials.CreateAsync(mailbox, request.Username, cancellationToken);
        return new CreatedCredentialResponse(CredentialResponse.From(created.Credential), created.Password);
    }

    [HttpDelete("api/v0/mailboxes/{mailbox}/credentials/{credential}")]
    public async Task<IActionResult> Revoke([FromRoute] Mailbox mailbox, [FromRoute] SmtpCredential credential, CancellationToken cancellationToken)
    {
        await credentials.RevokeAsync(credential, cancellationToken);
        return NoContent();
    }
}
