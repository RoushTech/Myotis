namespace RoushTech.Myotis.Smtp;

public class SmtpOptions
{
    public const string Section = "Myotis:Smtp";

    // Until limits move to the admin settings, the 25 MB default from the brief is fixed.
    public const int MaxMessageBytes = 25 * 1024 * 1024;

    public string ServerName { get; set; } = "myotis";

    public List<SmtpListenerOptions> Listeners { get; set; } = [];
}

public class SmtpListenerOptions
{
    public int Port { get; set; }
}
