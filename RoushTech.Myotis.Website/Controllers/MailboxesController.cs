namespace RoushTech.Myotis.Website.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoushTech.Myotis.Auth;
using RoushTech.Myotis.Data.Models;
using RoushTech.Myotis.Mailboxes;
using RoushTech.Myotis.Website.Models;

[ApiController]
[Authorize]
public class MailboxesController(MailboxService mailboxes) : ControllerBase
{
    [HttpGet("api/v0/mailboxes")]
    public async Task<IEnumerable<MailboxResponse>> List(CancellationToken cancellationToken) =>
        (await mailboxes.ListVisibleAsync(User.RequiredUserId(), User.IsAdministrator(), cancellationToken))
            .Select(MailboxResponse.From);

    [HttpPost("api/v0/mailboxes")]
    [Authorize(Roles = MyotisRoles.Administrator)]
    public async Task<MailboxResponse> Create(MailboxRequest request, CancellationToken cancellationToken)
    {
        var mailbox = await mailboxes.CreateAsync(request.Name, cancellationToken);
        return new MailboxResponse(mailbox.Id, mailbox.Name, mailbox.CreatedAt, 0, 0);
    }

    [HttpGet("api/v0/mailboxes/{mailbox}")]
    public async Task<MailboxResponse> Get([FromRoute] Mailbox mailbox, CancellationToken cancellationToken) =>
        MailboxResponse.From(await mailboxes.SummaryAsync(mailbox, cancellationToken));

    [HttpDelete("api/v0/mailboxes/{mailbox}")]
    [Authorize(Roles = MyotisRoles.Administrator)]
    public async Task<IActionResult> Delete([FromRoute] Mailbox mailbox, CancellationToken cancellationToken)
    {
        await mailboxes.DeleteAsync(mailbox, cancellationToken);
        return NoContent();
    }
}
