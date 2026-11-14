using DisplayFlip.Core;
using MonitorDisplay = DisplayFlip.Core.DisplayMonitor;

namespace DisplayFlip.Windows;

/// <summary>
/// Topology adapter (D6): CCD via QDC_ONLY_ACTIVE_PATHS with ScreenFallback when CCD
/// returns ERROR_INVALID_PARAMETER on this driver. Validate/Apply try CCD first,
/// then fall back to ChangeDisplaySettingsEx so Save/Mirror keep working.
/// </summary>
public sealed class CcdTopologyAdapter
{
    public List<MonitorDisplay> ReadTopologies(out List<NativeMethods.DISPLAYCONFIG_PATH_INFO> paths,
                                        out List<NativeMethods.DISPLAYCONFIG_MODE_INFO> modes)
    {
        paths = new List<NativeMethods.DISPLAYCONFIG_PATH_INFO>();
        modes = new List<NativeMethods.DISPLAYCONFIG_MODE_INFO>();

        var status = NativeMethods.QueryDisplayConfig(
            NativeMethods.QDC_ONLY_ACTIVE_PATHS, out var pathCount, out var modeCount,
            IntPtr.Zero,
            IntPtr.Zero, IntPtr.Zero);
        if (status != NativeMethods.ERROR_SUCCESS || pathCount == 0)
        {
            // Intel UHD + MSI/BOE driver returns 87 for CCD; fall back to EnumDisplayMonitors.
            var fallback = ScreenFallback.ReadTopologies();
            if (fallback.Count > 0)
                return fallback;
            return new List<MonitorDisplay>();
        }

        var pathArr = new NativeMethods.DISPLAYCONFIG_PATH_INFO[pathCount];
        var modeArr = new NativeMethods.DISPLAYCONFIG_MODE_INFO[modeCount];

        status = NativeMethods.QueryDisplayConfig(
            NativeMethods.QDC_ONLY_ACTIVE_PATHS, out pathCount, out modeCount,
            IntPtr.Zero, pathArr, modeArr);
        if (status != NativeMethods.ERROR_SUCCESS)
        {
            var fallback2 = ScreenFallback.ReadTopologies();
            if (fallback2.Count > 0)
                return fallback2;
            return new List<MonitorDisplay>();
        }

        paths.AddRange(pathArr);
        modes.AddRange(modeArr);

        var monitors = new List<MonitorDisplay>();
        foreach (var path in pathArr)
        {
            var modeIdx = path.sourceInfo.modeInfoIdx;
            if (modeIdx >= modeArr.Length)
                continue;
            var mode = modeArr[modeIdx];

            var identity = MonitorIdentity.Describe(path.targetInfo.adapterId, path.targetInfo.id);
            var pos = mode.sourceMode.position;
            var width = (int)mode.sourceMode.width;
            var height = (int)mode.sourceMode.height;
            if (width <= 0) width = 1920;
            if (height <= 0) height = 1080;

            monitors.Add(new MonitorDisplay(
                StableKey: identity.StableKey,
                FriendlyName: identity.FriendlyName,
                Position: new Position(pos.x, pos.y, width, height),
                ResolutionWidth: width,
                ResolutionHeight: height,
                Orientation: DisplayFlip.Core.Orientation.Landscape,
                IsEnabled: true));
        }

        return monitors;
    }

    public (bool ok, string message) Validate(List<NativeMethods.DISPLAYCONFIG_PATH_INFO> paths,
                                               List<NativeMethods.DISPLAYCONFIG_MODE_INFO> modes)
    {
        if (paths.Count == 0 || modes.Count == 0)
        {
            // CCD unavailable (driver returns 87); validation is done in ApplyViaFallback legalizer.
            return (true, "Valid via fallback");
        }
        var status = NativeMethods.SetDisplayConfigNative(
            (uint)paths.Count, paths.ToArray(),
            (uint)modes.Count, modes.ToArray(),
            NativeMethods.SDC_VALIDATE);
        return status switch
        {
            NativeMethods.ERROR_SUCCESS => (true, "Valid"),
            NativeMethods.ERROR_BAD_CONFIGURATION => (false, "ERROR_BAD_CONFIGURATION"),
            _ => (false, $"SetDisplayConfig failed: {status}")
        };
    }

    public (bool ok, string message) Apply(List<NativeMethods.DISPLAYCONFIG_PATH_INFO> paths,
                                           List<NativeMethods.DISPLAYCONFIG_MODE_INFO> modes)
    {
        if (paths.Count == 0 || modes.Count == 0)
        {
            var fallback = ScreenFallback.ReadTopologies();
            var legal = DisplayTopology.Legalize(fallback);
            return ScreenFallback.ApplyTopologies(legal);
        }
        var status = NativeMethods.SetDisplayConfigNative(
            (uint)paths.Count, paths.ToArray(),
            (uint)modes.Count, modes.ToArray(),
            NativeMethods.SDC_APPLY);
        if (status != NativeMethods.ERROR_SUCCESS)
        {
            var fallback = ScreenFallback.ReadTopologies();
            if (fallback.Count > 0)
            {
                var legal = DisplayTopology.Legalize(fallback);
                var (ok, msg) = ScreenFallback.ApplyTopologies(legal);
                if (ok)
                    return (true, msg + " (via fallback)");
            }
            return (false, $"SetDisplayConfig failed: {status}");
        }
        return (true, "Applied");
    }

