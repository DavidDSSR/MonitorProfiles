using MonitorProfiles.Core.Models;

namespace MonitorProfiles.Windows;

public static class DisplayModeConverter
{
    public static int ToRoundedRefreshRate(uint numerator, uint denominator)
    {
        if (denominator == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(denominator), "A refresh-rate denominator cannot be zero.");
        }

        return checked((int)Math.Round((double)numerator / denominator, MidpointRounding.AwayFromZero));
    }

    public static DisplayOrientation ToOrientation(int windowsRotation) => windowsRotation switch
    {
        0 => DisplayOrientation.Landscape,
        1 => DisplayOrientation.Landscape,
        2 => DisplayOrientation.Portrait,
        3 => DisplayOrientation.LandscapeFlipped,
        4 => DisplayOrientation.PortraitFlipped,
        _ => throw new ArgumentOutOfRangeException(nameof(windowsRotation), windowsRotation, "Unknown Windows display rotation value.")
    };

    public static int ToWindowsRotation(DisplayOrientation orientation) => orientation switch
    {
        DisplayOrientation.Landscape => 1,
        DisplayOrientation.Portrait => 2,
        DisplayOrientation.LandscapeFlipped => 3,
        DisplayOrientation.PortraitFlipped => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(orientation), orientation, "Unknown display orientation.")
    };
}
