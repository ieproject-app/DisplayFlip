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
            var monitors = new List<DisplayMonitor>
            {
                new("MON1", "Dell Primary", new Position(0, 0, 1920, 1080), 1920, 1080, CoreOrientation.Landscape, true),
                new("MON2", "Dell Secondary", new Position(1920, -417, 1920, 1080), 1920, 1080, CoreOrientation.Landscape, true)
            };

            var canvas = new MonitorCanvasControl
            {
                Monitors = monitors,
                SelectedMonitorKey = "MON2",
                PrimaryMonitorKey = "MON1",
                IsSnapEnabled = true
            };

            int changeCount = 0;
            canvas.ArrangementChanged += (_, updated) =>
            {
                changeCount++;
                canvas.Monitors = updated;
            };

            // Pushing further right when already rightmost must NOT fire a no-op change
            canvas.Nudge(Key.Right).Should().BeFalse();
            changeCount.Should().Be(0);

            // Left arrow must cross MON1 to the left side (-1920) even with snap enabled
            canvas.Nudge(Key.Left).Should().BeTrue();
            changeCount.Should().Be(1);
            canvas.Monitors.First(m => m.StableKey == "MON2").Position.X.Should().Be(-1920);

            // Down arrow shifts vertical alignment
            canvas.Nudge(Key.Down).Should().BeTrue();
            changeCount.Should().Be(2);

            // Up arrow shifts vertical alignment
            canvas.Nudge(Key.Up).Should().BeTrue();
            changeCount.Should().Be(3);

            // Right arrow now swaps MON2 back across MON1 to the right side (+1920)
            canvas.Nudge(Key.Right).Should().BeTrue();
            changeCount.Should().Be(4);
            canvas.Monitors.First(m => m.StableKey == "MON2").Position.X.Should().Be(1920);

            // Unhandled key returns false
            canvas.Nudge(Key.Enter).Should().BeFalse();
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

    [Fact]
    public void CanvasDrag_GhostSnapPreview_DoesNotOverlapStationaryMonitorWhenDraggedLeft()
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

            // Start dragging MON2 and drag it far to the left of MON1
            canvas.StartDrag("MON2", new Point(300, 100));
            canvas.UpdateDrag(new Point(50, 100)); // Dragged 250px left

            double canvasW = 800;
            double canvasH = 260;

            // Compute ghost preview rectangle and MON1 stationary rectangle
            var ghostRect = canvas.GetGhostPreviewRect(canvasW, canvasH);
            var mon1Rect = canvas.GetTileRect("MON1", canvasW, canvasH);

            ghostRect.Should().NotBe(Rect.Empty);
            mon1Rect.Should().NotBe(Rect.Empty);

            // Snapped target should be adjacent to the left of MON1
            canvas.DragSnappedDesktopPos.X.Should().Be(-1920);

            // Visual ghost box must NOT be clipped off screen (X >= 4)
            ghostRect.X.Should().BeGreaterOrEqualTo(4, "Ghost preview must not be clipped off the left canvas boundary");

            // Visual ghost box right edge must NOT overlap MON1 left edge
            ghostRect.Right.Should().BeLessThanOrEqualTo(mon1Rect.X + 0.5, "Ghost preview must sit adjacent to MON1 without overlapping it");
        });
    }

    [Fact]
    public void CanvasDrag_AlignmentGuides_GeneratedForAbuttingSeamsAndWithinCanvasBounds()
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

            canvas.StartDrag("MON2", new Point(200, 100));
            // Move MON2 to snap right adjacent to MON1
            canvas.UpdateDrag(new Point(205, 100));

            // Guidelines should be computed
            canvas.GuideLinesCount.Should().BeGreaterThan(0, "Magnetic snapping should generate alignment guidelines");

            double canvasW = 800;
            double canvasH = 260;
            foreach (var (p1, p2) in canvas.GuideLines)
            {
                p1.X.Should().BeInRange(-100, canvasW + 100);
                p2.X.Should().BeInRange(-100, canvasW + 100);
                p1.Y.Should().BeInRange(-100, canvasH + 100);
                p2.Y.Should().BeInRange(-100, canvasH + 100);
            }
        });
    }

    [Fact]
    public void CanvasDrag_4KAnd8KNonLinearSetup_AccommodatesMassiveDisplaysWithoutClipping()
    {
        ExecuteOnSta(() =>
        {
            var mon4k = new DisplayMonitor(
                StableKey: "MON_4K",
                FriendlyName: "4K Primary",
                Position: new Position(0, 0, 3840, 2160),
                ResolutionWidth: 3840,
                ResolutionHeight: 2160,
                Orientation: CoreOrientation.Landscape,
                IsEnabled: true);

            var mon8k = new DisplayMonitor(
                StableKey: "MON_8K",
                FriendlyName: "8K Secondary",
                Position: new Position(3840, 0, 7680, 4320),
                ResolutionWidth: 7680,
                ResolutionHeight: 4320,
                Orientation: CoreOrientation.Landscape,
                IsEnabled: true);

            var canvas = new MonitorCanvasControl
            {
                Monitors = new List<DisplayMonitor> { mon4k, mon8k },
                SelectedMonitorKey = "MON_8K",
                PrimaryMonitorKey = "MON_4K",
                IsSnapEnabled = true
            };

            canvas.StartDrag("MON_8K", new Point(400, 150));
            canvas.UpdateDrag(new Point(100, 150));

            var ghostRect = canvas.GetGhostPreviewRect(800, 260);
            ghostRect.Should().NotBe(Rect.Empty);
            ghostRect.X.Should().BeGreaterOrEqualTo(4);
            ghostRect.Width.Should().BeGreaterThan(50);
            ghostRect.Height.Should().BeGreaterThan(40);
        });
    }

    [Fact]
    public void CanvasDrag_LostMouseCaptureMidDragCancels_WhileNormalReleaseCommitsArrangement()
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

            IReadOnlyList<DisplayMonitor>? committed = null;
            canvas.ArrangementChanged += (_, updated) => committed = updated;

            // Mid-drag LostMouseCapture (e.g. Alt+Tab) must cancel and NOT commit
            canvas.StartDrag("MON2", new Point(300, 100));
            canvas.UpdateDrag(new Point(50, 100));
            canvas.IsDragging.Should().BeTrue();

            canvas.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, 0)
            {
                RoutedEvent = UIElement.LostMouseCaptureEvent
            });

            canvas.IsDragging.Should().BeFalse();
            canvas.DraggingKey.Should().BeNull();
            committed.Should().BeNull();

            // Calling EndDrag right after canceled drag still must not commit
            canvas.EndDrag();
            committed.Should().BeNull();

            // Normal drag + real MouseLeftButtonUp (EndDrag) must commit swapped coordinates
            canvas.StartDrag("MON2", new Point(300, 100));
            canvas.UpdateDrag(new Point(50, 100));
            canvas.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
            {
                RoutedEvent = UIElement.MouseLeftButtonUpEvent
            });

            canvas.IsDragging.Should().BeFalse();
            committed.Should().NotBeNull();
            committed!.First(m => m.StableKey == "MON2").Position.X.Should().Be(-1920);
        });
    }

    [Fact]
    public void DisplaysPage_RefreshData_PreservesUnappliedCanvasArrangement()
    {
        ExecuteOnSta(() =>
        {
            var page = new DisplaysPage();
            var customMonitors = new List<DisplayMonitor>
            {
                new("TEST_M1", "Test Primary", new Position(0, 0, 1920, 1080), 1920, 1080, CoreOrientation.Landscape, true),
                new("TEST_M2", "Test Secondary", new Position(1920, -417, 1920, 1080), 1920, 1080, CoreOrientation.Landscape, true)
            };

            page.MonitorCanvas.Monitors = customMonitors;
            page.MonitorCanvas.PrimaryMonitorKey = "TEST_M1";
            page.MonitorCanvas.SelectedMonitorKey = "TEST_M2";

            // Perform a drag across to the left of TEST_M1 (-1920, -417) via MonitorCanvas
            page.MonitorCanvas.StartDrag("TEST_M2", new Point(360, 100));
            page.MonitorCanvas.UpdateDrag(new Point(40, 100));
            page.MonitorCanvas.EndDrag();

            (page.HasUnappliedChanges || page.IsApplyingArrangement).Should().BeTrue();
            page.CanvasMonitors.Should().Contain(m => m.StableKey == "TEST_M2" && m.Position.X == -1920);

            // Simulate tab switch reload or DisplaySettingsChanged RefreshData()
            page.RefreshData();

            page.CanvasMonitors.Should().Contain(m => m.StableKey == "TEST_M2" && m.Position.X == -1920);
        });
    }

    [Fact]
    public void SetPrimary_NormalizeArrangement_PreservesVerticalOffset()
    {
        var monitors = new List<DisplayMonitor>
        {
            new("MON1", "Monitor 1", new Position(0, 0, 1920, 1080), 1920, 1080, CoreOrientation.Landscape, true),
            new("MON2", "Monitor 2", new Position(1920, -417, 1920, 1080), 1920, 1080, CoreOrientation.Landscape, true)
        };

        var shifted = DisplayTopology.NormalizeArrangement(monitors, "MON2");
        var m1 = shifted.First(m => m.StableKey == "MON1");
        var m2 = shifted.First(m => m.StableKey == "MON2");

        m2.Position.X.Should().Be(0);
        m2.Position.Y.Should().Be(0);
        m1.Position.X.Should().Be(-1920);
        m1.Position.Y.Should().Be(417);
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
