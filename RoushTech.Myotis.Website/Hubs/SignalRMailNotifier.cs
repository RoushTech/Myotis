namespace RoushTech.Myotis.Website.Hubs;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using RoushTech.Myotis.Auth;
using RoushTech.Myotis.Data.Models;
using RoushTech.Myotis.Mailboxes;
using RoushTech.Myotis.Messages;

// Works out who may read the mailbox when each message arrives, so a revoked grant stops events at once.
public class SignalRMailNotifier(IHubContext<MailHub> hub, IServiceScopeFactory scopes) : IMailNotifier
{
    public const string MessageReceived = "MessageReceived";

    public async Task MessageReceivedAsync(Message message, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var administrators = (await scope.ServiceProvider.GetRequiredService<UserManager<User>>().GetUsersInRoleAsync(MyotisRoles.Administrator))
            .Select(u => u.Id)
            .ToList();
        var readers = await scope.ServiceProvider.GetRequiredService<MailboxService>().ReaderIdsAsync(message.MailboxId, administrators, cancellationToken);
        await hub.Clients.Users(readers.Select(id => id.ToString())).SendAsync(MessageReceived, MessageReader.ToSummary(message), cancellationToken);
    }
}
