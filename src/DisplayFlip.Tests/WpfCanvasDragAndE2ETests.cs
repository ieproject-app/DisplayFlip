using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using DisplayFlip.Core;
using DisplayFlip.UI.Controls;
using DisplayFlip.UI.Helpers;
using DisplayFlip.UI.Views.Pages;
using FluentAssertions;
using Xunit;
using CoreOrientation = DisplayFlip.Core.Orientation;

namespace DisplayFlip.Tests;

public class WpfCanvasDragAndE2ETests
{
    private static List<DisplayMonitor> CreateDualMonitors()
    {
        var mon1 = new DisplayMonitor(
            StableKey: "MON1",
            FriendlyName: "Dell Primary",
            Position: new Position(0, 0, 1920, 1080),
            ResolutionWidth: 1920,
            ResolutionHeight: 1080,
            Orientation: CoreOrientation.Landscape,
            IsEnabled: true);

        var mon2 = new DisplayMonitor(
            StableKey: "MON2",
            FriendlyName: "Dell Secondary",
            Position: new Position(1920, 0, 1920, 1080),
            ResolutionWidth: 1920,
            ResolutionHeight: 1080,
            Orientation: CoreOrientation.Landscape,
            IsEnabled: true);

        return new List<DisplayMonitor> { mon1, mon2 };
    }

    [Fact]
    public void CanvasDrag_SmoothRawTracking_UpdatesRawDesktopPosFreeFromSnap()
    {
        ExecuteOnSta(() =>
        {
            var canvas = new MonitorCanvasControl
            {
                Monitors = CreateDualMonitors(),
                SelectedMonitorKey = "MON2",
                PrimaryMonitorKey = "MON1",
                IsSnapEnabled = true
            };

            // Initial state
            canvas.IsDragging.Should().BeFalse();

            // Start drag on MON2
            canvas.StartDrag("MON2", new Point(100, 100));
            canvas.DraggingKey.Should().Be("MON2");
            canvas.DragRawDesktopPos.X.Should().Be(1920);
            canvas.DragRawDesktopPos.Y.Should().Be(0);

            // Move mouse by 15px X and 10px Y (exceeds 4px drag threshold)
            canvas.UpdateDrag(new Point(115, 110));
            canvas.IsDragging.Should().BeTrue();

            // Raw position must move smoothly with mouse cursor
            canvas.DragRawDesktopPos.X.Should().NotBe(1920);
            canvas.DragRawDesktopPos.Y.Should().NotBe(0);

            canvas.CancelDrag();
            canvas.IsDragging.Should().BeFalse();
            canvas.DraggingKey.Should().BeNull();
        });
    }

    [Fact]
    public void CanvasDrag_WhenSnapEnabled_ComputesMagneticSnapTarget()
    {
        ExecuteOnSta(() =>
        {
            var monitors = CreateDualMonitors();
            var canvas = new MonitorCanvasControl
            {
                Monitors = monitors,
                SelectedMonitorKey = "MON2",
                PrimaryMonitorKey = "MON1",
                IsSnapEnabled = true
            };

            canvas.StartDrag("MON2", new Point(100, 100));

            // Move mouse slightly (e.g. 5px right and 5px down)
            canvas.UpdateDrag(new Point(105, 105));
            canvas.IsDragging.Should().BeTrue();

            // Magnetic snap targets alignment edges
            canvas.DragSnappedDesktopPos.Should().NotBeNull();
            canvas.DragSnappedDesktopPos.Width.Should().Be(1920);
            canvas.DragSnappedDesktopPos.Height.Should().Be(1080);

            canvas.CancelDrag();
        });
    }

    [Fact]
    public void CanvasDrag_WhenSnapDisabled_RawAndSnappedPositionsAreIdentical()
    {
        ExecuteOnSta(() =>
        {
            var monitors = CreateDualMonitors();
            var canvas = new MonitorCanvasControl
            {
                Monitors = monitors,
                SelectedMonitorKey = "MON2",
                PrimaryMonitorKey = "MON1",
                IsSnapEnabled = false
            };

            canvas.StartDrag("MON2", new Point(100, 100));
            canvas.UpdateDrag(new Point(125, 118));

            canvas.IsDragging.Should().BeTrue();
            canvas.DragSnappedDesktopPos.X.Should().Be(canvas.DragRawDesktopPos.X);
            canvas.DragSnappedDesktopPos.Y.Should().Be(canvas.DragRawDesktopPos.Y);

            canvas.CancelDrag();
        });
    }

    [Fact]
    public void CanvasDrag_LeftRightSwap_SwapsPositionsOnEndDrag()
    {
        ExecuteOnSta(() =>
        {
            var monitors = CreateDualMonitors();
            var canvas = new MonitorCanvasControl
            {
                Monitors = monitors,
                SelectedMonitorKey = "MON1",
                PrimaryMonitorKey = "MON1",
                IsSnapEnabled = true
            };

            IReadOnlyList<DisplayMonitor>? arrangementResult = null;
            canvas.ArrangementChanged += (_, result) => arrangementResult = result;

            canvas.StartDrag("MON1", new Point(50, 50));
            // Drag MON1 to the right of MON2 (+250px)
            canvas.UpdateDrag(new Point(300, 50));
            canvas.EndDrag();

            arrangementResult.Should().NotBeNull();
            arrangementResult!.Count.Should().Be(2);

            // MON1 is primary, so coordinates should normalize with MON1 at (0,0) and MON2 to its left (-1920,0)
            // or MON2 at (0,0) and MON1 at (1920,0)
            var m1 = arrangementResult.First(m => m.StableKey == "MON1");
            var m2 = arrangementResult.First(m => m.StableKey == "MON2");

            // Horizontal relationship should be inverted from initial (where MON1 was at X=0, MON2 was at X=1920)
            (m1.Position.X > m2.Position.X).Should().BeTrue("MON1 was dragged to the right of MON2");
        });
    }

