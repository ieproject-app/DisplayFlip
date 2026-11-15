using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using DisplayFlip.Core;

namespace DisplayFlip.UI.Helpers;

/// <summary>
/// Display numbering, friendly label, refresh rate, and overlay geometry helper.
/// </summary>
public static class DisplayHelper
{
    private static readonly Regex DisplayPattern = new(
        @"(?:DISPLAY|Layar|Monitor|Screen)\s*(\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex StandaloneNumberPattern = new(
        @"\b([1-9]|[12]\d|3[0-2])\b",
        RegexOptions.Compiled);

    private const int ENUM_CURRENT_SETTINGS = -1;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINTL
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DEVMODEW
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmDeviceName;
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
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmFormName;
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

    [DllImport("user32.dll", EntryPoint = "EnumDisplaySettingsExW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool EnumDisplaySettingsEx(string? lpszDeviceName, int iModeNum, ref DEVMODEW lpDevMode, uint dwFlags);

    public static string ExtractDisplayNumber(string? stableKey, string? friendlyName, int fallbackIndex)
    {
        string numFromStable = ExtractFromText(stableKey);
        if (!string.IsNullOrEmpty(numFromStable))
            return numFromStable;

        string numFromFriendly = ExtractFromText(friendlyName);
        if (!string.IsNullOrEmpty(numFromFriendly))
            return numFromFriendly;

        return fallbackIndex.ToString();
    }

    public static string ExtractDisplayNumber(string? friendlyName, int fallbackIndex)
    {
        return ExtractDisplayNumber(null, friendlyName, fallbackIndex);
    }

    private static string ExtractFromText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var mDisp = DisplayPattern.Match(text);
        if (mDisp.Success && int.TryParse(mDisp.Groups[1].Value, out int dVal) && dVal > 0 && dVal <= 32)
            return dVal.ToString();

        var mWord = StandaloneNumberPattern.Match(text);
        if (mWord.Success)
            return mWord.Groups[1].Value;

        return string.Empty;
    }

    public static string CleanDeviceName(string? friendlyName)
    {
        if (string.IsNullOrWhiteSpace(friendlyName))
            return "Monitor";

        string name = friendlyName.Trim();
        if (name.StartsWith(@"\\.\", StringComparison.Ordinal))
            name = name[4..];

        return name;
    }

    public static string GetShortDisplayName(string? friendlyName, string? stableKey, int index)
    {
        string num = ExtractDisplayNumber(stableKey, friendlyName, index);
        string clean = CleanDeviceName(friendlyName);
        return $"Layar {num} ({clean})";
    }

    /// <summary>
    /// Formats a concise, human-readable monitor label for the 2D canvas, enriching raw
    /// DISPLAYn identifiers with the hardware model ID from StableKey when available.
    /// </summary>
    public static string GetCanvasMonitorLabel(string? friendlyName, string? stableKey)
    {
        string clean = CleanDeviceName(friendlyName);
        if (!string.IsNullOrWhiteSpace(stableKey)
            && System.Text.RegularExpressions.Regex.IsMatch(clean, @"^DISPLAY\d+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                stableKey,
                @"MONITOR\\([^\\|]+)\\",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
            {
                return $"{clean} ({match.Groups[1].Value.Trim()})";
            }
        }

        return clean;
    }

    /// <summary>
    /// Queries the current refresh rate (in Hz) for the specified display device.
    /// Returns 0 if unable to retrieve.
    /// </summary>
    public static int GetCurrentRefreshRate(string? stableKey, string? friendlyName = null)
    {
        string? deviceName = null;
        if (!string.IsNullOrWhiteSpace(stableKey))
        {
            var parts = stableKey.Split('|');
            if (parts.Length > 0 && parts[0].StartsWith(@"\\.\DISPLAY", StringComparison.OrdinalIgnoreCase))
                deviceName = parts[0].Trim();
        }

        if (string.IsNullOrEmpty(deviceName) && !string.IsNullOrWhiteSpace(friendlyName) &&
            friendlyName.StartsWith(@"\\.\DISPLAY", StringComparison.OrdinalIgnoreCase))
        {
            deviceName = friendlyName.Trim();
        }

        try
        {
            var dev = new DEVMODEW { dmSize = (short)Marshal.SizeOf<DEVMODEW>() };
            if (EnumDisplaySettingsEx(deviceName, ENUM_CURRENT_SETTINGS, ref dev, 0) && dev.dmDisplayFreq > 0)
            {
                return (int)dev.dmDisplayFreq;
            }

            // Fallback: try with null for primary display if deviceName query failed
            if (EnumDisplaySettingsEx(null, ENUM_CURRENT_SETTINGS, ref dev, 0) && dev.dmDisplayFreq > 0)
            {
                return (int)dev.dmDisplayFreq;
            }
        }
        catch
        {
            // Ignore Win32 query errors gracefully
        }

        return 0;
    }

    /// <summary>
    /// Computes physical pixel bounds for placing the identify overlay in the bottom-left corner of the monitor.
    /// </summary>
    public static (int X, int Y, int Width, int Height) CalculateOverlayBounds(Position monitorPos, int overlayW = 220, int overlayH = 130)
    {
        int x = monitorPos.X + 48;
        int y = monitorPos.Bottom - overlayH - 72;
        if (y < monitorPos.Y + 24)
            y = monitorPos.Y + (monitorPos.Height - overlayH) / 2;

        return (x, y, overlayW, overlayH);
    }
}
