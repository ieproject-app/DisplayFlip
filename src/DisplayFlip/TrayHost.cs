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
    private readonly FluentMenu _projectionMenu;
    private readonly FluentMenuItem _mirrorItem;
    private readonly FluentMenuItem _projectionParent;
    private readonly FluentMenuItem _primaryParent;
    private readonly FluentMenuItem _applyParent;
    private readonly FluentMenuItem _startupItem;
    private readonly GlobalHotkeyHost _hotkeyHost;
    private FluentSettingsForm? _settingsForm;
    private bool _disposed;
    private bool _isMenuOpen;
    private bool _updatingStartup;
    private string _lastSignature = string.Empty;

    public TrayHost(bool showSettingsOnLaunch = false)
    {
        _adapter = new CcdTopologyAdapter();
        _store = new ProfileStore();
        _profiles = new ProfileService(_adapter, _store);
        _hotkeyHost = new GlobalHotkeyHost();

        _menu = new FluentMenu(FluentTheme.FlyoutWidth);
        _primaryMenu = new FluentMenu(FluentTheme.SubmenuWidth);
        _applyMenu = new FluentMenu(FluentTheme.SubmenuWidth);
        _projectionMenu = new FluentMenu(FluentTheme.SubmenuWidth);

        _mirrorItem = Row("Tukar kiri-kanan", FluentIcons.Swap, FluentTheme.FlyoutWidth);
        _mirrorItem.Click += async (_, _) => await OnMirrorClickedAsync();
        _menu.AddItem(_mirrorItem);

        var identifyItem = Row("Identifikasi layar", FluentIcons.Identify, FluentTheme.FlyoutWidth);
        identifyItem.Click += (_, _) => TriggerIdentifyMonitors();
        _menu.AddItem(identifyItem);

        _projectionParent = Row("Multi-layar", FluentIcons.Project, FluentTheme.FlyoutWidth);
        _projectionParent.DropDown = _projectionMenu;
        _menu.AddItem(_projectionParent);
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

        var settingsItem = Row("Pengaturan...", FluentIcons.Settings, FluentTheme.FlyoutWidth);
        settingsItem.Click += (_, _) => ShowSettings();
        _menu.AddItem(settingsItem);

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
            Icon = LoadAppIcon(GetSettings().IconStyle),
            Visible = true,
            ContextMenuStrip = _menu
        };
        _icon.DoubleClick += (_, _) => ShowSettings();
        _icon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
                ShowSettings();
        };

        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        ReloadHotkeys();
        RefreshMenu(force: true);
        if (showSettingsOnLaunch)
            ShowSettings();
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
        void Sync()
        {
            RefreshMenu(force: true);
            if (_settingsForm is { IsDisposed: false, Visible: true })
                _settingsForm.RefreshData();
        }
        if (_menu.InvokeRequired)
            _menu.BeginInvoke(new Action(Sync));
        else
            Sync();
    }

    public void ShowSettings()
    {
        if (_disposed)
            return;
        if (_settingsForm == null || _settingsForm.IsDisposed)
            _settingsForm = new FluentSettingsForm(this, _icon.Icon);
        _settingsForm.RefreshData();
        if (_settingsForm.WindowState == FormWindowState.Minimized)
            _settingsForm.WindowState = FormWindowState.Normal;
        if (!_settingsForm.Visible)
            _settingsForm.Show();
        _settingsForm.Activate();
    }

    internal string ProfilesFilePath => _store.FilePath;

    internal List<DisplayMonitor> GetCurrentMonitors() => _adapter.ReadTopologies(out _, out _);

    internal List<(int Width, int Height)> GetSupportedResolutions(DisplayMonitor monitor)
        => _adapter.GetSupportedResolutions(monitor);

    internal ProfileFile GetSettings()
    {
        try { return _store.Load(); }
        catch { return new ProfileFile(); }
    }

    internal List<LayoutProfile> GetAllProfiles()
    {
        try { return _store.Load().Profiles; }
        catch { return new List<LayoutProfile>(); }
    }

    internal void ReloadHotkeys()
    {
        try
        {
            _hotkeyHost.ClearAll();
            var cfg = GetSettings();
            if (!string.IsNullOrWhiteSpace(cfg.MirrorShortcut))
                _hotkeyHost.Register(cfg.MirrorShortcut, () => _ = OnMirrorClickedAsync());
            if (!string.IsNullOrWhiteSpace(cfg.CyclePrimaryShortcut))
                _hotkeyHost.Register(cfg.CyclePrimaryShortcut, () =>
                {
                    TriggerCyclePrimary();
                    if (_settingsForm is { IsDisposed: false, Visible: true })
                        _settingsForm.RefreshData();
                });
            if (!string.IsNullOrWhiteSpace(cfg.IdentifyShortcut))
                _hotkeyHost.Register(cfg.IdentifyShortcut, TriggerIdentifyMonitors);
            if (!string.IsNullOrWhiteSpace(cfg.Solo1Shortcut))
                _hotkeyHost.Register(cfg.Solo1Shortcut, () =>
                {
                    TriggerSoloDisplay(1);
                    if (_settingsForm is { IsDisposed: false, Visible: true })
                        _settingsForm.RefreshData();
                });
            if (!string.IsNullOrWhiteSpace(cfg.Solo2Shortcut))
                _hotkeyHost.Register(cfg.Solo2Shortcut, () =>
                {
                    TriggerSoloDisplay(2);
                    if (_settingsForm is { IsDisposed: false, Visible: true })
                        _settingsForm.RefreshData();
                });
            if (!string.IsNullOrWhiteSpace(cfg.ExtendShortcut))
                _hotkeyHost.Register(cfg.ExtendShortcut, () =>
                {
                    TriggerProjectionMode("extend");
                    if (_settingsForm is { IsDisposed: false, Visible: true })
                        _settingsForm.RefreshData();
                });

            foreach (var p in cfg.Profiles)
            {
                if (!string.IsNullOrWhiteSpace(p.Shortcut))
                {
                    string captured = p.Name;
                    _hotkeyHost.Register(p.Shortcut, () => _ = OnApplyClickedAsync(captured));
                }
            }
        }
        catch
        {
            // Keep tray running even if hotkey registration fails.
        }
    }

    internal void SetGlobalShortcut(string keyKind, string shortcut)
    {
        string clean = shortcut.Trim();
        _store.UpdateSettings(cfg =>
        {
            string mirror = keyKind == "mirror" ? clean
                : (string.Equals(cfg.MirrorShortcut, clean, StringComparison.OrdinalIgnoreCase) ? string.Empty : cfg.MirrorShortcut);
            string primary = keyKind == "primary" ? clean
                : (string.Equals(cfg.CyclePrimaryShortcut, clean, StringComparison.OrdinalIgnoreCase) ? string.Empty : cfg.CyclePrimaryShortcut);
            string identify = keyKind == "identify" ? clean
                : (string.Equals(cfg.IdentifyShortcut, clean, StringComparison.OrdinalIgnoreCase) ? string.Empty : cfg.IdentifyShortcut);
            string solo1 = keyKind == "solo1" ? clean
                : (string.Equals(cfg.Solo1Shortcut, clean, StringComparison.OrdinalIgnoreCase) ? string.Empty : cfg.Solo1Shortcut);
            string solo2 = keyKind == "solo2" ? clean
                : (string.Equals(cfg.Solo2Shortcut, clean, StringComparison.OrdinalIgnoreCase) ? string.Empty : cfg.Solo2Shortcut);
            string extend = keyKind == "extend" ? clean
                : (string.Equals(cfg.ExtendShortcut, clean, StringComparison.OrdinalIgnoreCase) ? string.Empty : cfg.ExtendShortcut);
            var profiles = string.IsNullOrEmpty(clean)
                ? cfg.Profiles
                : cfg.Profiles.Select(p =>
                    string.Equals(p.Shortcut, clean, StringComparison.OrdinalIgnoreCase)
                        ? p with { Shortcut = string.Empty }
                        : p).ToList();
            return cfg with
            {
                MirrorShortcut = mirror,
                CyclePrimaryShortcut = primary,
                IdentifyShortcut = identify,
                Solo1Shortcut = solo1,
                Solo2Shortcut = solo2,
                ExtendShortcut = extend,
                Profiles = profiles
            };
        });
        ReloadHotkeys();
        _lastSignature = string.Empty;
        RefreshMenu(force: true);
    }

    internal void SetProfileShortcut(string profileName, string shortcut)
    {
        var prof = _store.Find(profileName);
        if (prof == null)
            return;
        string clean = shortcut.Trim();
        if (!string.IsNullOrEmpty(clean))
        {
            _store.UpdateSettings(cfg => cfg with
            {
                MirrorShortcut = string.Equals(cfg.MirrorShortcut, clean, StringComparison.OrdinalIgnoreCase) ? string.Empty : cfg.MirrorShortcut,
                CyclePrimaryShortcut = string.Equals(cfg.CyclePrimaryShortcut, clean, StringComparison.OrdinalIgnoreCase) ? string.Empty : cfg.CyclePrimaryShortcut,
                IdentifyShortcut = string.Equals(cfg.IdentifyShortcut, clean, StringComparison.OrdinalIgnoreCase) ? string.Empty : cfg.IdentifyShortcut,
                Solo1Shortcut = string.Equals(cfg.Solo1Shortcut, clean, StringComparison.OrdinalIgnoreCase) ? string.Empty : cfg.Solo1Shortcut,
                Solo2Shortcut = string.Equals(cfg.Solo2Shortcut, clean, StringComparison.OrdinalIgnoreCase) ? string.Empty : cfg.Solo2Shortcut,
                ExtendShortcut = string.Equals(cfg.ExtendShortcut, clean, StringComparison.OrdinalIgnoreCase) ? string.Empty : cfg.ExtendShortcut,
                Profiles = cfg.Profiles.Select(p =>
                    !string.Equals(p.Name, profileName, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(p.Shortcut, clean, StringComparison.OrdinalIgnoreCase)
                        ? p with { Shortcut = string.Empty }
                        : p).ToList()
            });
        }
        _store.Save(prof with { Shortcut = clean });
        ReloadHotkeys();
        _lastSignature = string.Empty;
        RefreshMenu(force: true);
    }

    internal void SetAutoApplyDrag(bool enabled)
    {
        _store.UpdateSettings(cfg => cfg with { AutoApplyDrag = enabled });
    }

    internal void SetIconStyle(string style)
    {
        string normalized = string.IsNullOrWhiteSpace(style) ? "fluent" : style.Trim().ToLowerInvariant();
        _store.UpdateSettings(cfg => cfg with { IconStyle = normalized });
        try
        {
            var oldIcon = _icon.Icon;
            var newIcon = FluentIcons.CreateModernAppIcon(normalized, 32);
            _icon.Icon = newIcon;
            if (_settingsForm is { IsDisposed: false })
                _settingsForm.UpdateAppIcon(newIcon);
            if (oldIcon != null && !ReferenceEquals(oldIcon, SystemIcons.Application))
                oldIcon.Dispose();
        }
        catch
        {
            // Fallback to existing icon.
        }
    }

    internal void TriggerIdentifyMonitors()
    {
        try
        {
            var monitors = GetCurrentMonitors();
            IdentifyOverlayForm.ShowForMonitors(monitors);
        }
        catch
        {
            // Ignore overlay error.
        }
    }

    internal string TriggerCyclePrimary()
    {
        var monitors = GetCurrentMonitors();
        if (monitors.Count < 2)
            return "Perlu minimal 2 monitor untuk mengganti primary.";
        var ordered = monitors.OrderBy(m => m.Position.X).ThenBy(m => m.Position.Y).ToList();
        int curIdx = ordered.FindIndex(m => m.Position.X == 0 && m.Position.Y == 0);
        int nextIdx = (curIdx + 1) % ordered.Count;
        var target = ordered[nextIdx];
        return TriggerSetPrimary(target.StableKey, target.FriendlyName);
    }

    internal string TriggerProjectionMode(string modeSwitch)
    {
        var (ok, msg) = ProjectionDispatcher.Dispatch(modeSwitch);
        ShowBalloon(msg);
        return msg;
    }

    internal string TriggerSoloDisplay(int displayIndex)
    {
        var (ok, msg) = ProjectionDispatcher.TriggerSolo(displayIndex);
        ShowBalloon(msg);
        return msg;
    }

    internal void SetStartupEnabled(bool enabled)
    {
        StartupRegistry.SetEnabled(enabled);
        _updatingStartup = true;
        _startupItem.Checked = StartupRegistry.IsEnabled();
        _updatingStartup = false;
        _lastSignature = string.Empty;
    }

    private void OnStartupClicked()
    {
        SetStartupEnabled(!_startupItem.Checked);
    }

    private static Icon LoadAppIcon(string? style = null)
    {
        if (!string.IsNullOrWhiteSpace(style))
        {
            try
            {
                return FluentIcons.CreateModernAppIcon(style, 32);
            }
            catch
            {
                // Fall through to embedded/file icon.
            }
        }
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
            var cfg = GetSettings();
            var profiles = cfg.Profiles;
            var names = profiles.Select(p => p.Name).ToList();
            var startup = StartupRegistry.IsEnabled();
            var signature = string.Join("|", monitors.Select(m => $"{m.StableKey}@{m.Position.X},{m.Position.Y},{m.Position.Width}x{m.Position.Height}"))
                            + "#" + string.Join(",", profiles.Select(p => $"{p.Name}:{p.Shortcut}"))
                            + "#" + startup + "#" + cfg.MirrorShortcut + "#" + cfg.Solo1Shortcut + "#" + cfg.Solo2Shortcut + "#" + cfg.ExtendShortcut;
            if (!force && signature == _lastSignature)
                return;
            _lastSignature = signature;

            _mirrorItem.Enabled = monitors.Count >= 2;
            _mirrorItem.ShortcutHint = cfg.MirrorShortcut;
            // Keputusan owner 8: tanpa item Mirror baru; item yang ada disabled + subteks.
            // Baris 44px saat ada subteks agar dua baris tidak bertumpuk.
            _mirrorItem.SecondaryLine = monitors.Count < 2 ? "Perlu 2+ monitor" : null;
            _mirrorItem.Height = _mirrorItem.SecondaryLine != null
                ? FluentTheme.MonitorRowHeight
                : FluentTheme.ItemHeight;

            _projectionMenu.ClearItems();
            bool isMultiActive = monitors.Count >= 2;
            bool isClone = false;
            if (isMultiActive)
            {
                var first = monitors[0];
                isClone = monitors.Skip(1).All(m => m.Position.X == first.Position.X && m.Position.Y == first.Position.Y);
            }
            bool isExtend = isMultiActive && !isClone;
            bool isSolo1 = false;
            bool isSolo2 = false;
            if (monitors.Count == 1)
            {
                var single = monitors[0];
                int dispNum = int.TryParse(IdentifyOverlayForm.ExtractDisplayNumber(single.StableKey, single.FriendlyName, 1), out int dn) ? dn : 1;
                if (dispNum == 2 || single.FriendlyName.Contains("DISPLAY2", StringComparison.OrdinalIgnoreCase) || single.StableKey.Contains("DISPLAY2", StringComparison.OrdinalIgnoreCase))
                    isSolo2 = true;
                else
                    isSolo1 = true;
            }

            var extendItem = Row("Perluas (Extend)", null, FluentTheme.SubmenuWidth);
            extendItem.Radio = true;
            extendItem.RadioChecked = isExtend;
            extendItem.ShortcutHint = cfg.ExtendShortcut;
            extendItem.Click += (_, _) =>
            {
                TriggerProjectionMode("extend");
                if (_settingsForm is { IsDisposed: false, Visible: true })
                    _settingsForm.RefreshData();
            };
            _projectionMenu.AddItem(extendItem);

            var solo1Item = Row("Hanya Layar 1", null, FluentTheme.SubmenuWidth);
            solo1Item.Radio = true;
            solo1Item.RadioChecked = isSolo1;
            solo1Item.ShortcutHint = cfg.Solo1Shortcut;
            solo1Item.Click += (_, _) =>
            {
                TriggerSoloDisplay(1);
                if (_settingsForm is { IsDisposed: false, Visible: true })
                    _settingsForm.RefreshData();
            };
            _projectionMenu.AddItem(solo1Item);

            var solo2Item = Row("Hanya Layar 2", null, FluentTheme.SubmenuWidth);
            solo2Item.Radio = true;
            solo2Item.RadioChecked = isSolo2;
            solo2Item.ShortcutHint = cfg.Solo2Shortcut;
            solo2Item.Click += (_, _) =>
            {
                TriggerSoloDisplay(2);
                if (_settingsForm is { IsDisposed: false, Visible: true })
                    _settingsForm.RefreshData();
            };
            _projectionMenu.AddItem(solo2Item);

            var cloneItem = Row("Duplikat (Clone)", null, FluentTheme.SubmenuWidth);
            cloneItem.Radio = true;
            cloneItem.RadioChecked = isClone;
            cloneItem.Click += (_, _) =>
            {
                TriggerProjectionMode("clone");
                if (_settingsForm is { IsDisposed: false, Visible: true })
                    _settingsForm.RefreshData();
            };
            _projectionMenu.AddItem(cloneItem);

            _projectionParent.SecondaryLine = isExtend ? "Perluas" : (isSolo1 ? "Layar 1" : (isSolo2 ? "Layar 2" : (isClone ? "Duplikat" : null)));
            _projectionParent.Height = _projectionParent.SecondaryLine != null ? FluentTheme.MonitorRowHeight : FluentTheme.ItemHeight;

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
            if (profiles.Count == 0)
            {
                var empty = Row("(belum ada profil)", null, FluentTheme.SubmenuWidth);
                empty.Enabled = false;
                _applyMenu.AddItem(empty);
                _applyParent.Enabled = false;
            }
            else
            {
                _applyParent.Enabled = true;
                foreach (var p in profiles)
                {
                    var captured = p.Name;
                    var item = Row(captured, null, FluentTheme.SubmenuWidth);
                    item.ShortcutHint = p.Shortcut;
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

    internal async Task<string> TriggerMirrorAsync()
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
        return message;
    }

    private async Task OnMirrorClickedAsync()
    {
        await TriggerMirrorAsync();
        if (_settingsForm is { IsDisposed: false, Visible: true })
            _settingsForm.RefreshData();
    }

    internal string TriggerSetPrimary(string stableKey, string friendlyName)
    {
        var monitors = _adapter.ReadTopologies(out var paths, out var modes);
        var currentPrimary = monitors.FirstOrDefault(m => m.Position.X == 0 && m.Position.Y == 0);
        if (currentPrimary != null && currentPrimary.StableKey == stableKey)
        {
            var msgSame = $"{friendlyName} sudah menjadi primary.";
            ShowBalloon(msgSame);
            return msgSame;
        }

        var desired = DisplayTopology.SetPrimary(monitors, stableKey);
        if (!DisplayTopology.IsLegal(desired))
            desired = DisplayTopology.Legalize(desired);

        string message;
        if (paths.Count == 0 || modes.Count == 0)
        {
            var (fbOk, fbMsg) = _adapter.ApplyViaFallback(desired);
            message = fbOk ? $"{friendlyName} kini menjadi primary. {fbMsg}" : $"Gagal jadikan primary: {fbMsg}";
            ShowBalloon(message);
            _lastSignature = string.Empty;
            return message;
        }

        var (paths2, modes2, built) = _adapter.BuildTopology(desired);
        if (!built && paths2.Count > 0)
        {
            message = "Gagal membangun susunan primary.";
            ShowBalloon(message);
            return message;
        }
        var (validOk, validMsg) = _adapter.Validate(paths2, modes2);
        if (!validOk)
        {
            message = $"Validasi gagal: {validMsg}";
            ShowBalloon(message);
            return message;
        }
        var (applyOk, applyMsg) = _adapter.Apply(paths2, modes2);
        message = applyOk ? $"{friendlyName} kini menjadi primary." : $"Terapkan gagal: {applyMsg}";
        ShowBalloon(message);
        _lastSignature = string.Empty;
        return message;
    }

    private void OnSetPrimary(string stableKey, string friendlyName)
    {
        TriggerSetPrimary(stableKey, friendlyName);
        if (_settingsForm is { IsDisposed: false, Visible: true })
            _settingsForm.RefreshData();
    }

    internal async Task<(bool ok, string message)> TriggerApplyArrangementAsync(
        IReadOnlyList<DisplayMonitor> desired,
        string? primaryStableKey = null)
    {
        var result = await Task.Run(() => _adapter.ApplyArrangement(desired, primaryStableKey));
        var msg = result.ok
            ? "Susunan monitor berhasil diterapkan."
            : $"Gagal menerapkan susunan: {result.message}";
        ShowBalloon(msg);
        _lastSignature = string.Empty;
        RefreshMenu(force: false);
        return (result.ok, msg);
    }

    internal bool TriggerSaveNewProfile(
        out string? savedName,
        IReadOnlyList<DisplayMonitor>? customMonitors = null,
        string? customPrimaryKey = null,
        string? editingOriginalName = null)
    {
        var monitors = customMonitors ?? GetCurrentMonitors();
        var existingNames = _store.ListNames();
        var existingProfile = !string.IsNullOrWhiteSpace(editingOriginalName)
            ? _store.Find(editingOriginalName)
            : null;
        string defaultName = existingProfile?.Name ?? SuggestNewProfileName(existingNames);
        string initialShortcut = existingProfile?.Shortcut ?? string.Empty;
        string? initialPrimary = customPrimaryKey
            ?? existingProfile?.PrimaryStableKey
            ?? monitors.FirstOrDefault(m => m.Position.X == 0 && m.Position.Y == 0)?.StableKey;

        var setup = SaveProfileDialog.PromptSetup(
            existingNames,
            defaultName,
            initialShortcut,
            monitors,
            initialPrimary,
            editingOriginalName);

        if (setup == null || string.IsNullOrWhiteSpace(setup.Value.Name))
        {
            savedName = null;
            return false;
        }
        try
        {
            savedName = setup.Value.Name.Trim();
            if (!string.IsNullOrWhiteSpace(editingOriginalName)
                && !string.Equals(editingOriginalName, savedName, StringComparison.OrdinalIgnoreCase))
            {
                _store.Delete(editingOriginalName);
            }
            _profiles.SaveArrangementAs(savedName, monitors, setup.Value.PrimaryKey, setup.Value.Shortcut);
            if (!string.IsNullOrWhiteSpace(setup.Value.Shortcut))
                SetProfileShortcut(savedName, setup.Value.Shortcut);
            else
                ReloadHotkeys();
            ShowBalloon($"Profil disimpan: {savedName}");
            _lastSignature = string.Empty;
            RefreshMenu(force: false);
            return true;
        }
        catch (Exception ex)
        {
            savedName = null;
            ShowBalloon($"Simpan gagal: {ex.Message}");
            return false;
        }
    }

    internal bool TriggerOverwriteProfile(
        string name,
        out string message,
        IReadOnlyList<DisplayMonitor>? customMonitors = null,
        string? customPrimaryKey = null)
    {
        try
        {
            if (customMonitors is { Count: > 0 })
            {
                var existing = _store.Find(name);
                _profiles.SaveArrangementAs(name, customMonitors, customPrimaryKey, existing?.Shortcut);
            }
            else
            {
                _profiles.SaveCurrentAs(name);
            }
            ReloadHotkeys();
            message = $"Profil diperbarui: {name}";
            ShowBalloon(message);
            _lastSignature = string.Empty;
            RefreshMenu(force: false);
            return true;
        }
        catch (Exception ex)
        {
            message = $"Perbarui gagal: {ex.Message}";
            ShowBalloon(message);
            return false;
        }
    }

    internal bool TriggerDeleteProfile(string name)
    {
        var ok = _store.Delete(name);
        if (ok)
        {
            ReloadHotkeys();
            ShowBalloon($"Profil dihapus: {name}");
            _lastSignature = string.Empty;
            RefreshMenu(force: false);
        }
        return ok;
    }

    private static string SuggestNewProfileName(IReadOnlyList<string> existingNames)
    {
        var set = new HashSet<string>(existingNames, StringComparer.OrdinalIgnoreCase);
        string[] candidates = { "Primary Kanan", "Primary Kiri", "Mode Kerja", "Mode Presentasi" };
        foreach (var c in candidates)
        {
            if (!set.Contains(c))
                return c;
        }
        for (int i = 1; i <= 99; i++)
        {
            string numbered = $"Profil {i}";
            if (!set.Contains(numbered))
                return numbered;
        }
        return "Profil Baru";
    }

    private void OnSaveClicked()
    {
        if (TriggerSaveNewProfile(out _) && _settingsForm is { IsDisposed: false, Visible: true })
            _settingsForm.RefreshData();
    }

    internal async Task<(bool ok, string message)> TriggerApplyProfileAsync(string name)
    {
        var profile = _store.Find(name);
        if (profile == null)
        {
            var notFoundMsg = $"Profil tidak ditemukan: {name}";
            ShowBalloon(notFoundMsg);
            return (false, notFoundMsg);
        }
        var connected = _adapter.ReadTopologies(out _, out _);
        var (remaining, missing) = ProfilePolicy.SplitPresent(profile, connected.Select(c => c.StableKey));

        var continueIfMissing = false;
        if (missing.Count > 0)
        {
            var missingNames = profile.Monitors
                .Where(m => missing.Contains(m.StableKey))
                .Select(m => string.IsNullOrWhiteSpace(m.FriendlyName) ? m.StableKey : m.FriendlyName)
                .ToList();
            var remainingNames = remaining
                .Select(m => string.IsNullOrWhiteSpace(m.FriendlyName) ? m.StableKey : m.FriendlyName)
                .ToList();

            continueIfMissing = MissingMonitorDialog.Confirm(
                name, profile.Monitors.Count, missingNames, remainingNames);
            if (!continueIfMissing)
            {
                const string abortMsg = "Terapkan dibatalkan karena monitor hilang.";
                ShowBalloon(abortMsg);
                return (false, abortMsg);
            }
        }

        var (ok, msg) = _profiles.TryApply(name, continueIfMissing);
        var outMsg = ok ? $"Profil diterapkan: {name}" : $"Terapkan gagal: {msg}";
        ShowBalloon(outMsg);
        _lastSignature = string.Empty;
        RefreshMenu(force: false);
        await Task.CompletedTask;
        return (ok, outMsg);
    }

    private async Task OnApplyClickedAsync(string name)
    {
        await TriggerApplyProfileAsync(name);
        if (_settingsForm is { IsDisposed: false, Visible: true })
            _settingsForm.RefreshData();
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
            if (_settingsForm is { IsDisposed: false })
            {
                _settingsForm.ForceClose();
                _settingsForm.Dispose();
            }
            _hotkeyHost.Dispose();
            _icon.Visible = false;
            _icon.Dispose();
            _menu.Dispose();
            _primaryMenu.Dispose();
            _applyMenu.Dispose();
            _projectionMenu.Dispose();
        }
        base.Dispose(disposing);
    }
}
