using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using DisplayFlip.Core;
using DisplayFlip.UI.Helpers;
using Wpf.Ui.Controls;
using WpfMenuItem = Wpf.Ui.Controls.MenuItem;

namespace DisplayFlip.UI.Services;

/// <summary>
/// Pengelola System Tray Icon modern untuk DisplayFlip.
/// Mendukung Fluent Context Menu, balloon tips, dan background residency.
/// </summary>
public sealed class TrayIconManager : IDisposable
{
    private const int NIM_ADD = 0x00000000;
    private const int NIM_MODIFY = 0x00000001;
    private const int NIM_DELETE = 0x00000002;

    private const int NIF_MESSAGE = 0x00000001;
    private const int NIF_ICON = 0x00000002;
    private const int NIF_TIP = 0x00000004;
    private const int NIF_INFO = 0x00000010;

    private const int NIIF_INFO = 0x00000001;

    private const int WM_USER = 0x0400;
    private const int WM_TRAY = WM_USER + 102;
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_RBUTTONUP = 0x0205;
    private const int WM_CONTEXTMENU = 0x007B;
    private const int NIN_SELECT = 0x0400;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(int dwMessage, ref NOTIFYICONDATA lpData);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public uint dwState;
        public uint dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public uint uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    private readonly HwndSource _source;
    private readonly ProfileStore _store;
    private NOTIFYICONDATA _nid;
    private ContextMenu? _contextMenu;
    private bool _isCreated;
    private bool _disposed;

    public Action? ToggleOrRestoreWindowAction { get; set; }
    public Action? OpenSettingsAction { get; set; }
    public Action? ExitApplicationAction { get; set; }

    public TrayIconManager(ProfileStore? store = null)
    {
        _store = store ?? new ProfileStore();

        var parameters = new HwndSourceParameters("DisplayFlipTrayListener")
        {
            Width = 0,
            Height = 0,
            WindowStyle = 0
        };

        _source = new HwndSource(parameters);
        _source.AddHook(HwndHook);

        InitializeNotifyIcon();
    }

    private void InitializeNotifyIcon()
    {
        IntPtr hIcon = IconHelper.CreateNativeIconHandle(16);

        _nid = new NOTIFYICONDATA
        {
            cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = _source.Handle,
            uID = 1001,
            uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
            uCallbackMessage = WM_TRAY,
            hIcon = hIcon,
            szTip = "DisplayFlip — Multi-Monitor Manager",
            szInfo = string.Empty,
            szInfoTitle = string.Empty
        };

        _isCreated = Shell_NotifyIcon(NIM_ADD, ref _nid);
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_TRAY)
        {
            int mouseMsg = lParam.ToInt32();
            if (mouseMsg is WM_LBUTTONUP or NIN_SELECT)
            {
                handled = true;
                Application.Current?.Dispatcher.BeginInvoke(() =>
                {
                    ToggleOrRestoreWindowAction?.Invoke();
                });
            }
            else if (mouseMsg is WM_RBUTTONUP or WM_CONTEXTMENU)
            {
                handled = true;
                Application.Current?.Dispatcher.BeginInvoke(() =>
                {
                    ShowFluentContextMenu();
                });
            }
        }

