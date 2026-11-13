namespace RoushTech.Myotis.Website.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoushTech.Myotis.Auth;
using RoushTech.Myotis.Data.Models;
using RoushTech.Myotis.Website.Models;

// Each person manages their own tokens; a token reaches exactly the mailboxes its owner can.
[ApiController]
[Authorize]
[Route("api/v0/tokens")]
public class TokensController(PersonalAccessTokenService tokens) : ControllerBase
{
    [HttpGet("")]
    public async Task<IEnumerable<TokenResponse>> List(CancellationToken cancellationToken) =>
        (await tokens.ListAsync(User.RequiredUserId(), cancellationToken)).Select(ToResponse);

    [HttpPost("")]
    public async Task<CreatedTokenResponse> Create(CreateTokenRequest request, CancellationToken cancellationToken)
    {
        var (token, plainText) = await tokens.CreateAsync(User.RequiredUserId(), request.Name, request.ExpiresAt, cancellationToken);
        return new CreatedTokenResponse(ToResponse(token), plainText);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken cancellationToken) =>
        await tokens.RevokeAsync(User.RequiredUserId(), id, cancellationToken) ? NoContent() : NotFound();

    private static TokenResponse ToResponse(PersonalAccessToken t) =>
        new(t.Id, t.Name, t.Prefix, t.CreatedAt, t.ExpiresAt, t.LastUsedAt);
}
