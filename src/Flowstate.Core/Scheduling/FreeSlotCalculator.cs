using Flowstate.Core.Models;

namespace Flowstate.Core.Scheduling;

public static class FreeSlotCalculator
{
    public static IReadOnlyList<TimeBlock> Calculate(DateTime windowStart, DateTime windowEnd, IEnumerable<TimeBlock> busy)
    {
        if (windowEnd <= windowStart) return Array.Empty<TimeBlock>();

        var merged = new List<TimeBlock>();
        foreach (var b in busy.Where(b => b.End > b.Start).OrderBy(b => b.Start))
        {
            if (merged.Count > 0 && b.Start <= merged[^1].End)
            {
                var last = merged[^1];
                merged[^1] = last with { End = b.End > last.End ? b.End : last.End };
            }
            else merged.Add(b);
        }

        var free = new List<TimeBlock>();
        var cursor = windowStart;
        foreach (var b in merged)
        {
            if (b.End <= cursor) continue;
            if (b.Start >= windowEnd) break;
            if (b.Start > cursor) free.Add(new TimeBlock(cursor, b.Start));
            if (b.End > cursor) cursor = b.End;
        }
        if (cursor < windowEnd) free.Add(new TimeBlock(cursor, windowEnd));
        return free;
    }
}
