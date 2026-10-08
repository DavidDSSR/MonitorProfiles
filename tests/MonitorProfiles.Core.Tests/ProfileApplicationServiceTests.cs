using MonitorProfiles.Core.Models;
using MonitorProfiles.Core.Services;

namespace MonitorProfiles.Core.Tests;

public sealed class ProfileApplicationServiceTests
{
    [Fact]
    public async Task ApplySavedAsync_applies_a_valid_saved_profile_without_a_confirmation_step()
    {
        var displayService = FakeDisplayService.WithDisplays(CreateDisplay("device-1", 1920, 1080, 144));
        var applicationService = new ProfileApplicationService(displayService);
        var profile = CreateProfile("device-1", new DisplayMode(1920, 1080, 144, DisplayOrientation.Landscape));

        var result = await applicationService.ApplySavedAsync(profile);

        Assert.True(result.Succeeded);
        Assert.Equal(1, displayService.ApplyCount);
        Assert.Equal(0, displayService.RestoreCount);
        Assert.Equal("device-1", displayService.LastPrimaryDisplayId);
    }

    [Fact]
    public async Task ApplySavedAsync_rejects_an_unavailable_mode_before_applying()
    {
        var displayService = FakeDisplayService.WithDisplays(CreateDisplay("device-1", 1920, 1080, 60, catalogIsComplete: true));
        var applicationService = new ProfileApplicationService(displayService);
        var profile = CreateProfile("device-1", new DisplayMode(2560, 1440, 144, DisplayOrientation.Landscape));

        var result = await applicationService.ApplySavedAsync(profile);

        Assert.False(result.Succeeded);
        Assert.Contains("mode", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, displayService.ApplyCount);
        Assert.Equal(0, displayService.RestoreCount);
    }

    [Fact]
    public async Task ApplySavedAsync_defers_mode_validation_to_windows_when_catalog_is_incomplete()
    {
        var displayService = FakeDisplayService.WithDisplays(CreateDisplay("device-1", 1920, 1080, 60, catalogIsComplete: false));
        var applicationService = new ProfileApplicationService(displayService);
        var profile = CreateProfile("device-1", new DisplayMode(2560, 1440, 144, DisplayOrientation.Landscape));

        var result = await applicationService.ApplySavedAsync(profile);

        Assert.True(result.Succeeded);
        Assert.Equal(1, displayService.ApplyCount);
    }

    [Fact]
    public async Task ApplySavedAsync_restores_previous_configuration_when_platform_apply_fails()
    {
        var displayService = FakeDisplayService.WithDisplays(CreateDisplay("device-1", 1920, 1080, 144));
        displayService.FailApply = true;
        var applicationService = new ProfileApplicationService(displayService);
        var profile = CreateProfile("device-1", new DisplayMode(1920, 1080, 144, DisplayOrientation.Landscape));

        var result = await applicationService.ApplySavedAsync(profile);

        Assert.False(result.Succeeded);
        Assert.Equal(1, displayService.ApplyCount);
        Assert.Equal(1, displayService.RestoreCount);
        Assert.True(result.RestorationSucceeded);
    }

    private static DisplayProfile CreateProfile(string displayId, DisplayMode mode) => new(
        Guid.NewGuid(),
        "Saved profile",
        [new DisplayAssignment(displayId, true, mode, true)]);

    private static DisplayDescriptor CreateDisplay(string id, int width, int height, int refreshRate, bool catalogIsComplete = false)
    {
        var mode = new DisplayMode(width, height, refreshRate, DisplayOrientation.Landscape);
        return new DisplayDescriptor(id, $"Display {id}", IsConnected: true, IsActive: true, IsPrimary: true, mode, [mode], catalogIsComplete);
    }

    private sealed class FakeDisplayService : IDisplayService
    {
        private readonly IReadOnlyList<DisplayDescriptor> _displays;

        private FakeDisplayService(IReadOnlyList<DisplayDescriptor> displays) => _displays = displays;

        public int ApplyCount { get; private set; }
        public int RestoreCount { get; private set; }
        public bool FailApply { get; set; }
        public string? LastPrimaryDisplayId { get; private set; }

        public static FakeDisplayService WithDisplays(params DisplayDescriptor[] displays) => new(displays);

        public Task<IReadOnlyList<DisplayDescriptor>> GetDisplaysAsync(CancellationToken cancellationToken = default) => Task.FromResult(_displays);

        public Task<IDisplayConfigurationSnapshot> CaptureConfigurationAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IDisplayConfigurationSnapshot>(new FakeSnapshot());

        public Task ApplyProfileAsync(DisplayProfile profile, string primaryDisplayId, CancellationToken cancellationToken = default)
        {
            ApplyCount++;
            LastPrimaryDisplayId = primaryDisplayId;
            if (FailApply)
            {
                throw new InvalidOperationException("Native apply failed.");
            }

            return Task.CompletedTask;
        }

        public Task RestoreConfigurationAsync(IDisplayConfigurationSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            RestoreCount++;
            return Task.CompletedTask;
        }

        private sealed record FakeSnapshot : IDisplayConfigurationSnapshot;
    }
}
