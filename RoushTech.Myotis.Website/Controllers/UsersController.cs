namespace RoushTech.Myotis.Website.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoushTech.Myotis.Auth;
using RoushTech.Myotis.Data.Models;
using RoushTech.Myotis.Users;
using RoushTech.Myotis.Website.Models;

[ApiController]
[Authorize(Roles = MyotisRoles.Administrator)]
public class UsersController(UserService users) : ControllerBase
{
    [HttpGet("api/v0/users")]
    public async Task<IEnumerable<UserResponse>> List(CancellationToken cancellationToken) =>
        (await users.ListAsync(cancellationToken)).Select(UserResponse.From);

    [HttpPost("api/v0/users")]
    public async Task<UserResponse> Create(UserRequest request, CancellationToken cancellationToken)
    {
        var user = await users.CreateAsync(request.Email, request.Password, request.IsAdministrator, request.MailboxIds, cancellationToken);
        return UserResponse.From(await users.SummaryAsync(user, cancellationToken));
    }

    [HttpPatch("api/v0/users/{user}")]
    public async Task<UserResponse> Update([FromRoute] User user, UserUpdateRequest request, CancellationToken cancellationToken)
    {
        await users.UpdateAsync(user, User.RequiredUserId(), request.IsAdministrator, request.MailboxIds, request.Password, cancellationToken);
        return UserResponse.From(await users.SummaryAsync(user, cancellationToken));
    }

    [HttpDelete("api/v0/users/{user}")]
    public async Task<IActionResult> Delete([FromRoute] User user)
    {
        await users.DeleteAsync(user, User.RequiredUserId());
        return NoContent();
    }
}
