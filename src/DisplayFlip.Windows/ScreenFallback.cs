using DisplayFlip.Core;
using System.Runtime.InteropServices;

namespace DisplayFlip.Windows;

/// <summary>
/// Fallback topology reader/writer that works when CCD (QueryDisplayConfig/SetDisplayConfig)
/// returns ERROR_INVALID_PARAMETER (87) on this driver. Uses EnumDisplayMonitors for reads
/// and ChangeDisplaySettingsEx for writes. Proven by diagnostics: Screen/EnumDisplayMonitors
/// sees 2 monitors where CCD sees 0.
/// </summary>
internal static class ScreenFallback
{
    private const int DM_POSITION = 0x00000020;
    private const int DM_DISPLAYORIENTATION = 0x00000080;
    private const int DM_PELSWIDTH = 0x00080000;
    private const int DM_PELSHEIGHT = 0x00100000;
    private const int DM_BITSPERPEL = 0x00040000;
    private const uint CDS_UPDATEREGISTRY = 0x00000001;
    private const uint CDS_SET_PRIMARY = 0x00000010;
    private const uint CDS_NORESET = 0x10000000;
    private const int ENUM_CURRENT_SETTINGS = -1;
    private const int DISP_CHANGE_SUCCESSFUL = 0;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct POINTL
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public uint cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MONITORINFOEX
    {
        public MONITORINFO mi;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string szDevice;
    }

