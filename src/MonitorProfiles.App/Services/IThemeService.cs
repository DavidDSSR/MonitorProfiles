using MonitorProfiles.Storage;

namespace MonitorProfiles.App.Services;

public interface IThemeService
{
    ThemePreference Preference { get; }

    event EventHandler? ThemeChanged;

    void Apply(ThemePreference preference);
}
