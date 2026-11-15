using System.Diagnostics;
using DisplayFlip.Core.Localization;

namespace DisplayFlip.Windows;

public enum ProjectionMode
{
    Internal,
    External,
    Extend,
    Clone
}

/// <summary>
/// Dual-engine projection dispatcher (SetDisplayConfig Direct API + DisplaySwitch.exe fallback).
/// </summary>
public static class ProjectionDispatcher
{
    // Test hooks to allow deterministic unit tests without affecting physical display hardware.
    public static Func<uint, int>? SetDisplayConfigHook { get; set; }
    public static Func<string, (bool Success, string Message)>? DisplaySwitchHook { get; set; }

    public static bool TryParseMode(string? modeSwitch, out ProjectionMode mode, out string friendlyName, out string switchArg, out uint topology)
        => TryParseMode(modeSwitch, out mode, out friendlyName, out switchArg, out topology, null);

    public static bool TryParseMode(
        string? modeSwitch,
        out ProjectionMode mode,
        out string friendlyName,
        out string switchArg,
        out uint topology,
        string? language)
    {
        string effectiveLang = LocalizationCatalog.TryResolveExplicitLanguage(language)
                               ?? LocalizationCatalog.ResolveEffectiveLanguage(null);
        string norm = (modeSwitch ?? string.Empty).TrimStart('/', '-').Trim().ToLowerInvariant();
        switch (norm)
        {
            case "internal":
            case "1":
            case "solo 1":
            case "solo1":
                mode = ProjectionMode.Internal;
                topology = NativeMethods.SDC_TOPOLOGY_INTERNAL;
                switchArg = "/internal";
                friendlyName = LocalizationCatalog.Get("Projection.Mode.Internal", effectiveLang);
                return true;

            case "external":
            case "2":
            case "solo 2":
            case "solo2":
                mode = ProjectionMode.External;
                topology = NativeMethods.SDC_TOPOLOGY_EXTERNAL;
                switchArg = "/external";
                friendlyName = LocalizationCatalog.Get("Projection.Mode.External", effectiveLang);
                return true;

            case "clone":
            case "duplicate":
                mode = ProjectionMode.Clone;
                topology = NativeMethods.SDC_TOPOLOGY_CLONE;
                switchArg = "/clone";
                friendlyName = LocalizationCatalog.Get("Projection.Mode.Clone", effectiveLang);
                return true;

            case "extend":
                mode = ProjectionMode.Extend;
                topology = NativeMethods.SDC_TOPOLOGY_EXTEND;
                switchArg = "/extend";
                friendlyName = LocalizationCatalog.Get("Projection.Mode.Extend", effectiveLang);
                return true;

            default:
                mode = default;
                friendlyName = string.Empty;
                switchArg = string.Empty;
                topology = 0;
                return false;
        }
    }

    public static (bool Success, string Message) Dispatch(string? modeSwitch)
        => Dispatch(modeSwitch, null);

    public static (bool Success, string Message) Dispatch(string? modeSwitch, string? language)
    {
        string effectiveLang = LocalizationCatalog.TryResolveExplicitLanguage(language)
                               ?? LocalizationCatalog.ResolveEffectiveLanguage(null);

        if (!TryParseMode(modeSwitch, out var mode, out var friendlyName, out var switchArg, out var topology, effectiveLang))
        {
            return (false, LocalizationCatalog.Format("Projection.UnknownMode", effectiveLang, modeSwitch));
        }

        // Engine 1: Win32 SetDisplayConfig direct API
        try
        {
            uint flags = NativeMethods.SDC_APPLY | topology;
            int ret = SetDisplayConfigHook != null
                ? SetDisplayConfigHook(flags)
                : NativeMethods.SetDisplayConfig(0, IntPtr.Zero, 0, IntPtr.Zero, flags);

            if (ret == NativeMethods.ERROR_SUCCESS)
            {
                return (true, LocalizationCatalog.Format("Projection.Applied", effectiveLang, friendlyName));
            }
        }
        catch
        {
            // Fall through to DisplaySwitch.exe fallback.
        }

        // Engine 2: OS Shell DisplaySwitch.exe fallback
        if (DisplaySwitchHook != null)
        {
            return DisplaySwitchHook(switchArg);
        }

        try
        {
            var exe = ResolveDisplaySwitchExe();
            if (!string.IsNullOrEmpty(exe) && File.Exists(exe))
            {
                using var proc = Process.Start(new ProcessStartInfo(exe, switchArg)
                {
                    UseShellExecute = true
                });
                return (true, LocalizationCatalog.Format("Projection.DispatchedViaSwitch", effectiveLang, friendlyName, switchArg));
            }
            return (false, LocalizationCatalog.Format("Projection.DisplaySwitchMissing", effectiveLang, friendlyName));
        }
        catch (Exception ex)
        {
            return (false, LocalizationCatalog.Format("Projection.ChangeFailed", effectiveLang, ex.Message));
        }
    }

    public static (bool Success, string Message) TriggerSolo(int displayIndex)
        => TriggerSolo(displayIndex, null);

    public static (bool Success, string Message) TriggerSolo(int displayIndex, string? language)
    {
        string effectiveLang = LocalizationCatalog.TryResolveExplicitLanguage(language)
                               ?? LocalizationCatalog.ResolveEffectiveLanguage(null);
        return displayIndex switch
        {
            1 => Dispatch("internal", effectiveLang),
            2 => Dispatch("external", effectiveLang),
            _ => (false, LocalizationCatalog.Get("Projection.SoloUnsupportedIndex", effectiveLang))
        };
    }

    internal static string? ResolveDisplaySwitchExe()
    {
        var sysDir = Environment.GetFolderPath(Environment.SpecialFolder.System);
        var direct = Path.Combine(sysDir, "DisplaySwitch.exe");
        if (File.Exists(direct))
            return direct;

        if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
        {
            var winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            var sysnative = Path.Combine(winDir, "Sysnative", "DisplaySwitch.exe");
            if (File.Exists(sysnative))
                return sysnative;
        }

        var system32 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "DisplaySwitch.exe");
        if (File.Exists(system32))
            return system32;

        return null;
    }
}
