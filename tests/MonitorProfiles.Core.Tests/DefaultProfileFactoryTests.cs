using MonitorProfiles.Core.Models;
using MonitorProfiles.Core.Profiles;

namespace MonitorProfiles.Core.Tests;

public sealed class DefaultProfileFactoryTests
{
    private static readonly IReadOnlyDictionary<string, string> DisplayIds =
        new Dictionary<string, string>
        {
            ["Monitor 1"] = "device-1",
            ["Monitor 2"] = "device-2",
            ["Monitor 3"] = "device-3",
            ["Monitor 4"] = "device-4"
        };

    [Fact]
    public void Create_returns_three_profiles_with_requested_display_modes()
    {
        var profiles = DefaultProfileFactory.Create(DisplayIds);

        Assert.Equal(new[] { "Trabajo", "Competitivo", "Historia" }, profiles.Select(profile => profile.Name));

        var work = profiles[0];
        AssertActiveMode(work, "device-2", 2560, 1440, 144, DisplayOrientation.Landscape, primary: true);
        AssertActiveMode(work, "device-3", 1080, 1920, 60, DisplayOrientation.PortraitFlipped, primary: false);
        AssertInactive(work, "device-1");
        AssertInactive(work, "device-4");

        var competitive = profiles[1];
        AssertActiveMode(competitive, "device-2", 1920, 1080, 144, DisplayOrientation.Landscape, primary: true);
        AssertInactive(competitive, "device-1");
        AssertInactive(competitive, "device-3");
        AssertInactive(competitive, "device-4");

        var story = profiles[2];
        AssertActiveMode(story, "device-4", 2560, 1440, 144, DisplayOrientation.Landscape, primary: true);
        AssertInactive(story, "device-1");
        AssertInactive(story, "device-2");
        AssertInactive(story, "device-3");
    }

    [Fact]
    public void Create_requires_all_four_named_display_mappings()
    {
        var incomplete = new Dictionary<string, string>(DisplayIds);
        incomplete.Remove("Monitor 4");

        Assert.Throws<ArgumentException>(() => DefaultProfileFactory.Create(incomplete));
    }

    private static void AssertActiveMode(
        DisplayProfile profile,
        string displayId,
        int width,
        int height,
        int refreshRate,
        DisplayOrientation orientation,
        bool primary)
    {
        var assignment = Assert.Single(profile.Displays, display => display.DisplayId == displayId);
        Assert.True(assignment.IsEnabled);
        Assert.Equal(new DisplayMode(width, height, refreshRate, orientation), assignment.Mode);
        Assert.Equal(primary, assignment.IsPrimary);
    }

    private static void AssertInactive(DisplayProfile profile, string displayId)
    {
        var assignment = Assert.Single(profile.Displays, display => display.DisplayId == displayId);
        Assert.False(assignment.IsEnabled);
        Assert.False(assignment.IsPrimary);
    }
}
