using MonitorProfiles.Core.Models;
using MonitorProfiles.Core.Profiles;

namespace MonitorProfiles.Core.Tests;

public sealed class PrimaryDisplayResolverTests
{
    [Fact]
    public void Resolve_keeps_the_preferred_primary_when_it_is_active_and_connected()
    {
        var profile = CreateProfile("device-2");

        var resolution = PrimaryDisplayResolver.Resolve(profile, ["device-1", "device-2"]);

        Assert.Equal("device-2", resolution.DisplayId);
        Assert.False(resolution.WasFallback);
    }

    [Fact]
    public void Resolve_uses_a_connected_active_display_when_preferred_primary_is_unavailable()
    {
        var profile = CreateProfile("device-2");

        var resolution = PrimaryDisplayResolver.Resolve(profile, ["device-1"]);

        Assert.Equal("device-1", resolution.DisplayId);
        Assert.True(resolution.WasFallback);
    }

    private static DisplayProfile CreateProfile(string preferredPrimary) => new(
        Guid.NewGuid(),
        "Test",
        [
            new DisplayAssignment("device-2", true, new DisplayMode(1920, 1080, 60, DisplayOrientation.Landscape), true),
            new DisplayAssignment("device-1", true, new DisplayMode(1920, 1080, 60, DisplayOrientation.Landscape), false)
        ]);
}
