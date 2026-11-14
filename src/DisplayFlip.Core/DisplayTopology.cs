namespace DisplayFlip.Core;

/// <summary>
/// Portable topology math (D3/D4): Mirror primary-preserving + Legalizer + SetPrimary.
/// No Win32 calls --- pure logic for testability and PowerToys portability.
/// </summary>
public static class DisplayTopology
{
    public static List<DisplayMonitor> Mirror(IReadOnlyList<DisplayMonitor> monitors)
    {
        if (monitors.Count < 2)
            return monitors.ToList();

        var primary = monitors.FirstOrDefault(m => m.Position.X == 0 && m.Position.Y == 0)
                      ?? monitors.OrderBy(m => Math.Abs(m.Position.X)).First();

        var reversed = monitors
            .OrderBy(m => m.Position.X)
            .ThenBy(m => m.Position.Y)
            .Reverse()
            .ToList();

        var packed = new List<DisplayMonitor>(reversed.Count);
        int cursorX = 0;
        foreach (var m in reversed)
        {
            int y = m.StableKey == primary.StableKey ? 0 : m.Position.Y;
            packed.Add(m with { Position = m.Position with { X = cursorX, Y = y } });
            cursorX += m.Position.Width;
        }

        int shiftX = packed.First(m => m.StableKey == primary.StableKey).Position.X;
        var shifted = packed
            .Select(m => m with { Position = m.Position with { X = m.Position.X - shiftX } })
            .ToList();

        return Legalize(shifted);
    }

    public static List<DisplayMonitor> SetPrimary(IReadOnlyList<DisplayMonitor> monitors, string targetStableKey)
    {
        var target = monitors.FirstOrDefault(m => m.StableKey == targetStableKey);
        if (target == null)
            return monitors.ToList();

        var othersOrdered = monitors
            .Where(m => m.StableKey != targetStableKey)
            .OrderBy(m => m.Position.X)
            .ToList();

        var result = new List<DisplayMonitor>(monitors.Count);
        result.Add(target with { Position = target.Position with { X = 0, Y = 0 } });

        int cursor = target.Position.Width;
        foreach (var m in othersOrdered)
        {
            var pos = m.Position with { X = cursor, Y = target.Position.Y };
            cursor += m.Position.Width;
            result.Add(m with { Position = pos });
        }

        return Legalize(result);
    }

    public static List<DisplayMonitor> Legalize(IReadOnlyList<DisplayMonitor> monitors)
    {
        if (monitors.Count == 0)
            return new List<DisplayMonitor>();

        // Primary must be at (0,0)
        var primary = monitors.FirstOrDefault(m => m.Position.X == 0 && m.Position.Y == 0)
                      ?? monitors.OrderBy(m => Math.Abs(m.Position.X)).First();

        var ordered = monitors
            .OrderBy(m => m.Position.X)
            .ThenBy(m => m.Position.Y)
            .ToList();

        var packed = new List<DisplayMonitor>(ordered.Count);
        int cursorX = 0;
        foreach (var m in ordered)
        {
            int y = m.StableKey == primary.StableKey ? 0 : m.Position.Y;
            packed.Add(m with { Position = m.Position with { X = cursorX, Y = y } });
            cursorX += m.Position.Width;
        }

        int shiftX = packed.First(m => m.StableKey == primary.StableKey).Position.X;
        return packed
            .Select(m => m with { Position = m.Position with { X = m.Position.X - shiftX } })
            .ToList();
    }

