namespace RoushTech.Myotis.Website.Models;

using RoushTech.Myotis.Data.Models;
using RoushTech.Myotis.Mailboxes;

public record MailboxResponse(Guid Id, string Name, DateTime CreatedAt, int CredentialCount, int PeopleCount)
{
    public static MailboxResponse From(MailboxSummary m) => new(m.Id, m.Name, m.CreatedAt, m.CredentialCount, m.PeopleCount);
}

public class MailboxRequest
{
    public required string Name { get; init; }
}

public record CredentialResponse(Guid Id, string Username, DateTime CreatedAt, DateTime? LastUsedAt)
{
    public static CredentialResponse From(SmtpCredential c) => new(c.Id, c.Username, c.CreatedAt, c.LastUsedAt);
}

public record CreatedCredentialResponse(CredentialResponse Credential, string Password);

public class CredentialRequest
{
    public required string Username { get; init; }
}
