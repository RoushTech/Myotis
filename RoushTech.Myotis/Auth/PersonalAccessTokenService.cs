namespace RoushTech.Myotis.Auth;

using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RoushTech.Myotis.Data;
using RoushTech.Myotis.Data.Models;

public class PersonalAccessTokenService(MyotisDbContext db, TimeProvider time)
{
    public const string TokenPrefix = "myotis_pat_";

    private const int PrefixDisplayLength = 19;
    private static readonly TimeSpan LastUsedTouchInterval = TimeSpan.FromMinutes(1);

    public async Task<(PersonalAccessToken Token, string PlainText)> CreateAsync(Guid userId, string name, DateTime? expiresAt, CancellationToken cancellationToken)
    {
        name = name.Trim();
        if (name.Length is 0 or > 200)
        {
            throw new UserErrorException("Token name must be 1 to 200 characters");
        }

        var now = time.GetUtcNow().UtcDateTime;
        if (expiresAt is { } expiry && expiry.ToUniversalTime() <= now)
        {
            throw new UserErrorException("Expiry must be in the future");
        }

        var plainText = TokenPrefix + Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        var token = new PersonalAccessToken
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Name = name,
            TokenHash = Hash(plainText),
            Prefix = plainText[..PrefixDisplayLength],
            CreatedAt = now,
            ExpiresAt = expiresAt?.ToUniversalTime(),
        };

        db.PersonalAccessTokens.Add(token);
        await db.SaveChangesAsync(cancellationToken);
        return (token, plainText);
    }

    // Null when the token is unknown, expired or revoked.
    public async Task<User?> ValidateAsync(string plainText, CancellationToken cancellationToken)
    {
        var hash = Hash(plainText);
        var now = time.GetUtcNow().UtcDateTime;
        var token = await db.PersonalAccessTokens.AsNoTracking().Include(t => t.User).SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (token is null || token.RevokedAt is not null || token.ExpiresAt <= now)
        {
            return null;
        }

        if (token.LastUsedAt is null || now - token.LastUsedAt > LastUsedTouchInterval)
        {
            await db.PersonalAccessTokens
                .Where(t => t.Id == token.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.LastUsedAt, now), cancellationToken);
        }

        return token.User;
    }

    public Task<List<PersonalAccessToken>> ListAsync(Guid userId, CancellationToken cancellationToken) =>
        db.PersonalAccessTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<bool> RevokeAsync(Guid userId, Guid tokenId, CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var revoked = await db.PersonalAccessTokens
            .Where(t => t.Id == tokenId && t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), cancellationToken);
        return revoked > 0;
    }

    private static byte[] Hash(string plainText) => SHA256.HashData(Encoding.UTF8.GetBytes(plainText));
}
