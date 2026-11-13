namespace RoushTech.Myotis.Auth;

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using RoushTech.Myotis.Data.Models;

public static class PatAuthenticationDefaults
{
    public const string Scheme = "PersonalAccessToken";
}

// Tests and agents send "Authorization: Bearer myotis_pat_..." to /api/v1 and /mcp; the browser cookie never reaches those.
public class PatAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    PersonalAccessTokenService tokens,
    IUserClaimsPrincipalFactory<User> claimsPrincipalFactory) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private const string BearerPrefix = "Bearer ";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers[HeaderNames.Authorization].ToString();
        if (!header.StartsWith(BearerPrefix, StringComparison.Ordinal))
        {
            return AuthenticateResult.NoResult();
        }

        var token = header[BearerPrefix.Length..].Trim();
        if (!token.StartsWith(PersonalAccessTokenService.TokenPrefix, StringComparison.Ordinal))
        {
            return AuthenticateResult.NoResult();
        }

        var user = await tokens.ValidateAsync(token, Context.RequestAborted);
        if (user is null)
        {
            return AuthenticateResult.Fail("Invalid, expired or revoked token");
        }

        var principal = await claimsPrincipalFactory.CreateAsync(user);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }
}
