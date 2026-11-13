namespace RoushTech.Myotis.Website.Models;

using RoushTech.Myotis.Users;

public record UserResponse(Guid Id, string Email, bool IsAdministrator, DateTime? LastSignInAt, List<Guid> MailboxIds)
{
    public static UserResponse From(UserSummary u) => new(u.Id, u.Email, u.IsAdministrator, u.LastSignInAt, u.MailboxIds);
}

public class UserRequest
{
    public required string Email { get; init; }

    public required string Password { get; init; }

    public bool IsAdministrator { get; init; }

    public List<Guid> MailboxIds { get; init; } = [];
}

public class UserUpdateRequest
{
    public bool? IsAdministrator { get; init; }

    public List<Guid>? MailboxIds { get; init; }

    public string? Password { get; init; }
}

public record PersonResponse(Guid UserId, string Email, bool IsAdministrator)
{
    public static PersonResponse From(Person p) => new(p.UserId, p.Email, p.IsAdministrator);
}

public class PersonRequest
{
    public required Guid UserId { get; init; }
}