    [Fact]
    public void CanvasDrag_VerticalOffsets_ArrangesMonitorsAboveOrBelow()
    {
        ExecuteOnSta(() =>
        {
            var monitors = CreateDualMonitors();
            var canvas = new MonitorCanvasControl
            {
                Monitors = monitors,
                SelectedMonitorKey = "MON2",
                PrimaryMonitorKey = "MON1",
                IsSnapEnabled = true
            };

            IReadOnlyList<DisplayMonitor>? arrangementResult = null;
            canvas.ArrangementChanged += (_, result) => arrangementResult = result;

            canvas.StartDrag("MON2", new Point(100, 100));
            // Drag MON2 directly downwards by 200px
            canvas.UpdateDrag(new Point(100, 300));
            canvas.EndDrag();

            arrangementResult.Should().NotBeNull();
            var m2 = arrangementResult!.First(m => m.StableKey == "MON2");
            var m1 = arrangementResult!.First(m => m.StableKey == "MON1");

            // M2 should be positioned lower than M1 or at vertical offset
            m2.Position.Y.Should().BeGreaterOrEqualTo(m1.Position.Y);
        });
    }

    [Fact]
    public void CanvasNudge_ArrowKeys_MovesSelectedMonitorByStep()
    {
        ExecuteOnSta(() =>
        {
            var monitors = CreateDualMonitors();
            var canvas = new MonitorCanvasControl
            {
                Monitors = monitors,
                SelectedMonitorKey = "MON2",
                PrimaryMonitorKey = "MON1",
                IsSnapEnabled = false
            };

            int changeCount = 0;
            canvas.ArrangementChanged += (_, _) => changeCount++;

            // Right arrow
            bool handledRight = canvas.Nudge(Key.Right);
            handledRight.Should().BeTrue();
            changeCount.Should().Be(1);

            // Left arrow
            bool handledLeft = canvas.Nudge(Key.Left);
            handledLeft.Should().BeTrue();
            changeCount.Should().Be(2);

            // Down arrow
            bool handledDown = canvas.Nudge(Key.Down);
            handledDown.Should().BeTrue();
            changeCount.Should().Be(3);

            // Up arrow
            bool handledUp = canvas.Nudge(Key.Up);
            handledUp.Should().BeTrue();
            changeCount.Should().Be(4);

            // Unhandled key returns false
            bool handledEnter = canvas.Nudge(Key.Enter);
            handledEnter.Should().BeFalse();
            changeCount.Should().Be(4);
        });
    }

    [Fact]
    public void PrimaryMonitorShift_NormalizesDesktopCoordinates()
    {
        var monitors = CreateDualMonitors();

        // Switch primary from MON1 to MON2
        var shifted = DisplayTopology.SetPrimary(monitors, "MON2");
        shifted.First(m => m.StableKey == "MON2").Position.X.Should().Be(0);
        shifted.First(m => m.StableKey == "MON2").Position.Y.Should().Be(0);

        shifted.First(m => m.StableKey == "MON1").Position.X.Should().Be(1920);
        shifted.First(m => m.StableKey == "MON1").Position.Y.Should().Be(0);
    }

    [Fact]
    public void AutoApplyDrag_Persistence_InProfileStore()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"displayflip_autoapply_{Guid.NewGuid():N}.json");
        try
        {
            var store = new ProfileStore(tempFile);
            GeneralHelper.GetAutoApplyDrag(store).Should().BeFalse();

            var (ok1, _) = GeneralHelper.SetAutoApplyDrag(store, true);
            ok1.Should().BeTrue();

            var reloaded = new ProfileStore(tempFile);
            GeneralHelper.GetAutoApplyDrag(reloaded).Should().BeTrue();

            var (ok2, _) = GeneralHelper.SetAutoApplyDrag(store, false);
            ok2.Should().BeTrue();

            var reloaded2 = new ProfileStore(tempFile);
            GeneralHelper.GetAutoApplyDrag(reloaded2).Should().BeFalse();
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void DisplaysPageXaml_SubtitleShowsActiveMonitorCount_AndBadgeIsRemoved()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "DisplayFlip.slnx")))
            dir = dir.Parent;

        dir.Should().NotBeNull();
        var xamlPath = Path.Combine(dir!.FullName, "src", "DisplayFlip.Wpf", "Views", "Pages", "DisplaysPage.xaml");
        var xaml = File.ReadAllText(xamlPath);

        // Subtitle must exist and support wrap
        xaml.Should().Contain("x:Name=\"CanvasSubtitleText\"");
        xaml.Should().Contain("TextWrapping=\"Wrap\"");

        // Option A: Badge must be removed
        xaml.Should().NotContain("MonitorCountBadge", "Option A eliminates the blue badge in favor of clean Windows 11 subtitle text");
    }

    [Fact]
    public void DisplaysPage_UpdateUiFromState_UpdatesSubtitleActiveCount()
    {
        ExecuteOnSta(() =>
        {
            var page = new DisplaysPage();
            page.Should().NotBeNull();
            page.RefreshData();
            page.CanvasSubtitleText.Should().NotBeNull();
            page.CanvasSubtitleText.Text.Should().Contain("layar aktif");
        });
    }

    private static void ExecuteOnSta(Action action)
    {
        Exception? caught = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                caught = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (caught != null)
            throw new AggregateException("STA thread execution failed", caught);
    }
}
