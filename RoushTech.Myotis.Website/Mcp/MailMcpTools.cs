namespace RoushTech.Myotis.Website.Mcp;

using System.ComponentModel;
using ModelContextProtocol.Server;
using RoushTech.Myotis.Messages;
using RoushTech.Myotis.Website.Models.V1;

[McpServerToolType]
public class MailMcpTools(McpToolContext context, MessageReader messages, RetentionService retention)
{
    [McpServerTool(Name = "list_mailboxes", ReadOnly = true)]
    [Description("List the mailboxes you can read, with message and unread counts.")]
    public async Task<IEnumerable<MailboxV1>> ListMailboxes(CancellationToken cancellationToken = default) =>
        (await context.VisibleMailboxesAsync(cancellationToken)).Select(MailboxV1.Of);

    [McpServerTool(Name = "list_messages", ReadOnly = true)]
    [Description("List messages in a mailbox, newest first. Use since to see only what arrived after an action you just took.")]
    public async Task<IEnumerable<MessageSummaryV1>> ListMessages(
        [Description("Mailbox name or ID from list_mailboxes.")] string mailbox,
        [Description("Only messages whose subject, from or to contains this.")] string? search = null,
        [Description("Only messages received after this time, ISO 8601.")] DateTime? since = null,
        CancellationToken cancellationToken = default)
    {
        var found = await context.MailboxAsync(mailbox, cancellationToken);
        return (await messages.ListAsync(found, search, since, cancellationToken)).Select(MessageSummaryV1.Of);
    }

    [McpServerTool(Name = "read_message", ReadOnly = true)]
    [Description("Read one message: headers, envelope, text and HTML bodies, and every link in it, such as an invite or password reset link to follow.")]
    public async Task<MessageV1> ReadMessage(
        [Description("Mailbox name or ID from list_mailboxes.")] string mailbox,
        [Description("Message ID from list_messages.")] Guid message,
        CancellationToken cancellationToken = default)
    {
        var found = await context.MailboxAsync(mailbox, cancellationToken);
        return MessageV1.Of(await messages.DetailAsync(await context.MessageAsync(found, message, cancellationToken), cancellationToken));
    }

    [McpServerTool(Name = "empty_mailbox", Destructive = true)]
    [Description("Delete every message in a mailbox, for a clean start before a test.")]
    public async Task<string> EmptyMailbox(
        [Description("Mailbox name or ID from list_mailboxes.")] string mailbox,
        CancellationToken cancellationToken = default)
    {
        var found = await context.MailboxAsync(mailbox, cancellationToken);
        var deleted = await retention.EmptyAsync(found, cancellationToken);
        return $"Deleted {deleted} messages from {found.Name}.";
    }
}
