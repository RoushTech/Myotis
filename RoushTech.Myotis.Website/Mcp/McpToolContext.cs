namespace RoushTech.Myotis.Website.Mcp;

using ModelContextProtocol;
using RoushTech.Myotis.Auth;
using RoushTech.Myotis.Data;
using RoushTech.Myotis.Data.Models;
using RoushTech.Myotis.Mailboxes;

// /mcp skips MVC, so neither the role attributes nor MailboxAccessFilter apply: every tool checks access here, through the service the controllers use.
public class McpToolContext(IHttpContextAccessor httpContextAccessor, MailboxService mailboxes, MyotisDbContext db)
{
    private System.Security.Claims.ClaimsPrincipal User => httpContextAccessor.HttpContext?.User ?? throw new McpException("Not authenticated.");

    public Task<List<MailboxSummary>> VisibleMailboxesAsync(CancellationToken cancellationToken) =>
        mailboxes.ListVisibleAsync(User.RequiredUserId(), User.IsAdministrator(), cancellationToken);

    // A name or ID the caller can't see reads exactly like one that doesn't exist.
    public async Task<Mailbox> MailboxAsync(string nameOrId, CancellationToken cancellationToken)
    {
        var visible = await VisibleMailboxesAsync(cancellationToken);
        var match = visible.FirstOrDefault(m => m.Id.ToString().Equals(nameOrId.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? visible.FirstOrDefault(m => m.Name.Equals(nameOrId.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? throw new McpException($"No mailbox called {nameOrId}. list_mailboxes shows the ones you can read.");
        return await db.Mailboxes.FindAsync([match.Id], cancellationToken) ?? throw new McpException($"No mailbox called {nameOrId}.");
    }

    public async Task<Message> MessageAsync(Mailbox mailbox, Guid messageId, CancellationToken cancellationToken)
    {
        var message = await db.Messages.FindAsync([messageId], cancellationToken);
        return message is not null && message.MailboxId == mailbox.Id
            ? message
            : throw new McpException($"No message {messageId} in {mailbox.Name}.");
    }
}
