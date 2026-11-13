namespace RoushTech.Myotis.Users;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RoushTech.Myotis.Auth;
using RoushTech.Myotis.Data.Models;

public class UserService(UserManager<User> users, TimeProvider time)
{
    public async Task<bool> SetupRequiredAsync(CancellationToken cancellationToken) =>
        !await users.Users.AnyAsync(cancellationToken);

    public async Task<User> CreateFirstAdministratorAsync(string email, string password, CancellationToken cancellationToken)
    {
        if (!await SetupRequiredAsync(cancellationToken))
        {
            throw new UserErrorException("Setup is already done");
        }

        email = email.Trim();
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            UserName = email,
            Email = email,
            CreatedAt = time.GetUtcNow().UtcDateTime,
            LockoutEnabled = true,
        };
        Check(await users.CreateAsync(user, password));
        Check(await users.AddToRoleAsync(user, MyotisRoles.Administrator));
        return user;
    }

    private static void Check(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new UserErrorException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }
    }
}
