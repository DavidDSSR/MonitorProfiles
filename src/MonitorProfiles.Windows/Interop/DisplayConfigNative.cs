using System.Runtime.InteropServices;

namespace MonitorProfiles.Windows.Interop;

internal static class DisplayConfigNative
{
    internal const uint QueryAllPaths = 0x00000001;
    internal const uint QueryOnlyActivePaths = 0x00000002;
    internal const uint QueryVirtualRefreshRateAware = 0x00000040;
    internal const uint PathActive = 0x00000001;
    internal const uint PathSupportVirtualMode = 0x00000008;
    internal const uint ModeIndexInvalid = 0xFFFFFFFF;
    internal const uint DeviceInfoGetSourceName = 1;
    internal const uint DeviceInfoGetTargetName = 2;
    internal const uint DeviceInfoGetTargetPreferredMode = 3;
    internal const int ModeInfoTypeSource = 1;
    internal const int ModeInfoTypeTarget = 2;
    internal const int ErrorSuccess = 0;
    internal const int ErrorInsufficientBuffer = 122;

    [DllImport("user32.dll", ExactSpelling = true)]
    internal static extern int GetDisplayConfigBufferSizes(uint flags, out uint pathCount, out uint modeCount);

    [DllImport("user32.dll", ExactSpelling = true)]
    internal static extern int QueryDisplayConfig(
        uint flags,
        ref uint pathCount,
        [Out] DisplayConfigPathInfo[] paths,
        ref uint modeCount,
        [Out] DisplayConfigModeInfo[] modes,
        IntPtr currentTopologyId);

    [DllImport("user32.dll", ExactSpelling = true)]
    internal static extern int SetDisplayConfig(
        uint pathCount,
        [In] DisplayConfigPathInfo[] paths,
        uint modeCount,
        [In] DisplayConfigModeInfo[] modes,
        uint flags);

    [DllImport("user32.dll", ExactSpelling = true)]
    internal static extern int DisplayConfigGetDeviceInfo(ref DisplayConfigTargetDeviceName requestPacket);

    [DllImport("user32.dll", ExactSpelling = true)]
    internal static extern int DisplayConfigGetDeviceInfo(ref DisplayConfigSourceDeviceName requestPacket);

    [DllImport("user32.dll", ExactSpelling = true)]
    internal static extern int DisplayConfigGetDeviceInfo(ref DisplayConfigTargetPreferredMode requestPacket);

    [DllImport("user32.dll", EntryPoint = "EnumDisplaySettingsExW", CharSet = CharSet.Unicode, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool EnumDisplaySettingsEx(
        string deviceName,
        int modeNumber,
        ref DevMode displayMode,
        uint flags);
}

[StructLayout(LayoutKind.Sequential)]
public struct Luid
{
    public uint LowPart;
    public int HighPart;
}

[StructLayout(LayoutKind.Sequential)]
public struct DisplayConfigPathSourceInfo
{
    public Luid AdapterId;
    public uint Id;
    public uint ModeInfoIndex;
    public uint StatusFlags;
}

[StructLayout(LayoutKind.Sequential)]
public struct DisplayConfigRational
{
    public uint Numerator;
    public uint Denominator;
}

[StructLayout(LayoutKind.Sequential)]
public struct DisplayConfigPathTargetInfo
{
    public Luid AdapterId;
    public uint Id;
    public uint ModeInfoIndex;
    public int OutputTechnology;
    public int Rotation;
    public int Scaling;
    public DisplayConfigRational RefreshRate;
    public int ScanLineOrdering;
    [MarshalAs(UnmanagedType.Bool)]
    public bool TargetAvailable;
    public uint StatusFlags;
}

[StructLayout(LayoutKind.Sequential)]
public struct DisplayConfigPathInfo
{
    public DisplayConfigPathSourceInfo SourceInfo;
    public DisplayConfigPathTargetInfo TargetInfo;
    public uint Flags;
}

[StructLayout(LayoutKind.Sequential)]
public struct DisplayConfigDeviceInfoHeader
{
    public uint Type;
    public uint Size;
    public Luid AdapterId;
    public uint Id;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct DisplayConfigTargetDeviceName
{
    public DisplayConfigDeviceInfoHeader Header;
    public uint Flags;
    public int OutputTechnology;
    public ushort EdidManufactureId;
    public ushort EdidProductCodeId;
    public uint ConnectorInstance;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
    public string FriendlyName;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string MonitorDevicePath;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct DisplayConfigSourceDeviceName
{
    public DisplayConfigDeviceInfoHeader Header;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string ViewGdiDeviceName;
}

[StructLayout(LayoutKind.Sequential)]
public struct DisplayConfigVideoSignalInfo
{
    public ulong PixelRate;
    public DisplayConfigRational HorizontalSyncRate;
    public DisplayConfigRational VerticalSyncRate;
    public DisplayConfig2DRegion ActiveSize;
    public DisplayConfig2DRegion TotalSize;
    public uint VideoStandard;
    public int ScanLineOrdering;
}

[StructLayout(LayoutKind.Sequential)]
public struct DisplayConfig2DRegion
{
    public uint Width;
    public uint Height;
}

[StructLayout(LayoutKind.Sequential)]
public struct DisplayConfigTargetMode
{
    public DisplayConfigVideoSignalInfo TargetVideoSignalInfo;
}

[StructLayout(LayoutKind.Sequential)]
public struct DisplayConfigSourceMode
{
    public uint Width;
    public uint Height;
    public uint PixelFormat;
    public int PositionX;
    public int PositionY;
}

[StructLayout(LayoutKind.Explicit, Size = 48)]
public struct DisplayConfigModeUnion
{
    [FieldOffset(0)]
    public DisplayConfigTargetMode TargetMode;
    [FieldOffset(0)]
    public DisplayConfigSourceMode SourceMode;
}

[StructLayout(LayoutKind.Sequential)]
public struct DisplayConfigModeInfo
{
    public int InfoType;
    public uint Id;
    public Luid AdapterId;
    public DisplayConfigModeUnion Mode;
}

[StructLayout(LayoutKind.Sequential)]
public struct DisplayConfigTargetPreferredMode
{
    public DisplayConfigDeviceInfoHeader Header;
    public DisplayConfigTargetMode PreferredMode;
}

[StructLayout(LayoutKind.Sequential)]
public struct DevModePosition
{
    public int X;
    public int Y;
    public uint Orientation;
    public uint FixedOutput;
}

[StructLayout(LayoutKind.Explicit, Size = 16)]
public struct DevModeUnion
{
    [FieldOffset(0)]
    public DevModePosition Position;
    [FieldOffset(0)]
    public short PaperOrientation;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct DevMode
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string DeviceName;
    public ushort SpecVersion;
    public ushort DriverVersion;
    public ushort Size;
    public ushort DriverExtra;
    public uint Fields;
    public DevModeUnion Union;
    public short Color;
    public short Duplex;
    public short YResolution;
    public short TrueTypeOption;
    public short Collate;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string FormName;
    public ushort LogPixels;
    public uint BitsPerPixel;
    public uint PelsWidth;
    public uint PelsHeight;
    public uint DisplayFlags;
    public uint DisplayFrequency;
    public uint IcmMethod;
    public uint IcmIntent;
    public uint MediaType;
    public uint DitherType;
    public uint Reserved1;
    public uint Reserved2;
    public uint PanningWidth;
    public uint PanningHeight;
}
