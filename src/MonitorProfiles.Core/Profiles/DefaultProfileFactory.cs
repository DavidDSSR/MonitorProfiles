using MonitorProfiles.Core.Models;

namespace MonitorProfiles.Core.Profiles;

public static class DefaultProfileFactory
{
    private static readonly string[] RequiredDisplayNames =
    [
        "Monitor 1",
        "Monitor 2",
        "Monitor 3",
        "Monitor 4"
    ];

    public static IReadOnlyList<DisplayProfile> Create(IReadOnlyDictionary<string, string> displayIds)
    {
        ArgumentNullException.ThrowIfNull(displayIds);

        foreach (var name in RequiredDisplayNames)
        {
            if (!displayIds.TryGetValue(name, out var id) || string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException($"A non-empty device ID is required for {name}.", nameof(displayIds));
            }
        }

        var ids = RequiredDisplayNames.Select(name => displayIds[name]).ToArray();
        if (ids.Distinct(StringComparer.OrdinalIgnoreCase).Count() != ids.Length)
        {
            throw new ArgumentException("Each monitor name must map to a different device ID.", nameof(displayIds));
        }

        var profiles = new[]
        {
            CreateProfile("Trabajo", displayIds, "Monitor 2", new DisplayMode(2560, 1440, 144, DisplayOrientation.Landscape), "Monitor 3", new DisplayMode(1080, 1920, 60, DisplayOrientation.PortraitFlipped)),
            CreateProfile("Competitivo", displayIds, "Monitor 2", new DisplayMode(1920, 1080, 144, DisplayOrientation.Landscape)),
            CreateProfile("Historia", displayIds, "Monitor 4", new DisplayMode(2560, 1440, 144, DisplayOrientation.Landscape))
        };

        return Array.AsReadOnly(profiles);
    }

    private static DisplayProfile CreateProfile(
        string name,
        IReadOnlyDictionary<string, string> displayIds,
        string primaryName,
        DisplayMode primaryMode,
        string? secondaryName = null,
        DisplayMode? secondaryMode = null)
    {
        var assignments = RequiredDisplayNames
            .Select(displayName =>
            {
                var isPrimary = displayName == primaryName;
                var isSecondary = displayName == secondaryName;
                var enabled = isPrimary || isSecondary;
                var mode = isPrimary ? primaryMode : isSecondary ? secondaryMode : null;
                return new DisplayAssignment(displayIds[displayName], enabled, mode, isPrimary);
            })
            .ToArray();

        return new DisplayProfile(Guid.NewGuid(), name, Array.AsReadOnly(assignments), IsBuiltIn: true);
    }
}
