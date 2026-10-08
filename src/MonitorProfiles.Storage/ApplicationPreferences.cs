namespace MonitorProfiles.Storage;

public enum ThemePreference
{
    System,
    Light,
    Dark
}

public enum PreferencesRecoveryReason
{
    Malformed,
    UnsupportedSchema
}

public sealed record ApplicationPreferences(
    int SchemaVersion,
    string ThemeName,
    string LanguageCode,
    bool LaunchAtStartup = false,
    bool StartupReminderDismissed = false)
{
    public const int CurrentSchemaVersion = 1;

    public static ApplicationPreferences Default { get; } =
        new(CurrentSchemaVersion, nameof(ThemePreference.System), "en");

    private static readonly IReadOnlyDictionary<string, string> SupportedLanguages =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "en",
            ["es"] = "es",
            ["fr"] = "fr",
            ["it"] = "it",
            ["ja"] = "ja",
            ["de"] = "de",
            ["zh-Hans"] = "zh-Hans"
        };

    public ThemePreference EffectiveTheme =>
        Enum.TryParse<ThemePreference>(ThemeName, ignoreCase: true, out var theme)
            ? theme
            : ThemePreference.System;

    public string EffectiveLanguageCode =>
        !string.IsNullOrWhiteSpace(LanguageCode) && SupportedLanguages.TryGetValue(LanguageCode, out var language)
            ? language
            : "en";

    public ApplicationPreferences Normalize() => new(
        CurrentSchemaVersion,
        EffectiveTheme.ToString(),
        EffectiveLanguageCode,
        LaunchAtStartup,
        StartupReminderDismissed);
}

public sealed record ApplicationPreferencesLoadResult(
    ApplicationPreferences Preferences,
    PreferencesRecoveryReason? RecoveryReason);
