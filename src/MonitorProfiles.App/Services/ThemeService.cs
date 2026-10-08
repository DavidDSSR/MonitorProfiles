using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using MonitorProfiles.Storage;

namespace MonitorProfiles.App.Services;

public enum ThemeAppearance
{
    Light,
    Dark
}

public sealed class ThemeService : IThemeService, IDisposable
{
    private const string PersonalizationKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private readonly Application _application;
    private ThemePreference _preference = ThemePreference.System;

    public ThemeService(Application application)
    {
        _application = application;
        SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
    }

    public event EventHandler? ThemeChanged;

    public ThemePreference Preference => _preference;

    public ThemeAppearance Appearance => ResolveAppearance(_preference, IsWindowsUsingLightAppearance());

    public static ThemeAppearance ResolveAppearance(ThemePreference preference, bool windowsUsesLightAppearance) => preference switch
    {
        ThemePreference.Light => ThemeAppearance.Light,
        ThemePreference.Dark => ThemeAppearance.Dark,
        _ => windowsUsesLightAppearance ? ThemeAppearance.Light : ThemeAppearance.Dark
    };

    public void Apply(ThemePreference preference)
    {
        _preference = preference;
        var appearance = ResolveAppearance(preference, IsWindowsUsingLightAppearance());

#pragma warning disable WPF0001
        _application.ThemeMode = preference switch
        {
            ThemePreference.Light => ThemeMode.Light,
            ThemePreference.Dark => ThemeMode.Dark,
            _ => ThemeMode.System
        };
#pragma warning restore WPF0001

        ReplaceSemanticPalette(appearance);
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose() => SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;

    private void ReplaceSemanticPalette(ThemeAppearance appearance)
    {
        var fileName = appearance == ThemeAppearance.Light ? "Light.xaml" : "Dark.xaml";
        var dictionary = new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/MonitorProfiles.App;component/Resources/Themes/{fileName}", UriKind.Absolute)
        };
        var dictionaries = _application.Resources.MergedDictionaries;
        var existing = dictionaries.FirstOrDefault(candidate => candidate.Contains("AppBackgroundBrush"));
        if (existing is not null)
        {
            dictionaries.Remove(existing);
        }

        dictionaries.Insert(0, dictionary);
    }

    private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (_preference != ThemePreference.System || e.Category != UserPreferenceCategory.General)
        {
            return;
        }

        _application.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => Apply(ThemePreference.System)));
    }

    private static bool IsWindowsUsingLightAppearance()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(PersonalizationKey, writable: false);
            return Convert.ToInt32(key?.GetValue("AppsUseLightTheme", 1), System.Globalization.CultureInfo.InvariantCulture) != 0;
        }
        catch
        {
            return true;
        }
    }
}
