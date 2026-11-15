using System.Runtime.InteropServices;
using DisplayFlip.Core;
using DisplayFlip.Windows;

namespace DisplayFlip;

internal static class Program
{
    private const int ATTACH_PARENT_PROCESS = -1;

    [DllImport("kernel32.dll")]
    private static extern bool AttachConsole(int dwProcessId);

    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Contains("--export-screens") || args.Contains("--export-fase-f") || args.Contains("--export-fase-g") || args.Contains("--export-fase-h"))
        {
            ApplicationConfiguration.Initialize();
            var shotRoot = args.Length > 1 && !args[1].StartsWith("--") ? args[1] : Directory.GetCurrentDirectory();
            using var shotHost = new TrayHost(showSettingsOnLaunch: false);
            using var shotForm = new FluentSettingsForm(shotHost, FluentIcons.CreateModernAppIcon("fluent", 32));
            var dirName = args.Contains("--export-fase-h") ? "fase-h" : (args.Contains("--export-fase-g") ? "fase-g" : (args.Contains("--export-fase-f") ? "fase-f" : "fase-e"));
            var targetDir = Path.Combine(shotRoot, "docs", "review", dirName);
            shotForm.ExportResponsive(targetDir);
            shotForm.ExportScreenshots(targetDir);
            shotForm.ForceClose();
            Environment.Exit(0);
        }

        if (args.Contains("--export-icon"))
        {
            ApplicationConfiguration.Initialize();
            var root = args.Length > 1 ? args[1] : AppContext.BaseDirectory;
            var icoBytes = FluentIcons.BuildMultiSizeIcoBytes("fluent");
            File.WriteAllBytes(Path.Combine(root, "src", "DisplayFlip", "DisplayFlip.ico"), icoBytes);
            FluentIcons.SaveModernIconPreviewSheet(Path.Combine(root, "artifacts", "DisplayFlip-logo-preview.png"));
            using var host = new TrayHost(showSettingsOnLaunch: false);
            using var form = new FluentSettingsForm(host, FluentIcons.CreateModernAppIcon("fluent", 32));
            form.ExportScreenshots(Path.Combine(root, "docs", "review", "fase-d"));
            form.ForceClose();
            Environment.Exit(0);
        }

        if (args.Contains("--mirror") || args.Contains("--apply") || args.Contains("--list")
            || args.Contains("--solo") || args.Contains("--internal") || args.Contains("--external")
            || args.Contains("--extend") || args.Contains("--clone"))
        {
            int code = RunCli(args);
            Environment.Exit(code);
        }

        ApplicationConfiguration.Initialize();
        bool startInTray = args.Contains("--tray") || args.Contains("--minimized");
        Application.Run(new TrayHost(showSettingsOnLaunch: !startInTray));
    }

    private static int RunCli(string[] args)
    {
        if (!Console.IsOutputRedirected || !Console.IsErrorRedirected)
            AttachConsole(ATTACH_PARENT_PROCESS);

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
                if (paths.Count == 0 || modes.Count == 0)
                {
                    var (fbOk, fbMsg) = adapter.ApplyViaFallback(desired);
                    if (!fbOk)
                    {
                        Console.Error.WriteLine($"Apply failed: {fbMsg}");
                        return 5;
                    }
                    Console.WriteLine("Mirror applied.");
                    return 0;
                }
                var (np, nm, built) = adapter.BuildTopology(desired);
                if (!built && np.Count > 0)
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

            if (args.Contains("--solo"))
            {
                int idx = Array.IndexOf(args, "--solo");
                if (idx < 0 || idx + 1 >= args.Length || args[idx + 1].StartsWith("--"))
                {
                    Console.Error.WriteLine("Parameter --solo membutuhkan target monitor 1 atau 2 (contoh: --solo 1 atau --solo 2).");
                    return 4;
                }
                string target = args[idx + 1].Trim();
                if (!int.TryParse(target, out int soloIdx) || (soloIdx != 1 && soloIdx != 2))
                {
                    Console.Error.WriteLine($"Target monitor '{target}' tidak valid. Hanya Layar 1 atau Layar 2 yang didukung untuk --solo.");
                    return 4;
                }
                var (ok, msg) = ProjectionDispatcher.TriggerSolo(soloIdx);
                if (ok)
                {
                    Console.WriteLine(msg);
                    return 0;
                }
                Console.Error.WriteLine(msg);
                return 5;
            }

            if (args.Contains("--internal"))
            {
                var (ok, msg) = ProjectionDispatcher.Dispatch("internal");
                if (ok)
                {
                    Console.WriteLine(msg);
                    return 0;
                }
                Console.Error.WriteLine(msg);
                return 5;
            }

            if (args.Contains("--external"))
            {
                var (ok, msg) = ProjectionDispatcher.Dispatch("external");
                if (ok)
                {
                    Console.WriteLine(msg);
                    return 0;
                }
                Console.Error.WriteLine(msg);
                return 5;
            }

            if (args.Contains("--extend"))
            {
                var (ok, msg) = ProjectionDispatcher.Dispatch("extend");
                if (ok)
                {
                    Console.WriteLine(msg);
                    return 0;
                }
                Console.Error.WriteLine(msg);
                return 5;
            }

            if (args.Contains("--clone"))
            {
                var (ok, msg) = ProjectionDispatcher.Dispatch("clone");
                if (ok)
                {
                    Console.WriteLine(msg);
                    return 0;
                }
                Console.Error.WriteLine(msg);
                return 5;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 5;
        }

        Console.Error.WriteLine("Usage: DisplayFlip.exe [--mirror | --apply \"<name>\" | --list | --solo 1|2 | --internal | --external | --extend | --clone]");
        return 4;
    }
}


