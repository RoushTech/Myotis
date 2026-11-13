namespace RoushTech.Myotis.Settings;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RoushTech.Myotis.Data;
using RoushTech.Myotis.Data.Models;

public sealed record SettingView(string Key, string Group, string Name, string Unit, int Default, int Min, int Max, int Value);

// Clear removes the stored value so the default applies again.
public sealed record SettingChange(string Key, string? Value, bool Clear);

public class SettingsService(MyotisDbContext db, IConfiguration configuration, TimeProvider time)
{
    public List<SettingView> GetAll() =>
        [.. SettingsRegistry.All.Select(d => new SettingView(
            d.Key, d.Group, d.Name, d.Unit, d.Default, d.Min, d.Max,
            int.TryParse(configuration[d.Key], out var value) ? value : d.Default))];

    // Saves nothing when any change is invalid, and returns an error per bad key.
    public async Task<IReadOnlyDictionary<string, string>> SaveAsync(IReadOnlyCollection<SettingChange> changes, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string>();
        foreach (var change in changes)
        {
            var definition = SettingsRegistry.Find(change.Key);
            var problem = definition is null ? "That setting can't be changed here." : change.Clear ? null : definition.Error(change.Value);
            if (problem is not null)
            {
                errors[change.Key] = problem;
            }
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        foreach (var change in changes)
        {
            var key = SettingsRegistry.Find(change.Key)!.Key;
            var setting = await db.InstanceSettings.SingleOrDefaultAsync(s => s.Key == key, cancellationToken);
            if (change.Clear)
            {
                if (setting is not null)
                {
                    db.InstanceSettings.Remove(setting);
                }

                continue;
            }

            setting ??= db.InstanceSettings.Add(new InstanceSetting { Id = Guid.CreateVersion7(), Key = key, Value = "" }).Entity;
            setting.Value = int.Parse(change.Value!.Trim()).ToString();
            setting.UpdatedAt = time.GetUtcNow().UtcDateTime;
        }

        await db.SaveChangesAsync(cancellationToken);
        (configuration as IConfigurationRoot)?.Reload();
        return errors;
    }
}
