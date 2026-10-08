using MonitorProfiles.Core.Models;
using MonitorProfiles.Core.Profiles;

namespace MonitorProfiles.Core.Services;

public sealed class ProfileApplicationService(IDisplayService displayService)
{
    public async Task<ProfileApplicationResult> ApplySavedAsync(
        DisplayProfile profile,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var snapshot = await displayService.CaptureConfigurationAsync(cancellationToken);

        try
        {
            var validation = DisplayProfileValidator.Validate(profile);
            if (!validation.IsValid)
            {
                return ProfileApplicationResult.Failure(string.Join(Environment.NewLine, validation.Errors));
            }

            var displays = await displayService.GetDisplaysAsync(cancellationToken);
            var connectedIds = displays
                .Where(display => display.IsConnected)
                .Select(display => display.DisplayId)
                .ToArray();
            var availableById = displays
                .Where(display => display.IsConnected)
                .ToDictionary(display => display.DisplayId, StringComparer.OrdinalIgnoreCase);

            foreach (var assignment in profile.Displays.Where(display => display.IsEnabled))
            {
                if (!availableById.TryGetValue(assignment.DisplayId, out var display))
                {
                    return ProfileApplicationResult.Failure($"The configured display '{assignment.DisplayId}' is not connected.");
                }

                if (assignment.Mode is null ||
                    (display.SupportedModesAreComplete && !display.SupportedModes.Contains(assignment.Mode)))
                {
                    return ProfileApplicationResult.Failure($"The requested mode is unavailable on '{display.FriendlyName}'.");
                }
            }

            var primary = PrimaryDisplayResolver.Resolve(profile, connectedIds);
            if (primary.DisplayId is null)
            {
                return ProfileApplicationResult.Failure("No configured active display is currently connected.");
            }

            await displayService.ApplyProfileAsync(profile, primary.DisplayId, cancellationToken);
            var warnings = primary.WasFallback
                ? [$"The configured primary display is unavailable; '{availableById[primary.DisplayId].FriendlyName}' was selected instead."]
                : Array.Empty<string>();

            return ProfileApplicationResult.Success(warnings);
        }
        catch (Exception exception)
        {
            bool restorationSucceeded;
            try
            {
                await displayService.RestoreConfigurationAsync(snapshot, CancellationToken.None);
                restorationSucceeded = true;
            }
            catch
            {
                restorationSucceeded = false;
            }

            return ProfileApplicationResult.Failure(
                $"Could not apply profile '{profile.Name}': {exception.Message}",
                restorationSucceeded);
        }
    }
}
