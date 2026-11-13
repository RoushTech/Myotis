namespace RoushTech.Myotis.Data;

using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RoushTech.Myotis.Data.Models;

public class MyotisDbContext(DbContextOptions<MyotisDbContext> options)
    : IdentityDbContext<User, Role, Guid>(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    public DbSet<Mailbox> Mailboxes => Set<Mailbox>();

    public DbSet<SmtpCredential> SmtpCredentials => Set<SmtpCredential>();

    public DbSet<MailboxGrant> MailboxGrants => Set<MailboxGrant>();

    public DbSet<Message> Messages => Set<Message>();

    public DbSet<MessageContent> MessageContents => Set<MessageContent>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(MyotisDbContext).Assembly);
    }
}
