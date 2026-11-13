using DisplayFlip.Core;
using DisplayFlip.Windows;
using System.Windows.Forms;
using Microsoft.Win32;

namespace DisplayFlip;

/// <summary>Tray host. Menu Fluent (Fase A); rebuild hanya saat Opening/aksi/display berubah.</summary>
public sealed class TrayHost : ApplicationContext
{
    private readonly NotifyIcon _icon;
    private readonly CcdTopologyAdapter _adapter;
    private readonly ProfileService _profiles;
    private readonly ProfileStore _store;
    private readonly FluentMenu _menu;
    private readonly FluentMenu _primaryMenu;
    private readonly FluentMenu _applyMenu;
    private readonly FluentMenuItem _mirrorItem;
    private readonly FluentMenuItem _primaryParent;
    private readonly FluentMenuItem _applyParent;
    private readonly FluentMenuItem _startupItem;
    private bool _disposed;
    private bool _isMenuOpen;
    private bool _updatingStartup;
    private string _lastSignature = string.Empty;

    public TrayHost()
    {
        _adapter = new CcdTopologyAdapter();
        _store = new ProfileStore();
        _profiles = new ProfileService(_adapter, _store);

        _menu = new FluentMenu(FluentTheme.FlyoutWidth);
        _primaryMenu = new FluentMenu(FluentTheme.SubmenuWidth);
        _applyMenu = new FluentMenu(FluentTheme.SubmenuWidth);

        _mirrorItem = Row("Tukar kiri-kanan", FluentIcons.Swap, FluentTheme.FlyoutWidth);
        _mirrorItem.Click += async (_, _) => await OnMirrorClickedAsync();
        _menu.AddItem(_mirrorItem);
        _menu.Items.Add(new ToolStripSeparator());

        _primaryParent = Row("Jadikan primary", FluentIcons.StarOutline, FluentTheme.FlyoutWidth);
        _primaryParent.DropDown = _primaryMenu;
        _menu.AddItem(_primaryParent);

        var saveItem = Row("Simpan layout sekarang", FluentIcons.Save, FluentTheme.FlyoutWidth);
        saveItem.Click += (_, _) => OnSaveClicked();
        _menu.AddItem(saveItem);

        _applyParent = Row("Terapkan profil", FluentIcons.Play, FluentTheme.FlyoutWidth);
        _applyParent.DropDown = _applyMenu;
        _menu.AddItem(_applyParent);
        _menu.Items.Add(new ToolStripSeparator());

        _startupItem = Row("Jalan otomatis saat Windows start", null, FluentTheme.FlyoutWidth);
        _startupItem.CheckBox = true;
        _startupItem.CheckOnClick = false;
        _startupItem.Click += (_, _) => OnStartupClicked();
        _menu.AddItem(_startupItem);

        var exitItem = Row("Keluar", FluentIcons.Close, FluentTheme.FlyoutWidth);
        exitItem.Click += (_, _) => ExitThread();
        _menu.AddItem(exitItem);

        _menu.Opening += (_, _) =>
        {
            _isMenuOpen = true;
            RefreshMenu(force: true);
        };
        _menu.Closed += (_, _) => _isMenuOpen = false;

        _icon = new NotifyIcon
        {
            Text = "DisplayFlip",
            Icon = LoadAppIcon(),
            Visible = true,
            ContextMenuStrip = _menu
        };

        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        RefreshMenu(force: true);
    }

    private static FluentMenuItem Row(string text, char? glyph, int menuWidth, int height = 0)
    {
        var item = new FluentMenuItem(text, height == 0 ? FluentTheme.ItemHeight : height)
        {
            Glyph = glyph
        };
        item.Width = menuWidth - FluentTheme.MenuPadding * 2;
        return item;
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        if (_disposed || _isMenuOpen)
            return;
        if (_menu.InvokeRequired)
            _menu.BeginInvoke(new Action(() => RefreshMenu(force: true)));
        else
            RefreshMenu(force: true);
    }

    private void OnStartupClicked()
    {
        var next = !_startupItem.Checked;
        StartupRegistry.SetEnabled(next);
        _updatingStartup = true;
        _startupItem.Checked = StartupRegistry.IsEnabled();
        _updatingStartup = false;
        _lastSignature = string.Empty;
    }

