namespace RoushTech.Myotis.Website.Controllers.V1;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoushTech.Myotis.Auth;
using RoushTech.Myotis.Data.Models;
using RoushTech.Myotis.Mailboxes;
using RoushTech.Myotis.Messages;
using RoushTech.Myotis.Website.Models.V1;

[ApiController]
[Authorize(AuthenticationSchemes = PatAuthenticationDefaults.Scheme)]
public class MailController(MailboxService mailboxes, MessageReader messages, RetentionService retention) : ControllerBase
{
    [HttpGet("api/v1/mailboxes")]
    public async Task<IEnumerable<MailboxV1>> Mailboxes(CancellationToken cancellationToken) =>
        (await mailboxes.ListVisibleAsync(User.RequiredUserId(), User.IsAdministrator(), cancellationToken)).Select(MailboxV1.Of);

    [HttpGet("api/v1/mailboxes/{mailbox}/messages")]
    public async Task<IEnumerable<MessageSummaryV1>> Messages(
        [FromRoute] Mailbox mailbox,
        [FromQuery] string? search,
        [FromQuery] DateTime? since,
        CancellationToken cancellationToken) =>
        (await messages.ListAsync(mailbox, search, since, cancellationToken)).Select(MessageSummaryV1.Of);

    [HttpGet("api/v1/mailboxes/{mailbox}/messages/{message}")]
    public async Task<MessageV1> Message([FromRoute] Mailbox mailbox, [FromRoute] Message message, CancellationToken cancellationToken) =>
        MessageV1.Of(await messages.DetailAsync(message, cancellationToken));

    [HttpDelete("api/v1/mailboxes/{mailbox}/messages")]
    public async Task<IActionResult> EmptyMailbox([FromRoute] Mailbox mailbox, CancellationToken cancellationToken)
    {
        await retention.EmptyAsync(mailbox, cancellationToken);
        return NoContent();
    }
}
