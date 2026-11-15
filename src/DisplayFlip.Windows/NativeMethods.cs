using System.Runtime.InteropServices;

namespace DisplayFlip.Windows;

public static class NativeMethods
{
    public const uint QDC_ONLY_ACTIVE_PATHS = 0x00000002;
    public const uint QDC_ALL_PATHS = 0x00000001;

    public const uint SDC_USE_SUPPLIED_DISPLAY_CONFIG = 0x00000020;
    public const uint SDC_VALIDATE = 0x00000040;
    public const uint SDC_APPLY = 0x00000080;
    public const uint SDC_NO_OPTIMIZATION = 0x00000100;
    public const uint SDC_SAVE_TO_DATABASE = 0x00000200;
    public const uint SDC_ALLOW_CHANGES = 0x00000400;

    public const uint SDC_TOPOLOGY_INTERNAL = 0x00000001;
    public const uint SDC_TOPOLOGY_CLONE = 0x00000002;
    public const uint SDC_TOPOLOGY_EXTEND = 0x00000004;
    public const uint SDC_TOPOLOGY_EXTERNAL = 0x00000008;

    public const int ERROR_SUCCESS = 0;
    public const int ERROR_INVALID_PARAMETER = 87;
    public const int ERROR_BAD_CONFIGURATION = 1673;

    [DllImport("user32.dll")]
    public static extern int QueryDisplayConfig(
        uint flags,
        out uint pathCount,
        out uint modeCount,
        IntPtr currentTopologyId,
        IntPtr pathArray,
        IntPtr modeArray);

    [DllImport("user32.dll")]
    public static extern int QueryDisplayConfig(
        uint flags,
        out uint pathCount,
        out uint modeCount,
        IntPtr currentTopologyId,
        [Out] DISPLAYCONFIG_PATH_INFO[] pathArray,
        [Out] DISPLAYCONFIG_MODE_INFO[] modeArray);

    [DllImport("user32.dll")]
    public static extern int SetDisplayConfig(
        uint pathCount,
        PATH_INFO_N[] pathArray,
        uint modeCount,
        MODE_INFO_N[] modeArray,
        uint flags);

    [DllImport("user32.dll", EntryPoint = "SetDisplayConfig")]
    public static extern int SetDisplayConfig(
        uint pathCount,
        IntPtr pathArray,
        uint modeCount,
        IntPtr modeArray,
        uint flags);

    [DllImport("user32.dll", EntryPoint = "SetDisplayConfig", CharSet = CharSet.Unicode)]
    public static extern int SetDisplayConfigNative(
        uint pathCount,
        DISPLAYCONFIG_PATH_INFO[] pathArray,
        uint modeCount,
        DISPLAYCONFIG_MODE_INFO[] modeArray,
        uint flags);

    [DllImport("user32.dll", EntryPoint = "DisplayConfigGetDeviceInfo", CharSet = CharSet.Unicode)]
    public static extern int DisplayConfigGetTargetName(ref DISPLAYCONFIG_TARGET_DEVICE_NAME header);

    [DllImport("user32.dll")]
    public static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_DEVICE_INFO header);

    // Reused structs -- names without N for compat with existing code
    public struct PATH_INFO_N { }
    public struct MODE_INFO_N { }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DISPLAYCONFIG_PATH_INFO
    {
        public DISPLAYCONFIG_PATH_TARGET_INFO targetInfo;
        public DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo;
        public uint flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_PATH_TARGET_INFO
    {
        public uint adapterId;
        public uint id;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_PATH_SOURCE_INFO
    {
        public uint adapterId;
        public uint id;
        public uint modeInfoIdx;
        public uint statusFlags;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DISPLAYCONFIG_MODE_INFO
    {
        public uint infoType;
        public uint id;
        public ulong adapterId;
        public DISPLAYCONFIG_SOURCE_MODE sourceMode;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_SOURCE_MODE
    {
        public uint width;
        public uint height;
        public uint pixelFormat;
        public POINTL position;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINTL
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DISPLAYCONFIG_DEVICE_INFO
    {
        public uint size;
        public uint type;
        public uint adapterId;
        public uint id;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string deviceName;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DISPLAYCONFIG_TARGET_DEVICE_NAME
    {
        public uint size;
        public uint type;
        public uint adapterId;
        public uint id;
        public uint flags;
        public uint outputTechnology;
        public uint edidManufactureId;
        public uint edidProductCodeId;
        public uint connectorInstance;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string monitorFriendlyDeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string monitorDevicePath;
    }

    public const uint DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME = 4;
}
