namespace MonitorProfiles.Core.Models;

public sealed record DisplayProfile(
    Guid Id,
    string Name,
    IReadOnlyList<DisplayAssignment> Displays,
    bool IsBuiltIn = false);
