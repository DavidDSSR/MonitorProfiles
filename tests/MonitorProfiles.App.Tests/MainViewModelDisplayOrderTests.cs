using MonitorProfiles.App.Localization;
using MonitorProfiles.App.Services;
using MonitorProfiles.App.ViewModels;
using MonitorProfiles.Core.Models;
using MonitorProfiles.Core.Services;
using MonitorProfiles.Storage;

namespace MonitorProfiles.App.Tests;

public sealed class MainViewModelDisplayOrderTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "MonitorProfilesDisplayOrderTests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task InitializeAsync_orders_named_displays_by_their_monitor_alias()
    {
        var displays = new[]
        {
            CreateDisplay("id-2", "Zeta Panel"),
            CreateDisplay("id-4", "Beta Panel"),
            CreateDisplay("id-1", "Delta Panel"),
            CreateDisplay("id-3", "Alpha Panel")
        };
        var mappings = new Dictionary<string, string>
        {
            ["Monitor 1"] = "id-1",
            ["Monitor 2"] = "id-2",
            ["Monitor 3"] = "id-3",
            ["Monitor 4"] = "id-4"
        };
        var profileRepository = new ProfileRepository(Path.Combine(_directory, "profiles.json"));
        await profileRepository.SaveAsync(new ProfileStoreDocument(
            ProfileStoreDocument.CurrentSchemaVersion,
            [],
            mappings));
        var preferencesRepository = new ApplicationPreferencesRepository(Path.Combine(_directory, "preferences.json"));
        var viewModel = new MainViewModel(
            new FakeDisplayService(displays),
            profileRepository,
            preferencesRepository,
            new ApplicationPreferencesLoadResult(ApplicationPreferences.Default, null),
            new FakeThemeService(),
            new LocalizationService(),
            new FakeStartupRegistrationService());

        await viewModel.InitializeAsync();

        Assert.Equal(
            new[] { "Monitor 1", "Monitor 2", "Monitor 3", "Monitor 4" },
            viewModel.Displays.Select(display => display.Alias));
    }

    private static DisplayDescriptor CreateDisplay(string id, string name) =>
        new(id, name, IsConnected: true, IsActive: false, IsPrimary: false, null, Array.Empty<DisplayMode>());

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private sealed class FakeThemeService : IThemeService
    {
        public ThemePreference Preference => ThemePreference.System;
        public event EventHandler? ThemeChanged;
        public void Apply(ThemePreference preference) => ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    private sealed class FakeStartupRegistrationService : IStartupRegistrationService
    {
        public bool IsEnabled => false;
        public void SetEnabled(bool enabled) { }
    }

    private sealed class FakeDisplayService(IReadOnlyList<DisplayDescriptor> displays) : IDisplayService
    {
        public Task<IReadOnlyList<DisplayDescriptor>> GetDisplaysAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(displays);

        public Task<IDisplayConfigurationSnapshot> CaptureConfigurationAsync(CancellationToken cancellationToken = default) =>
            Task.FromException<IDisplayConfigurationSnapshot>(new NotSupportedException());

        public Task ApplyProfileAsync(DisplayProfile profile, string primaryDisplayId, CancellationToken cancellationToken = default) =>
            Task.FromException(new NotSupportedException());

        public Task RestoreConfigurationAsync(IDisplayConfigurationSnapshot snapshot, CancellationToken cancellationToken = default) =>
            Task.FromException(new NotSupportedException());
    }
}
