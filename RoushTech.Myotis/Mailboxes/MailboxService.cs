namespace RoushTech.Myotis.Mailboxes;

using Microsoft.EntityFrameworkCore;
using RoushTech.Myotis.Data;
using RoushTech.Myotis.Data.Models;

public record MailboxSummary(Guid Id, string Name, DateTime CreatedAt, int CredentialCount, int PeopleCount);

public class MailboxService(MyotisDbContext db, TimeProvider time)
{
    public async Task<bool> CanSeeAsync(Guid mailboxId, Guid userId, bool isAdministrator, CancellationToken cancellationToken) =>
        isAdministrator
            ? await db.Mailboxes.AnyAsync(m => m.Id == mailboxId, cancellationToken)
            : await db.MailboxGrants.AnyAsync(g => g.MailboxId == mailboxId && g.UserId == userId, cancellationToken);

    public Task<List<MailboxSummary>> ListVisibleAsync(Guid userId, bool isAdministrator, CancellationToken cancellationToken) =>
        Summaries(db.Mailboxes.Where(m => isAdministrator || m.Grants.Any(g => g.UserId == userId)).OrderBy(m => m.Name))
            .ToListAsync(cancellationToken);

    public Task<MailboxSummary> SummaryAsync(Mailbox mailbox, CancellationToken cancellationToken) =>
        Summaries(db.Mailboxes.Where(m => m.Id == mailbox.Id)).SingleAsync(cancellationToken);

    public async Task<Mailbox> CreateAsync(string name, CancellationToken cancellationToken)
    {
        name = name.Trim();
        if (name.Length is 0 or > Mailbox.NameMaxLength)
        {
            throw new UserErrorException($"Give the mailbox a name of up to {Mailbox.NameMaxLength} characters");
        }

        if (await db.Mailboxes.AnyAsync(m => m.Name == name, cancellationToken))
        {
            throw new UserErrorException($"There's already a mailbox called {name}");
        }

        var mailbox = new Mailbox { Id = Guid.CreateVersion7(), Name = name, CreatedAt = time.GetUtcNow().UtcDateTime };
        db.Mailboxes.Add(mailbox);
        await db.SaveChangesAsync(cancellationToken);
        return mailbox;
    }

    public async Task DeleteAsync(Mailbox mailbox, CancellationToken cancellationToken)
    {
        db.Mailboxes.Remove(mailbox);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<MailboxSummary> Summaries(IQueryable<Mailbox> mailboxes) =>
        mailboxes.Select(m => new MailboxSummary(m.Id, m.Name, m.CreatedAt, m.Credentials.Count, m.Grants.Count));
}
