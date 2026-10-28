using Flowstate.Core.Focus;
using Flowstate.Core.Models;
using Flowstate.Core.Scheduling;
using Flowstate.Core.Updates;
using Flowstate.Core.Validation;
using Xunit;

namespace Flowstate.Core.Tests;

public class TaskValidatorTests
{
    [Fact] public void EmptyTitle_IsInvalid() => Assert.NotEmpty(TaskValidator.Validate(new FlowTask { Title = " " }));
    [Fact] public void ValidTask_HasNoErrors() => Assert.Empty(TaskValidator.Validate(new FlowTask { Title = "Bericht", EstimatedMinutes = 30 }));
    [Fact] public void ZeroDuration_IsInvalid() => Assert.NotEmpty(TaskValidator.Validate(new FlowTask { Title = "X", EstimatedMinutes = 0 }));

    [Fact]
    public void CompleteAndReopen_UpdatesStatus()
    {
        var t = new FlowTask { Title = "X" };
        t.Complete(new DateTime(2026, 1, 1));
        Assert.False(t.IsActive);
        Assert.NotNull(t.CompletedAt);
        t.Reopen();
        Assert.True(t.IsActive);
        Assert.Null(t.CompletedAt);
    }
}

public class FreeSlotCalculatorTests
{
    private static readonly DateTime D = new(2026, 3, 2);

    [Fact]
    public void SubtractsAndMergesBusyBlocks()
    {
        var busy = new[]
        {
            new TimeBlock(D.AddHours(10), D.AddHours(11)),
            new TimeBlock(D.AddHours(10.5), D.AddHours(12)),
            new TimeBlock(D.AddHours(14), D.AddHours(15)),
        };
        var free = FreeSlotCalculator.Calculate(D.AddHours(8), D.AddHours(17), busy);
        Assert.Equal(3, free.Count);
        Assert.Equal(new TimeBlock(D.AddHours(8), D.AddHours(10)), free[0]);
        Assert.Equal(new TimeBlock(D.AddHours(12), D.AddHours(14)), free[1]);
        Assert.Equal(new TimeBlock(D.AddHours(15), D.AddHours(17)), free[2]);
    }

    [Fact]
    public void FullyBusy_ReturnsNothing()
        => Assert.Empty(FreeSlotCalculator.Calculate(D.AddHours(8), D.AddHours(9), new[] { new TimeBlock(D.AddHours(7), D.AddHours(10)) }));
}

public class AutoPlannerTests
{
    private static readonly DateTime D = new(2026, 3, 2);

    [Fact]
    public void HigherPriorityIsPlannedFirst()
    {
        var low = new FlowTask { Title = "Low", Priority = TaskPriority.Low, EstimatedMinutes = 60 };
        var high = new FlowTask { Title = "High", Priority = TaskPriority.Urgent, EstimatedMinutes = 60 };
        var r = AutoPlanner.Plan(new[] { low, high }, new[] { new TimeBlock(D.AddHours(8), D.AddHours(12)) }, D.AddHours(7));
        Assert.Equal(high.Id, r.Blocks[0].TaskId);
        Assert.Equal(D.AddHours(9).AddMinutes(5), r.Blocks[1].Start);
        Assert.Empty(r.Unscheduled);
    }

    [Fact]
    public void NeverPlansInThePastOrOverlapping()
    {
        var tasks = Enumerable.Range(0, 5).Select(i => new FlowTask { Title = $"T{i}", EstimatedMinutes = 40 }).ToList();
        var now = D.AddHours(9);
        var r = AutoPlanner.Plan(tasks, new[] { new TimeBlock(D.AddHours(8), D.AddHours(12)) }, now);
        Assert.All(r.Blocks, b => Assert.True(b.Start >= now));
        for (int i = 1; i < r.Blocks.Count; i++) Assert.True(r.Blocks[i].Start >= r.Blocks[i - 1].End);
    }

    [Fact]
    public void NonSplittableTaskTooLong_IsUnscheduled()
    {
        var t = new FlowTask { Title = "Lang", EstimatedMinutes = 120 };
        var r = AutoPlanner.Plan(new[] { t }, new[] { new TimeBlock(D.AddHours(8), D.AddHours(9)), new TimeBlock(D.AddHours(10), D.AddHours(11)) }, D);
        Assert.Empty(r.Blocks);
        Assert.Equal(120, r.Unscheduled.Single().MissingMinutes);
    }

    [Fact]
    public void SplittableTask_IsSplitAcrossSlots()
    {
        var t = new FlowTask { Title = "Split", EstimatedMinutes = 120, CanSplit = true, MinChunkMinutes = 30 };
        var r = AutoPlanner.Plan(new[] { t }, new[] { new TimeBlock(D.AddHours(8), D.AddHours(9)), new TimeBlock(D.AddHours(10), D.AddHours(11)) }, D);
        Assert.Equal(2, r.Blocks.Count);
        Assert.Empty(r.Unscheduled);
    }

