namespace RoushTech.Myotis.Website.Models;

public class CreateTokenRequest
{
    public required string Name { get; init; }

    public DateTime? ExpiresAt { get; init; }
}

public record TokenResponse(Guid Id, string Name, string Prefix, DateTime CreatedAt, DateTime? ExpiresAt, DateTime? LastUsedAt);

public record CreatedTokenResponse(TokenResponse Token, string PlainText);
