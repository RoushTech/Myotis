namespace RoushTech.Myotis.Data.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class MailboxGrant : IEntityTypeConfiguration<MailboxGrant>
{
    public Guid Id { get; set; }

    public Guid MailboxId { get; set; }

    public Mailbox Mailbox { get; set; } = null!;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public void Configure(EntityTypeBuilder<MailboxGrant> builder)
    {
        builder.HasIndex(g => new { g.UserId, g.MailboxId }).IsUnique();
        builder.HasOne(g => g.Mailbox).WithMany(m => m.Grants).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(g => g.User).WithMany(u => u.Grants).OnDelete(DeleteBehavior.Cascade);
    }
}
