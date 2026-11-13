namespace RoushTech.Myotis.Users;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RoushTech.Myotis.Auth;
using RoushTech.Myotis.Data;
using RoushTech.Myotis.Data.Models;

public record UserSummary(Guid Id, string Email, bool IsAdministrator, DateTime? LastSignInAt, List<Guid> MailboxIds);

public record Person(Guid UserId, string Email, bool IsAdministrator);

public class UserService(UserManager<User> users, MyotisDbContext db, TimeProvider time)
{
    public async Task<bool> SetupRequiredAsync(CancellationToken cancellationToken) =>
        !await users.Users.AnyAsync(cancellationToken);

    public async Task<User> CreateFirstAdministratorAsync(string email, string password, CancellationToken cancellationToken)
    {
        if (!await SetupRequiredAsync(cancellationToken))
        {
            throw new UserErrorException("Setup is already done");
        }

        return await CreateAsync(email, password, isAdministrator: true, [], cancellationToken);
    }

    public async Task<List<UserSummary>> ListAsync(CancellationToken cancellationToken)
    {
        var administrators = await AdministratorIdsAsync();
        var all = await db.Users
            .OrderBy(u => u.Email)
            .Select(u => new { u.Id, u.Email, u.LastSignInAt, MailboxIds = u.Grants.Select(g => g.MailboxId).ToList() })
            .ToListAsync(cancellationToken);
        return all.Select(u => new UserSummary(u.Id, u.Email!, administrators.Contains(u.Id), u.LastSignInAt, u.MailboxIds)).ToList();
    }

    public async Task<UserSummary> SummaryAsync(User user, CancellationToken cancellationToken) =>
        (await ListAsync(cancellationToken)).Single(u => u.Id == user.Id);

    public async Task<User> CreateAsync(string email, string password, bool isAdministrator, IReadOnlyCollection<Guid> mailboxIds, CancellationToken cancellationToken)
    {
        email = email.Trim();
        if (await users.FindByEmailAsync(email) is not null)
        {
            throw new UserErrorException($"{email} already has an account");
        }

        var user = new User
        {
            Id = Guid.CreateVersion7(),
            UserName = email,
            Email = email,
            CreatedAt = time.GetUtcNow().UtcDateTime,
            LockoutEnabled = true,
        };
        Check(await users.CreateAsync(user, password));
        if (isAdministrator)
        {
            Check(await users.AddToRoleAsync(user, MyotisRoles.Administrator));
        }

        await SetMailboxesAsync(user, mailboxIds, cancellationToken);
        return user;
    }

    public async Task UpdateAsync(User user, Guid actingUserId, bool? isAdministrator, IReadOnlyCollection<Guid>? mailboxIds, string? password, CancellationToken cancellationToken)
    {
        if (isAdministrator is false && user.Id == actingUserId)
        {
            throw new UserErrorException("You can't remove your own administrator role");
        }

        if (password is not null)
        {
            Check(await users.ResetPasswordAsync(user, await users.GeneratePasswordResetTokenAsync(user), password));
        }

        if (isAdministrator is not null && isAdministrator != await users.IsInRoleAsync(user, MyotisRoles.Administrator))
        {
            Check(isAdministrator.Value
                ? await users.AddToRoleAsync(user, MyotisRoles.Administrator)
                : await users.RemoveFromRoleAsync(user, MyotisRoles.Administrator));
        }

        if (mailboxIds is not null)
        {
            await SetMailboxesAsync(user, mailboxIds, cancellationToken);
        }

        // Signs the user out everywhere within a minute, so lost access doesn't linger in an open browser.
        Check(await users.UpdateSecurityStampAsync(user));
    }

    public async Task DeleteAsync(User user, Guid actingUserId)
    {
        if (user.Id == actingUserId)
        {
            throw new UserErrorException("You can't delete your own account");
        }

        Check(await users.DeleteAsync(user));
    }

    public async Task RecordSignInAsync(User user)
    {
        user.LastSignInAt = time.GetUtcNow().UtcDateTime;
        Check(await users.UpdateAsync(user));
    }

    public async Task<List<Person>> PeopleAsync(Mailbox mailbox, CancellationToken cancellationToken)
    {
        var administrators = await AdministratorIdsAsync();
        var people = await db.Users
            .Where(u => administrators.Contains(u.Id) || u.Grants.Any(g => g.MailboxId == mailbox.Id))
            .OrderBy(u => u.Email)
            .Select(u => new { u.Id, u.Email })
            .ToListAsync(cancellationToken);
        return people.Select(p => new Person(p.Id, p.Email!, administrators.Contains(p.Id))).ToList();
    }

    public async Task AddPersonAsync(Mailbox mailbox, User user, CancellationToken cancellationToken)
    {
        if (!await db.MailboxGrants.AnyAsync(g => g.MailboxId == mailbox.Id && g.UserId == user.Id, cancellationToken))
        {
            db.MailboxGrants.Add(NewGrant(user.Id, mailbox.Id));
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RemovePersonAsync(Mailbox mailbox, User user, CancellationToken cancellationToken)
    {
        await db.MailboxGrants.Where(g => g.MailboxId == mailbox.Id && g.UserId == user.Id).ExecuteDeleteAsync(cancellationToken);
        Check(await users.UpdateSecurityStampAsync(user));
    }

    private async Task SetMailboxesAsync(User user, IReadOnlyCollection<Guid> mailboxIds, CancellationToken cancellationToken)
    {
        var wanted = mailboxIds.ToHashSet();
        if (await db.Mailboxes.CountAsync(m => wanted.Contains(m.Id), cancellationToken) != wanted.Count)
        {
            throw new UserErrorException("One of those mailboxes doesn't exist any more");
        }

        var current = await db.MailboxGrants.Where(g => g.UserId == user.Id).ToListAsync(cancellationToken);
        db.MailboxGrants.RemoveRange(current.Where(g => !wanted.Contains(g.MailboxId)));
        db.MailboxGrants.AddRange(wanted.Except(current.Select(g => g.MailboxId)).Select(id => NewGrant(user.Id, id)));
        await db.SaveChangesAsync(cancellationToken);
    }

    private MailboxGrant NewGrant(Guid userId, Guid mailboxId) =>
        new() { Id = Guid.CreateVersion7(), UserId = userId, MailboxId = mailboxId, CreatedAt = time.GetUtcNow().UtcDateTime };

    private async Task<HashSet<Guid>> AdministratorIdsAsync() =>
        (await users.GetUsersInRoleAsync(MyotisRoles.Administrator)).Select(u => u.Id).ToHashSet();

    private static void Check(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new UserErrorException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }
    }
}
