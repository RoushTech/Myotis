namespace RoushTech.Myotis.Messages;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RoushTech.Myotis.Data;
using RoushTech.Myotis.Data.Models;
using RoushTech.Myotis.Settings;

public class RetentionService(MyotisDbContext db, IOptionsMonitor<LimitsOptions> limits, TimeProvider time)
{
    public Task<int> TrimToCountAsync(Guid mailboxId, CancellationToken cancellationToken) =>
        db.Messages
            .Where(m => m.MailboxId == mailboxId)
            .OrderByDescending(m => m.ReceivedAt)
            .ThenByDescending(m => m.Id)
            .Skip(limits.CurrentValue.KeepMessages)
            .ExecuteDeleteAsync(cancellationToken);

    public async Task<RetentionRun> RunAsync(CancellationToken cancellationToken)
    {
        var run = new RetentionRun { Id = Guid.CreateVersion7(), StartedAt = time.GetUtcNow().UtcDateTime };
        db.RetentionRuns.Add(run);
        await db.SaveChangesAsync(cancellationToken);
        try
        {
            var cutoff = time.GetUtcNow().UtcDateTime.AddDays(-limits.CurrentValue.KeepDays);
            run.DeletedCount = await db.Messages.Where(m => m.ReceivedAt < cutoff).ExecuteDeleteAsync(cancellationToken);
            foreach (var mailboxId in await db.Mailboxes.Select(m => m.Id).ToListAsync(cancellationToken))
            {
                run.DeletedCount += await TrimToCountAsync(mailboxId, cancellationToken);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            run.Error = ex.Message;
        }

        run.FinishedAt = time.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(cancellationToken);
        return run;
    }

    public async Task DeleteAsync(Message message, CancellationToken cancellationToken)
    {
        db.Messages.Remove(message);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<int> EmptyAsync(Mailbox mailbox, CancellationToken cancellationToken) =>
        db.Messages.Where(m => m.MailboxId == mailbox.Id).ExecuteDeleteAsync(cancellationToken);
}
