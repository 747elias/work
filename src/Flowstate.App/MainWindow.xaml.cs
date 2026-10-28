using Flowstate.Core.Focus;
using Microsoft.UI.Xaml;

namespace Flowstate.App;

public sealed partial class MainWindow : Window
{
    private readonly PomodoroSettings _settings = new();
    private readonly DispatcherTimer _tick = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private FocusPhase _phase = FocusPhase.Focus;
    private int _completed;
    private FocusTimer _timer;

    public MainWindow()
    {
        InitializeComponent();
        _timer = new FocusTimer(PomodoroCycle.Duration(_phase, _settings));
        _tick.Tick += (_, _) => Refresh();
        Refresh();
    }

    private void OnStartPause(object sender, RoutedEventArgs e)
    {
        var now = DateTime.Now;
        if (_timer.IsRunning) { _timer.Pause(now); _tick.Stop(); }
        else { _timer.Start(now); _tick.Start(); }
        Refresh();
    }

    private void OnReset(object sender, RoutedEventArgs e)
    {
        _tick.Stop();
        _timer = new FocusTimer(PomodoroCycle.Duration(_phase, _settings));
        Refresh();
    }

    private void OnSkip(object sender, RoutedEventArgs e)
    {
        Advance(countFocus: false);
        Refresh();
    }

    private void Refresh()
    {
        var now = DateTime.Now;
        if (_timer.IsRunning && _timer.IsFinished(now)) Advance(countFocus: true);
        var r = _timer.Remaining(now);
        var secs = (int)Math.Ceiling(r.TotalSeconds);
        TimeText.Text = $"{secs / 60:00}:{secs % 60:00}";
        PhaseText.Text = _phase switch
        {
            FocusPhase.Focus => "Fokus",
            FocusPhase.ShortBreak => "Kurze Pause",
            _ => "Lange Pause",
        };
        SessionText.Text = $"Abgeschlossene Sitzungen: {_completed}";
        StartPauseButton.Content = _timer.IsRunning ? "Pause" : "Start";
    }

    private void Advance(bool countFocus)
    {
        _tick.Stop();
        if (_phase == FocusPhase.Focus)
        {
            if (countFocus) _completed++;
            _phase = _completed > 0 ? PomodoroCycle.BreakAfter(_completed, _settings) : FocusPhase.ShortBreak;
        }
        else
        {
            _phase = FocusPhase.Focus;
        }
        _timer = new FocusTimer(PomodoroCycle.Duration(_phase, _settings));
    }
}
