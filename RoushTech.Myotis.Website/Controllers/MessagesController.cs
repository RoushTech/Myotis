namespace RoushTech.Myotis.Website.Controllers;

using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoushTech.Myotis.Data.Models;
using RoushTech.Myotis.Messages;
using RoushTech.Myotis.Website.Models;

[ApiController]
[Authorize]
public class MessagesController(MessageReader messages, RetentionService retention) : ControllerBase
{
    [HttpGet("api/v0/mailboxes/{mailbox}/messages")]
    public Task<List<MessageSummary>> List([FromRoute] Mailbox mailbox, [FromQuery] string? search, CancellationToken cancellationToken) =>
        messages.ListAsync(mailbox, search, cancellationToken);

    [HttpGet("api/v0/mailboxes/{mailbox}/messages/{message}")]
    public Task<MessageDetail> Get([FromRoute] Mailbox mailbox, [FromRoute] Message message, CancellationToken cancellationToken) =>
        messages.DetailAsync(message, cancellationToken);

    [HttpPatch("api/v0/mailboxes/{mailbox}/messages/{message}")]
    public async Task<MessageSummary> Update([FromRoute] Mailbox mailbox, [FromRoute] Message message, MessageUpdateRequest request, CancellationToken cancellationToken)
    {
        if (request.IsRead is { } isRead)
        {
            await messages.SetReadAsync(message, isRead, cancellationToken);
        }

        return MessageReader.ToSummary(message);
    }

    [HttpDelete("api/v0/mailboxes/{mailbox}/messages/{message}")]
    public async Task<IActionResult> Delete([FromRoute] Mailbox mailbox, [FromRoute] Message message, CancellationToken cancellationToken)
    {
        await retention.DeleteAsync(message, cancellationToken);
        return NoContent();
    }

    [HttpPatch("api/v0/mailboxes/{mailbox}/messages")]
    public async Task<IActionResult> UpdateAll([FromRoute] Mailbox mailbox, MessageUpdateRequest request, CancellationToken cancellationToken)
    {
        if (request.IsRead is { } isRead)
        {
            await messages.SetAllReadAsync(mailbox, isRead, cancellationToken);
        }

        return NoContent();
    }

    [HttpDelete("api/v0/mailboxes/{mailbox}/messages")]
    public async Task<IActionResult> DeleteAll([FromRoute] Mailbox mailbox, CancellationToken cancellationToken)
    {
        await retention.EmptyAsync(mailbox, cancellationToken);
        return NoContent();
    }

    // Message HTML is untrusted: it is only ever served here, under a CSP that blocks scripts, forms and, unless asked, remote images.
    [HttpGet("api/v0/mailboxes/{mailbox}/messages/{message}/html")]
    public async Task<IActionResult> Html([FromRoute] Mailbox mailbox, [FromRoute] Message message, [FromQuery] bool remoteImages, CancellationToken cancellationToken)
    {
        var html = await messages.HtmlAsync(message, cancellationToken);
        if (html is null)
        {
            return NotFound();
        }

        var images = remoteImages ? "data: https: http:" : "data:";
        // Links open in a new window outside the sandbox, so the app they lead to can run its scripts.
        Response.Headers.ContentSecurityPolicy =
            $"default-src 'none'; style-src 'unsafe-inline'; img-src {images}; form-action 'none'; base-uri 'none'; frame-ancestors 'self'; sandbox allow-popups allow-popups-to-escape-sandbox";
        Response.Headers.XContentTypeOptions = "nosniff";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers.CacheControl = "no-store";
        return Content(html, "text/html; charset=utf-8", Encoding.UTF8);
    }

    [HttpGet("api/v0/mailboxes/{mailbox}/messages/{message}/raw")]
    public async Task<IActionResult> Raw([FromRoute] Mailbox mailbox, [FromRoute] Message message, [FromQuery] bool download, CancellationToken cancellationToken)
    {
        var raw = await messages.RawAsync(message, cancellationToken);
        Response.Headers.XContentTypeOptions = "nosniff";
        return download
            ? File(raw, "application/octet-stream", $"message-{message.Id}.eml")
            : File(raw, "text/plain; charset=utf-8");
    }

    [HttpGet("api/v0/mailboxes/{mailbox}/messages/{message}/attachments/{index:int}")]
    public async Task<IActionResult> Attachment([FromRoute] Mailbox mailbox, [FromRoute] Message message, int index, CancellationToken cancellationToken)
    {
        var attachment = await messages.AttachmentAsync(message, index, cancellationToken);
        if (attachment is null)
        {
            return NotFound();
        }

        Response.Headers.XContentTypeOptions = "nosniff";
        return File(attachment.Content, "application/octet-stream", attachment.FileName);
    }
}
