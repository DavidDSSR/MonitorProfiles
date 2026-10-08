using System.Runtime.InteropServices;
using MonitorProfiles.Windows.Interop;

namespace MonitorProfiles.Windows.Tests;

public sealed class DisplayConfigInteropTests
{
    [Theory]
    [InlineData(typeof(Luid), 8)]
    [InlineData(typeof(DisplayConfigPathSourceInfo), 20)]
    [InlineData(typeof(DisplayConfigPathTargetInfo), 48)]
    [InlineData(typeof(DisplayConfigPathInfo), 72)]
    [InlineData(typeof(DisplayConfigDeviceInfoHeader), 20)]
    [InlineData(typeof(DisplayConfigSourceDeviceName), 84)]
    [InlineData(typeof(DisplayConfigTargetDeviceName), 420)]
    [InlineData(typeof(DisplayConfigModeInfo), 64)]
    [InlineData(typeof(DisplayConfigTargetPreferredMode), 72)]
    [InlineData(typeof(DevMode), 220)]
    public void Native_structures_match_windows_sdk_layout(Type type, int expectedSize)
    {
        Assert.Equal(expectedSize, Marshal.SizeOf(type));
    }
}
