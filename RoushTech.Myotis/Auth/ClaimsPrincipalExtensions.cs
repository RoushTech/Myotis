namespace RoushTech.Myotis.Auth;

using System.Security.Claims;

public static class ClaimsPrincipalExtensions
{
    public static Guid? UserId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public static Guid RequiredUserId(this ClaimsPrincipal user) =>
        user.UserId() ?? throw new InvalidOperationException("Not signed in");

    public static bool IsAdministrator(this ClaimsPrincipal user) => user.IsInRole(MyotisRoles.Administrator);
}
