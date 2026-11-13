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

        var totalWidth = monitors.Sum(m => m.Position.Width);
        var primary = monitors.FirstOrDefault(m => m.Position.X == 0 && m.Position.Y == 0)
                      ?? monitors[0];

        var mirrored = new List<DisplayMonitor>(monitors.Count);
        foreach (var m in monitors)
        {
            var newX = totalWidth - (m.Position.X + m.Position.Width);
            var pos = m.Position with { X = newX };
            mirrored.Add(m with { Position = pos });
        }

        // Re-assign primary to keep identity primary at (0,0)
        var newPrimary = mirrored.First(m => m.StableKey == primary.StableKey);
        var rest = mirrored.Where(m => m.StableKey != primary.StableKey)
                           .OrderBy(m => m.Position.X)
                           .ToList();

        var result = new List<DisplayMonitor>(mirrored.Count);
        result.Add(newPrimary with { Position = newPrimary.Position with { X = 0, Y = newPrimary.Position.Y } });

        // Legalize: pack without gaps, preserve Y from mirrored
        int cursor = result[0].Position.Width;
        foreach (var m in rest)
        {
            var pos = m.Position with { X = cursor };
            cursor += m.Position.Width;
            result.Add(m with { Position = pos });
        }

        return Legalize(result);
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
                      ?? monitors.OrderBy(m => m.Position.X).First();

        var ordered = monitors
            .OrderBy(m => m.Position.X)
            .ThenBy(m => m.Position.Y)
            .ToList();

        var result = new List<DisplayMonitor>(ordered.Count);
        int cursorX = 0;
        foreach (var m in ordered)
        {
            var pos = m.Position with { X = cursorX };
            result.Add(m with { Position = pos });
            cursorX += m.Position.Width;
        }

        // Ensure primary at 0,0 --- if not first after sort, reorder with primary first
        var primaryTuple = result.First(m => m.StableKey == primary.StableKey);
        if (result[0].StableKey != primary.StableKey)
        {
            var others = result.Where(m => m.StableKey != primary.StableKey).ToList();
            result.Clear();
            result.Add(primaryTuple with { Position = primaryTuple.Position with { X = 0, Y = 0 } });
            int cx = primaryTuple.Position.Width;
            foreach (var m in others)
            {
                result.Add(m with { Position = m.Position with { X = cx } });
                cx += m.Position.Width;
            }
        }
        else
        {
            result[0] = result[0] with { Position = result[0].Position with { Y = 0 } };
        }

        return result;
    }

    public static bool IsLegal(IReadOnlyList<DisplayMonitor> monitors)
    {
        if (monitors.Count == 0)
            return true;
        // Check sorted no gap/overlap and primary at 0,0
        var sorted = monitors.OrderBy(m => m.Position.X).ToList();
        if (sorted[0].Position.X != 0)
            return false;
        for (int i = 1; i < sorted.Count; i++)
        {
            var prev = sorted[i - 1];
            var curr = sorted[i];
            if (curr.Position.X != prev.Position.Right)
                return false;
        }
        return true;
    }
}


