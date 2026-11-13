using System.Text.Json;
using System.Text.Json.Serialization;

namespace DisplayFlip.Core;

public sealed record ProfileFile
{
    public int SchemaVersion { get; init; } = LayoutProfile.SupportedSchemaVersion;
    public List<LayoutProfile> Profiles { get; init; } = new();
}

public sealed class ProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _filePath;
    private readonly object _gate = new();

    public ProfileStore(string filePath) => _filePath = filePath;

    public ProfileStore() : this(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DisplayFlip", "profiles.json")) { }

    public string FilePath => _filePath;

    public ProfileFile Load()
    {
        lock (_gate)
        {
            if (!File.Exists(_filePath))
                return new ProfileFile();

            try
            {
                var json = File.ReadAllText(_filePath);
                var data = JsonSerializer.Deserialize<ProfileFile>(json, JsonOptions);
                if (data == null)
                    return new ProfileFile();
                if (data.SchemaVersion > LayoutProfile.SupportedSchemaVersion)
                    throw new InvalidDataException(
                        $"Unsupported schemaVersion {data.SchemaVersion}, expected <= {LayoutProfile.SupportedSchemaVersion}.");
                return data;
            }
            catch (InvalidDataException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Profile file corrupt: {ex.Message}", ex);
            }
        }
    }

    public List<string> ListNames()
    {
        var data = Load();
        return data.Profiles.Select(p => p.Name).ToList();
    }

    public LayoutProfile? Find(string name)
    {
        var data = Load();
        return data.Profiles.FirstOrDefault(p =>
            string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public void Save(LayoutProfile profile)
    {
        if (!profile.Validate(out var err))
            throw new ArgumentException(err);

        lock (_gate)
        {
            var data = File.Exists(_filePath) ? LoadSafe() : new ProfileFile();
            var idx = data.Profiles.FindIndex(p =>
                string.Equals(p.Name, profile.Name, StringComparison.OrdinalIgnoreCase));
            var now = DateTime.UtcNow.ToString("o");
            var withTime = profile with
            {
                SchemaVersion = LayoutProfile.SupportedSchemaVersion,
                CreatedAt = idx >= 0 ? data.Profiles[idx].CreatedAt : now,
                UpdatedAt = now
            };
            if (idx >= 0)
                data.Profiles[idx] = withTime;
            else
                data.Profiles.Add(withTime);

            WriteAtomic(data);
        }
    }

    public bool Delete(string name)
    {
        lock (_gate)
        {
            var data = LoadSafe();
            var idx = data.Profiles.FindIndex(p =>
                string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
            if (idx < 0)
                return false;
            data.Profiles.RemoveAt(idx);
            WriteAtomic(data);
            return true;
        }
    }

    private ProfileFile LoadSafe()
    {
        try { return Load(); }
        catch (Exception) { return new ProfileFile(); }
    }

    private void WriteAtomic(ProfileFile data)
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        var tmp = _filePath + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(data, JsonOptions));
        File.Move(tmp, _filePath, overwrite: true);
    }
}

/// <summary>Missing-monitor policy (D5) --- pure decision helper for testability.</summary>
public static class ProfilePolicy
{
    public static (List<MonitorState> remaining, List<string> missing) SplitPresent(
        LayoutProfile profile,
        IEnumerable<string> connectedStableKeys)
    {
        var connected = new HashSet<string>(connectedStableKeys, StringComparer.Ordinal);
        var remaining = new List<MonitorState>();
        var missing = new List<string>();
        foreach (var ms in profile.Monitors)
        {
            if (connected.Contains(ms.StableKey))
                remaining.Add(ms);
            else
                missing.Add(ms.StableKey);
        }
        return (remaining, missing);
    }

    public enum MissingAction
    {
        Abort,
        Continue
    }

    public static MissingAction Decide(IEnumerable<string> missing, bool userOptedContinue)
        => missing.Any() && !userOptedContinue ? MissingAction.Abort : MissingAction.Continue;
}