        return IntPtr.Zero;
    }

    /// <summary>
    /// Menampilkan Fluent Context Menu di koordinat kursor mouse.
    /// </summary>
    public void ShowFluentContextMenu()
    {
        if (_disposed)
            return;

        SetForegroundWindow(_source.Handle);

        _contextMenu = BuildFluentContextMenu();
        _contextMenu.Placement = PlacementMode.MousePoint;
        _contextMenu.IsOpen = true;
    }

    private ContextMenu BuildFluentContextMenu()
    {
        var menu = new ContextMenu();
        var actions = DisplayActionService.Instance;

        // 1. ⚡ Tukar Kiri-Kanan (Quick Swap)
        var swapItem = new WpfMenuItem
        {
            Header = "Tukar Kiri-Kanan (Quick Swap)",
            Icon = new SymbolIcon { Symbol = SymbolRegular.ArrowSwap24 }
        };
        swapItem.Click += async (_, _) =>
        {
            var (ok, msg) = await actions.QuickSwapAsync();
            ShowNotification("DisplayFlip", msg);
        };
        menu.Items.Add(swapItem);

        // 2. 👁 Identifikasi Layar
        var identifyItem = new WpfMenuItem
        {
            Header = "Identifikasi Layar",
            Icon = new SymbolIcon { Symbol = SymbolRegular.Eye24 }
        };
        identifyItem.Click += (_, _) => actions.IdentifyMonitors();
        menu.Items.Add(identifyItem);

        // 3. 🖥 Multi-Layar (Submenu)
        var projectionParent = new WpfMenuItem
        {
            Header = "Multi-Layar",
            Icon = new SymbolIcon { Symbol = SymbolRegular.Desktop24 }
        };

        var solo1Item = new WpfMenuItem
        {
            Header = "Hanya Layar 1",
            Icon = new SymbolIcon { Symbol = SymbolRegular.Desktop16 }
        };
        solo1Item.Click += (_, _) =>
        {
            var (ok, msg) = actions.TriggerSolo(1);
            ShowNotification("DisplayFlip", msg);
        };
        projectionParent.Items.Add(solo1Item);

        var solo2Item = new WpfMenuItem
        {
            Header = "Hanya Layar 2",
            Icon = new SymbolIcon { Symbol = SymbolRegular.Desktop16 }
        };
        solo2Item.Click += (_, _) =>
        {
            var (ok, msg) = actions.TriggerSolo(2);
            ShowNotification("DisplayFlip", msg);
        };
        projectionParent.Items.Add(solo2Item);

        var extendItem = new WpfMenuItem
        {
            Header = "Bentangkan / Extend",
            Icon = new SymbolIcon { Symbol = SymbolRegular.ArrowExpandAll24 }
        };
        extendItem.Click += (_, _) =>
        {
            var (ok, msg) = actions.TriggerProjection("extend");
            ShowNotification("DisplayFlip", msg);
        };
        projectionParent.Items.Add(extendItem);

        menu.Items.Add(projectionParent);

        // 4. 📁 Profil Tampilan (Submenu Dinamis)
        var profileParent = new WpfMenuItem
        {
            Header = "Profil Tampilan",
            Icon = new SymbolIcon { Symbol = SymbolRegular.Folder24 }
        };

        var profiles = _store.Load().Profiles;
        if (profiles.Count == 0)
        {
            var emptyItem = new WpfMenuItem
            {
                Header = "(Belum ada profil tersimpan)",
                IsEnabled = false
            };
            profileParent.Items.Add(emptyItem);
        }
        else
        {
            foreach (var profile in profiles)
            {
                string profName = profile.Name;
                var pItem = new WpfMenuItem
                {
                    Header = profName,
                    InputGestureText = profile.Shortcut,
                    Icon = new SymbolIcon { Symbol = SymbolRegular.CheckmarkCircle24 }
                };
                pItem.Click += (_, _) =>
                {
                    var (ok, msg) = actions.ApplyProfile(profName);
                    ShowNotification("DisplayFlip", ok ? $"Profil '{profName}' diterapkan." : msg);
                };
                profileParent.Items.Add(pItem);
            }
        }
        menu.Items.Add(profileParent);

        // Separator
        menu.Items.Add(new Separator());

        // 5. ⚙️ Buka Pengaturan
        var settingsItem = new WpfMenuItem
        {
            Header = "Buka Pengaturan",
            Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 }
        };
        settingsItem.Click += (_, _) => OpenSettingsAction?.Invoke();
        menu.Items.Add(settingsItem);

        // 6. ❌ Keluar
        var exitItem = new WpfMenuItem
        {
            Header = "Keluar",
            Icon = new SymbolIcon { Symbol = SymbolRegular.Dismiss24 }
        };
        exitItem.Click += (_, _) => ExitApplicationAction?.Invoke();
        menu.Items.Add(exitItem);

        return menu;
    }

    /// <summary>
    /// Menampilkan notifikasi balon native Windows dari tray icon.
    /// </summary>
    public void ShowNotification(string title, string message)
    {
        if (_disposed || !_isCreated)
            return;

        try
        {
            _nid.uFlags |= NIF_INFO;
            _nid.szInfoTitle = title ?? "DisplayFlip";
            _nid.szInfo = message ?? string.Empty;
            _nid.dwInfoFlags = NIIF_INFO;

            Shell_NotifyIcon(NIM_MODIFY, ref _nid);
        }
        catch
        {
            // Mengabaikan error notifikasi
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_isCreated)
        {
            Shell_NotifyIcon(NIM_DELETE, ref _nid);
            _isCreated = false;
        }

        _source.RemoveHook(HwndHook);
        _source.Dispose();
    }
}
