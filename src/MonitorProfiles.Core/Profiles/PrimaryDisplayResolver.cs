using MonitorProfiles.Core.Models;

namespace MonitorProfiles.Core.Profiles;

public sealed record PrimaryDisplayResolution(string? DisplayId, bool WasFallback);

public static class PrimaryDisplayResolver
{
    public static PrimaryDisplayResolution Resolve(
        DisplayProfile profile,
        IEnumerable<string> connectedDisplayIds)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(connectedDisplayIds);

        var connected = new HashSet<string>(connectedDisplayIds, StringComparer.OrdinalIgnoreCase);
        var activeConnected = profile.Displays
            .Where(display => display.IsEnabled && connected.Contains(display.DisplayId))
            .ToArray();

        var preferred = activeConnected.FirstOrDefault(display => display.IsPrimary);
        if (preferred is not null)
        {
            return new PrimaryDisplayResolution(preferred.DisplayId, WasFallback: false);
        }

        var fallback = activeConnected
            .OrderBy(display => display.DisplayId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        return new PrimaryDisplayResolution(fallback?.DisplayId, WasFallback: true);
    }
}
