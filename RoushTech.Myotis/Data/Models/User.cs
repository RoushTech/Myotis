namespace RoushTech.Myotis.Data.Models;

using Microsoft.AspNetCore.Identity;

public class User : IdentityUser<Guid>
{
    public DateTime CreatedAt { get; set; }
}

public class Role : IdentityRole<Guid>;
