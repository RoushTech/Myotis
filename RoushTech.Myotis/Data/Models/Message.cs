namespace RoushTech.Myotis.Data.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class Message : IMailboxScoped, IEntityTypeConfiguration<Message>
{
    public Guid Id { get; set; }

    public Guid MailboxId { get; set; }

    public Mailbox Mailbox { get; set; } = null!;

    public DateTime ReceivedAt { get; set; }

    public required string EnvelopeFrom { get; set; }

    public List<string> EnvelopeTo { get; set; } = [];

    public required string Subject { get; set; }

    public required string From { get; set; }

    public required string To { get; set; }

    public required string Cc { get; set; }

    public DateTime? SentAt { get; set; }

    public string? MessageIdHeader { get; set; }

    public int Size { get; set; }

    public int AttachmentCount { get; set; }

    public MessageContent Content { get; set; } = null!;

    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.HasOne(m => m.Mailbox).WithMany().OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(m => m.Content).WithOne().HasForeignKey<MessageContent>(c => c.MessageId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(m => new { m.MailboxId, m.ReceivedAt });
    }
}

public class MessageContent
{
    public Guid Id { get; set; }

    public Guid MessageId { get; set; }

    public required byte[] Raw { get; set; }
}
