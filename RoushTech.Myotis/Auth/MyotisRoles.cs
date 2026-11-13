namespace RoushTech.Myotis.Auth;

public static class MyotisRoles
{
    public const string Administrator = "Administrator";

    public static IReadOnlyList<string> All { get; } = [Administrator];
}
