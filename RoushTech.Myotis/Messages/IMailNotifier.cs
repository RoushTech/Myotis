namespace RoushTech.Myotis.Messages;

using RoushTech.Myotis.Data.Models;

public interface IMailNotifier
{
    Task MessageReceivedAsync(Message message, CancellationToken cancellationToken);
}

public class NoMailNotifier : IMailNotifier
{
    public Task MessageReceivedAsync(Message message, CancellationToken cancellationToken) => Task.CompletedTask;
}
