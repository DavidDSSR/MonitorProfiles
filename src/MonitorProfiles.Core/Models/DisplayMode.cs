namespace MonitorProfiles.Core.Models;

public sealed record DisplayMode(
    int Width,
    int Height,
    int RefreshRate,
    DisplayOrientation Orientation);
