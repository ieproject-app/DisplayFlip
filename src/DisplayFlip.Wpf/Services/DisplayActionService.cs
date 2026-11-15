using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DisplayFlip.Core;
using DisplayFlip.UI.Views;
using DisplayFlip.Windows;

namespace DisplayFlip.UI.Services;

/// <summary>
/// Layanan terpusat untuk eksekusi aksi multi-layar dari System Tray, Global Hotkeys, dan CLI.
/// </summary>
public sealed class DisplayActionService
{
    private static readonly Lazy<DisplayActionService> s_instance = new(() => new DisplayActionService());
    public static DisplayActionService Instance => s_instance.Value;

    private readonly CcdTopologyAdapter _adapter;
    private readonly ProfileStore _store;
    private readonly ProfileService _profiles;

    public CcdTopologyAdapter Adapter => _adapter;
    public ProfileStore Store => _store;
    public ProfileService Profiles => _profiles;

    public DisplayActionService(CcdTopologyAdapter? adapter = null, ProfileStore? store = null)
    {
        _adapter = adapter ?? new CcdTopologyAdapter();
        _store = store ?? new ProfileStore();
        _profiles = new ProfileService(_adapter, _store);
    }

    /// <summary>
    /// Menukar susunan posisi horizontal monitor (Mirror/Swap) secara instan.
    /// </summary>
    public async Task<(bool Success, string Message)> QuickSwapAsync()
    {
        return await Task.Run(() =>
        {
            var monitors = _adapter.ReadTopologies(out var paths, out var modes);
            if (monitors.Count < 2)
                return (false, "Perlu minimal 2 monitor aktif untuk menukar posisi.");

            var desired = DisplayTopology.Mirror(monitors);

            if (paths.Count == 0 || modes.Count == 0)
            {
                var (fbOk, fbMsg) = _adapter.ApplyViaFallback(desired);
                return fbOk
                    ? (true, "Susunan monitor berhasil ditukar (kiri-kanan).")
                    : (false, $"Gagal menukar susunan: {fbMsg}");
            }

            var (newPaths, newModes, built) = _adapter.BuildTopology(desired);
            if (!built && newPaths.Count > 0)
                return (false, "Gagal membangun susunan cermin monitor.");

            var (validOk, validMsg) = _adapter.Validate(newPaths, newModes);
            if (!validOk)
                return (false, $"Validasi topologi gagal: {validMsg}");

            var (applyOk, applyMsg) = _adapter.Apply(newPaths, newModes);
            return applyOk
                ? (true, "Susunan monitor berhasil ditukar (kiri-kanan).")
                : (false, $"Gagal menerapkan susunan: {applyMsg}");
        });
    }

    /// <summary>
    /// Memunculkan overlay identifikasi nomor layar fisik pada seluruh monitor yang aktif.
    /// </summary>
    public void IdentifyMonitors()
    {
        try
        {
            var monitors = _adapter.ReadTopologies(out _, out _);
            if (monitors.Count > 0)
            {
                IdentifyOverlayWindow.ShowForMonitors(monitors);
            }
        }
        catch
        {
            // Mengabaikan error overlay transient
        }
    }

    /// <summary>
    /// Mengganti layar utama (Primary) berurutan dari daftar monitor aktif.
    /// </summary>
    public (bool Success, string Message) CyclePrimary()
    {
        var monitors = _adapter.ReadTopologies(out var paths, out var modes);
        if (monitors.Count < 2)
            return (false, "Perlu minimal 2 monitor aktif untuk mengganti layar utama.");

        var ordered = monitors.OrderBy(m => m.Position.X).ThenBy(m => m.Position.Y).ToList();
        int curIdx = ordered.FindIndex(m => m.Position.X == 0 && m.Position.Y == 0);
        int nextIdx = (curIdx + 1) % ordered.Count;
        var target = ordered[nextIdx];

        var desired = DisplayTopology.SetPrimary(monitors, target.StableKey);
        if (!DisplayTopology.IsLegal(desired))
            desired = DisplayTopology.Legalize(desired);

        if (paths.Count == 0 || modes.Count == 0)
        {
            var (fbOk, fbMsg) = _adapter.ApplyViaFallback(desired);
            return fbOk
                ? (true, $"{target.FriendlyName} kini menjadi layar utama.")
                : (false, $"Gagal menetapkan layar utama: {fbMsg}");
        }

        var (newPaths, newModes, built) = _adapter.BuildTopology(desired);
        if (!built && newPaths.Count > 0)
            return (false, "Gagal membangun susunan topologi.");

        var (validOk, validMsg) = _adapter.Validate(newPaths, newModes);
        if (!validOk)
            return (false, $"Validasi gagal: {validMsg}");

        var (applyOk, applyMsg) = _adapter.Apply(newPaths, newModes);
        return applyOk
            ? (true, $"{target.FriendlyName} kini menjadi layar utama.")
            : (false, $"Gagal menerapkan: {applyMsg}");
    }

    /// <summary>
    /// Mengaktifkan mode layar tunggal (Solo 1 atau Solo 2).
    /// </summary>
    public (bool Success, string Message) TriggerSolo(int displayIndex)
    {
        return ProjectionDispatcher.TriggerSolo(displayIndex);
    }

    /// <summary>
    /// Mengaktifkan mode proyeksi Windows (extend, clone, internal, external).
    /// </summary>
    public (bool Success, string Message) TriggerProjection(string mode)
    {
        return ProjectionDispatcher.Dispatch(mode);
    }

    /// <summary>
    /// Menerapkan profil tata letak monitor tersimpan berdasarkan nama.
    /// </summary>
    public (bool Success, string Message) ApplyProfile(string profileName)
    {
        return _profiles.TryApply(profileName, userOptedContinueIfMissing: true);
    }
}
