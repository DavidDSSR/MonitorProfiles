namespace MonitorProfiles.Core.Models;

public sealed record DisplayDescriptor(
    string DisplayId,
    string FriendlyName,
    bool IsConnected,
    bool IsActive,
    bool IsPrimary,
    DisplayMode? CurrentMode,
    IReadOnlyList<DisplayMode> SupportedModes,
    bool SupportedModesAreComplete = false,
    string? GdiDeviceName = null);
