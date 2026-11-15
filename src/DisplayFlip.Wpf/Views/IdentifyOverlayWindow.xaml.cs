using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using DisplayFlip.Core;
using DisplayFlip.UI.Helpers;
using DisplayFlip.UI.Services;

namespace DisplayFlip.UI.Views;

/// <summary>
/// Borderless, non-activating overlay window displaying monitor identification number and name
/// in the bottom-left corner of each physical monitor (matching Windows 11 and WinForms reference).
/// </summary>
public partial class IdentifyOverlayWindow : Window
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_TRANSPARENT = 0x00000020;

    private static readonly IntPtr HWND_TOPMOST = new(-1);
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
    private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

    private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
    {
        return IntPtr.Size == 8 ? GetWindowLongPtr64(hWnd, nIndex) : (IntPtr)GetWindowLong32(hWnd, nIndex);
    }

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        return IntPtr.Size == 8 ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong) : (IntPtr)SetWindowLong32(hWnd, nIndex, (int)dwNewLong);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private static readonly List<IdentifyOverlayWindow> ActiveOverlays = new();
    private static readonly object Gate = new();

    private readonly Position _monitorPos;

    public IdentifyOverlayWindow(Position monitorPos, string displayNumber, string subtitle)
    {
        _monitorPos = monitorPos;
        InitializeComponent();

        DisplayNumberText.Text = displayNumber;
        DisplaySubtitleText.Text = subtitle;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var helper = new WindowInteropHelper(this);
        IntPtr hwnd = helper.Handle;

        // Apply non-activating, toolwindow (no taskbar/alt-tab), and click-through styles
        var currentExStyle = (long)GetWindowLongPtr(hwnd, GWL_EXSTYLE);
        var newExStyle = currentExStyle | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW | WS_EX_TRANSPARENT;
        SetWindowLongPtr(hwnd, GWL_EXSTYLE, (IntPtr)newExStyle);

        // Position window directly in physical screen pixels
        var (x, y, w, h) = DisplayHelper.CalculateOverlayBounds(_monitorPos, 220, 130);
        SetWindowPos(hwnd, HWND_TOPMOST, x, y, w, h, SWP_NOACTIVATE | SWP_SHOWWINDOW);
    }

    public static void ShowForMonitors(IReadOnlyList<DisplayMonitor> monitors)
    {
        if (monitors == null || monitors.Count == 0)
            return;

        CloseAll();

        var windows = new List<IdentifyOverlayWindow>();
        for (int i = 0; i < monitors.Count; i++)
        {
            var m = monitors[i];
            string num = DisplayHelper.ExtractDisplayNumber(m.StableKey, m.FriendlyName, i + 1);
            string cleanName = DisplayHelper.CleanDeviceName(m.FriendlyName);
            bool isPrimary = m.Position.X == 0 && m.Position.Y == 0;
            string subtitle = isPrimary
                ? Loc.Instance.Format("Overlay.PrimarySuffix", cleanName)
                : cleanName;

            var win = new IdentifyOverlayWindow(m.Position, num, subtitle);
            windows.Add(win);
            win.Show();
        }

        lock (Gate)
        {
            ActiveOverlays.AddRange(windows);
        }

        _ = DismissAfterDelayAsync(windows, 2200);
    }

    public static void CloseAll()
    {
        List<IdentifyOverlayWindow> toClose;
        lock (Gate)
        {
            toClose = new List<IdentifyOverlayWindow>(ActiveOverlays);
            ActiveOverlays.Clear();
        }

        foreach (var w in toClose)
        {
            try
            {
                w.Close();
            }
            catch
            {
                // Ignore window close exceptions
            }
        }
    }

    private static async Task DismissAfterDelayAsync(List<IdentifyOverlayWindow> windows, int delayMs)
    {
        await Task.Delay(delayMs);

        foreach (var win in windows)
        {
            try
            {
                if (win.IsLoaded)
                {
                    var fadeAnim = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(200));
                    fadeAnim.Completed += (_, _) =>
                    {
                        win.Close();
                        lock (Gate)
                        {
                            ActiveOverlays.Remove(win);
                        }
                    };
                    win.BeginAnimation(OpacityProperty, fadeAnim);
                }
                else
                {
                    win.Close();
                    lock (Gate)
                    {
                        ActiveOverlays.Remove(win);
                    }
                }
            }
            catch
            {
                // Ignore exceptions during dismissal
            }
        }
    }
}