    private static Icon LoadAppIcon()
    {
        try
        {
            var asm = typeof(TrayHost).Assembly;
            using var stream = asm.GetManifestResourceStream("DisplayFlip.ico");
            if (stream != null)
            {
                using var copy = new MemoryStream();
                stream.CopyTo(copy);
                copy.Position = 0;
                return new Icon(copy);
            }
        }
        catch
        {
            // Fall through to file / system icon.
        }
        try
        {
            var icoPath = Path.Combine(AppContext.BaseDirectory, "DisplayFlip.ico");
            if (File.Exists(icoPath))
                return new Icon(icoPath);
            var exeDir = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
            var alt = Path.Combine(exeDir, "DisplayFlip.ico");
            if (File.Exists(alt))
                return new Icon(alt);
        }
        catch
        {
            // Fall through to system icon.
        }
        return SystemIcons.Application;
    }

    private void RefreshMenu(bool force)
    {
        if (_disposed)
            return;
        // Panggilan non-forced saat menu terbuka diabaikan agar menu stabil.
        if (_isMenuOpen && !force)
            return;

        try
        {
            var monitors = _adapter.ReadTopologies(out _, out _);
            var names = _store.ListNames();
            var startup = StartupRegistry.IsEnabled();
            var signature = string.Join("|", monitors.Select(m => $"{m.StableKey}@{m.Position.X},{m.Position.Y},{m.Position.Width}x{m.Position.Height}"))
                            + "#" + string.Join(",", names) + "#" + startup;
            if (!force && signature == _lastSignature)
                return;
            _lastSignature = signature;

            _mirrorItem.Enabled = monitors.Count >= 2;
            // Keputusan owner 8: tanpa item Mirror baru; item yang ada disabled + subteks.
            // Baris 44px saat ada subteks agar dua baris tidak bertumpuk.
            _mirrorItem.SecondaryLine = monitors.Count < 2 ? "Perlu 2+ monitor" : null;
            _mirrorItem.Height = _mirrorItem.SecondaryLine != null
                ? FluentTheme.MonitorRowHeight
                : FluentTheme.ItemHeight;

            _primaryMenu.ClearItems();
            if (monitors.Count == 0)
            {
                var none = Row("(tidak ada monitor aktif)", null, FluentTheme.SubmenuWidth);
                none.Enabled = false;
                _primaryMenu.AddItem(none);
                _primaryParent.Enabled = false;
            }
            else
            {
                _primaryParent.Enabled = true;
                foreach (var m in monitors)
                {
                    var id = m.StableKey;
                    var isPrimary = m.Position.X == 0 && m.Position.Y == 0;
                    var item = Row(m.FriendlyName, null, FluentTheme.SubmenuWidth, FluentTheme.MonitorRowHeight);
                    item.Radio = true;
                    item.RadioChecked = isPrimary;
                    item.BadgePrimary = isPrimary;
                    // ASCII "x" (bukan "x") agar file sumber tetap ASCII-bersih.
                    item.SecondaryLine = $"{m.Position.Width}x{m.Position.Height}";
                    item.Click += (_, _) => OnSetPrimary(id, m.FriendlyName);
                    _primaryMenu.AddItem(item);
                }
            }

            _applyMenu.ClearItems();
            if (names.Count == 0)
            {
                var empty = Row("(belum ada profil)", null, FluentTheme.SubmenuWidth);
                empty.Enabled = false;
                _applyMenu.AddItem(empty);
                _applyParent.Enabled = false;
            }
            else
            {
                _applyParent.Enabled = true;
                foreach (var n in names)
                {
                    var captured = n;
                    var item = Row(captured, null, FluentTheme.SubmenuWidth);
                    item.Click += async (_, _) => await OnApplyClickedAsync(captured);
                    _applyMenu.AddItem(item);
                }
            }

            if (!_updatingStartup)
                _startupItem.Checked = startup;

            var tooltip = $"DisplayFlip ({monitors.Count} monitor)";
            _icon.Text = tooltip.Length > 63 ? tooltip[..63] : tooltip;
        }
        catch
        {
            // Keep tray alive if a transient CCD read fails.
        }
    }

