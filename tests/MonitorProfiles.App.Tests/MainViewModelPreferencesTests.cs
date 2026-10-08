using MonitorProfiles.App.Localization;
using MonitorProfiles.App.Services;
using MonitorProfiles.App.ViewModels;
using MonitorProfiles.Core.Models;
using MonitorProfiles.Core.Services;
using MonitorProfiles.Storage;

namespace MonitorProfiles.App.Tests;

public sealed class MainViewModelPreferencesTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "MonitorProfilesAppPreferencesTests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task SetLanguageAsync_updates_localization_and_persists_the_choice()
    {
        var preferencesRepository = new ApplicationPreferencesRepository(Path.Combine(_directory, "preferences.json"));
        var localization = new LocalizationService();
        var viewModel = CreateViewModel(preferencesRepository, localization, new FakeThemeService());

        await viewModel.SetLanguageAsync("fr");

        var saved = await preferencesRepository.LoadAsync();
        Assert.Equal("fr", localization.LanguageCode);
        Assert.Equal("fr", viewModel.SelectedLanguageCode);
        Assert.Equal("fr", saved.Preferences.EffectiveLanguageCode);
    }

    [Fact]
    public async Task SetThemeAsync_applies_and_persists_the_choice()
    {
        var preferencesRepository = new ApplicationPreferencesRepository(Path.Combine(_directory, "preferences.json"));
        var themeService = new FakeThemeService();
        var viewModel = CreateViewModel(preferencesRepository, new LocalizationService(), themeService);

        await viewModel.SetThemeAsync(ThemePreference.Dark);

        var saved = await preferencesRepository.LoadAsync();
        Assert.Equal(ThemePreference.Dark, themeService.Preference);
        Assert.Equal(ThemePreference.Dark, viewModel.SelectedTheme);
        Assert.Equal(ThemePreference.Dark, saved.Preferences.EffectiveTheme);
    }

    private MainViewModel CreateViewModel(
        ApplicationPreferencesRepository preferencesRepository,
        LocalizationService localization,
        IThemeService themeService) => new(
            new EmptyDisplayService(),
            new ProfileRepository(Path.Combine(_directory, "profiles.json")),
            preferencesRepository,
            new ApplicationPreferencesLoadResult(ApplicationPreferences.Default, null),
            themeService,
            localization);

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private sealed class FakeThemeService : IThemeService
    {
        public ThemePreference Preference { get; private set; } = ThemePreference.System;
        public event EventHandler? ThemeChanged;

        public void Apply(ThemePreference preference)
        {
            Preference = preference;
            ThemeChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private sealed class EmptyDisplayService : IDisplayService
    {
        public Task<IReadOnlyList<DisplayDescriptor>> GetDisplaysAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DisplayDescriptor>>(Array.Empty<DisplayDescriptor>());

        public Task<IDisplayConfigurationSnapshot> CaptureConfigurationAsync(CancellationToken cancellationToken = default) =>
            Task.FromException<IDisplayConfigurationSnapshot>(new NotSupportedException());

        public Task ApplyProfileAsync(DisplayProfile profile, string primaryDisplayId, CancellationToken cancellationToken = default) =>
            Task.FromException(new NotSupportedException());

        public Task RestoreConfigurationAsync(IDisplayConfigurationSnapshot snapshot, CancellationToken cancellationToken = default) =>
            Task.FromException(new NotSupportedException());
    }
}
