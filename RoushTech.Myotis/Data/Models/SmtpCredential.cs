namespace RoushTech.Myotis.Data.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class SmtpCredential : IMailboxScoped, IEntityTypeConfiguration<SmtpCredential>
{
    public const int UsernameMaxLength = 100;

    public Guid Id { get; set; }

    public Guid MailboxId { get; set; }

    public Mailbox Mailbox { get; set; } = null!;

    public required string Username { get; set; }

    public required byte[] PasswordHash { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastUsedAt { get; set; }

    public void Configure(EntityTypeBuilder<SmtpCredential> builder)
    {
        builder.Property(c => c.Username).HasMaxLength(UsernameMaxLength).UseCollation("NOCASE");
        builder.HasIndex(c => c.Username).IsUnique();
        builder.HasOne(c => c.Mailbox).WithMany(m => m.Credentials).OnDelete(DeleteBehavior.Cascade);
    }
}
