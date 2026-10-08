using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace MonitorProfiles.App.Localization;

public sealed class LocalizationService : INotifyPropertyChanged
{
    private static readonly ResourceManager Resources = new(
        "MonitorProfiles.App.Resources.Strings",
        typeof(LocalizationService).Assembly);

    public static IReadOnlyList<LanguageOption> SupportedLanguages { get; } = Array.AsReadOnly<LanguageOption>(
    [
        new("en", "English"),
        new("es", "Español"),
        new("fr", "Français"),
        new("it", "Italiano"),
        new("ja", "日本語"),
        new("de", "Deutsch"),
        new("zh-Hans", "简体中文")
    ]);

    public static IReadOnlyList<string> RequiredResourceKeys { get; } = Array.AsReadOnly<string>(
    [
        "App.Title", "App.Tagline",
        "Status.Detecting", "Status.SetupRequired", "Status.ScreensDetected", "Status.ProfilesReady", "Status.Restored",
        "Status.ApplyingProfile", "Status.ProfileApplied", "Status.ApplyFailed", "Status.RestoreFailed", "Status.OperationInProgress",
        "Status.ProfileSaved", "Status.ProfileDeleted", "Status.PreferencesRecovered", "Status.ActiveCount",
        "Error.ConfigMalformed", "Error.ConfigFuture", "Error.DisplayDetection", "Error.DisplayUnavailable",
        "Error.ProfileModeUnavailable", "Error.InvalidProfile", "Error.ProfileSave", "Error.Preferences", "Error.TechnicalDetails",
        "Setup.Title", "Setup.Description", "Setup.Save", "Common.Identify",
        "Profiles.Title", "Profiles.Subtitle", "Profiles.New", "Profiles.Apply", "Profiles.Edit", "Profiles.Delete",
        "Profiles.BuiltIn", "Profiles.Custom",
        "Displays.Title", "Display.Active", "Display.Inactive", "Display.NoMode", "Display.Unassigned",
        "Theme.Menu", "Theme.System", "Theme.Light", "Theme.Dark", "Language.Menu",
        "Editor.Title", "Editor.Description", "Editor.ProfileName", "Editor.Screen", "Editor.Width",
        "Editor.Height", "Editor.Refresh", "Editor.Orientation", "Editor.Role", "Editor.Primary",
        "Editor.DetectedModes", "Editor.Landscape", "Editor.Portrait", "Editor.LandscapeFlipped",
        "Editor.PortraitFlipped", "Editor.Cancel", "Editor.Preview", "Editor.Save", "Editor.InvalidMode",
        "Editor.NameRequired", "Editor.DuplicateName",
        "Preview.Title", "Preview.Description", "Preview.Countdown", "Preview.Keep", "Preview.Revert",
        "Tray.Open", "Tray.Revert", "Tray.Exit",
        "Startup.Reminder", "Startup.Enable", "Startup.Later", "Startup.Menu", "Error.StartupRegistration",
        "Dialog.DeleteTitle", "Dialog.DeleteConfirm", "Dialog.ErrorTitle", "Dialog.RevertTitle",
        "Identify.OffTitle", "Identify.OffDetail"
    ]);

    private static readonly IReadOnlyDictionary<string, string> CanonicalLanguageCodes =
        SupportedLanguages.ToDictionary(language => language.Code, language => language.Code, StringComparer.OrdinalIgnoreCase);

    private CultureInfo _culture = CultureInfo.GetCultureInfo("en");

    public static LocalizationService Instance { get; } = new();

    public string LanguageCode => CanonicalLanguageCodes.TryGetValue(_culture.Name, out var code) ? code : "en";

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? LanguageChanged;

    public string this[string key] => Get(key);

    public string Get(string key, params object?[] arguments)
    {
        var value = Resources.GetString(key, _culture) ?? Resources.GetString(key, CultureInfo.GetCultureInfo("en")) ?? key;
        return arguments.Length == 0 ? value : string.Format(_culture, value, arguments);
    }

    public void SetLanguage(string? languageCode)
    {
        var canonicalCode = languageCode is not null && CanonicalLanguageCodes.TryGetValue(languageCode, out var code)
            ? code
            : "en";
        if (string.Equals(LanguageCode, canonicalCode, StringComparison.Ordinal))
        {
            return;
        }

        _culture = CultureInfo.GetCultureInfo(canonicalCode);
        OnPropertyChanged(nameof(LanguageCode));
        OnPropertyChanged("Item[]");
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
