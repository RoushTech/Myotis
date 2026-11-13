namespace RoushTech.Myotis.Website.Models.V1;

using RoushTech.Myotis.Mailboxes;
using RoushTech.Myotis.Messages;

// /api/v1 is a stable contract for tests and scripts: fields may be added, never renamed or removed.
public record MailboxV1(Guid Id, string Name, int MessageCount, int UnreadCount)
{
    public static MailboxV1 Of(MailboxSummary m) => new(m.Id, m.Name, m.MessageCount, m.UnreadCount);
}

public record MessageSummaryV1(Guid Id, Guid MailboxId, DateTime ReceivedAt, string Subject, string From, string To, string Cc, int AttachmentCount)
{
    public static MessageSummaryV1 Of(MessageSummary m) => new(m.Id, m.MailboxId, m.ReceivedAt, m.Subject, m.From, m.To, m.Cc, m.AttachmentCount);
}

public record AttachmentV1(int Index, string FileName, string ContentType, long Size);

public record MessageV1(
    Guid Id,
    Guid MailboxId,
    DateTime ReceivedAt,
    string Subject,
    string From,
    string To,
    string Cc,
    string EnvelopeFrom,
    IReadOnlyList<string> EnvelopeTo,
    string? Text,
    string? Html,
    IReadOnlyList<string> Links,
    IReadOnlyList<AttachmentV1> Attachments)
{
    public static MessageV1 Of(MessageDetail d) => new(
        d.Summary.Id,
        d.Summary.MailboxId,
        d.Summary.ReceivedAt,
        d.Summary.Subject,
        d.Summary.From,
        d.Summary.To,
        d.Summary.Cc,
        d.EnvelopeFrom,
        d.EnvelopeTo,
        d.Text,
        d.Html,
        d.Links,
        [.. d.Attachments.Select(a => new AttachmentV1(a.Index, a.FileName, a.ContentType, a.Size))]);
}
