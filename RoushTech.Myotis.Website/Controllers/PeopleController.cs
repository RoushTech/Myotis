namespace RoushTech.Myotis.Website.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RoushTech.Myotis.Auth;
using RoushTech.Myotis.Data.Models;
using RoushTech.Myotis.Users;
using RoushTech.Myotis.Website.Models;

[ApiController]
[Authorize(Roles = MyotisRoles.Administrator)]
public class PeopleController(UserService users, UserManager<User> userManager) : ControllerBase
{
    [HttpGet("api/v0/mailboxes/{mailbox}/people")]
    public async Task<IEnumerable<PersonResponse>> List([FromRoute] Mailbox mailbox, CancellationToken cancellationToken) =>
        (await users.PeopleAsync(mailbox, cancellationToken)).Select(PersonResponse.From);

    [HttpPost("api/v0/mailboxes/{mailbox}/people")]
    public async Task<ActionResult<IEnumerable<PersonResponse>>> Add([FromRoute] Mailbox mailbox, PersonRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            return NotFound();
        }

        await users.AddPersonAsync(mailbox, user, cancellationToken);
        return Ok((await users.PeopleAsync(mailbox, cancellationToken)).Select(PersonResponse.From));
    }

    [HttpDelete("api/v0/mailboxes/{mailbox}/people/{user}")]
    public async Task<IActionResult> Remove([FromRoute] Mailbox mailbox, [FromRoute] User user, CancellationToken cancellationToken)
    {
        await users.RemovePersonAsync(mailbox, user, cancellationToken);
        return NoContent();
    }
}