    /// <summary>
    /// Fallback apply from desired positions directly (used when CCD paths are empty).
    /// Legalizes, then applies via ChangeDisplaySettingsEx.
    /// </summary>
    public (bool ok, string message) ApplyViaFallback(List<MonitorDisplay> desired)
    {
        var legal = DisplayTopology.Legalize(desired);
        return ScreenFallback.ApplyTopologies(legal);
    }

    /// <summary>
    /// Applies a 2D monitor arrangement (supporting free drag-and-drop X/Y offsets and top/bottom/side-by-side layouts).
    /// </summary>
    public (bool ok, string message) ApplyArrangement(IReadOnlyList<MonitorDisplay> desired, string? primaryStableKey = null)
    {
        var normalized = DisplayTopology.NormalizeArrangement(desired, primaryStableKey);
        if (normalized.Count == 0)
            return (false, "Tidak ada monitor untuk diterapkan.");

        ReadTopologies(out var paths, out var modes);
        if (paths.Count == 0 || modes.Count == 0)
            return ScreenFallback.ApplyTopologies(normalized);

        var (paths2, modes2, built) = BuildTopology(normalized);
        if (built && paths2.Count > 0)
        {
            var (validOk, _) = Validate(paths2, modes2);
            if (validOk)
            {
                var status = NativeMethods.SetDisplayConfigNative(
                    (uint)paths2.Count, paths2.ToArray(),
                    (uint)modes2.Count, modes2.ToArray(),
                    NativeMethods.SDC_APPLY);
                if (status == NativeMethods.ERROR_SUCCESS)
                    return (true, "Applied");
            }
        }
        return ScreenFallback.ApplyTopologies(normalized);
    }

    public List<(int Width, int Height)> GetSupportedResolutions(MonitorDisplay monitor)
    {
        var key = monitor.StableKey.StartsWith("\\\\.\\DISPLAY", StringComparison.OrdinalIgnoreCase)
            ? monitor.StableKey
            : monitor.FriendlyName;
        var raw = ScreenFallback.GetSupportedResolutions(key, monitor.Position.Width, monitor.Position.Height);
        bool wantPortrait = monitor.Orientation is DisplayFlip.Core.Orientation.Portrait or DisplayFlip.Core.Orientation.Unhandled;
        return raw
            .Select(r => wantPortrait
                ? (Width: Math.Min(r.Width, r.Height), Height: Math.Max(r.Width, r.Height))
                : (Width: Math.Max(r.Width, r.Height), Height: Math.Min(r.Width, r.Height)))
            .Distinct()
            .OrderByDescending(r => r.Width * r.Height)
            .ThenByDescending(r => r.Width)
            .ToList();
    }

    public (List<NativeMethods.DISPLAYCONFIG_PATH_INFO> paths,
            List<NativeMethods.DISPLAYCONFIG_MODE_INFO> modes,
            bool ok) BuildTopology(List<MonitorDisplay> desired)
    {
        var current = ReadTopologies(out var paths, out var modes);
        if (desired.Count == 0)
            return (paths, modes, false);

        var sortedCurrent = current.OrderBy(m => m.Position.X).ToList();
        var sortedDesired = desired.OrderBy(m => m.Position.X).ToList();
        var desiredByKey = desired.ToDictionary(m => m.StableKey);

        for (int i = 0; i < sortedCurrent.Count; i++)
        {
            var cur = sortedCurrent[i];
            var target = desiredByKey.TryGetValue(cur.StableKey, out var matched)
                ? matched
                : (i < sortedDesired.Count ? sortedDesired[i] : null);
            if (target == null)
                continue;

            var desiredPos = target.Position;
            int origIdx = current.IndexOf(cur);
            if (origIdx >= 0 && origIdx < paths.Count && origIdx < modes.Count)
            {
                var idx = (int)paths[origIdx].sourceInfo.modeInfoIdx;
                if (idx >= 0 && idx < modes.Count)
                {
                    var old = modes[idx];
                    modes[idx] = new NativeMethods.DISPLAYCONFIG_MODE_INFO
                    {
                        infoType = old.infoType,
                        id = old.id,
                        adapterId = old.adapterId,
                        sourceMode = new NativeMethods.DISPLAYCONFIG_SOURCE_MODE
                        {
                            width = (uint)desiredPos.Width,
                            height = (uint)desiredPos.Height,
                            pixelFormat = old.sourceMode.pixelFormat,
                            position = new NativeMethods.POINTL { x = desiredPos.X, y = desiredPos.Y }
                        }
                    };
                }
            }
        }

        return (paths, modes, true);
    }
}
