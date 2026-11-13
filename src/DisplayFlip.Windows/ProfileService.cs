using DisplayFlip.Core;

namespace DisplayFlip.Windows;

/// <summary>Capture/Apply profiles on top of CCD adapter + D5 missing-monitor policy.</summary>
public sealed class ProfileService
{
    private readonly CcdTopologyAdapter _adapter;
    private readonly ProfileStore _store;

    public ProfileService(CcdTopologyAdapter? adapter = null, ProfileStore? store = null)
    {
        _adapter = adapter ?? new CcdTopologyAdapter();
        _store = store ?? new ProfileStore();
    }

    public LayoutProfile Capture(string name)
    {
        var monitors = _adapter.ReadTopologies(out _, out _);
        if (monitors.Count == 0)
            throw new InvalidOperationException("No active monitors to capture.");

        var primary = monitors.FirstOrDefault(m => m.Position.X == 0 && m.Position.Y == 0)
                      ?? monitors[0];

        var states = monitors.Select(m => new MonitorState(
            StableKey: m.StableKey,
            FriendlyName: m.FriendlyName,
            Position: m.Position,
            ResolutionWidth: m.ResolutionWidth,
            ResolutionHeight: m.ResolutionHeight,
            Orientation: m.Orientation,
            IsEnabled: m.IsEnabled)).ToList();

        return new LayoutProfile
        {
            SchemaVersion = LayoutProfile.SupportedSchemaVersion,
            Name = name,
            PrimaryStableKey = primary.StableKey,
            Monitors = states,
            CreatedAt = DateTime.UtcNow.ToString("o"),
            UpdatedAt = DateTime.UtcNow.ToString("o")
        };
    }

    public void SaveCurrentAs(string name)
    {
        var profile = Capture(name);
        _store.Save(profile);
    }

    public (bool ok, string message) TryApply(string profileName, bool userOptedContinueIfMissing)
    {
        var profile = _store.Find(profileName);
        if (profile == null)
            return (false, $"Profile not found: '{profileName}'");

        var connected = _adapter.ReadTopologies(out _, out _);
        var (remaining, missing) = ProfilePolicy.SplitPresent(profile, connected.Select(c => c.StableKey));

        if (missing.Count > 0 && !userOptedContinueIfMissing)
            return (false, $"MissingMonitor: {string.Join(", ", missing)} not connected.");

        // Build desired from remaining + already-connected monitors not in profile are ignored
        var presentMap = connected.ToDictionary(c => c.StableKey);
        var desired = new List<DisplayMonitor>();

        foreach (var ms in remaining)
        {
            var baseMon = presentMap.TryGetValue(ms.StableKey, out var cur) ? cur : null;
            var w = ms.Position.Width > 0 ? ms.Position.Width : baseMon?.ResolutionWidth ?? ms.ResolutionWidth;
            var h = ms.Position.Height > 0 ? ms.Position.Height : baseMon?.ResolutionHeight ?? ms.ResolutionHeight;
            desired.Add(new DisplayMonitor(
                StableKey: ms.StableKey,
                FriendlyName: baseMon?.FriendlyName ?? ms.FriendlyName,
                Position: ms.Position,
                ResolutionWidth: ms.ResolutionWidth > 0 ? ms.ResolutionWidth : w,
                ResolutionHeight: ms.ResolutionHeight > 0 ? ms.ResolutionHeight : h,
                Orientation: ms.Orientation,
                IsEnabled: ms.IsEnabled));
        }

        // Ensure primary kept if still present
        if (!desired.Any(d => d.StableKey == profile.PrimaryStableKey) && desired.Count > 0)
        {
            var first = desired[0];
            desired[0] = first with { Position = first.Position with { X = 0, Y = 0 } };
        }

        var desiredLegal = DisplayTopology.Legalize(desired);

        var (paths, modes, built) = _adapter.BuildTopology(desiredLegal);
        // When CCD is unavailable BuildTopology returns empty paths; Validate/Apply fall back.
        if (!built && paths.Count > 0)
            return (false, "BuildTopology failed.");

        var (validOk, validMsg) = _adapter.Validate(paths, modes);
        if (!validOk)
            return (false, $"Validation failed: {validMsg} (ERROR_BAD_CONFIGURATION)");

        if (paths.Count == 0 || modes.Count == 0)
        {
            var (fbOk, fbMsg) = _adapter.ApplyViaFallback(desiredLegal);
            return fbOk ? (true, $"Applied '{profileName}'. {fbMsg}") : (false, fbMsg);
        }

        var (applyOk, applyMsg) = _adapter.Apply(paths, modes);
        return applyOk ? (true, $"Applied '{profileName}'.") : (false, applyMsg);
    }
}


