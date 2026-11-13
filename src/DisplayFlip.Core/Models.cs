namespace DisplayFlip.Core;

public enum Orientation
{
    Landscape,
    Portrait,
    Rotated,
    Unhandled
}

public enum LayoutOperationKind
{
    Mirror,
    SetPrimary,
    ApplyProfile
}

public readonly record struct Position(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;
    public int Bottom => Y + Height;
}

public sealed record DisplayMonitor(
    string StableKey,
    string FriendlyName,
    Position Position,
    int ResolutionWidth,
    int ResolutionHeight,
    Orientation Orientation,
    bool IsEnabled
);

public sealed record MonitorState(
    string StableKey,
    string FriendlyName,
    Position Position,
    int ResolutionWidth,
    int ResolutionHeight,
    Orientation Orientation,
    bool IsEnabled
);

public sealed record LayoutProfile
{
    public const int SupportedSchemaVersion = 1;

    public required int SchemaVersion { get; init; }
    public required string Name { get; init; }
    public required string PrimaryStableKey { get; init; }
    public required List<MonitorState> Monitors { get; init; }
    public string CreatedAt { get; init; } = string.Empty;
    public string UpdatedAt { get; init; } = string.Empty;

    public bool Validate(out string error)
    {
        if (SchemaVersion != SupportedSchemaVersion)
        {
            error = $"Unsupported schemaVersion {SchemaVersion}, expected {SupportedSchemaVersion}.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(Name) || Name.Length > 100)
        {
            error = "Profile name must be 1-100 chars.";
            return false;
        }
        if (Monitors.Count == 0)
        {
            error = "Profile must contain at least 1 DisplayMonitor.";
            return false;
        }
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var m in Monitors)
        {
            if (string.IsNullOrWhiteSpace(m.StableKey))
            {
                error = "DisplayMonitor StableKey must be non-empty.";
                return false;
            }
            if (!keys.Add(m.StableKey))
            {
                error = $"Duplicate StableKey {m.StableKey}.";
                return false;
            }
            if (m.Position.Width <= 0 || m.Position.Height <= 0)
            {
                error = $"DisplayMonitor {m.StableKey} has non-positive size.";
                return false;
            }
        }
        if (!keys.Contains(PrimaryStableKey))
        {
            error = "PrimaryStableKey must reference a DisplayMonitor in the profile.";
            return false;
        }
        error = string.Empty;
        return true;
    }
}

public sealed record LayoutResult(bool Applied, string Message, LayoutOperationKind Kind);