    [Fact]
    public void DeadlineIsRespected()
    {
        var t = new FlowTask { Title = "Frist", EstimatedMinutes = 60, DueDate = D.AddHours(9) };
        var r = AutoPlanner.Plan(new[] { t }, new[] { new TimeBlock(D.AddHours(8.5), D.AddHours(12)) }, D);
        Assert.Empty(r.Blocks);
        Assert.Single(r.Unscheduled);
    }

    [Fact]
    public void DoneTasksAreIgnored()
    {
        var t = new FlowTask { Title = "Fertig" };
        t.Complete(D);
        var r = AutoPlanner.Plan(new[] { t }, new[] { new TimeBlock(D.AddHours(8), D.AddHours(12)) }, D);
        Assert.Empty(r.Blocks);
        Assert.Empty(r.Unscheduled);
    }
}

public class RecurrenceRuleTests
{
    [Fact] public void Daily() => Assert.Equal(new DateTime(2026, 1, 3), new RecurrenceRule(RecurrenceFrequency.Daily, 2).NextOccurrence(new DateTime(2026, 1, 1)));
    [Fact] public void MonthlyClampsEndOfMonth() => Assert.Equal(new DateTime(2026, 2, 28), new RecurrenceRule(RecurrenceFrequency.Monthly).NextOccurrence(new DateTime(2026, 1, 31)));

    [Fact]
    public void WeeklyOnSelectedDays()
    {
        var rule = new RecurrenceRule(RecurrenceFrequency.Weekly, 1, new[] { DayOfWeek.Monday, DayOfWeek.Thursday });
        var mon = new DateTime(2026, 3, 2, 9, 0, 0);
        var thu = rule.NextOccurrence(mon);
        Assert.Equal(new DateTime(2026, 3, 5, 9, 0, 0), thu);
        Assert.Equal(new DateTime(2026, 3, 9, 9, 0, 0), rule.NextOccurrence(thu));
    }

    [Fact]
    public void EveryTwoWeeks()
    {
        var rule = new RecurrenceRule(RecurrenceFrequency.Weekly, 2, new[] { DayOfWeek.Monday });
        Assert.Equal(new DateTime(2026, 3, 16, 9, 0, 0), rule.NextOccurrence(new DateTime(2026, 3, 2, 9, 0, 0)));
    }
}

public class FocusTests
{
    private static readonly PomodoroSettings S = new();

    [Theory]
    [InlineData(1, FocusPhase.ShortBreak)]
    [InlineData(3, FocusPhase.ShortBreak)]
    [InlineData(4, FocusPhase.LongBreak)]
    [InlineData(8, FocusPhase.LongBreak)]
    public void BreakAfter(int n, FocusPhase expected) => Assert.Equal(expected, PomodoroCycle.BreakAfter(n, S));

    [Fact]
    public void TimerHandlesPauseWithoutDrift()
    {
        var t0 = new DateTime(2026, 1, 1, 10, 0, 0);
        var timer = new FocusTimer(TimeSpan.FromMinutes(25));
        timer.Start(t0);
        timer.Pause(t0.AddMinutes(10));
        Assert.Equal(TimeSpan.FromMinutes(15), timer.Remaining(t0.AddHours(1)));
        timer.Start(t0.AddMinutes(30));
        Assert.True(timer.IsFinished(t0.AddMinutes(45)));
        Assert.Equal(TimeSpan.Zero, timer.Remaining(t0.AddHours(3)));
    }
}

public class SemanticVersionTests
{
    [Theory]
    [InlineData("1.0.0", "1.0.1", true)]
    [InlineData("v1.0.0", "v1.0.0", false)]
    [InlineData("1.2.0", "1.10.0", true)]
    [InlineData("1.0.0", "0.9.9", false)]
    [InlineData("0.9.0-beta", "0.9.0", true)]
    [InlineData("1.0.0", "1.1.0-beta", false)]
    public void UpdateAvailable_Stable(string cur, string latest, bool expected)
        => Assert.Equal(expected, SemanticVersion.IsUpdateAvailable(cur, latest, includePreReleases: false));

    [Fact] public void PreReleaseChannel() => Assert.True(SemanticVersion.IsUpdateAvailable("1.0.0", "1.1.0-beta.2", true));
    [Fact] public void PreReleaseOrdering() => Assert.True(SemanticVersion.TryParse("1.0.0-beta.2", out var a) && SemanticVersion.TryParse("1.0.0-beta.10", out var b) && a!.CompareTo(b) < 0);
    [Fact] public void InvalidVersionIsRejected() => Assert.False(SemanticVersion.TryParse("1.0", out _));
    [Fact] public void InvalidInputNeverOffersUpdate() => Assert.False(SemanticVersion.IsUpdateAvailable("1.0.0", "kaputt", true));
}
