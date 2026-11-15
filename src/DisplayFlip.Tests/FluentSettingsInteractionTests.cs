using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using DisplayFlip;
using DisplayFlip.Core;
using DisplayFlip.Windows;
using FluentAssertions;
using Xunit;
using CoreOrientation = DisplayFlip.Core.Orientation;

namespace DisplayFlip.Tests;

public class FluentSettingsInteractionTests
{
    private const int HTCLIENT = 1;
    private const int HTCAPTION = 2;
    private const int HTMAXBUTTON = 9;
    private const int HTLEFT = 10;
    private const int HTRIGHT = 11;
    private const int HTTOP = 12;
    private const int HTTOPLEFT = 13;
    private const int HTTOPRIGHT = 14;
    private const int HTBOTTOM = 15;
    private const int HTBOTTOMLEFT = 16;
    private const int HTBOTTOMRIGHT = 17;

    [Fact]
    public void HitTestPoint_ReturnsAccurateHitCodes_ForGripsAndControls()
    {
        using var host = new TrayHost(showSettingsOnLaunch: false);
        using var form = new FluentSettingsForm(host, null);
        form.Show();
        form.Size = new Size(980, 720);

        var hitTestMethod = typeof(FluentSettingsForm).GetMethod("HitTestPoint", BindingFlags.NonPublic | BindingFlags.Instance);
        hitTestMethod.Should().NotBeNull();

        int InvokeHitTest(Point pt) => (int)hitTestMethod!.Invoke(form, new object[] { pt })!;

        var rootField = typeof(FluentSettingsForm).GetField("_root", BindingFlags.NonPublic | BindingFlags.Instance);
        var root = (Panel)rootField!.GetValue(form)!;
        int w = root.ClientSize.Width;
        int h = root.ClientSize.Height;

        // 1. Four corner resize grips
        InvokeHitTest(new Point(2, 2)).Should().Be(HTTOPLEFT, "Top-left corner should return HTTOPLEFT");
        InvokeHitTest(new Point(w - 2, 2)).Should().Be(HTTOPRIGHT, "Top-right corner should return HTTOPRIGHT");
        InvokeHitTest(new Point(2, h - 2)).Should().Be(HTBOTTOMLEFT, "Bottom-left corner should return HTBOTTOMLEFT");
        InvokeHitTest(new Point(w - 2, h - 2)).Should().Be(HTBOTTOMRIGHT, "Bottom-right corner should return HTBOTTOMRIGHT");

        // 2. Four border resize grips
        InvokeHitTest(new Point(2, 200)).Should().Be(HTLEFT, "Left border should return HTLEFT");
        InvokeHitTest(new Point(w - 2, 200)).Should().Be(HTRIGHT, "Right border should return HTRIGHT");
        InvokeHitTest(new Point(w / 2, h - 2)).Should().Be(HTBOTTOM, "Bottom border should return HTBOTTOM");
        InvokeHitTest(new Point(w / 2, 2)).Should().Be(HTTOP, "Top border should return HTTOP");

        // 3. Caption bar
        InvokeHitTest(new Point(100, 18)).Should().Be(HTCAPTION, "Title bar empty space should return HTCAPTION");

        // 4. Caption buttons
        var closeBtnField = typeof(FluentSettingsForm).GetField("_closeBtn", BindingFlags.NonPublic | BindingFlags.Instance);
        var closeBtn = (Control)closeBtnField!.GetValue(form)!;
        var maxBtnField = typeof(FluentSettingsForm).GetField("_maxBtn", BindingFlags.NonPublic | BindingFlags.Instance);
        var maxBtn = (Control)maxBtnField!.GetValue(form)!;
        var minBtnField = typeof(FluentSettingsForm).GetField("_minBtn", BindingFlags.NonPublic | BindingFlags.Instance);
        var minBtn = (Control)minBtnField!.GetValue(form)!;

        // Close button: center should be HTCLIENT
        InvokeHitTest(new Point(closeBtn.Left + closeBtn.Width / 2, 18)).Should().Be(HTCLIENT, "Close button body should return HTCLIENT");

        // Maximize button: center should be HTMAXBUTTON (Windows 11 Snap Layouts)
        InvokeHitTest(new Point(maxBtn.Left + maxBtn.Width / 2, 18)).Should().Be(HTMAXBUTTON, "Maximize button should return HTMAXBUTTON");

        // Minimize button: center should be HTCLIENT
        InvokeHitTest(new Point(minBtn.Left + minBtn.Width / 2, 18)).Should().Be(HTCLIENT, "Minimize button body should return HTCLIENT");

        // 5. Maximized mode: Fitts's law on close button corner (returns HTCLIENT, not HTTOPRIGHT)
        form.WindowState = FormWindowState.Maximized;
        int maxW = root.ClientSize.Width;
        InvokeHitTest(new Point(maxW - 1, 0)).Should().Be(HTCLIENT, "Maximized top-right corner should return HTCLIENT for Fitts's law");
    }

