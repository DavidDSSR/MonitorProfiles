using MonitorProfiles.Core.Models;
using MonitorProfiles.Windows;

namespace MonitorProfiles.Windows.Tests;

public sealed class DisplayModeConverterTests
{
    [Theory]
    [InlineData(144000, 1000, 144)]
    [InlineData(60000, 1000, 60)]
    [InlineData(60000, 1001, 60)]
    public void ToRoundedRefreshRate_converts_windows_rational_values(uint numerator, uint denominator, int expected)
    {
        Assert.Equal(expected, DisplayModeConverter.ToRoundedRefreshRate(numerator, denominator));
    }

    [Theory]
    [InlineData(1, DisplayOrientation.Landscape)]
    [InlineData(0, DisplayOrientation.Landscape)]
    [InlineData(2, DisplayOrientation.Portrait)]
    [InlineData(3, DisplayOrientation.LandscapeFlipped)]
    [InlineData(4, DisplayOrientation.PortraitFlipped)]
    public void ToOrientation_maps_windows_rotation_values(int rotation, DisplayOrientation expected)
    {
        Assert.Equal(expected, DisplayModeConverter.ToOrientation(rotation));
    }

    [Theory]
    [InlineData(DisplayOrientation.Landscape, 1)]
    [InlineData(DisplayOrientation.Portrait, 2)]
    [InlineData(DisplayOrientation.LandscapeFlipped, 3)]
    [InlineData(DisplayOrientation.PortraitFlipped, 4)]
    public void ToWindowsRotation_maps_profile_orientation(DisplayOrientation orientation, int expected)
    {
        Assert.Equal(expected, DisplayModeConverter.ToWindowsRotation(orientation));
    }

    [Theory]
    [InlineData(2560, 1440, DisplayOrientation.Landscape, 2560, 1440)]
    [InlineData(1080, 1920, DisplayOrientation.PortraitFlipped, 1920, 1080)]
    public void ToSourceDimensions_swaps_portrait_mode_dimensions(
        int width,
        int height,
        DisplayOrientation orientation,
        int expectedWidth,
        int expectedHeight)
    {
        Assert.Equal(
            (expectedWidth, expectedHeight),
            DisplayModeConverter.ToSourceDimensions(new MonitorProfiles.Core.Models.DisplayMode(width, height, 60, orientation)));
    }

    [Theory]
    [InlineData(DisplayOrientation.Landscape)]
    [InlineData(DisplayOrientation.PortraitFlipped)]
    public void ToWindowsScanLineOrdering_uses_progressive_scan_for_profile_modes(DisplayOrientation orientation)
    {
        var mode = new MonitorProfiles.Core.Models.DisplayMode(1920, 1080, 60, orientation);

        Assert.Equal(1, DisplayModeConverter.ToWindowsScanLineOrdering(mode));
    }
}
