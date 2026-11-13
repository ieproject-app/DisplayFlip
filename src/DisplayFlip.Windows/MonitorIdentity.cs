using System.Runtime.InteropServices;

namespace DisplayFlip.Windows;

/// <summary>
/// Stable DisplayMonitor identity (D2): StableKey = monitorDevicePath + EDID manufacture/product + friendlyName.
/// Mirrors research: use DisplayConfigGetDeviceInfo TARGET_NAME and SetupAPI-friendly fields.
/// </summary>
public sealed record MonitorIdentityInfo(
    string StableKey,
    string FriendlyName,
    string MonitorDevicePath,
    uint EdidManufactureId,
    uint EdidProductCodeId);

public static class MonitorIdentity
{
    public static MonitorIdentityInfo Describe(uint adapterId, uint id)
    {
        var size = Marshal.SizeOf<NativeMethods.DISPLAYCONFIG_TARGET_DEVICE_NAME>();
        var targetName = new NativeMethods.DISPLAYCONFIG_TARGET_DEVICE_NAME
        {
            size = (uint)size,
            type = NativeMethods.DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME,
            adapterId = adapterId,
            id = id
        };

        string friendly = $"Display {id}";
        string path = $"DISPLAY{adapterId}_{id}";
        uint manu = 0;
        uint prod = 0;

        var status = NativeMethods.DisplayConfigGetTargetName(ref targetName);
        if (status == NativeMethods.ERROR_SUCCESS)
        {
            if (!string.IsNullOrWhiteSpace(targetName.monitorFriendlyDeviceName))
                friendly = targetName.monitorFriendlyDeviceName.Trim();
            if (!string.IsNullOrWhiteSpace(targetName.monitorDevicePath))
                path = targetName.monitorDevicePath.Trim();
            manu = targetName.edidManufactureId;
            prod = targetName.edidProductCodeId;
        }

        var stable = string.IsNullOrWhiteSpace(path)
            ? $"{friendly}#{manu:X4}{prod:X4}"
            : $"{path}#{manu:X4}{prod:X4}#{friendly}";

        return new MonitorIdentityInfo(stable, friendly, path, manu, prod);
    }

    /// <summary>
    /// For path-to-DeviceID matching via SetupAPI substring shared (research -section 4).
    /// Returned prefix e.g. "DELF06B" derived from monitorDevicePath.
    /// </summary>
    public static string? TryExtractSharedSubstring(string monitorDevicePath)
    {
        if (string.IsNullOrWhiteSpace(monitorDevicePath))
            return null;
        var parts = monitorDevicePath.Split(new[] { '#', '\\' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var p in parts)
        {
            if (p.Length >= 7 && p.Length <= 12 && p.All(c => char.IsLetterOrDigit(c)))
                return p;
        }
        return null;
    }
}


