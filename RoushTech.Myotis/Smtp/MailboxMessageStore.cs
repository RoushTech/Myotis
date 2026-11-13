namespace RoushTech.Myotis.Smtp;

using System.Buffers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RoushTech.Myotis.Messages;
using SmtpServer;
using SmtpServer.Mail;
using SmtpServer.Protocol;
using SmtpServer.Storage;

public class MailboxMessageStore(IServiceScopeFactory scopes, ILogger<MailboxMessageStore> logger) : MessageStore
{
    public override async Task<SmtpResponse> SaveAsync(ISessionContext context, IMessageTransaction transaction, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
    {
        // The credential that signed in is the only routing key; recipients never choose the mailbox.
        if (!context.Properties.TryGetValue(CredentialAuthenticator.MailboxIdKey, out var value) || value is not Guid mailboxId)
        {
            return SmtpResponse.AuthenticationRequired;
        }

        try
        {
            await using var scope = scopes.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<MessageIntake>().SaveAsync(
                mailboxId,
                transaction.From.AsAddress(),
                transaction.To.Select(t => t.AsAddress()).ToList(),
                buffer.ToArray(),
                cancellationToken);
            return SmtpResponse.Ok;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Couldn't store a message for mailbox {MailboxId}", mailboxId);
            return SmtpResponse.TransactionFailed;
        }
    }
}
