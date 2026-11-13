namespace RoushTech.Myotis.Website.Controllers;

using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoushTech.Myotis.Data.Models;
using RoushTech.Myotis.Messages;
using RoushTech.Myotis.Website.Models;

[ApiController]
[Authorize]
public class MessagesController(MessageReader messages) : ControllerBase
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
        Response.Headers.ContentSecurityPolicy =
            $"default-src 'none'; style-src 'unsafe-inline'; img-src {images}; form-action 'none'; base-uri 'none'; frame-ancestors 'self'; sandbox";
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
