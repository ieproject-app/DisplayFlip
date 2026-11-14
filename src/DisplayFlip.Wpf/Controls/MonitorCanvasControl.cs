using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DisplayFlip.Core;
using DisplayFlip.UI.Helpers;

namespace DisplayFlip.UI.Controls;

/// <summary>
/// Interactive 2D canvas control visualizing display monitors with proportional scaling,
/// selection, drag-and-drop repositioning, magnetic edge snap, and alignment guidelines.
/// </summary>
public sealed class MonitorCanvasControl : FrameworkElement
{
    public static readonly DependencyProperty MonitorsProperty =
        DependencyProperty.Register(
            nameof(Monitors),
            typeof(IReadOnlyList<DisplayMonitor>),
            typeof(MonitorCanvasControl),
            new FrameworkPropertyMetadata(
                Array.Empty<DisplayMonitor>(),
                FrameworkPropertyMetadataOptions.AffectsRender,
                OnMonitorsPropertyChanged));

    public static readonly DependencyProperty SelectedMonitorKeyProperty =
        DependencyProperty.Register(
            nameof(SelectedMonitorKey),
            typeof(string),
            typeof(MonitorCanvasControl),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty PrimaryMonitorKeyProperty =
        DependencyProperty.Register(
            nameof(PrimaryMonitorKey),
            typeof(string),
            typeof(MonitorCanvasControl),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty IsSnapEnabledProperty =
        DependencyProperty.Register(
            nameof(IsSnapEnabled),
            typeof(bool),
            typeof(MonitorCanvasControl),
            new FrameworkPropertyMetadata(
                true,
                FrameworkPropertyMetadataOptions.AffectsRender));

    public IReadOnlyList<DisplayMonitor> Monitors
    {
        get => (IReadOnlyList<DisplayMonitor>)GetValue(MonitorsProperty);
        set => SetValue(MonitorsProperty, value);
    }

    public string? SelectedMonitorKey
    {
        get => (string?)GetValue(SelectedMonitorKeyProperty);
        set => SetValue(SelectedMonitorKeyProperty, value);
    }

    public string? PrimaryMonitorKey
    {
        get => (string?)GetValue(PrimaryMonitorKeyProperty);
        set => SetValue(PrimaryMonitorKeyProperty, value);
    }

    public bool IsSnapEnabled
    {
        get => (bool)GetValue(IsSnapEnabledProperty);
        set => SetValue(IsSnapEnabledProperty, value);
    }

    public event EventHandler<string>? MonitorSelected;
    public event EventHandler<IReadOnlyList<DisplayMonitor>>? ArrangementChanged;

    private readonly List<(Rect Rect, DisplayMonitor Monitor, int DisplayNum)> _hitBoxes = new();
    private readonly List<(Point Start, Point End)> _guideLines = new();

    private bool _isDragging;
    private string? _draggingKey;
    private Point _dragMouseStart;
    private Position _dragMonitorOriginalPos;
    private Position _dragSnappedDesktopPos;
    private string? _hoveredKey;

    private double _lastScale = 0.08;
    private int _lastMinX;
    private int _lastMinY;
    private double _lastOffsetX;
    private double _lastOffsetY;

    public MonitorCanvasControl()
    {
        ClipToBounds = true;
        Focusable = true;
    }

    private static void OnMonitorsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MonitorCanvasControl canvas)
        {
            canvas.CancelDrag();
        }
    }

