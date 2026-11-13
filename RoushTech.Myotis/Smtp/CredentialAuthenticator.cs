namespace RoushTech.Myotis.Smtp;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RoushTech.Myotis.Data;
using RoushTech.Myotis.Mailboxes;
using RoushTech.Myotis.Settings;
using SmtpServer;
using SmtpServer.Authentication;
using SmtpServer.Protocol;

public class CredentialAuthenticator(
    IServiceScopeFactory scopes,
    SignInGuard guard,
    ClientAddress clientAddress,
    IOptionsMonitor<LimitsOptions> limits,
    TimeProvider time) : IUserAuthenticator
{
    public const string MailboxIdKey = "Myotis:MailboxId";

    public const string UsernameKey = "Myotis:Username";

    private const string FailuresKey = "Myotis:SignInFailures";

    private static readonly SmtpResponse Blocked = new(SmtpReplyCode.ServiceUnavailable, "4.7.0 Too many failed sign-ins, try again later");

    public async Task<bool> AuthenticateAsync(ISessionContext context, string user, string password, CancellationToken cancellationToken)
    {
        var address = clientAddress.Resolve(context);
        if (guard.IsBlocked(address))
        {
            throw new SmtpResponseException(Blocked, quit: true);
        }

        await using var scope = scopes.CreateAsyncScope();
        var mailboxId = await scope.ServiceProvider.GetRequiredService<SmtpCredentialService>().VerifyAsync(user, password, cancellationToken);
        if (mailboxId is null)
        {
            var failures = (context.Properties.TryGetValue(FailuresKey, out var count) ? (int)count : 0) + 1;
            context.Properties[FailuresKey] = failures;
            if (guard.RecordFailure(address) || failures >= limits.CurrentValue.AttemptsPerConnection)
            {
                throw new SmtpResponseException(Blocked, quit: true);
            }

            return false;
        }

        context.Properties[MailboxIdKey] = mailboxId.Value;
        context.Properties[UsernameKey] = user.Trim();
        var now = time.GetUtcNow().UtcDateTime;
        await scope.ServiceProvider.GetRequiredService<MyotisDbContext>().SmtpCredentials
            .Where(c => c.Username == user.Trim())
            .ExecuteUpdateAsync(c => c.SetProperty(x => x.LastUsedAt, now), cancellationToken);
        return true;
    }
}
