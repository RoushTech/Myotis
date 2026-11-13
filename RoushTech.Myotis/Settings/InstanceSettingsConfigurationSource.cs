namespace RoushTech.Myotis.Settings;

using Microsoft.Extensions.Configuration;

// Layers the InstanceSettings table over appsettings under the same keys. Configuration is built before the
// database is reachable, so it reads nothing until InstanceSettingsLoader hands it a reader.
public class InstanceSettingsConfigurationSource : IConfigurationSource
{
    public Func<IDictionary<string, string?>>? Reader { get; set; }

    public IConfigurationProvider Build(IConfigurationBuilder builder) => new InstanceSettingsConfigurationProvider(this);
}

public class InstanceSettingsConfigurationProvider(InstanceSettingsConfigurationSource source) : ConfigurationProvider
{
    public override void Load()
    {
        // A database that isn't reachable yet must not stop the server starting; appsettings is enough to boot.
        try
        {
            Data = source.Reader?.Invoke() ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            Data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
