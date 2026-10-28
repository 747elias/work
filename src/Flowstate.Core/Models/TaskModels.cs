namespace Flowstate.Core.Models;

public enum TaskPriority { Low = 0, Medium = 1, High = 2, Urgent = 3 }

public enum FlowTaskStatus { Open, InProgress, Waiting, Done, Cancelled }

public enum EnergyLevel { Low, Medium, High }

public sealed class FlowTask
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public FlowTaskStatus Status { get; set; } = FlowTaskStatus.Open;
    public int EstimatedMinutes { get; set; } = 30;
    public DateTime? DueDate { get; set; }
    public string? Context { get; set; }
    public EnergyLevel Energy { get; set; } = EnergyLevel.Medium;
    public bool CanSplit { get; set; }
    public int MinChunkMinutes { get; set; } = 25;
    public DateTime CreatedAt { get; init; } = DateTime.Now;
    public DateTime? CompletedAt { get; private set; }

    public bool IsActive => Status is not (FlowTaskStatus.Done or FlowTaskStatus.Cancelled);

    public void Complete(DateTime when)
    {
        Status = FlowTaskStatus.Done;
        CompletedAt = when;
    }

    public void Reopen()
    {
        Status = FlowTaskStatus.Open;
        CompletedAt = null;
    }
}

public sealed record TimeBlock(DateTime Start, DateTime End)
{
    public TimeSpan Duration => End - Start;
    public bool Overlaps(TimeBlock other) => Start < other.End && other.Start < End;
}
