namespace RoushTech.Myotis.Smtp;

using SmtpServer;
using SmtpServer.Mail;
using SmtpServer.Protocol;
using SmtpServer.Storage;

// Checked at MAIL FROM, before the sender uploads anything. A 4xx reply makes well-behaved senders retry later.
public class RateLimitingMailboxFilter(SendRateLimiter limiter) : IMailboxFilter
{
    private static readonly SmtpResponse TooMany = new(SmtpReplyCode.Aborted, "4.7.0 Too many messages from this credential, try again shortly");

    public Task<bool> CanAcceptFromAsync(ISessionContext context, IMailbox from, int size, CancellationToken cancellationToken)
    {
        var credential = context.Properties.TryGetValue(CredentialAuthenticator.UsernameKey, out var value) ? value as string : null;
        if (credential is not null && !limiter.TryAcquire(credential))
        {
            throw new SmtpResponseException(TooMany);
        }

        return Task.FromResult(true);
    }

    public Task<bool> CanDeliverToAsync(ISessionContext context, IMailbox to, IMailbox from, CancellationToken cancellationToken) =>
        Task.FromResult(true);
}
