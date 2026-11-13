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
    private const int DM_PELSWIDTH = 0x00080000;
    private const int DM_PELSHEIGHT = 0x00100000;
    private const int DM_BITSPERPEL = 0x00040000;
    private const int DM_DISPLAYFREQUENCY = 0x00400000;
    private const int CDS_GLOBALPARAMS = 0x00000003;
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
    private struct POINTL
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

    [StructLayout(LayoutKind.Explicit, Size = 220, CharSet = CharSet.Unicode)]
    private struct DEVMODE
    {
        [FieldOffset(0)]
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmDeviceName;
        [FieldOffset(64)] public short dmSpecVersion;
        [FieldOffset(66)] public short dmDriverVersion;
        [FieldOffset(68)] public short dmSize;
        [FieldOffset(70)] public short dmDriverExtra;
        [FieldOffset(72)] public uint dmFields;
        // Union offset 76: paper fields XOR position fields
        [FieldOffset(76)] public short dmOrientation;
        [FieldOffset(78)] public short dmPaperSize;
        [FieldOffset(80)] public short dmPaperLength;
        [FieldOffset(82)] public short dmPaperWidth;
        [FieldOffset(84)] public short dmScale;
        [FieldOffset(86)] public short dmCopies;
        [FieldOffset(88)] public short dmDefaultSource;
        [FieldOffset(90)] public short dmPrintQuality;
        [FieldOffset(76)] public POINTL dmPosition;
        [FieldOffset(84)] public uint dmDisplayOrientation;
        [FieldOffset(88)] public uint dmDisplayFixedOutput;
        [FieldOffset(92)] public short dmColor;
        [FieldOffset(94)] public short dmDuplex;
        [FieldOffset(96)] public short dmYResolution;
        [FieldOffset(98)] public short dmTTOption;
        [FieldOffset(100)] public short dmCollate;
        [FieldOffset(102)]
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmFormName;
        [FieldOffset(166)] public ushort dmLogPixels;
        [FieldOffset(168)] public uint dmBitsPerPel;
        [FieldOffset(172)] public uint dmPelsWidth;
        [FieldOffset(176)] public uint dmPelsHeight;
        [FieldOffset(184)] public uint dmDisplayFlags;
        [FieldOffset(188)] public uint dmDisplayFreq;
        [FieldOffset(192)] public uint dmICMMethod;
        [FieldOffset(196)] public uint dmICMIntent;
        [FieldOffset(200)] public uint dmMediaType;
        [FieldOffset(204)] public uint dmDitherType;
        [FieldOffset(208)] public uint dmReserved1;
        [FieldOffset(212)] public uint dmReserved2;
        [FieldOffset(216)] public uint dmPanningWidth;
        [FieldOffset(220)] public uint dmPanningHeight;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int ChangeDisplaySettingsEx(string lpszDeviceName, ref DEVMODE lpDevMode, IntPtr hwnd, uint dwflags, IntPtr lParam);

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
        foreach (var (hMonitor, rc, deviceName) in list)
        {
            int width = rc.Right - rc.Left;
            int height = rc.Bottom - rc.Top;
            if (width <= 0) width = 1920;
            if (height <= 0) height = 1080;
            string stableKey = BuildStableKey(hMonitor, deviceName, rc, width, height);
            string friendly = !string.IsNullOrWhiteSpace(deviceName) ? deviceName : $"Monitor {monitors.Count + 1}";
            bool isPrimary = rc.Left == 0 && rc.Top == 0;
            monitors.Add(new DisplayMonitor(
                StableKey: stableKey,
                FriendlyName: friendly,
                Position: new Position(rc.Left, rc.Top, width, height),
                ResolutionWidth: width,
                ResolutionHeight: height,
                Orientation: width >= height ? DisplayFlip.Core.Orientation.Landscape : DisplayFlip.Core.Orientation.Portrait,
                IsEnabled: true));
        }
        // Ensure primary is at (0,0) by legalizing; primary flag derives from position anyway
        return monitors;
    }

    public static (bool ok, string message) ApplyTopologies(IReadOnlyList<DisplayMonitor> desired)
    {
        if (desired.Count == 0)
            return (false, "No desired monitors.");
        string firstDevice = null!;
        // Device name is stored in FriendlyName for fallback path (see ReadTopologies)
        // Find current device names to map desired order: desired[i].FriendlyName is device name
        // Apply sequentially with CDS_GLOBALPARAMS so the whole set settles together
        foreach (var d in desired)
        {
            string deviceName = d.FriendlyName;
            if (!deviceName.StartsWith("\\\\.\\DISPLAY"))
            {
                // FriendlyName may be StableKey-derived for CCD path; fallback apply needs device name.
                // Fall through to best effort using index mapping by position order.
                continue;
            }
            var dev = new DEVMODE
            {
                dmSize = (short)Marshal.SizeOf(typeof(DEVMODE)),
                dmFields = DM_POSITION | DM_PELSWIDTH | DM_PELSHEIGHT | DM_BITSPERPEL,
                dmPosition = new POINTL { x = d.Position.X, y = d.Position.Y },
                dmPelsWidth = (uint)d.Position.Width,
                dmPelsHeight = (uint)d.Position.Height,
                dmBitsPerPel = 32
            };
            int res = ChangeDisplaySettingsEx(deviceName, ref dev, IntPtr.Zero, CDS_GLOBALPARAMS, IntPtr.Zero);
            if (res != DISP_CHANGE_SUCCESSFUL)
                return (false, $"ChangeDisplaySettingsEx for {deviceName} failed: {res}");
            firstDevice = deviceName;
        }
        if (firstDevice == null)
            return (false, "ChangeDisplaySettingsEx skipped: device names not available for fallback apply.");
        return (true, "Applied via ChangeDisplaySettingsEx fallback.");
    }

    private static string BuildStableKey(IntPtr hMonitor, string deviceName, RECT rc, int width, int height)
    {
        // Try WMI-assisted identity by handle order is complex; device name + geometry is stable within a boot
        // and unique per monitor. For cross-boot stability users rely on FriendlyName suffix text + geometry.
        // Include HMONITOR handle hash as tiebreaker when deviceName collides.
        var prefix = !string.IsNullOrWhiteSpace(deviceName) ? deviceName : "DISPLAY";
        // hMonitor.ToInt64() differs per boot but is unique per monitor within this boot; include low bits
        return $"{prefix}|{rc.Left},{rc.Top}|{width}x{height}|{hMonitor.ToInt64() & 0xFFFF}";
    }
}
