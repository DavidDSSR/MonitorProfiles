using System.ComponentModel;
using System.Runtime.InteropServices;
using MonitorProfiles.Core.Models;
using MonitorProfiles.Core.Services;
using MonitorProfiles.Windows.Interop;

namespace MonitorProfiles.Windows;

public sealed class WindowsDisplayService : IDisplayService
{
    private const int MaximumQueryAttempts = 4;
    private const uint SetApply = 0x00000080;
    private const uint SetUseSuppliedDisplayConfig = 0x00000020;
    private const uint SetSaveToDatabase = 0x00000200;
    private const uint SetValidate = 0x00000040;

    public Task<IReadOnlyList<DisplayDescriptor>> GetDisplaysAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run<IReadOnlyList<DisplayDescriptor>>(EnumerateDisplays, cancellationToken);
    }

    public Task<IDisplayConfigurationSnapshot> CaptureConfigurationAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = QueryDisplayConfiguration(DisplayConfigNative.QueryOnlyActivePaths);
        return Task.FromResult<IDisplayConfigurationSnapshot>(new WindowsDisplayConfigurationSnapshot(result.Paths, result.Modes));
    }

    public Task ApplyProfileAsync(
        DisplayProfile profile,
        string primaryDisplayId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentException.ThrowIfNullOrWhiteSpace(primaryDisplayId);

        var allPaths = QueryDisplayConfiguration(DisplayConfigNative.QueryAllPaths).Paths;
        var currentConfiguration = QueryDisplayConfiguration(DisplayConfigNative.QueryOnlyActivePaths);
        var currentPositions = GetCurrentPositions(currentConfiguration);
        var targetNames = new Dictionary<TargetKey, DisplayConfigTargetDeviceName>();
        var pathsByDisplayId = new Dictionary<string, List<DisplayConfigPathInfo>>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in allPaths)
        {
            var targetKey = TargetKey.From(path.TargetInfo.AdapterId, path.TargetInfo.Id);
            if (!targetNames.TryGetValue(targetKey, out var targetName))
            {
                if (!TryGetTargetName(path.TargetInfo, out targetName))
                {
                    continue;
                }

                targetNames.Add(targetKey, targetName);
            }

            if (string.IsNullOrWhiteSpace(targetName.MonitorDevicePath))
            {
                continue;
            }

            if (!pathsByDisplayId.TryGetValue(targetName.MonitorDevicePath, out var targetPaths))
            {
                targetPaths = [];
                pathsByDisplayId.Add(targetName.MonitorDevicePath, targetPaths);
            }

            targetPaths.Add(path);
        }

        var enabledAssignments = profile.Displays.Where(display => display.IsEnabled).ToArray();
        if (enabledAssignments.Length == 0 || !enabledAssignments.Any(display => string.Equals(display.DisplayId, primaryDisplayId, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("The primary display must be included among the enabled profile displays.", nameof(primaryDisplayId));
        }

        var orderedAssignments = enabledAssignments
            .OrderByDescending(display => string.Equals(display.DisplayId, primaryDisplayId, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var selectedPaths = new List<DisplayConfigPathInfo>(orderedAssignments.Length);
        var modeTable = new List<DisplayConfigModeInfo>(orderedAssignments.Length);
        var usedSources = new HashSet<SourceKey>();
        var horizontalPosition = 0;

        foreach (var assignment in orderedAssignments)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (assignment.Mode is null)
            {
                throw new ArgumentException($"Enabled display '{assignment.DisplayId}' has no mode.", nameof(profile));
            }

            if (!pathsByDisplayId.TryGetValue(assignment.DisplayId, out var candidates))
            {
                throw new InvalidOperationException($"Windows did not report a path for display '{assignment.DisplayId}'.");
            }

            var selectedPath = default(DisplayConfigPathInfo);
            var foundPath = false;
            foreach (var candidate in candidates)
            {
                if (usedSources.Add(SourceKey.From(candidate.SourceInfo.AdapterId, candidate.SourceInfo.Id)))
                {
                    selectedPath = candidate;
                    foundPath = true;
                    break;
                }
            }

            if (!foundPath)
            {
                throw new InvalidOperationException("Windows did not provide enough independent display sources for this profile.");
            }

            var path = selectedPath;
            var isPrimary = string.Equals(assignment.DisplayId, primaryDisplayId, StringComparison.OrdinalIgnoreCase);
            var preferredPosition = currentPositions.GetValueOrDefault(
                TargetKey.From(path.TargetInfo.AdapterId, path.TargetInfo.Id),
                new DisplayPosition(horizontalPosition, 0));
            var position = isPrimary ? new DisplayPosition(0, 0) : preferredPosition;
            if (!isPrimary && position.X == 0 && position.Y == 0)
            {
                position = new DisplayPosition(horizontalPosition, 0);
            }

            var sourceModeIndex = checked((uint)modeTable.Count);
            modeTable.Add(new DisplayConfigModeInfo
            {
                InfoType = DisplayConfigNative.ModeInfoTypeSource,
                Id = path.SourceInfo.Id,
                AdapterId = path.SourceInfo.AdapterId,
                Mode = new DisplayConfigModeUnion
                {
                    SourceMode = new DisplayConfigSourceMode
                    {
                        Width = checked((uint)assignment.Mode.Width),
                        Height = checked((uint)assignment.Mode.Height),
                        PixelFormat = 4,
                        PositionX = position.X,
                        PositionY = position.Y
                    }
                }
            });

            path.Flags |= DisplayConfigNative.PathActive;
            path.SourceInfo.ModeInfoIndex = sourceModeIndex;
            path.TargetInfo.ModeInfoIndex = DisplayConfigNative.ModeIndexInvalid;
            path.TargetInfo.Rotation = DisplayModeConverter.ToWindowsRotation(assignment.Mode.Orientation);
            path.TargetInfo.RefreshRate = new DisplayConfigRational
            {
                Numerator = checked((uint)assignment.Mode.RefreshRate),
                Denominator = 1
            };
            selectedPaths.Add(path);
            horizontalPosition = checked(position.X + assignment.Mode.Width);
        }

        var paths = selectedPaths.ToArray();
        var modes = modeTable.ToArray();
        var validationResult = DisplayConfigNative.SetDisplayConfig(
            (uint)paths.Length,
            paths,
            (uint)modes.Length,
            modes,
            SetValidate | SetUseSuppliedDisplayConfig);
        ThrowForDisplayConfigError(validationResult, "validate the requested display profile");

        cancellationToken.ThrowIfCancellationRequested();
        var applyResult = DisplayConfigNative.SetDisplayConfig(
            (uint)paths.Length,
            paths,
            (uint)modes.Length,
            modes,
            SetApply | SetUseSuppliedDisplayConfig | SetSaveToDatabase);
        ThrowForDisplayConfigError(applyResult, "apply the requested display profile");
        return Task.CompletedTask;
    }

    public Task RestoreConfigurationAsync(
        IDisplayConfigurationSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (snapshot is not WindowsDisplayConfigurationSnapshot windowsSnapshot)
        {
            throw new ArgumentException("The snapshot was not created by WindowsDisplayService.", nameof(snapshot));
        }

        var validationResult = DisplayConfigNative.SetDisplayConfig(
            (uint)windowsSnapshot.Paths.Length,
            windowsSnapshot.Paths,
            (uint)windowsSnapshot.Modes.Length,
            windowsSnapshot.Modes,
            SetValidate | SetUseSuppliedDisplayConfig);
        ThrowForDisplayConfigError(validationResult, "validate the previous display configuration");

        var applyResult = DisplayConfigNative.SetDisplayConfig(
            (uint)windowsSnapshot.Paths.Length,
            windowsSnapshot.Paths,
            (uint)windowsSnapshot.Modes.Length,
            windowsSnapshot.Modes,
            SetApply | SetUseSuppliedDisplayConfig | SetSaveToDatabase);
        ThrowForDisplayConfigError(applyResult, "restore the previous display configuration");
        return Task.CompletedTask;
    }

    private static IReadOnlyList<DisplayDescriptor> EnumerateDisplays()
    {
        var all = QueryDisplayConfiguration(DisplayConfigNative.QueryAllPaths);
        var active = QueryDisplayConfiguration(DisplayConfigNative.QueryOnlyActivePaths);
        var activeTargets = active.Paths
            .Select(path => TargetKey.From(path.TargetInfo.AdapterId, path.TargetInfo.Id))
            .ToHashSet();
        var descriptors = new List<DisplayDescriptor>();

        foreach (var targetGroup in all.Paths.GroupBy(path => TargetKey.From(path.TargetInfo.AdapterId, path.TargetInfo.Id)))
        {
            var path = targetGroup.First();
            if (!TryGetTargetName(path.TargetInfo, out var targetName) || string.IsNullOrWhiteSpace(targetName.MonitorDevicePath))
            {
                continue;
            }

            var displayModes = new HashSet<DisplayMode>();
            DisplayMode? currentMode = null;
            var isPrimary = false;
            string? gdiDeviceName = null;

            if (activeTargets.Contains(targetGroup.Key))
            {
                var currentPath = active.Paths.First(activePath => TargetKey.From(activePath.TargetInfo.AdapterId, activePath.TargetInfo.Id) == targetGroup.Key);
                currentMode = GetCurrentMode(currentPath, active.Modes);
                isPrimary = currentMode is not null && IsPrimarySource(currentPath, active.Modes);
                if (TryGetSourceName(currentPath.SourceInfo, out gdiDeviceName))
                {
                    foreach (var mode in EnumerateSourceModes(gdiDeviceName, currentPath.TargetInfo.Rotation))
                    {
                        displayModes.Add(mode);
                    }
                }
            }

            if (currentMode is not null)
            {
                displayModes.Add(currentMode);
            }

            if (!activeTargets.Contains(targetGroup.Key) && TryGetPreferredMode(path.TargetInfo, out var preferredMode))
            {
                displayModes.Add(preferredMode);
            }

            var friendlyName = string.IsNullOrWhiteSpace(targetName.FriendlyName)
                ? $"Display ({targetName.EdidManufactureId:X4}:{targetName.EdidProductCodeId:X4})"
                : targetName.FriendlyName;

            descriptors.Add(new DisplayDescriptor(
                targetName.MonitorDevicePath,
                friendlyName,
                IsConnected: path.TargetInfo.TargetAvailable || !string.IsNullOrWhiteSpace(targetName.FriendlyName),
                IsActive: activeTargets.Contains(targetGroup.Key),
                IsPrimary: isPrimary,
                currentMode,
                displayModes.OrderBy(mode => mode.Width).ThenBy(mode => mode.Height).ThenBy(mode => mode.RefreshRate).ToArray(),
                SupportedModesAreComplete: false,
                gdiDeviceName));
        }

        return descriptors
            .DistinctBy(display => display.DisplayId, StringComparer.OrdinalIgnoreCase)
            .OrderBy(display => display.FriendlyName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<DisplayMode> EnumerateSourceModes(string deviceName, int currentRotation)
    {
        var modes = new HashSet<DisplayMode>();
        for (var index = 0; index < 512; index++)
        {
            var mode = CreateInitializedDevMode();
            if (!DisplayConfigNative.EnumDisplaySettingsEx(deviceName, index, ref mode, 0))
            {
                break;
            }

            var width = checked((int)mode.PelsWidth);
            var height = checked((int)mode.PelsHeight);
            var refreshRate = checked((int)mode.DisplayFrequency);
            if (width <= 0 || height <= 0 || refreshRate <= 0)
            {
                continue;
            }

            var orientation = ToOrientationFromDevMode(mode.Union.Position.Orientation, currentRotation);
            modes.Add(new DisplayMode(width, height, refreshRate, orientation));
        }

        return modes.ToArray();
    }

    private static DisplayMode? GetCurrentMode(DisplayConfigPathInfo path, IReadOnlyList<DisplayConfigModeInfo> modes)
    {
        var sourceModeIndex = path.SourceInfo.ModeInfoIndex;
        if (sourceModeIndex == DisplayConfigNative.ModeIndexInvalid || sourceModeIndex >= modes.Count)
        {
            return null;
        }

        var sourceMode = modes[(int)sourceModeIndex];
        if (sourceMode.InfoType != DisplayConfigNative.ModeInfoTypeSource)
        {
            return null;
        }

        var width = checked((int)sourceMode.Mode.SourceMode.Width);
        var height = checked((int)sourceMode.Mode.SourceMode.Height);
        var refreshRate = path.TargetInfo.RefreshRate.Denominator == 0
            ? 0
            : DisplayModeConverter.ToRoundedRefreshRate(path.TargetInfo.RefreshRate.Numerator, path.TargetInfo.RefreshRate.Denominator);
        if (width <= 0 || height <= 0 || refreshRate <= 0)
        {
            return null;
        }

        return new DisplayMode(width, height, refreshRate, DisplayModeConverter.ToOrientation(path.TargetInfo.Rotation));
    }

    private static bool IsPrimarySource(DisplayConfigPathInfo path, IReadOnlyList<DisplayConfigModeInfo> modes)
    {
        var index = path.SourceInfo.ModeInfoIndex;
        if (index == DisplayConfigNative.ModeIndexInvalid || index >= modes.Count)
        {
            return false;
        }

        var sourceMode = modes[(int)index];
        return sourceMode.InfoType == DisplayConfigNative.ModeInfoTypeSource &&
               sourceMode.Mode.SourceMode.PositionX == 0 &&
               sourceMode.Mode.SourceMode.PositionY == 0;
    }

    private static Dictionary<TargetKey, DisplayPosition> GetCurrentPositions(QueryResult configuration)
    {
        var positions = new Dictionary<TargetKey, DisplayPosition>();
        foreach (var path in configuration.Paths)
        {
            var modeIndex = path.SourceInfo.ModeInfoIndex;
            if (modeIndex == DisplayConfigNative.ModeIndexInvalid || modeIndex >= configuration.Modes.Length)
            {
                continue;
            }

            var mode = configuration.Modes[(int)modeIndex];
            if (mode.InfoType == DisplayConfigNative.ModeInfoTypeSource)
            {
                positions[TargetKey.From(path.TargetInfo.AdapterId, path.TargetInfo.Id)] =
                    new DisplayPosition(mode.Mode.SourceMode.PositionX, mode.Mode.SourceMode.PositionY);
            }
        }

        return positions;
    }

    private static bool TryGetTargetName(DisplayConfigPathTargetInfo target, out DisplayConfigTargetDeviceName targetName)
    {
        targetName = new DisplayConfigTargetDeviceName
        {
            Header = new DisplayConfigDeviceInfoHeader
            {
                Type = DisplayConfigNative.DeviceInfoGetTargetName,
                Size = (uint)Marshal.SizeOf<DisplayConfigTargetDeviceName>(),
                AdapterId = target.AdapterId,
                Id = target.Id
            },
            FriendlyName = string.Empty,
            MonitorDevicePath = string.Empty
        };

        return DisplayConfigNative.DisplayConfigGetDeviceInfo(ref targetName) == DisplayConfigNative.ErrorSuccess;
    }

    private static bool TryGetSourceName(DisplayConfigPathSourceInfo source, out string deviceName)
    {
        var sourceName = new DisplayConfigSourceDeviceName
        {
            Header = new DisplayConfigDeviceInfoHeader
            {
                Type = DisplayConfigNative.DeviceInfoGetSourceName,
                Size = (uint)Marshal.SizeOf<DisplayConfigSourceDeviceName>(),
                AdapterId = source.AdapterId,
                Id = source.Id
            },
            ViewGdiDeviceName = string.Empty
        };

        if (DisplayConfigNative.DisplayConfigGetDeviceInfo(ref sourceName) != DisplayConfigNative.ErrorSuccess ||
            string.IsNullOrWhiteSpace(sourceName.ViewGdiDeviceName))
        {
            deviceName = string.Empty;
            return false;
        }

        deviceName = sourceName.ViewGdiDeviceName;
        return true;
    }

    private static bool TryGetPreferredMode(DisplayConfigPathTargetInfo target, out DisplayMode mode)
    {
        var request = new DisplayConfigTargetPreferredMode
        {
            Header = new DisplayConfigDeviceInfoHeader
            {
                Type = DisplayConfigNative.DeviceInfoGetTargetPreferredMode,
                Size = (uint)Marshal.SizeOf<DisplayConfigTargetPreferredMode>(),
                AdapterId = target.AdapterId,
                Id = target.Id
            }
        };

        if (DisplayConfigNative.DisplayConfigGetDeviceInfo(ref request) == DisplayConfigNative.ErrorSuccess)
        {
            var signal = request.PreferredMode.TargetVideoSignalInfo;
            var width = checked((int)signal.ActiveSize.Width);
            var height = checked((int)signal.ActiveSize.Height);
            var refreshRate = DisplayModeConverter.ToRoundedRefreshRate(
                signal.VerticalSyncRate.Numerator,
                signal.VerticalSyncRate.Denominator);
            if (width > 0 && height > 0 && refreshRate > 0)
            {
                mode = new DisplayMode(width, height, refreshRate, DisplayModeConverter.ToOrientation(target.Rotation));
                return true;
            }
        }

        mode = default!;
        return false;
    }

    private static DisplayOrientation ToOrientationFromDevMode(uint orientation, int currentRotation) => orientation switch
    {
        0 => DisplayOrientation.Landscape,
        1 => DisplayOrientation.Portrait,
        2 => DisplayOrientation.LandscapeFlipped,
        3 => DisplayOrientation.PortraitFlipped,
        _ => DisplayModeConverter.ToOrientation(currentRotation)
    };

    private static DevMode CreateInitializedDevMode() => new()
    {
        DeviceName = string.Empty,
        FormName = string.Empty,
        Size = (ushort)Marshal.SizeOf<DevMode>()
    };

    private static QueryResult QueryDisplayConfiguration(uint flags)
    {
        for (var attempt = 0; attempt < MaximumQueryAttempts; attempt++)
        {
            var sizeResult = DisplayConfigNative.GetDisplayConfigBufferSizes(flags, out var pathCount, out var modeCount);
            ThrowForDisplayConfigError(sizeResult, "get display configuration buffer sizes");

            var paths = new DisplayConfigPathInfo[pathCount];
            var modes = new DisplayConfigModeInfo[modeCount];
            var queryResult = DisplayConfigNative.QueryDisplayConfig(flags, ref pathCount, paths, ref modeCount, modes, IntPtr.Zero);
            if (queryResult == DisplayConfigNative.ErrorInsufficientBuffer)
            {
                continue;
            }

            ThrowForDisplayConfigError(queryResult, "query display configuration");
            Array.Resize(ref paths, checked((int)pathCount));
            Array.Resize(ref modes, checked((int)modeCount));
            return new QueryResult(paths, modes);
        }

        throw new InvalidOperationException("Display topology kept changing while it was being enumerated. Try again.");
    }

    private static void ThrowForDisplayConfigError(int errorCode, string operation)
    {
        if (errorCode != DisplayConfigNative.ErrorSuccess)
        {
            throw new Win32Exception(errorCode, $"Windows could not {operation}.");
        }
    }

    private sealed record QueryResult(DisplayConfigPathInfo[] Paths, DisplayConfigModeInfo[] Modes);

    private readonly record struct SourceKey(uint AdapterLow, int AdapterHigh, uint SourceId)
    {
        public static SourceKey From(Luid adapterId, uint sourceId) => new(adapterId.LowPart, adapterId.HighPart, sourceId);
    }

    private readonly record struct DisplayPosition(int X, int Y);

    private readonly record struct TargetKey(uint AdapterLow, int AdapterHigh, uint TargetId)
    {
        public static TargetKey From(Luid adapterId, uint targetId) => new(adapterId.LowPart, adapterId.HighPart, targetId);
    }

    private sealed record WindowsDisplayConfigurationSnapshot(
        DisplayConfigPathInfo[] Paths,
        DisplayConfigModeInfo[] Modes) : IDisplayConfigurationSnapshot;
}
