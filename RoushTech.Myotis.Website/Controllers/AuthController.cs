namespace RoushTech.Myotis.Website.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RoushTech.Myotis.Auth;
using RoushTech.Myotis.Data.Models;
using RoushTech.Myotis.Users;
using RoushTech.Myotis.Website.Models;

[ApiController]
[Route("api/v0/auth")]
public class AuthController(UserManager<User> users, SignInManager<User> signIn, UserService userService) : ControllerBase
{
    private const string WrongCredentials = "Wrong email or password";

    [HttpGet("state")]
    [AllowAnonymous]
    public async Task<AuthStateResponse> State(CancellationToken cancellationToken) =>
        new(await userService.SetupRequiredAsync(cancellationToken));

    [HttpPost("setup")]
    [AllowAnonymous]
    public async Task<MeResponse> Setup(SetupRequest request, CancellationToken cancellationToken)
    {
        var user = await userService.CreateFirstAdministratorAsync(request.Email, request.Password, cancellationToken);
        await signIn.SignInAsync(user, isPersistent: true);
        return await MeAsync(user);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<MeResponse>> Login(LoginRequest request)
    {
        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            return Problem(WrongCredentials, StatusCodes.Status401Unauthorized);
        }

        var result = await signIn.PasswordSignInAsync(user, request.Password, request.RememberMe, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            return Problem("Too many failed attempts. Try again in 15 minutes.", StatusCodes.Status401Unauthorized);
        }

        return result.Succeeded ? await MeAsync(user) : Problem(WrongCredentials, StatusCodes.Status401Unauthorized);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        await signIn.SignOutAsync();
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<MeResponse>> Me()
    {
        var user = await users.GetUserAsync(User);
        return user is null ? Unauthorized() : await MeAsync(user);
    }

    private async Task<MeResponse> MeAsync(User user) =>
        new(user.Id, user.Email!, await users.IsInRoleAsync(user, MyotisRoles.Administrator));

    private ObjectResult Problem(string title, int status) =>
        StatusCode(status, new ProblemDetails { Title = title, Status = status });
}
