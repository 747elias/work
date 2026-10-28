namespace Flowstate.Core.Focus;

public enum FocusPhase { Focus, ShortBreak, LongBreak }

public sealed record PomodoroSettings(int FocusMinutes = 25, int ShortBreakMinutes = 5, int LongBreakMinutes = 15, int SessionsBeforeLongBreak = 4);

public static class PomodoroCycle
{
    public static FocusPhase BreakAfter(int completedFocusSessions, PomodoroSettings s)
    {
        if (completedFocusSessions <= 0) throw new ArgumentOutOfRangeException(nameof(completedFocusSessions));
        return completedFocusSessions % Math.Max(1, s.SessionsBeforeLongBreak) == 0 ? FocusPhase.LongBreak : FocusPhase.ShortBreak;
    }

    public static TimeSpan Duration(FocusPhase phase, PomodoroSettings s) => phase switch
    {
        FocusPhase.Focus => TimeSpan.FromMinutes(s.FocusMinutes),
        FocusPhase.ShortBreak => TimeSpan.FromMinutes(s.ShortBreakMinutes),
        _ => TimeSpan.FromMinutes(s.LongBreakMinutes),
    };
}

/// <summary>Zeitbasierter Timer: rechnet mit Zeitstempeln statt Ticks, damit nichts driftet.</summary>
public sealed class FocusTimer
{
    private readonly TimeSpan _duration;
    private DateTime? _runningSince;
    private TimeSpan _elapsedBeforePause = TimeSpan.Zero;

    public FocusTimer(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(duration));
        _duration = duration;
    }

    public bool IsRunning => _runningSince is not null;

    public void Start(DateTime now) => _runningSince ??= now;

    public void Pause(DateTime now)
    {
        if (_runningSince is { } since)
        {
            _elapsedBeforePause += now - since;
            _runningSince = null;
        }
    }

    public TimeSpan Elapsed(DateTime now)
    {
        var e = _elapsedBeforePause + (_runningSince is { } s ? now - s : TimeSpan.Zero);
        return e > _duration ? _duration : e;
    }

    public TimeSpan Remaining(DateTime now) => _duration - Elapsed(now);

    public bool IsFinished(DateTime now) => Remaining(now) <= TimeSpan.Zero;
}