    [Fact]
    public void CanvasDrag_EscapeCancelsDrag_AndRestoresPositions()
    {
        using var host = new TrayHost(showSettingsOnLaunch: false);
        using var form = new FluentSettingsForm(host, null);
        form.Show();
        form.Size = new Size(980, 720);

        var canvasField = typeof(FluentSettingsForm).GetField("_monitorCanvas", BindingFlags.NonPublic | BindingFlags.Instance);
        var canvas = (Panel)canvasField!.GetValue(form)!;

        var canvasMonitorsField = typeof(FluentSettingsForm).GetField("_canvasMonitors", BindingFlags.NonPublic | BindingFlags.Instance);
        var initialMonitors = new List<DisplayMonitor>
        {
            new("mon1", "DISPLAY1", new Position(0, 0, 1920, 1080), 1920, 1080, CoreOrientation.Landscape, true),
            new("mon2", "DISPLAY2", new Position(1920, 0, 1920, 1080), 1920, 1080, CoreOrientation.Landscape, true)
        };
        canvasMonitorsField!.SetValue(form, initialMonitors);

        // Simulate start drag on monitor 2
        var isDraggingField = typeof(FluentSettingsForm).GetField("_isDraggingMonitor", BindingFlags.NonPublic | BindingFlags.Instance);
        var dragKeyField = typeof(FluentSettingsForm).GetField("_draggingMonitorKey", BindingFlags.NonPublic | BindingFlags.Instance);

        isDraggingField!.SetValue(form, true);
        dragKeyField!.SetValue(form, "mon2");
        canvas.Capture = true;

        // Process Escape key
        var processCmdKeyMethod = typeof(FluentSettingsForm).GetMethod("ProcessCmdKey", BindingFlags.NonPublic | BindingFlags.Instance);
        var msg = new Message();
        bool handled = (bool)processCmdKeyMethod!.Invoke(form, new object[] { msg, Keys.Escape })!;

        handled.Should().BeTrue("Escape should be handled by drag cancellation");
        ((bool)isDraggingField.GetValue(form)!).Should().BeFalse("Drag state should be cleared");
        dragKeyField.GetValue(form).Should().BeNull("Dragging key should be reset");
        canvas.Capture.Should().BeFalse("Mouse capture should be released");

        // Positions should remain unchanged
        var finalMonitors = (List<DisplayMonitor>)canvasMonitorsField.GetValue(form)!;
        finalMonitors[1].Position.X.Should().Be(1920);
        finalMonitors[1].Position.Y.Should().Be(0);
    }

    [Fact]
    public void ArrowKeys_OnlyNudgeWhenOnProfilTab()
    {
        using var host = new TrayHost(showSettingsOnLaunch: false);
        using var form = new FluentSettingsForm(host, null);
        form.Show();
        form.Size = new Size(980, 720);

        var canvasMonitorsField = typeof(FluentSettingsForm).GetField("_canvasMonitors", BindingFlags.NonPublic | BindingFlags.Instance);
        var initialMonitors = new List<DisplayMonitor>
        {
            new("mon1", "DISPLAY1", new Position(0, 0, 1920, 1080), 1920, 1080, CoreOrientation.Landscape, true),
            new("mon2", "DISPLAY2", new Position(1920, 0, 1920, 1080), 1920, 1080, CoreOrientation.Landscape, true)
        };
        canvasMonitorsField!.SetValue(form, initialMonitors);

        var selectedKeyField = typeof(FluentSettingsForm).GetField("_selectedMonitorKey", BindingFlags.NonPublic | BindingFlags.Instance);
        selectedKeyField!.SetValue(form, "mon2");

        var hasUnappliedField = typeof(FluentSettingsForm).GetField("_hasUnappliedCanvasChanges", BindingFlags.NonPublic | BindingFlags.Instance);
        hasUnappliedField!.SetValue(form, false);

        var switchTabMethod = typeof(FluentSettingsForm).GetMethod("SwitchTab", BindingFlags.NonPublic | BindingFlags.Instance);
        var processCmdKeyMethod = typeof(FluentSettingsForm).GetMethod("ProcessCmdKey", BindingFlags.NonPublic | BindingFlags.Instance);

        // 1. On "Pintasan" tab: Arrow keys should NOT nudge
        switchTabMethod!.Invoke(form, new object[] { "Pintasan" });
        var msg = new Message();
        bool handledOnPintasan = (bool)processCmdKeyMethod!.Invoke(form, new object[] { msg, Keys.Down })!;
        handledOnPintasan.Should().BeFalse("Arrow keys should not be consumed when on Pintasan tab");
        ((bool)hasUnappliedField.GetValue(form)!).Should().BeFalse("Canvas should not have unapplied changes from Pintasan tab");

        // 2. On "Profil" tab: Arrow keys SHOULD nudge
        switchTabMethod.Invoke(form, new object[] { "Profil" });
        bool handledOnProfil = (bool)processCmdKeyMethod.Invoke(form, new object[] { msg, Keys.Down })!;
        handledOnProfil.Should().BeTrue("Arrow keys should be handled on Profil tab");
        ((bool)hasUnappliedField.GetValue(form)!).Should().BeTrue("Canvas should be marked with unapplied changes after nudge");
    }
}
