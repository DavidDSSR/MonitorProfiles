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
}
