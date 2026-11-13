namespace RoushTech.Myotis.Smtp;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RoushTech.Myotis.Data;
using RoushTech.Myotis.Mailboxes;
using SmtpServer;
using SmtpServer.Authentication;

public class CredentialAuthenticator(IServiceScopeFactory scopes, TimeProvider time) : IUserAuthenticator
{
    public const string MailboxIdKey = "Myotis:MailboxId";

    public async Task<bool> AuthenticateAsync(ISessionContext context, string user, string password, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var mailboxId = await scope.ServiceProvider.GetRequiredService<SmtpCredentialService>().VerifyAsync(user, password, cancellationToken);
        if (mailboxId is null)
        {
            return false;
        }

        context.Properties[MailboxIdKey] = mailboxId.Value;
        var now = time.GetUtcNow().UtcDateTime;
        await scope.ServiceProvider.GetRequiredService<MyotisDbContext>().SmtpCredentials
            .Where(c => c.Username == user.Trim())
            .ExecuteUpdateAsync(c => c.SetProperty(x => x.LastUsedAt, now), cancellationToken);
        return true;
    }
}
