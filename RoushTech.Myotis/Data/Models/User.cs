namespace RoushTech.Myotis.Data.Models;

using Microsoft.AspNetCore.Identity;

public class User : IdentityUser<Guid>, IEntityBinding
{
    public DateTime CreatedAt { get; set; }

    public DateTime? LastSignInAt { get; set; }

    public List<MailboxGrant> Grants { get; set; } = [];
}

public class Role : IdentityRole<Guid>;
