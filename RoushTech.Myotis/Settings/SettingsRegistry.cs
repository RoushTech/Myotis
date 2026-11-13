namespace RoushTech.Myotis.Settings;

public class SettingDefinition
{
    public required string Key { get; init; }

    public required string Group { get; init; }

    public required string Name { get; init; }

    public required string Unit { get; init; }

    public required int Default { get; init; }

    public int Min { get; init; } = 1;

    public int Max { get; init; } = 1_000_000;

    public string? Error(string? value) =>
        int.TryParse(value, out var number) && number >= Min && number <= Max
            ? null
            : $"Enter a whole number from {Min:N0} to {Max:N0}.";
}

// Every setting an administrator may change at runtime. Anything needed before the database is up stays in appsettings.
public static class SettingsRegistry
{
    private static readonly LimitsOptions Defaults = new();

    public static IReadOnlyList<SettingDefinition> All { get; } =
    [
        Limit(nameof(LimitsOptions.MaxMessageMegabytes), "Messages", "Largest message", "MB", Defaults.MaxMessageMegabytes, max: 1024),
        Limit(nameof(LimitsOptions.KeepMessages), "Retention", "Keep at most", "messages per mailbox", Defaults.KeepMessages),
        Limit(nameof(LimitsOptions.KeepDays), "Retention", "Delete messages after", "days", Defaults.KeepDays, max: 3650),
        Limit(nameof(LimitsOptions.MessagesPerMinute), "Sending rate", "Each credential can send", "messages a minute", Defaults.MessagesPerMinute),
        Limit(nameof(LimitsOptions.AttemptsPerConnection), "Failed sign-ins", "Close the connection after", "failed attempts", Defaults.AttemptsPerConnection, max: 100),
        Limit(nameof(LimitsOptions.BlockAfterFailures), "Failed sign-ins", "Block an address after", "failures", Defaults.BlockAfterFailures, max: 10_000),
        Limit(nameof(LimitsOptions.BlockWindowMinutes), "Failed sign-ins", "within", "minutes", Defaults.BlockWindowMinutes, max: 1440),
        Limit(nameof(LimitsOptions.BlockMinutes), "Failed sign-ins", "for", "minutes", Defaults.BlockMinutes, max: 10_080),
    ];

    private static readonly Dictionary<string, SettingDefinition> ByKey = All.ToDictionary(d => d.Key, StringComparer.OrdinalIgnoreCase);

    public static SettingDefinition? Find(string key) => ByKey.GetValueOrDefault(key);

    private static SettingDefinition Limit(string property, string group, string name, string unit, int defaultValue, int max = 1_000_000) =>
        new() { Key = $"{LimitsOptions.Section}:{property}", Group = group, Name = name, Unit = unit, Default = defaultValue, Max = max };
}