    public void CancelDrag()
    {
        if (IsMouseCaptured)
            ReleaseMouseCapture();

        _guideLines.Clear();
        _isDragging = false;
        _draggingKey = null;
        Cursor = Cursors.Arrow;
        InvalidateVisual();
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        if (_isDragging || _draggingKey != null)
        {
            CancelDrag();
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape && (_isDragging || _draggingKey != null))
        {
            CancelDrag();
            e.Handled = true;
        }
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        double w = ActualWidth;
        double h = ActualHeight;
        if (w <= 0 || h <= 0)
            return;

        double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var canvasRect = new Rect(0, 0, w, h);

        // Canvas Card Background
        var bgBrush = GetThemeBrush("CardBackgroundFillColorDefaultBrush", new SolidColorBrush(Color.FromArgb(0x28, 0x80, 0x80, 0x80)));
        var borderBrush = GetThemeBrush("CardStrokeColorDefaultBrush", new SolidColorBrush(Color.FromArgb(0x30, 0x80, 0x80, 0x80)));
        var canvasBorderPen = new Pen(borderBrush, 1.0);
        canvasBorderPen.Freeze();

        dc.DrawRoundedRectangle(bgBrush, canvasBorderPen, canvasRect, 8, 8);

        _hitBoxes.Clear();
        var monitors = Monitors;
        if (monitors == null || monitors.Count == 0)
        {
            DrawEmptyState(dc, canvasRect, dpi);
            return;
        }

        // Calculate virtual desktop bounding box
        int minX = monitors.Min(m => m.Position.X);
        int minY = monitors.Min(m => m.Position.Y);
        int maxR = monitors.Max(m => m.Position.Right);
        int maxB = monitors.Max(m => m.Position.Bottom);
        double totalW = Math.Max(1, maxR - minX);
        double totalH = Math.Max(1, maxB - minY);

        double padX = 24;
        double padY = 20;
        double availW = Math.Max(10, w - padX * 2);
        double availH = Math.Max(10, h - padY * 2);

        double avgW = monitors.Average(m => (double)m.Position.Width);
        double avgH = monitors.Average(m => (double)m.Position.Height);
        double viewW = totalW + avgW * 0.4;
        double viewH = totalH + avgH * 0.4;
        double scale = Math.Max(0.005, Math.Min(availW / viewW, availH / viewH));

        double drawnW = totalW * scale;
        double drawnH = totalH * scale;
        double offsetX = padX + (availW - drawnW) / 2;
        double offsetY = padY + (availH - drawnH) / 2;

        _lastScale = scale;
        _lastMinX = minX;
        _lastMinY = minY;
        _lastOffsetX = offsetX;
        _lastOffsetY = offsetY;

        // Render monitors (except the currently dragged floating one)
        for (int i = 0; i < monitors.Count; i++)
        {
            var m = monitors[i];
            int dispNum = int.TryParse(DisplayHelper.ExtractDisplayNumber(m.StableKey, m.FriendlyName, i + 1), out int dn) ? dn : (i + 1);

            double x = offsetX + (m.Position.X - minX) * scale;
            double y = offsetY + (m.Position.Y - minY) * scale;
            double mw = Math.Max(56, m.Position.Width * scale - 4);
            double mh = Math.Max(40, m.Position.Height * scale - 4);

            var rect = new Rect(x + 2, y + 2, mw, mh);
            _hitBoxes.Add((rect, m, dispNum));

            if (_isDragging && m.StableKey == _draggingKey)
                continue; // Rendered later on top as floating tile

            DrawMonitorTile(dc, rect, m, dispNum, isFloating: false, dpi);
        }

        // Render snapping guide lines
        if (_isDragging && _guideLines.Count > 0)
        {
            var accentBrush = GetThemeBrush("AccentFillColorDefaultBrush", new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4)));
            var guidePen = new Pen(accentBrush, 1.5) { DashStyle = DashStyles.Dash };
            guidePen.Freeze();

