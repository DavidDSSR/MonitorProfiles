namespace MonitorProfiles.Core.Models;

public sealed record DisplayAssignment(
    string DisplayId,
    bool IsEnabled,
    DisplayMode? Mode,
    bool IsPrimary);
