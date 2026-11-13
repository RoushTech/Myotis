namespace RoushTech.Myotis.Mailboxes;

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using RoushTech.Myotis.Data;
using RoushTech.Myotis.Data.Models;

public record CreatedCredential(SmtpCredential Credential, string Password);

public partial class SmtpCredentialService(MyotisDbContext db, TimeProvider time)
{
    private const string PasswordAlphabet = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private const int PasswordLength = 24;

    public Task<List<SmtpCredential>> ListAsync(Mailbox mailbox, CancellationToken cancellationToken) =>
        db.SmtpCredentials.Where(c => c.MailboxId == mailbox.Id).OrderBy(c => c.CreatedAt).ToListAsync(cancellationToken);

    public async Task<CreatedCredential> CreateAsync(Mailbox mailbox, string username, CancellationToken cancellationToken)
    {
        username = username.Trim();
        if (!ValidUsername().IsMatch(username))
        {
            throw new UserErrorException(
                $"Use up to {SmtpCredential.UsernameMaxLength} letters, digits, dots, dashes, underscores or @ for the username");
        }

        if (await db.SmtpCredentials.AnyAsync(c => c.Username == username, cancellationToken))
        {
            throw new UserErrorException($"The username {username} is already taken");
        }

        var password = RandomNumberGenerator.GetString(PasswordAlphabet, PasswordLength);
        var credential = new SmtpCredential
        {
            Id = Guid.CreateVersion7(),
            MailboxId = mailbox.Id,
            Username = username,
            PasswordHash = Hash(password),
            CreatedAt = time.GetUtcNow().UtcDateTime,
        };
        db.SmtpCredentials.Add(credential);
        await db.SaveChangesAsync(cancellationToken);
        return new CreatedCredential(credential, password);
    }

    public async Task RevokeAsync(SmtpCredential credential, CancellationToken cancellationToken)
    {
        db.SmtpCredentials.Remove(credential);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <returns>The credential's mailbox, or null when the username is unknown or the password is wrong.</returns>
    public async Task<Guid?> VerifyAsync(string username, string password, CancellationToken cancellationToken)
    {
        var credential = await db.SmtpCredentials.AsNoTracking()
            .Where(c => c.Username == username.Trim())
            .Select(c => new { c.MailboxId, c.PasswordHash })
            .FirstOrDefaultAsync(cancellationToken);
        var expected = credential?.PasswordHash ?? new byte[SHA256.HashSizeInBytes];
        var matches = CryptographicOperations.FixedTimeEquals(Hash(password), expected);
        return credential is not null && matches ? credential.MailboxId : null;
    }

    // The passwords are random and long, so a fast hash is as safe as a slow one and keeps SMTP sign-in cheap.
    private static byte[] Hash(string password) => SHA256.HashData(Encoding.UTF8.GetBytes(password));

    [GeneratedRegex(@"^[A-Za-z0-9._@-]{1,100}$")]
    private static partial Regex ValidUsername();
}