            foreach (var (p1, p2) in _guideLines)
            {
                dc.DrawLine(guidePen, p1, p2);
            }
        }

        // Render dragged floating tile on top
        if (_isDragging && _draggingKey != null)
        {
            var draggedMon = monitors.FirstOrDefault(m => m.StableKey == _draggingKey);
            if (draggedMon != null)
            {
                int idx = monitors.ToList().IndexOf(draggedMon) + 1;
                int dispNum = int.TryParse(DisplayHelper.ExtractDisplayNumber(draggedMon.StableKey, draggedMon.FriendlyName, idx), out int dn) ? dn : idx;

                double fx = offsetX + (_dragSnappedDesktopPos.X - minX) * scale + 2;
                double fy = offsetY + (_dragSnappedDesktopPos.Y - minY) * scale + 2;
                double fw = Math.Max(56, draggedMon.Position.Width * scale - 4);
                double fh = Math.Max(40, draggedMon.Position.Height * scale - 4);

                var floatingRect = new Rect(fx, fy, fw, fh);
                DrawMonitorTile(dc, floatingRect, draggedMon, dispNum, isFloating: true, dpi);
            }
        }
    }

    private void DrawMonitorTile(DrawingContext dc, Rect rect, DisplayMonitor m, int dispNum, bool isFloating, double dpi)
    {
        bool isSelected = m.StableKey == SelectedMonitorKey;
        bool isPrimary = m.StableKey == PrimaryMonitorKey || (m.Position.X == 0 && m.Position.Y == 0);
        bool isHovered = m.StableKey == _hoveredKey && !isFloating;

        var accentBrush = GetThemeBrush("AccentFillColorDefaultBrush", new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4)));
        var primaryTextBrush = GetThemeBrush("TextFillColorPrimaryBrush", Brushes.White);
        var secondaryTextBrush = GetThemeBrush("TextFillColorSecondaryBrush", Brushes.Gray);

        Brush tileFill;
        if (isSelected)
        {
            var accentColor = (accentBrush as SolidColorBrush)?.Color ?? Color.FromRgb(0x00, 0x78, 0xD4);
            tileFill = new SolidColorBrush(Color.FromArgb((byte)(isFloating ? 80 : 45), accentColor.R, accentColor.G, accentColor.B));
        }
        else if (isHovered)
        {
            tileFill = GetThemeBrush("ControlFillColorSecondaryBrush", new SolidColorBrush(Color.FromArgb(0x40, 0x80, 0x80, 0x80)));
        }
        else
        {
            tileFill = GetThemeBrush("ControlFillColorDefaultBrush", new SolidColorBrush(Color.FromArgb(0x28, 0x80, 0x80, 0x80)));
        }

        Pen tilePen;
        if (isSelected)
        {
            tilePen = new Pen(accentBrush, 2.0);
        }
        else if (isHovered)
        {
            tilePen = new Pen(secondaryTextBrush, 1.2);
        }
        else
        {
            tilePen = new Pen(GetThemeBrush("ControlStrokeColorDefaultBrush", new SolidColorBrush(Color.FromArgb(0x30, 0x80, 0x80, 0x80))), 1.0);
        }
        tilePen.Freeze();

        dc.DrawRoundedRectangle(tileFill, tilePen, rect, 6, 6);

        // Center Display Number
        bool isCompact = rect.Width < 80 || rect.Height < 60;
        double numFontSize = isCompact ? 18 : 28;
        var numTypeface = new Typeface(new FontFamily("Segoe UI Variable Display, Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
        var numText = new FormattedText(
            dispNum.ToString(),
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            numTypeface,
            numFontSize,
            primaryTextBrush,
            dpi);

        double numX = rect.X + (rect.Width - numText.Width) / 2;
        double numY = isCompact
            ? rect.Y + (rect.Height - numText.Height) / 2
            : rect.Y + (rect.Height - numText.Height) / 2 - 4;
        dc.DrawText(numText, new Point(numX, numY));

        // Primary badge (Pill badge if room, otherwise star icon)
        if (isPrimary)
        {
            if (rect.Width >= 115 && !isCompact)
            {
                var badgeTypeface = new Typeface(new FontFamily("Segoe UI Variable Text, Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
                var badgeText = new FormattedText(
                    "★ Utama",
                    CultureInfo.CurrentUICulture,
                    FlowDirection.LeftToRight,
                    badgeTypeface,
                    9.5,
                    accentBrush,
                    dpi);

                double badgePadH = 5;
                double badgePadV = 2;
                double badgeW = badgeText.Width + badgePadH * 2;
                double badgeH = badgeText.Height + badgePadV * 2;
                double badgeX = rect.Right - badgeW - 6;
                double badgeY = rect.Y + 5;

                var badgeBgColor = (accentBrush as SolidColorBrush)?.Color ?? Color.FromRgb(0x00, 0x78, 0xD4);
                var badgeBg = new SolidColorBrush(Color.FromArgb(40, badgeBgColor.R, badgeBgColor.G, badgeBgColor.B));
                var badgePen = new Pen(new SolidColorBrush(Color.FromArgb(90, badgeBgColor.R, badgeBgColor.G, badgeBgColor.B)), 0.8);
                badgePen.Freeze();

                dc.DrawRoundedRectangle(badgeBg, badgePen, new Rect(badgeX, badgeY, badgeW, badgeH), 3, 3);
                dc.DrawText(badgeText, new Point(badgeX + badgePadH, badgeY + badgePadV));
            }
            else
            {
                var starTypeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
                var starText = new FormattedText(
                    "★",
                    CultureInfo.CurrentUICulture,
                    FlowDirection.LeftToRight,
                    starTypeface,
                    isCompact ? 10 : 13,
                    accentBrush,
                    dpi);

                double starX = rect.Right - starText.Width - 6;
                double starY = rect.Y + 5;
                dc.DrawText(starText, new Point(starX, starY));
            }
        }

        // Title and Resolution labels when tile has sufficient space
        if (!isCompact)
        {
            var captionTypeface = new Typeface(new FontFamily("Segoe UI Variable Text, Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

            // Header title (friendly name)
            string shortTitle = DisplayHelper.CleanDeviceName(m.FriendlyName);
            int maxChars = rect.Width >= 130 ? 14 : 9;
            if (shortTitle.Length > maxChars)
                shortTitle = shortTitle[..(maxChars - 3)] + "...";

            var titleText = new FormattedText(
                shortTitle,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                captionTypeface,
                10,
                secondaryTextBrush,
                dpi);

            double maxTitleWidth = rect.Width - (isPrimary ? (rect.Width >= 115 ? 65 : 24) : 12);
            if (titleText.Width <= maxTitleWidth && maxTitleWidth > 20)
            {
                dc.DrawText(titleText, new Point(rect.X + 6, rect.Y + 5));
            }

            // Footer (resolution or snap position)
            var pos = isFloating ? _dragSnappedDesktopPos : m.Position;
            string footer = isFloating
                ? $"Snap ({pos.X},{pos.Y})"
                : $"{pos.Width} \u00D7 {pos.Height}";

            var footerText = new FormattedText(
                footer,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                captionTypeface,
                9.5,
                secondaryTextBrush,
                dpi);

            if (footerText.Width <= rect.Width - 8)
            {
                double footX = rect.X + (rect.Width - footerText.Width) / 2;
                double footY = rect.Bottom - footerText.Height - 4;
                dc.DrawText(footerText, new Point(footX, footY));
            }
        }
    }

    private void DrawEmptyState(DrawingContext dc, Rect canvasRect, double dpi)
    {
        var textBrush = GetThemeBrush("TextFillColorSecondaryBrush", Brushes.Gray);
        var typeface = new Typeface(new FontFamily("Segoe UI Variable Text, Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        var text = new FormattedText(
            "Tidak ada monitor aktif terdeteksi.",
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            typeface,
            14,
            textBrush,
            dpi);

        double x = (canvasRect.Width - text.Width) / 2;
        double y = (canvasRect.Height - text.Height) / 2;
        dc.DrawText(text, new Point(x, y));
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        var pt = e.GetPosition(this);
        for (int i = _hitBoxes.Count - 1; i >= 0; i--)
        {
            var (rect, mon, _) = _hitBoxes[i];
            if (rect.Contains(pt))
            {
                SelectedMonitorKey = mon.StableKey;
                _draggingKey = mon.StableKey;
                _dragMouseStart = pt;
                _dragMonitorOriginalPos = mon.Position;
                _dragSnappedDesktopPos = mon.Position;
                _isDragging = false;

                CaptureMouse();
                MonitorSelected?.Invoke(this, mon.StableKey);
                InvalidateVisual();
                break;
            }
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        var pt = e.GetPosition(this);
        var monitors = Monitors;

        if (_draggingKey != null && e.LeftButton == MouseButtonState.Pressed && monitors != null && monitors.Count >= 2)
        {
            double dxPx = pt.X - _dragMouseStart.X;
            double dyPx = pt.Y - _dragMouseStart.Y;

            if (!_isDragging && (Math.Abs(dxPx) >= 4 || Math.Abs(dyPx) >= 4))
            {
                _isDragging = true;
                Cursor = Cursors.SizeAll;
            }

            if (_isDragging)
            {
                double scale = Math.Max(0.005, _lastScale);
                int snapThresholdDesktop = (int)Math.Clamp(Math.Round(12.0 / scale), 40, 240);
                int rawDesktopX = _dragMonitorOriginalPos.X + (int)Math.Round(dxPx / scale);
                int rawDesktopY = _dragMonitorOriginalPos.Y + (int)Math.Round(dyPx / scale);

                if (IsSnapEnabled)
                {
                    _dragSnappedDesktopPos = DisplayTopology.SnapMonitorPosition(
                        monitors, _draggingKey, rawDesktopX, rawDesktopY, snapThresholdDesktop);
                    ComputeAlignmentGuides(scale);
                }
                else
                {
                    _dragSnappedDesktopPos = new Position(rawDesktopX, rawDesktopY, _dragMonitorOriginalPos.Width, _dragMonitorOriginalPos.Height);
                    _guideLines.Clear();
                }

                InvalidateVisual();
                return;
            }
        }

        // Hover test
        string? hitKey = null;
        foreach (var (rect, mon, _) in _hitBoxes)
        {
            if (rect.Contains(pt))
            {
                hitKey = mon.StableKey;
                break;
            }
        }

        if (hitKey != _hoveredKey)
        {
            _hoveredKey = hitKey;
            Cursor = hitKey != null ? Cursors.Hand : Cursors.Arrow;
            InvalidateVisual();
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);

        if (IsMouseCaptured)
            ReleaseMouseCapture();

        Cursor = Cursors.Arrow;
        bool wasDragging = _isDragging;
        string? draggedKey = _draggingKey;
        var snappedPos = _dragSnappedDesktopPos;

        _draggingKey = null;
        _isDragging = false;
        _guideLines.Clear();

        if (wasDragging && draggedKey != null && Monitors != null)
        {
            double scale = Math.Max(0.005, _lastScale);
            int snapThresholdDesktop = (int)Math.Clamp(Math.Round(12.0 / scale), 40, 240);
            int effectiveSnap = IsSnapEnabled ? snapThresholdDesktop : 0;

            var updated = Monitors
                .Select(m => m.StableKey == draggedKey ? m with { Position = snappedPos } : m)
                .ToList();

            var normalized = DisplayTopology.NormalizeArrangement(updated, PrimaryMonitorKey, effectiveSnap);
            ArrangementChanged?.Invoke(this, normalized);
        }

        InvalidateVisual();
    }

    private void ComputeAlignmentGuides(double scale)
    {
        _guideLines.Clear();
        if (_draggingKey == null || Monitors == null)
            return;

        var others = Monitors.Where(m => m.StableKey != _draggingKey).ToList();
        var sp = _dragSnappedDesktopPos;

        foreach (var anchor in others)
        {
            var ap = anchor.Position;

            // Top edge alignment
            if (sp.Y == ap.Y)
            {
                double y = _lastOffsetY + (sp.Y - _lastMinY) * scale + 2;
                double x1 = _lastOffsetX + (Math.Min(sp.X, ap.X) - _lastMinX) * scale;
                double x2 = _lastOffsetX + (Math.Max(sp.Right, ap.Right) - _lastMinX) * scale;
                _guideLines.Add((new Point(x1 - 8, y), new Point(x2 + 8, y)));
            }

            // Bottom edge alignment
            if (sp.Bottom == ap.Bottom)
            {
                double y = _lastOffsetY + (sp.Bottom - _lastMinY) * scale - 2;
                double x1 = _lastOffsetX + (Math.Min(sp.X, ap.X) - _lastMinX) * scale;
                double x2 = _lastOffsetX + (Math.Max(sp.Right, ap.Right) - _lastMinX) * scale;
                _guideLines.Add((new Point(x1 - 8, y), new Point(x2 + 8, y)));
            }

            // Vertical center alignment
            if (sp.Y + sp.Height / 2 == ap.Y + ap.Height / 2)
            {
                double y = _lastOffsetY + (sp.Y + sp.Height / 2.0 - _lastMinY) * scale;
                double x1 = _lastOffsetX + (Math.Min(sp.X, ap.X) - _lastMinX) * scale;
                double x2 = _lastOffsetX + (Math.Max(sp.Right, ap.Right) - _lastMinX) * scale;
                _guideLines.Add((new Point(x1 - 8, y), new Point(x2 + 8, y)));
            }

            // Left edge alignment
            if (sp.X == ap.X)
            {
                double x = _lastOffsetX + (sp.X - _lastMinX) * scale + 2;
                double y1 = _lastOffsetY + (Math.Min(sp.Y, ap.Y) - _lastMinY) * scale;
                double y2 = _lastOffsetY + (Math.Max(sp.Bottom, ap.Bottom) - _lastMinY) * scale;
                _guideLines.Add((new Point(x, y1 - 8), new Point(x, y2 + 8)));
            }

            // Right edge alignment
            if (sp.Right == ap.Right)
            {
                double x = _lastOffsetX + (sp.Right - _lastMinX) * scale - 2;
                double y1 = _lastOffsetY + (Math.Min(sp.Y, ap.Y) - _lastMinY) * scale;
                double y2 = _lastOffsetY + (Math.Max(sp.Bottom, ap.Bottom) - _lastMinY) * scale;
                _guideLines.Add((new Point(x, y1 - 8), new Point(x, y2 + 8)));
            }

            // Horizontal center alignment
            if (sp.X + sp.Width / 2 == ap.X + ap.Width / 2)
            {
                double x = _lastOffsetX + (sp.X + sp.Width / 2.0 - _lastMinX) * scale;
                double y1 = _lastOffsetY + (Math.Min(sp.Y, ap.Y) - _lastMinY) * scale;
                double y2 = _lastOffsetY + (Math.Max(sp.Bottom, ap.Bottom) - _lastMinY) * scale;
                _guideLines.Add((new Point(x, y1 - 8), new Point(x, y2 + 8)));
            }
        }
    }

    private Brush GetThemeBrush(string resourceKey, Brush fallback)
    {
        if (TryFindResource(resourceKey) is Brush b)
            return b;
        if (Application.Current?.TryFindResource(resourceKey) is Brush appB)
            return appB;
        return fallback;
    }
}
