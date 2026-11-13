namespace RoushTech.Myotis.Data.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class Mailbox : IMailboxScoped, IEntityTypeConfiguration<Mailbox>
{
    public const int NameMaxLength = 100;

    public Guid Id { get; set; }

    public required string Name { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<SmtpCredential> Credentials { get; set; } = [];

    Guid IMailboxScoped.MailboxId => Id;

    public void Configure(EntityTypeBuilder<Mailbox> builder)
    {
        builder.Property(m => m.Name).HasMaxLength(NameMaxLength).UseCollation("NOCASE");
        builder.HasIndex(m => m.Name).IsUnique();
    }
}
