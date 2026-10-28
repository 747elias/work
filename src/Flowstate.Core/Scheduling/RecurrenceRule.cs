namespace Flowstate.Core.Scheduling;

public enum RecurrenceFrequency { Daily, Weekly, Monthly }

public sealed record RecurrenceRule(RecurrenceFrequency Frequency, int Interval = 1, IReadOnlyCollection<DayOfWeek>? DaysOfWeek = null)
{
    public DateTime NextOccurrence(DateTime previous)
    {
        if (Interval < 1) throw new ArgumentOutOfRangeException(nameof(Interval));
        switch (Frequency)
        {
            case RecurrenceFrequency.Daily:
                return previous.AddDays(Interval);
            case RecurrenceFrequency.Monthly:
                return previous.AddMonths(Interval);
            case RecurrenceFrequency.Weekly:
                if (DaysOfWeek is null || DaysOfWeek.Count == 0) return previous.AddDays(7 * Interval);
                var candidate = previous.AddDays(1);
                var weekStart = previous.Date.AddDays(-(((int)previous.DayOfWeek + 6) % 7));
                while (true)
                {
                    var candWeekStart = candidate.Date.AddDays(-(((int)candidate.DayOfWeek + 6) % 7));
                    var weeksApart = (int)((candWeekStart - weekStart).TotalDays / 7);
                    if (weeksApart > 0 && weeksApart < Interval)
                    {
                        candidate = weekStart.AddDays(7 * Interval) + previous.TimeOfDay;
                        continue;
                    }
                    if (DaysOfWeek.Contains(candidate.DayOfWeek)) return candidate;
                    candidate = candidate.AddDays(1);
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(Frequency));
        }
    }
}
