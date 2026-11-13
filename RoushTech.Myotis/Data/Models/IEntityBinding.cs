namespace RoushTech.Myotis.Data.Models;

// Controllers take these as parameters; the binder loads them by the route's ID and the access filter checks them.
public interface IEntityBinding;

public interface IMailboxScoped : IEntityBinding
{
    Guid MailboxId { get; }
}
