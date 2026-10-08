using MonitorProfiles.Core.Models;

namespace MonitorProfiles.Core.Profiles;

public static class DisplayProfileValidator
{
    public static ProfileValidationResult Validate(
        DisplayProfile profile,
        IEnumerable<DisplayProfile>? existingProfiles = null)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(profile.Name))
        {
            errors.Add("Profile name cannot be empty.");
        }

        if (existingProfiles?.Any(existing =>
                existing.Id != profile.Id &&
                string.Equals(existing.Name.Trim(), profile.Name.Trim(), StringComparison.OrdinalIgnoreCase)) == true)
        {
            errors.Add($"A profile named '{profile.Name.Trim()}' already exists.");
        }

        if (profile.Displays.Count == 0)
        {
            errors.Add("At least one display must be configured.");
            return new ProfileValidationResult(errors.AsReadOnly());
        }

        var repeatedId = profile.Displays
            .Where(display => !string.IsNullOrWhiteSpace(display.DisplayId))
            .GroupBy(display => display.DisplayId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (repeatedId is not null)
        {
            errors.Add($"Display '{repeatedId.Key}' is assigned more than once.");
        }

        foreach (var display in profile.Displays)
        {
            if (string.IsNullOrWhiteSpace(display.DisplayId))
            {
                errors.Add("Every display assignment must have a device ID.");
            }

            if (display.IsEnabled && display.Mode is null)
            {
                errors.Add($"Enabled display '{display.DisplayId}' must have a display mode.");
            }
            else if (display.IsEnabled && display.Mode is { } mode &&
                     (mode.Width <= 0 || mode.Height <= 0 || mode.RefreshRate <= 0))
            {
                errors.Add($"Display '{display.DisplayId}' has an invalid mode.");
            }

            if (!display.IsEnabled && display.IsPrimary)
            {
                errors.Add($"Disabled display '{display.DisplayId}' cannot be primary.");
            }
        }

        var activeDisplays = profile.Displays.Where(display => display.IsEnabled).ToArray();
        if (activeDisplays.Length == 0)
        {
            errors.Add("At least one display must be active.");
        }

        if (activeDisplays.Count(display => display.IsPrimary) != 1)
        {
            errors.Add("Exactly one active display must be primary.");
        }

        return new ProfileValidationResult(errors.AsReadOnly());
    }
}
