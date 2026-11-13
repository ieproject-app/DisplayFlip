using DisplayFlip.Core;
using DisplayFlip.Windows;

namespace DisplayFlip;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Contains("--mirror") || args.Contains("--apply") || args.Contains("--list"))
        {
            int code = RunCli(args);
            Environment.Exit(code);
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayHost());
    }

    private static int RunCli(string[] args)
    {
        var adapter = new CcdTopologyAdapter();
        var store = new ProfileStore();
        var profiles = new ProfileService(adapter, store);

        try
        {
            if (args.Contains("--mirror"))
            {
                var monitors = adapter.ReadTopologies(out var paths, out var modes);
                if (monitors.Count < 2)
                {
                    Console.Error.WriteLine("Mirror requires 2+ active monitors.");
                    return 4;
                }
                var desired = DisplayTopology.Mirror(monitors);
                var (np, nm, built) = adapter.BuildTopology(desired);
                if (!built)
                {
                    Console.Error.WriteLine("Failed to build mirror topology.");
                    return 5;
                }
                var (vok, vmsg) = adapter.Validate(np, nm);
                if (!vok)
                {
                    Console.Error.WriteLine($"Validation failed: {vmsg}");
                    return 2;
                }
                var (aok, amsg) = adapter.Apply(np, nm);
                if (!aok)
                {
                    Console.Error.WriteLine($"Apply failed: {amsg}");
                    return 5;
                }
                Console.WriteLine("Mirror applied.");
                return 0;
            }

            if (args.Contains("--apply"))
            {
                var idx = Array.IndexOf(args, "--apply");
                if (idx < 0 || idx + 1 >= args.Length)
                {
                    Console.Error.WriteLine("Usage: DisplayFlip.exe --apply \"<ProfileName>\"");
                    return 4;
                }
                var name = args[idx + 1].Trim();
                var profile = store.Find(name);
                if (profile == null)
                {
                    Console.Error.WriteLine($"Profile not found: '{name}' (see --list)");
                    return 3;
                }

                // Missing-monitor: headless = abort unless --force-missing flag present (not in spec, abort)
                var connected = adapter.ReadTopologies(out _, out _);
                var (_, missing) = ProfilePolicy.SplitPresent(profile, connected.Select(c => c.StableKey));
                if (missing.Count > 0)
                {
                    Console.Error.WriteLine($"MissingMonitor: {string.Join(", ", missing)} not connected.");
                    return 4;
                }

                var (ok, msg) = profiles.TryApply(name, userOptedContinueIfMissing: false);
                if (!ok && msg.Contains("Validation"))
                {
                    Console.Error.WriteLine(msg);
                    return 2;
                }
                if (!ok)
                {
                    Console.Error.WriteLine(msg);
                    return 5;
                }
                Console.WriteLine(msg);
                return 0;
            }

            if (args.Contains("--list"))
            {
                var names = store.ListNames();
                if (args.Contains("--json"))
                {
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(names));
                }
                else if (names.Count == 0)
                {
                    Console.WriteLine("(no profiles)");
                }
                else
                {
                    foreach (var n in names)
                        Console.WriteLine(n);
                }
                return 0;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 5;
        }

        Console.Error.WriteLine("Usage: DisplayFlip.exe [--mirror | --apply \"<name>\" | --list]");
        return 4;
    }
}


