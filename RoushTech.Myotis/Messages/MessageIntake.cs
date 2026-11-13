namespace RoushTech.Myotis.Messages;

using MimeKit;
using RoushTech.Myotis.Data;
using RoushTech.Myotis.Data.Models;

public class MessageIntake(MyotisDbContext db, TimeProvider time)
{
    public async Task<Message> SaveAsync(Guid mailboxId, string envelopeFrom, IReadOnlyList<string> envelopeTo, byte[] raw, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(raw, writable: false);
        var parsed = await MimeMessage.LoadAsync(stream, cancellationToken);
        var message = new Message
        {
            Id = Guid.CreateVersion7(),
            MailboxId = mailboxId,
            ReceivedAt = time.GetUtcNow().UtcDateTime,
            EnvelopeFrom = envelopeFrom,
            EnvelopeTo = [.. envelopeTo],
            Subject = parsed.Subject ?? "",
            From = parsed.From.ToString(),
            To = parsed.To.ToString(),
            Cc = parsed.Cc.ToString(),
            SentAt = parsed.Date == DateTimeOffset.MinValue ? null : parsed.Date.UtcDateTime,
            MessageIdHeader = parsed.MessageId,
            Size = raw.Length,
            AttachmentCount = parsed.Attachments.Count(),
            Content = new MessageContent { Id = Guid.CreateVersion7(), Raw = raw },
        };
        db.Messages.Add(message);
        await db.SaveChangesAsync(cancellationToken);
        return message;
    }
}
