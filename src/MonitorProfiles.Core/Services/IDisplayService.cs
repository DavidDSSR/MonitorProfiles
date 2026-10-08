using MonitorProfiles.Core.Models;

namespace MonitorProfiles.Core.Services;

public interface IDisplayService
{
    Task<IReadOnlyList<DisplayDescriptor>> GetDisplaysAsync(CancellationToken cancellationToken = default);

    Task<IDisplayConfigurationSnapshot> CaptureConfigurationAsync(CancellationToken cancellationToken = default);

    Task ApplyProfileAsync(
        DisplayProfile profile,
        string primaryDisplayId,
        CancellationToken cancellationToken = default);

    Task RestoreConfigurationAsync(
        IDisplayConfigurationSnapshot snapshot,
        CancellationToken cancellationToken = default);
}
