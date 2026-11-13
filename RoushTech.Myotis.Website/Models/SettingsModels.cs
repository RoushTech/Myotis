namespace RoushTech.Myotis.Website.Models;

public class SettingsRequest
{
    public required List<SettingChangeRequest> Changes { get; init; }
}

public class SettingChangeRequest
{
    public required string Key { get; init; }

    public string? Value { get; init; }

    public bool Clear { get; init; }
}
