using Flowstate.Core.Models;

namespace Flowstate.Core.Scheduling;

public sealed record AutoPlanOptions
{
    public int BreakMinutes { get; init; } = 5;
}

public sealed record PlannedBlock(Guid TaskId, DateTime Start, DateTime End, string Reason);

public sealed record UnscheduledTask(Guid TaskId, int MissingMinutes, string Reason);

public sealed record PlanResult(IReadOnlyList<PlannedBlock> Blocks, IReadOnlyList<UnscheduledTask> Unscheduled);

/// <summary>Deterministischer, erklaerbarer Greedy-Planer fuer AutoPlan.</summary>
public static class AutoPlanner
{
    public static double Score(FlowTask task, DateTime now)
    {
        double score = (int)task.Priority * 100;
        if (task.DueDate is { } due)
        {
            var hours = (due - now).TotalHours;
            score += hours <= 0 ? 400 : hours <= 24 ? 250 : hours <= 72 ? 120 : hours <= 168 ? 50 : 0;
        }
        return score;
    }

    public static PlanResult Plan(IEnumerable<FlowTask> tasks, IEnumerable<TimeBlock> freeSlots, DateTime now, AutoPlanOptions? options = null)
    {
        options ??= new AutoPlanOptions();
        var breakSpan = TimeSpan.FromMinutes(Math.Max(0, options.BreakMinutes));

        var slots = freeSlots
            .Select(s => s.Start < now ? s with { Start = now } : s)
            .Where(s => s.End > s.Start)
            .OrderBy(s => s.Start)
            .ToList();

        var ordered = tasks
            .Where(t => t.IsActive && t.EstimatedMinutes > 0)
            .OrderByDescending(t => Score(t, now))
            .ThenBy(t => t.DueDate ?? DateTime.MaxValue)
            .ThenBy(t => t.CreatedAt)
            .ToList();

        var blocks = new List<PlannedBlock>();
        var unscheduled = new List<UnscheduledTask>();

        foreach (var task in ordered)
        {
            var remaining = TimeSpan.FromMinutes(task.EstimatedMinutes);
            var reason = BuildReason(task, now);
            var minChunk = TimeSpan.FromMinutes(Math.Max(1, task.MinChunkMinutes));

            for (int i = 0; i < slots.Count && remaining > TimeSpan.Zero; i++)
            {
                var slot = slots[i];
                var latestEnd = task.DueDate is { } due && due < slot.End ? due : slot.End;
                var available = latestEnd - slot.Start;
                if (available <= TimeSpan.Zero) continue;

                TimeSpan alloc;
                if (!task.CanSplit)
                {
                    if (available < remaining) continue;
                    alloc = remaining;
                }
                else
                {
                    alloc = available < remaining ? available : remaining;
                    var needed = remaining < minChunk ? remaining : minChunk;
                    if (alloc < needed) continue;
                }

                var end = slot.Start + alloc;
                blocks.Add(new PlannedBlock(task.Id, slot.Start, end, reason));
                remaining -= alloc;

                var nextStart = end + breakSpan;
                if (nextStart < slot.End) slots[i] = slot with { Start = nextStart };
                else { slots.RemoveAt(i); i--; }
            }

            if (remaining > TimeSpan.Zero)
            {
                var why = task.DueDate is not null
                    ? "Nicht genug freie Zeit vor der Faelligkeit."
                    : "Nicht genug zusammenhaengende freie Zeit.";
                unscheduled.Add(new UnscheduledTask(task.Id, (int)Math.Ceiling(remaining.TotalMinutes), why));
            }
        }

        return new PlanResult(blocks.OrderBy(b => b.Start).ToList(), unscheduled);
    }

    private static string BuildReason(FlowTask task, DateTime now)
    {
        var parts = new List<string> { $"Prioritaet {task.Priority}" };
        if (task.DueDate is { } due)
            parts.Add(due <= now ? "ueberfaellig" : $"faellig am {due:dd.MM.yyyy HH:mm}");
        return string.Join(", ", parts);
    }
}
