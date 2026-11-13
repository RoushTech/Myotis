namespace RoushTech.Myotis.Settings;

// Administrators change these at runtime from the Limits page; the defaults here apply until they do.
public class LimitsOptions
{
    public const string Section = "Myotis:Limits";

    public int MaxMessageMegabytes { get; set; } = 25;

    public int KeepMessages { get; set; } = 1000;

    public int KeepDays { get; set; } = 14;

    public int MessagesPerMinute { get; set; } = 300;

    public int AttemptsPerConnection { get; set; } = 3;

    public int BlockAfterFailures { get; set; } = 10;

    public int BlockWindowMinutes { get; set; } = 10;

    public int BlockMinutes { get; set; } = 15;

    public int MaxMessageBytes => MaxMessageMegabytes * 1024 * 1024;
}
