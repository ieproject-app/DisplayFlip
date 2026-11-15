using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using DisplayFlip.Core;
using DisplayFlip.UI.Helpers;

namespace DisplayFlip.UI.Services;

/// <summary>
/// Engine listener pintasan keyboard global (Background Hotkey Listener)
/// berbasis Win32 RegisterHotKey dan HwndSource top-level invisible.
/// </summary>
public sealed class GlobalHotkeyManager : IDisposable
{
    private const int WM_HOTKEY = 0x0312;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private static readonly Lazy<GlobalHotkeyManager> s_instance = new(() => new GlobalHotkeyManager());
    public static GlobalHotkeyManager Instance => s_instance.Value;

    private readonly Dictionary<int, Action> _handlers = new();
    private readonly HwndSource _source;
    private readonly ProfileStore _store;
    private int _nextId = 0x2000;
    private bool _disposed;

    public IntPtr Handle => _source.Handle;
    public int RegisteredCount => _handlers.Count;

    public GlobalHotkeyManager(ProfileStore? store = null)
    {
        _store = store ?? new ProfileStore();

        var parameters = new HwndSourceParameters("DisplayFlipGlobalHotkeyListener")
        {
            Width = 0,
            Height = 0,
            WindowStyle = 0
        };

        _source = new HwndSource(parameters);
        _source.AddHook(HwndHook);
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            if (_handlers.TryGetValue(id, out var action))
            {
                try
                {
                    action();
                }
                catch
                {
                    // Jangan biarkan exception callback mematikan hook
                }
                handled = true;
            }
        }

        return IntPtr.Zero;
    }

    /// <summary>
    /// Membersihkan seluruh hotkey yang sedang terdaftar di sistem Windows.
    /// </summary>
    public void UnregisterAll()
    {
        if (_source.Handle != IntPtr.Zero)
        {
            foreach (var id in _handlers.Keys)
            {
                UnregisterHotKey(_source.Handle, id);
            }
        }

        _handlers.Clear();
    }

    /// <summary>
    /// Mendaftarkan sebuah shortcut string ke aksi callback.
    /// </summary>
    public bool Register(string? shortcut, Action callback)
    {
        if (_disposed || _source.Handle == IntPtr.Zero || callback == null)
            return false;

        if (!HotkeyInterop.TryParse(shortcut, out uint modifiers, out uint vk))
            return false;

        int id = _nextId++;
        if (RegisterHotKey(_source.Handle, id, modifiers | HotkeyInterop.MOD_NOREPEAT, vk))
        {
            _handlers[id] = callback;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Memuat ulang seluruh pintasan global dari ProfileStore dan meregistrasikannya secara dinamis.
    /// </summary>
    public void ReloadHotkeys()
    {
        if (_disposed)
            return;

        try
        {
            UnregisterAll();
            var cfg = _store.Load();
            var actions = DisplayActionService.Instance;

            // 1. Tukar Posisi (Mirror / Quick Swap)
            if (!string.IsNullOrWhiteSpace(cfg.MirrorShortcut))
            {
                Register(cfg.MirrorShortcut, () => _ = actions.QuickSwapAsync());
            }

            // 2. Ganti Layar Utama Berurutan (Cycle Primary)
            if (!string.IsNullOrWhiteSpace(cfg.CyclePrimaryShortcut))
            {
                Register(cfg.CyclePrimaryShortcut, () => actions.CyclePrimary());
            }

            // 3. Identifikasi Layar (Identify)
            if (!string.IsNullOrWhiteSpace(cfg.IdentifyShortcut))
            {
                Register(cfg.IdentifyShortcut, () => actions.IdentifyMonitors());
            }

            // 4. Hanya Layar 1 (Solo 1)
            if (!string.IsNullOrWhiteSpace(cfg.Solo1Shortcut))
            {
                Register(cfg.Solo1Shortcut, () => actions.TriggerSolo(1));
            }

            // 5. Hanya Layar 2 (Solo 2)
            if (!string.IsNullOrWhiteSpace(cfg.Solo2Shortcut))
            {
                Register(cfg.Solo2Shortcut, () => actions.TriggerSolo(2));
            }

            // 6. Bentangkan / Extend
            if (!string.IsNullOrWhiteSpace(cfg.ExtendShortcut))
            {
                Register(cfg.ExtendShortcut, () => actions.TriggerProjection("extend"));
            }

            // 7. Pintasan per-profil
            foreach (var profile in cfg.Profiles)
            {
                if (!string.IsNullOrWhiteSpace(profile.Shortcut))
                {
                    string profileName = profile.Name;
                    Register(profile.Shortcut, () => actions.ApplyProfile(profileName));
                }
            }
        }
        catch
        {
            // Jangan biarkan kesalahan I/O registri mematikan listener
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        UnregisterAll();
        _source.RemoveHook(HwndHook);
        _source.Dispose();
    }
}
