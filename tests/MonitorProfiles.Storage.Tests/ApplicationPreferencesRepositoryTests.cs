using MonitorProfiles.Storage;

namespace MonitorProfiles.Storage.Tests;

public sealed class ApplicationPreferencesRepositoryTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "MonitorProfilesPreferencesTests", Guid.NewGuid().ToString("N"));
    private readonly string _path;

    public ApplicationPreferencesRepositoryTests()
    {
        _path = Path.Combine(_directory, "preferences.json");
    }

    [Fact]
    public async Task LoadAsync_uses_system_theme_and_english_when_file_is_missing()
    {
        var repository = new ApplicationPreferencesRepository(_path);

        var result = await repository.LoadAsync();

        Assert.Equal(ThemePreference.System, result.Preferences.EffectiveTheme);
        Assert.Equal("en", result.Preferences.EffectiveLanguageCode);
        Assert.Null(result.RecoveryReason);
    }

    [Fact]
    public async Task SaveAsync_and_LoadAsync_round_trip_a_supported_theme_and_language()
    {
        var repository = new ApplicationPreferencesRepository(_path);
        var preferences = new ApplicationPreferences(
            ApplicationPreferences.CurrentSchemaVersion,
            "Dark",
            "zh-Hans");

        await repository.SaveAsync(preferences);
        var result = await repository.LoadAsync();

        Assert.Equal(ThemePreference.Dark, result.Preferences.EffectiveTheme);
        Assert.Equal("zh-Hans", result.Preferences.EffectiveLanguageCode);
        Assert.Null(result.RecoveryReason);
    }

    [Fact]
    public async Task LoadAsync_falls_back_independently_for_unknown_theme_and_language()
    {
        Directory.CreateDirectory(_directory);
        await File.WriteAllTextAsync(_path, "{\"schemaVersion\":1,\"theme\":\"Neon\",\"languageCode\":\"xx\"}");
        var repository = new ApplicationPreferencesRepository(_path);

        var result = await repository.LoadAsync();

        Assert.Equal(ThemePreference.System, result.Preferences.EffectiveTheme);
        Assert.Equal("en", result.Preferences.EffectiveLanguageCode);
        Assert.Null(result.RecoveryReason);
    }

    [Fact]
    public async Task LoadAsync_preserves_a_malformed_file_and_returns_default_preferences_with_recovery_reason()
    {
        Directory.CreateDirectory(_directory);
        const string malformed = "{ bad json";
        await File.WriteAllTextAsync(_path, malformed);
        var repository = new ApplicationPreferencesRepository(_path);

        var result = await repository.LoadAsync();

        Assert.Equal(ThemePreference.System, result.Preferences.EffectiveTheme);
        Assert.Equal("en", result.Preferences.EffectiveLanguageCode);
        Assert.Equal(PreferencesRecoveryReason.Malformed, result.RecoveryReason);
        Assert.Equal(malformed, await File.ReadAllTextAsync(_path));
    }

    [Fact]
    public async Task LoadAsync_preserves_preferences_from_a_future_schema_and_returns_defaults()
    {
        Directory.CreateDirectory(_directory);
        const string future = "{\"schemaVersion\":999,\"theme\":\"Dark\",\"languageCode\":\"fr\"}";
        await File.WriteAllTextAsync(_path, future);
        var repository = new ApplicationPreferencesRepository(_path);

        var result = await repository.LoadAsync();

        Assert.Equal(ThemePreference.System, result.Preferences.EffectiveTheme);
        Assert.Equal("en", result.Preferences.EffectiveLanguageCode);
        Assert.Equal(PreferencesRecoveryReason.UnsupportedSchema, result.RecoveryReason);
        Assert.Equal(future, await File.ReadAllTextAsync(_path));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
