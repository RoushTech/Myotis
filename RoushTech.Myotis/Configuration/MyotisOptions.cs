namespace RoushTech.Myotis.Configuration;

public class MyotisOptions
{
    public const string Section = "Myotis";

    public bool MigrateOnStartup { get; set; } = true;

    public SeedOptions Seed { get; set; } = new();
}

public class SeedOptions
{
    public string? AdminEmail { get; set; }

    public string? AdminPassword { get; set; }
}
