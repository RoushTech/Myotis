namespace RoushTech.Myotis.Website.Filters;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RoushTech.Myotis.Auth;
using RoushTech.Myotis.Data.Models;
using RoushTech.Myotis.Mailboxes;

// Global so no endpoint can skip it. Missing, mismatched and unseen entities all answer 404 so IDs and names don't leak.
public class MailboxAccessFilter(MailboxService mailboxes) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var bound = new List<IMailboxScoped>();
        foreach (var parameter in context.ActionDescriptor.Parameters.Where(p => p.ParameterType.IsAssignableTo(typeof(IEntityBinding))))
        {
            if (!context.ActionArguments.TryGetValue(parameter.Name, out var value) || value is not IMailboxScoped scoped)
            {
                context.Result = new NotFoundResult();
                return;
            }

            bound.Add(scoped);
        }

        var mailboxIds = bound.Select(e => e.MailboxId).Distinct().ToList();
        if (mailboxIds.Count > 1)
        {
            context.Result = new NotFoundResult();
            return;
        }

        if (mailboxIds.Count == 1)
        {
            var user = context.HttpContext.User;
            var userId = user.UserId();
            if (userId is null || !await mailboxes.CanSeeAsync(mailboxIds[0], userId.Value, user.IsAdministrator(), context.HttpContext.RequestAborted))
            {
                context.Result = new NotFoundResult();
                return;
            }
        }

        await next();
    }
}
