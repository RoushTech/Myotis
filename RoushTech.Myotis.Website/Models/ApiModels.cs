namespace RoushTech.Myotis.Website.Models;

public record AboutResponse(string Version);

public record AuthStateResponse(bool SetupRequired);

public record MeResponse(Guid Id, string Email, bool IsAdministrator);

public class LoginRequest
{
    public required string Email { get; init; }

    public required string Password { get; init; }

    public bool RememberMe { get; init; }
}

public class SetupRequest
{
    public required string Email { get; init; }

    public required string Password { get; init; }
}