    private async Task OnMirrorClickedAsync()
    {
        var message = await Task.Run(() =>
        {
            var monitors = _adapter.ReadTopologies(out var paths, out var modes);
            if (monitors.Count < 2)
                return "Perlu minimal 2 monitor aktif untuk tukar kiri-kanan.";
            var desired = DisplayTopology.Mirror(monitors);
            if (paths.Count == 0 || modes.Count == 0)
            {
                var (fbOk, fbMsg) = _adapter.ApplyViaFallback(desired);
                return fbOk ? $"Layout dicerminkan. Primary tetap. {fbMsg}" : $"Gagal tukar: {fbMsg}";
            }
            var (paths2, modes2, built) = _adapter.BuildTopology(desired);
            if (!built && paths2.Count > 0)
                return "Gagal membangun susunan baru.";
            var (validOk, validMsg) = _adapter.Validate(paths2, modes2);
            if (!validOk)
                return $"Validasi gagal: {validMsg}";
            var (applyOk, applyMsg) = _adapter.Apply(paths2, modes2);
            return applyOk ? "Layout dicerminkan. Primary tetap." : $"Terapkan gagal: {applyMsg}";
        });
        ShowBalloon(message);
        _lastSignature = string.Empty;
        RefreshMenu(force: false);
    }

    private void OnSetPrimary(string stableKey, string friendlyName)
    {
        var monitors = _adapter.ReadTopologies(out var paths, out var modes);
        var currentPrimary = monitors.FirstOrDefault(m => m.Position.X == 0 && m.Position.Y == 0);
        if (currentPrimary != null && currentPrimary.StableKey == stableKey)
        {
            ShowBalloon($"{friendlyName} sudah menjadi primary.");
            return;
        }

        var desired = DisplayTopology.SetPrimary(monitors, stableKey);
        if (!DisplayTopology.IsLegal(desired))
            desired = DisplayTopology.Legalize(desired);

        if (paths.Count == 0 || modes.Count == 0)
        {
            var (fbOk, fbMsg) = _adapter.ApplyViaFallback(desired);
            ShowBalloon(fbOk ? $"{friendlyName} kini menjadi primary. {fbMsg}" : $"Gagal jadikan primary: {fbMsg}");
            _lastSignature = string.Empty;
            return;
        }

        var (paths2, modes2, built) = _adapter.BuildTopology(desired);
        if (!built && paths2.Count > 0)
        {
            ShowBalloon("Gagal membangun susunan primary.");
            return;
        }
        var (validOk, validMsg) = _adapter.Validate(paths2, modes2);
        if (!validOk)
        {
            ShowBalloon($"Validasi gagal: {validMsg}");
            return;
        }
        var (applyOk, applyMsg) = _adapter.Apply(paths2, modes2);
        ShowBalloon(applyOk ? $"{friendlyName} kini menjadi primary." : $"Terapkan gagal: {applyMsg}");
        _lastSignature = string.Empty;
    }

    private void OnSaveClicked()
    {
        var name = Microsoft.VisualBasic.Interaction.InputBox(
            "Nama profil (mis. Primary Kanan):", "DisplayFlip - Simpan profil", "Primary Kanan");
        if (string.IsNullOrWhiteSpace(name))
            return;

        try
        {
            _profiles.SaveCurrentAs(name.Trim());
            ShowBalloon($"Profil disimpan: {name.Trim()}");
            _lastSignature = string.Empty;
        }
        catch (Exception ex)
        {
            ShowBalloon($"Simpan gagal: {ex.Message}");
        }
    }

    private async Task OnApplyClickedAsync(string name)
    {
        var profile = _store.Find(name);
        if (profile == null)
        {
            ShowBalloon($"Profil tidak ditemukan: {name}");
            return;
        }
        var connected = _adapter.ReadTopologies(out _, out _);
        var (_, missing) = ProfilePolicy.SplitPresent(profile, connected.Select(c => c.StableKey));

        var continueIfMissing = false;
        if (missing.Count > 0)
        {
            var answer = MessageBox.Show(
                $"Monitor hilang: {string.Join(", ", missing)}\nLanjutkan dengan yang tersisa?",
                "DisplayFlip - Monitor hilang",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            continueIfMissing = answer == DialogResult.Yes;
            if (!continueIfMissing)
            {
                ShowBalloon("Terapkan dibatalkan karena monitor hilang.");
                return;
            }
        }

        var (ok, msg) = _profiles.TryApply(name, continueIfMissing);
        ShowBalloon(ok ? $"Profil diterapkan: {name}" : $"Terapkan gagal: {msg}");
        _lastSignature = string.Empty;
        await Task.CompletedTask;
    }

    private void ShowBalloon(string text)
    {
        if (_disposed)
            return;
        _icon.BalloonTipTitle = "DisplayFlip";
        _icon.BalloonTipText = text;
        _icon.ShowBalloonTip(2500);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _disposed = true;
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            _icon.Visible = false;
            _icon.Dispose();
            _menu.Dispose();
            _primaryMenu.Dispose();
            _applyMenu.Dispose();
        }
        base.Dispose(disposing);
    }
}
