using System.Windows;
using System.Windows.Media.Animation;
using DisplayFlip.Core;
using DisplayFlip.UI.Helpers;

namespace DisplayFlip.UI.Views;

/// <summary>
/// Borderless overlay window displaying monitor identification number and name.
/// </summary>
public partial class IdentifyOverlayWindow : Window
{
    private static readonly List<IdentifyOverlayWindow> ActiveOverlays = new();
    private static readonly object Gate = new();

    public IdentifyOverlayWindow(Position monitorPos, string displayNumber, string subtitle)
    {
        InitializeComponent();

        Left = monitorPos.X;
        Top = monitorPos.Y;
        Width = monitorPos.Width;
        Height = monitorPos.Height;

        DisplayNumberText.Text = displayNumber;
        DisplaySubtitleText.Text = subtitle;
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
            string subtitle = isPrimary ? $"{cleanName} • Layar Utama" : cleanName;

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
