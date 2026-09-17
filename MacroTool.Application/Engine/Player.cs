using System.Diagnostics;
using MacroTool.Application.Ports;
using MacroTool.Domain;

namespace MacroTool.Application.Engine;

public sealed class Player
{
    private readonly MacroTimeline _timeline;
    private readonly EngineSettings _settings;
    private readonly IInputSink _sink;
    private volatile bool _stop;
    private Thread? _thread;
    private int _round;
    private readonly Stopwatch _clock = new();

    public Player(MacroTimeline timeline, EngineSettings settings, IInputSink sink)
    {
        _timeline = timeline;
        _settings = settings;
        _sink = sink;
    }

    public int Round => Volatile.Read(ref _round);

    public double ElapsedMs => _clock.Elapsed.TotalMilliseconds;

    public event Action<int>? RoundCompleted;

    public event Action? InjectionFailed;

    /// <summary>回放达到设定次数后自然完成（用户手动停止不触发）。</summary>
    public event Action<int>? Completed;

    public void Start()
    {
        _stop = false;
        _clock.Start();
        _thread = new Thread(Run) { IsBackground = true, Name = "macro-player" };
        _thread.Start();
    }

    public void Stop()
    {
        _stop = true;
        _thread?.Join(3000);
        _thread = null;
    }

    private void Run()
    {
        var events = _timeline.Events;
        if (events.Count == 0) return;

        double lastT = 0;
        foreach (var e in events)
            if (e.T > lastT) lastT = e.T;
        double roundMs = Math.Max(_timeline.DurationMs, lastT);

        bool keyDown = false;
        int pendingScan = 0;
        bool pendingExt = false;
        bool clickDown = false;

        while (!_stop)
        {
            int repeatTarget = _settings.RepeatCount;
            if (repeatTarget > 0 && Volatile.Read(ref _round) >= repeatTarget)
                break;

            double speed = _settings.Speed;
            if (speed <= 0) speed = 1.0;
            int jitter = Math.Max(0, _settings.Jitter);
            Random? rnd = jitter > 0 ? Random.Shared : null;

            Interlocked.Increment(ref _round);
            long roundStart = _clock.ElapsedTicks;
            bool aborted = false;

            foreach (var e in events)
            {
                long target = roundStart + (long)(e.T * TimeSpan.TicksPerMillisecond / speed);
                if (!WaitUntil(target))
                {
                    aborted = true;
                    break;
                }

                if (e.IsKey)
                {
                    if (!_sink.SendKey(e.Scan, e.Up, e.Ext))
                        InjectionFailed?.Invoke();
                    if (!e.Up)
                    {
                        keyDown = true;
                        pendingScan = e.Scan;
                        pendingExt = e.Ext;
                    }
                    else
                    {
                        keyDown = false;
                    }
                }
                else
                {
                    if (!e.Up)
                    {
                        int jx = e.X;
                        int jy = e.Y;
                        if (rnd is not null)
                        {
                            jx += rnd.Next(-jitter, jitter + 1);
                            jy += rnd.Next(-jitter, jitter + 1);
                        }
                        if (!_sink.SendMouseDown(jx, jy))
                            InjectionFailed?.Invoke();
                        clickDown = true;
                    }
                    else
                    {
                        if (!_sink.SendMouseUp())
                            InjectionFailed?.Invoke();
                        clickDown = false;
                    }
                }
            }

            if (aborted || _stop) break;

            long roundEnd = roundStart + (long)(roundMs * TimeSpan.TicksPerMillisecond / speed);
            if (!WaitUntil(roundEnd)) break;
            RoundCompleted?.Invoke(Round);
        }

        if (keyDown) _sink.SendKey(pendingScan, true, pendingExt);
        if (clickDown) _sink.SendMouseUp();

        if (!_stop) Completed?.Invoke(_round);
    }

    private bool WaitUntil(long targetTicks)
    {
        while (_clock.ElapsedTicks < targetTicks)
        {
            if (_stop) return false;
            long remainTicks = targetTicks - _clock.ElapsedTicks;
            double remainMs = remainTicks * 1000.0 / Stopwatch.Frequency;
            if (remainMs > 15) Thread.Sleep(1);
        }
        return true;
    }
}
