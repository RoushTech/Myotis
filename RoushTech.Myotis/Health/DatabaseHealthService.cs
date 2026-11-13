namespace RoushTech.Myotis.Health;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RoushTech.Myotis.Data;
using RoushTech.Myotis.Messages;

public record MailboxUsage(Guid Id, string Name, int MessageCount, long Bytes, DateTime? OldestReceivedAt);

public record RetentionSummary(DateTime StartedAt, DateTime? FinishedAt, int DeletedCount, string? Error, DateTime NextRunAt);

public record DatabaseHealth(
    string FileName,
    long FileBytes,
    long WriteAheadLogBytes,
    long ReclaimableBytes,
    int MessageCount,
    IReadOnlyList<MailboxUsage> Mailboxes,
    RetentionSummary? LastRetention);

public record IntegrityResult(bool Ok, IReadOnlyList<string> Problems, DateTime CheckedAt);

public class DatabaseHealthService(MyotisDbContext db, TimeProvider time)
{
    public async Task<DatabaseHealth> ReportAsync(CancellationToken cancellationToken)
    {
        var path = new SqliteConnectionStringBuilder(db.Database.GetConnectionString()).DataSource;
        var pageSize = await db.Database.SqlQueryRaw<long>("SELECT page_size AS Value FROM pragma_page_size()").SingleAsync(cancellationToken);
        var freePages = await db.Database.SqlQueryRaw<long>("SELECT freelist_count AS Value FROM pragma_freelist_count()").SingleAsync(cancellationToken);
        var mailboxes = await db.Mailboxes
            .Select(m => new MailboxUsage(
                m.Id,
                m.Name,
                db.Messages.Count(x => x.MailboxId == m.Id),
                db.Messages.Where(x => x.MailboxId == m.Id).Sum(x => (long)x.Size),
                db.Messages.Where(x => x.MailboxId == m.Id).Min(x => (DateTime?)x.ReceivedAt)))
            .ToListAsync(cancellationToken);
        var last = await db.RetentionRuns.OrderByDescending(r => r.StartedAt).FirstOrDefaultAsync(cancellationToken);

        return new DatabaseHealth(
            Path.GetFileName(path),
            FileSize(path),
            FileSize(path + "-wal"),
            pageSize * freePages,
            mailboxes.Sum(m => m.MessageCount),
            [.. mailboxes.OrderByDescending(m => m.Bytes).ThenBy(m => m.Name)],
            last is null ? null : new RetentionSummary(last.StartedAt, last.FinishedAt, last.DeletedCount, last.Error, last.StartedAt + RetentionWorker.Interval));
    }

    // quick_check rather than integrity_check: it skips index cross-checks, so it stays fast on a large file.
    public async Task<IntegrityResult> CheckAsync(CancellationToken cancellationToken)
    {
        var rows = await db.Database.SqlQueryRaw<string>("SELECT quick_check AS Value FROM pragma_quick_check()").ToListAsync(cancellationToken);
        var ok = rows is ["ok"];
        return new IntegrityResult(ok, ok ? [] : rows, time.GetUtcNow().UtcDateTime);
    }

    private static long FileSize(string path) => File.Exists(path) ? new FileInfo(path).Length : 0;
}