    public static bool IsLegal(IReadOnlyList<DisplayMonitor> monitors)
    {
        if (monitors.Count == 0)
            return true;
        // Check exactly one primary at (0,0) and contiguous left-to-right without gap/overlap
        if (monitors.Count(m => m.Position.X == 0 && m.Position.Y == 0) != 1)
            return false;
        var sorted = monitors.OrderBy(m => m.Position.X).ToList();
        for (int i = 1; i < sorted.Count; i++)
        {
            var prev = sorted[i - 1];
            var curr = sorted[i];
            if (curr.Position.X != prev.Position.Right)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Computes the magnetic snapped position (X, Y) for <paramref name="draggedKey"/>
    /// when dragged to <paramref name="rawX"/>, <paramref name="rawY"/> relative to the other monitors.
    /// Allows attaching to Left, Right, Top, or Bottom edges with smooth offset &amp; edge alignment snap.
    /// </summary>
    public static Position SnapMonitorPosition(
        IReadOnlyList<DisplayMonitor> monitors,
        string draggedKey,
        int rawX,
        int rawY,
        int alignSnapThreshold = 96)
    {
        var dragged = monitors.FirstOrDefault(m => m.StableKey == draggedKey);
        if (dragged == null)
            return new Position(rawX, rawY, 1920, 1080);

        int w = dragged.Position.Width;
        int h = dragged.Position.Height;
        var others = monitors.Where(m => m.StableKey != draggedKey).ToList();
        if (others.Count == 0)
            return new Position(0, 0, w, h);

        const int minSharedEdge = 120;
        var candidates = new List<(int x, int y, double dist)>();

        foreach (var anchor in others)
        {
            var ap = anchor.Position;
            int minY = ap.Y - h + Math.Min(minSharedEdge, Math.Min(h, ap.Height));
            int maxY = ap.Bottom - Math.Min(minSharedEdge, Math.Min(h, ap.Height));
            int clampedY = Math.Clamp(rawY, minY, maxY);
            clampedY = SnapValue(clampedY, new[]
            {
                ap.Y,
                ap.Bottom - h,
                ap.Y + (ap.Height - h) / 2,
                0
            }, alignSnapThreshold, minY, maxY);

            // 1. Left of anchor
            int leftX = ap.X - w;
            AddIfNoOverlap(candidates, leftX, clampedY, w, h, rawX, rawY, others);

            // 2. Right of anchor
            int rightX = ap.Right;
            AddIfNoOverlap(candidates, rightX, clampedY, w, h, rawX, rawY, others);

            int minX = ap.X - w + Math.Min(minSharedEdge, Math.Min(w, ap.Width));
            int maxX = ap.Right - Math.Min(minSharedEdge, Math.Min(w, ap.Width));
            int clampedX = Math.Clamp(rawX, minX, maxX);
            clampedX = SnapValue(clampedX, new[]
            {
                ap.X,
                ap.Right - w,
                ap.X + (ap.Width - w) / 2,
                0
            }, alignSnapThreshold, minX, maxX);

            // 3. Above anchor
            int topY = ap.Y - h;
            AddIfNoOverlap(candidates, clampedX, topY, w, h, rawX, rawY, others);

            // 4. Below anchor
            int bottomY = ap.Bottom;
            AddIfNoOverlap(candidates, clampedX, bottomY, w, h, rawX, rawY, others);
        }

        if (candidates.Count == 0)
        {
            int maxRight = others.Max(o => o.Position.Right);
            int refY = others.OrderByDescending(o => o.Position.Right).First().Position.Y;
            return new Position(maxRight, refY, w, h);
        }

        var best = candidates.OrderBy(c => c.dist).First();
        return new Position(best.x, best.y, w, h);
    }

    /// <summary>
    /// Normalizes a 2D monitor arrangement so the primary monitor is at (0,0)
    /// and all other monitors touch cleanly without overlaps or gaps, preserving
    /// user-chosen 2D offsets (left/right/top/bottom &amp; vertical Y shifts).
    /// </summary>
    public static List<DisplayMonitor> NormalizeArrangement(
        IReadOnlyList<DisplayMonitor> monitors,
        string? primaryStableKey = null,
        int alignSnapThreshold = 96)
    {
        if (monitors.Count == 0)
            return new List<DisplayMonitor>();

        var primary = (!string.IsNullOrWhiteSpace(primaryStableKey)
                          ? monitors.FirstOrDefault(m => m.StableKey == primaryStableKey)
                          : null)
                      ?? monitors.FirstOrDefault(m => m.Position.X == 0 && m.Position.Y == 0)
                      ?? monitors.OrderBy(m => Math.Abs(m.Position.X) + Math.Abs(m.Position.Y)).First();

        int dx = -primary.Position.X;
        int dy = -primary.Position.Y;
        var shifted = monitors
            .Select(m => m with
            {
                Position = new Position(
                    m.Position.X + dx,
                    m.Position.Y + dy,
                    m.Position.Width,
                    m.Position.Height)
            })
            .ToList();

        var placed = new List<DisplayMonitor>
        {
            shifted.First(m => m.StableKey == primary.StableKey) with
            {
                Position = new Position(0, 0, primary.Position.Width, primary.Position.Height)
            }
        };

        var remaining = shifted
            .Where(m => m.StableKey != primary.StableKey)
            .ToList();

        while (remaining.Count > 0)
        {
            // First prefer any monitor that already touches the placed group without overlap,
            // so multi-monitor L-shapes and chains preserve their exact arrangement.
            var touchingIdx = -1;
            double bestTouchDist = double.MaxValue;
            for (int i = 0; i < remaining.Count; i++)
            {
                if (TouchesGroupWithoutOverlap(remaining[i].Position, placed))
                {
                    double d = EdgeDistanceToGroup(remaining[i].Position, placed);
                    if (d < bestTouchDist)
                    {
                        bestTouchDist = d;
                        touchingIdx = i;
                    }
                }
            }

            if (touchingIdx >= 0)
            {
                placed.Add(remaining[touchingIdx]);
                remaining.RemoveAt(touchingIdx);
                continue;
            }

            int closestIdx = 0;
            double bestDist = double.MaxValue;
            for (int i = 0; i < remaining.Count; i++)
            {
                double d = EdgeDistanceToGroup(remaining[i].Position, placed);
                if (d < bestDist)
                {
                    bestDist = d;
                    closestIdx = i;
                }
            }

            var next = remaining[closestIdx];
            remaining.RemoveAt(closestIdx);
            var tempGroup = new List<DisplayMonitor>(placed) { next };
            var snappedPos = SnapMonitorPosition(tempGroup, next.StableKey, next.Position.X, next.Position.Y, alignSnapThreshold);
            placed.Add(next with { Position = snappedPos });
        }

        return placed;
    }

    /// <summary>
    /// Aligns vertical Y positions of monitors relative to the primary monitor at (0,0).
    /// </summary>
    public static List<DisplayMonitor> AlignMonitorsVertically(
        IReadOnlyList<DisplayMonitor> monitors,
        VerticalAlignment alignment,
        string? primaryStableKey = null)
    {
        if (monitors.Count == 0)
            return new List<DisplayMonitor>();

        var normalized = NormalizeArrangement(monitors, primaryStableKey);
        var primary = (!string.IsNullOrWhiteSpace(primaryStableKey)
                          ? normalized.FirstOrDefault(m => m.StableKey == primaryStableKey)
                          : null)
                      ?? normalized.FirstOrDefault(m => m.Position.X == 0 && m.Position.Y == 0)
                      ?? normalized[0];

        int pH = primary.Position.Height;
        var aligned = new List<DisplayMonitor>(normalized.Count);
        foreach (var m in normalized)
        {
            if (m.StableKey == primary.StableKey)
            {
                aligned.Add(m with { Position = m.Position with { X = 0, Y = 0 } });
                continue;
            }

            int targetY = alignment switch
            {
                VerticalAlignment.Top => 0,
                VerticalAlignment.Center => (pH - m.Position.Height) / 2,
                VerticalAlignment.Bottom => pH - m.Position.Height,
                _ => 0
            };

            var candidatePos = m.Position with { Y = targetY };
            // If setting Y=targetY causes horizontal overlap with an already-aligned monitor
            // (e.g. monitors were previously stacked vertically at X=0), place it to the right.
            if (aligned.Any(a => Overlaps(candidatePos, a.Position)))
            {
                int maxRight = aligned.Max(a => a.Position.Right);
                candidatePos = candidatePos with { X = maxRight };
            }
            aligned.Add(m with { Position = candidatePos });
        }

        return NormalizeArrangement(aligned, primary.StableKey);
    }

    private static int SnapValue(int value, int[] targets, int threshold, int min, int max)
    {
        if (threshold <= 0)
            return value;
        int bestTarget = value;
        int bestDiff = threshold + 1;
        foreach (int t in targets)
        {
            if (t < min || t > max)
                continue;
            int diff = Math.Abs(value - t);
            if (diff <= threshold && diff < bestDiff)
            {
                bestDiff = diff;
                bestTarget = t;
            }
        }
        return bestTarget;
    }

    private static void AddIfNoOverlap(
        List<(int x, int y, double dist)> candidates,
        int x,
        int y,
        int w,
        int h,
        int rawX,
        int rawY,
        List<DisplayMonitor> others)
    {
        var candidatePos = new Position(x, y, w, h);
        foreach (var o in others)
        {
            if (Overlaps(candidatePos, o.Position))
                return;
        }
        double dx = x - rawX;
        double dy = y - rawY;
        candidates.Add((x, y, Math.Sqrt(dx * dx + dy * dy)));
    }

    private static bool Overlaps(Position a, Position b)
    {
        return a.X < b.Right && a.Right > b.X && a.Y < b.Bottom && a.Bottom > b.Y;
    }

    private static bool TouchesGroupWithoutOverlap(Position candidate, List<DisplayMonitor> placed)
    {
        foreach (var p in placed)
        {
            if (Overlaps(candidate, p.Position))
                return false;
        }
        foreach (var p in placed)
        {
            var b = p.Position;
            bool touchX = (candidate.Right == b.X || candidate.X == b.Right)
                          && Math.Min(candidate.Bottom, b.Bottom) - Math.Max(candidate.Y, b.Y) >= 60;
            bool touchY = (candidate.Bottom == b.Y || candidate.Y == b.Bottom)
                          && Math.Min(candidate.Right, b.Right) - Math.Max(candidate.X, b.X) >= 60;
            if (touchX || touchY)
                return true;
        }
        return false;
    }

    private static double EdgeDistanceToGroup(Position p, List<DisplayMonitor> placed)
    {
        return placed.Min(o =>
        {
            var b = o.Position;
            int dx = Math.Max(0, Math.Max(b.X - p.Right, p.X - b.Right));
            int dy = Math.Max(0, Math.Max(b.Y - p.Bottom, p.Y - b.Bottom));
            if (dx == 0 && dy == 0)
            {
                double cx = p.X + p.Width / 2.0;
                double cy = p.Y + p.Height / 2.0;
                double ox = b.X + b.Width / 2.0;
                double oy = b.Y + b.Height / 2.0;
                return Math.Sqrt((cx - ox) * (cx - ox) + (cy - oy) * (cy - oy)) * 0.001;
            }
            return Math.Sqrt((double)dx * dx + (double)dy * dy);
        });
    }
}