    private delegate bool EnumMonitorsProc(IntPtr hMonitor, IntPtr hdc, ref RECT rcMonitor, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, EnumMonitorsProc lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct DEVMODE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public uint dmFields;
        public POINTL dmPosition;
        public uint dmDisplayOrientation;
        public uint dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmFormName;
        public ushort dmLogPixels;
        public uint dmBitsPerPel;
        public uint dmPelsWidth;
        public uint dmPelsHeight;
        public uint dmDisplayFlags;
        public uint dmDisplayFreq;
        public uint dmICMMethod;
        public uint dmICMIntent;
        public uint dmMediaType;
        public uint dmDitherType;
        public uint dmReserved1;
        public uint dmReserved2;
        public uint dmPanningWidth;
        public uint dmPanningHeight;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DISPLAY_DEVICE
    {
        public uint cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceString;
        public uint StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceKey;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool EnumDisplayDevices(string? lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool EnumDisplaySettingsEx(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int ChangeDisplaySettingsEx(string lpszDeviceName, ref DEVMODE lpDevMode, IntPtr hwnd, uint dwflags, IntPtr lParam);

    [DllImport("user32.dll", EntryPoint = "ChangeDisplaySettingsExW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int CommitDisplaySettings(string? lpszDeviceName, IntPtr lpDevMode, IntPtr hwnd, uint dwflags, IntPtr lParam);

    public static List<DisplayMonitor> ReadTopologies()
    {
        var list = new List<(IntPtr hMonitor, RECT rcMonitor, string deviceName)>();
        EnumMonitorsProc callback = (IntPtr hMonitor, IntPtr hdc, ref RECT rcMonitor, IntPtr lParam) =>
        {
            var info = new MONITORINFOEX
            {
                mi = new MONITORINFO { cbSize = (uint)Marshal.SizeOf(typeof(MONITORINFOEX)) }
            };
            string name = string.Empty;
            if (GetMonitorInfo(hMonitor, ref info))
            {
                name = info.szDevice ?? string.Empty;
                rcMonitor = info.mi.rcMonitor;
            }
            list.Add((hMonitor, rcMonitor, name));
            return true;
        };
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);

        var monitors = new List<DisplayMonitor>();
        foreach (var (_, rc, deviceName) in list)
        {
            int width = rc.Right - rc.Left;
            int height = rc.Bottom - rc.Top;
            if (width <= 0) width = 1920;
            if (height <= 0) height = 1080;
            string stableKey = BuildStableKey(deviceName);
            string friendly = !string.IsNullOrWhiteSpace(deviceName) ? deviceName : $"Monitor {monitors.Count + 1}";
            var orient = width >= height ? DisplayFlip.Core.Orientation.Landscape : DisplayFlip.Core.Orientation.Portrait;
            if (!string.IsNullOrWhiteSpace(deviceName))
            {
                var dev = new DEVMODE { dmSize = (short)Marshal.SizeOf<DEVMODE>() };
                if (EnumDisplaySettingsEx(deviceName, ENUM_CURRENT_SETTINGS, ref dev, 0))
                {
                    orient = dev.dmDisplayOrientation switch
                    {
                        1 => DisplayFlip.Core.Orientation.Portrait,
                        2 => DisplayFlip.Core.Orientation.Rotated,
                        3 => DisplayFlip.Core.Orientation.Unhandled,
                        _ => width >= height ? DisplayFlip.Core.Orientation.Landscape : DisplayFlip.Core.Orientation.Portrait
                    };
                }
            }
            monitors.Add(new DisplayMonitor(
                StableKey: stableKey,
                FriendlyName: friendly,
                Position: new Position(rc.Left, rc.Top, width, height),
                ResolutionWidth: width,
                ResolutionHeight: height,
                Orientation: orient,
                IsEnabled: true));
        }
        return monitors;
    }

    public static List<(int Width, int Height)> GetSupportedResolutions(string deviceKeyOrName, int currentWidth = 1920, int currentHeight = 1080)
    {
        string deviceName = (deviceKeyOrName ?? string.Empty).Split('|')[0].Trim();
        var set = new HashSet<(int Width, int Height)>();
        if (currentWidth > 0 && currentHeight > 0)
            set.Add((currentWidth, currentHeight));

        if (deviceName.StartsWith("\\\\.\\DISPLAY", StringComparison.OrdinalIgnoreCase))
        {
            for (int i = 0; i < 512; i++)
            {
                var dev = new DEVMODE { dmSize = (short)Marshal.SizeOf<DEVMODE>() };
                if (!EnumDisplaySettingsEx(deviceName, i, ref dev, 0))
                    break;
                if (dev.dmPelsWidth >= 800 && dev.dmPelsHeight >= 600)
                    set.Add(((int)dev.dmPelsWidth, (int)dev.dmPelsHeight));
            }
        }

        if (set.Count <= 1)
        {
            set.Add((1920, 1080));
            set.Add((1680, 1050));
            set.Add((1600, 900));
            set.Add((1366, 768));
            set.Add((1280, 720));
        }

        return set
            .OrderByDescending(r => r.Width * r.Height)
            .ThenByDescending(r => r.Width)
            .ToList();
    }

    public static (bool ok, string message) ApplyTopologies(IReadOnlyList<DisplayMonitor> desired)
    {
        if (desired.Count == 0)
            return (false, "No desired monitors.");

        int stagedCount = 0;
        // Stage the primary monitor (0,0) FIRST with CDS_SET_PRIMARY before moving any
        // secondary monitors so Win32 drivers never see a transient state with no primary at (0,0).
        var ordered = desired
            .OrderByDescending(d => d.Position.X == 0 && d.Position.Y == 0)
            .ToList();
        foreach (var d in ordered)
        {
            string deviceName = d.StableKey.Split('|')[0];
            if (!deviceName.StartsWith("\\\\.\\DISPLAY", StringComparison.OrdinalIgnoreCase))
            {
                if (d.FriendlyName.StartsWith("\\\\.\\DISPLAY", StringComparison.OrdinalIgnoreCase))
                    deviceName = d.FriendlyName;
                else
                    continue;
            }

            var dev = new DEVMODE { dmSize = (short)Marshal.SizeOf<DEVMODE>() };
            if (!EnumDisplaySettingsEx(deviceName, ENUM_CURRENT_SETTINGS, ref dev, 0))
            {
                dev = new DEVMODE
                {
                    dmSize = (short)Marshal.SizeOf<DEVMODE>(),
                    dmBitsPerPel = 32
                };
                dev.dmFields = DM_POSITION | DM_PELSWIDTH | DM_PELSHEIGHT | DM_BITSPERPEL;
            }
            else
            {
                dev.dmFields = DM_POSITION | DM_PELSWIDTH | DM_PELSHEIGHT;
            }

            uint desiredOrient = d.Orientation switch
            {
                DisplayFlip.Core.Orientation.Portrait => 1u,
                DisplayFlip.Core.Orientation.Rotated => 2u,
                DisplayFlip.Core.Orientation.Unhandled => 3u,
                _ => 0u
            };
            if (dev.dmDisplayOrientation != desiredOrient)
            {
                dev.dmDisplayOrientation = desiredOrient;
                dev.dmFields |= DM_DISPLAYORIENTATION;
            }

            dev.dmPosition = new POINTL { x = d.Position.X, y = d.Position.Y };
            dev.dmPelsWidth = (uint)d.Position.Width;
            dev.dmPelsHeight = (uint)d.Position.Height;

            bool isPrimary = d.Position.X == 0 && d.Position.Y == 0;
            uint flags = CDS_UPDATEREGISTRY | CDS_NORESET | (isPrimary ? CDS_SET_PRIMARY : 0u);
            int res = ChangeDisplaySettingsEx(deviceName, ref dev, IntPtr.Zero, flags, IntPtr.Zero);
            if (res != DISP_CHANGE_SUCCESSFUL)
                return (false, $"ChangeDisplaySettingsEx stage for {deviceName} failed: {res}");
            stagedCount++;
        }

        if (stagedCount == 0)
            return (false, "ChangeDisplaySettingsEx skipped: device names not available for fallback apply.");

        int commitRes = CommitDisplaySettings(null, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
        if (commitRes != DISP_CHANGE_SUCCESSFUL)
            return (false, $"ChangeDisplaySettingsEx commit failed: {commitRes}");

        return (true, "Applied via ChangeDisplaySettingsEx fallback.");
    }

    private static string BuildStableKey(string deviceName)
    {
        var prefix = !string.IsNullOrWhiteSpace(deviceName) ? deviceName.Trim() : "DISPLAY";
        try
        {
            var mon = new DISPLAY_DEVICE { cb = (uint)Marshal.SizeOf<DISPLAY_DEVICE>() };
            if (EnumDisplayDevices(prefix, 0, ref mon, 0) && !string.IsNullOrWhiteSpace(mon.DeviceID))
                return $"{prefix}|{mon.DeviceID.Trim()}";
        }
        catch
        {
            // Fall through to device name only.
        }
        return prefix;
    }
}
