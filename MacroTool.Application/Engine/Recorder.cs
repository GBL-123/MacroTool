using System.Collections.Concurrent;
using System.Diagnostics;
using MacroTool.Application.Ports;
using MacroTool.Domain;

namespace MacroTool.Application.Engine;

public sealed class Recorder
{
    public const double TrailingFilterWindowMs = 300;

    private const int VkF10 = 0x7B;
    private const int VkF11 = 0x7C;
    private const int VkF12 = 0x7D;

    private readonly IInputCaptureSource _source;
    private readonly ConcurrentQueue<MacroEvent> _events = new();
    private readonly HashSet<int> _downKeys = [];
    private bool _mouseDown;
    private readonly object _gate = new();
    private readonly Stopwatch _clock = new();
    private double _durationMs;
    private int _clickCount;
    private int _keyCount;

    public Recorder(IInputCaptureSource source)
    {
        _source = source;
        _source.KeyCaptured += OnKeyCaptured;
        _source.MouseCaptured += OnMouseCaptured;
    }

    public int ClickCount => Volatile.Read(ref _clickCount);

    public int KeyCount => Volatile.Read(ref _keyCount);

    public double ElapsedMs => _clock.Elapsed.TotalMilliseconds;

    public void Start()
    {
        _clock.Start();
        _source.Start();
    }

    public MacroTimeline Stop(bool dropTrailingMouse)
    {
        double stopT = _clock.Elapsed.TotalMilliseconds;
        _source.Stop();
        _durationMs = _clock.Elapsed.TotalMilliseconds;

        var list = new List<MacroEvent>();
        while (_events.TryDequeue(out var e))
            list.Add(e);

        if (dropTrailingMouse)
            list = DropTrailingMouse(list, stopT - TrailingFilterWindowMs);

        list.Sort((a, b) => a.T.CompareTo(b.T));
        return new MacroTimeline
        {
            Events = list,
            DurationMs = _durationMs > 0 ? _durationMs : stopT
        };
    }

    public static List<MacroEvent> DropTrailingMouse(IEnumerable<MacroEvent> events, double cutoffMs)
        => events.Where(e => e.IsKey || e.T < cutoffMs).ToList();

    private void OnKeyCaptured(int vk, int scan, bool ext, bool up)
    {
        if (vk is VkF10 or VkF11 or VkF12)
            return;

        bool dup;
        lock (_gate)
        {
            dup = up ? !_downKeys.Remove(vk) : !_downKeys.Add(vk);
        }
        if (dup)
            return;

        _events.Enqueue(new MacroEvent
        {
            T = _clock.Elapsed.TotalMilliseconds,
            IsKey = true,
            Vk = vk,
            Scan = scan,
            Ext = ext,
            Up = up
        });
        if (!up) Interlocked.Increment(ref _keyCount);
    }

    private void OnMouseCaptured(bool isDown, int x, int y)
    {
        bool dup;
        lock (_gate)
        {
            dup = isDown ? _mouseDown : !_mouseDown;
            if (!dup) _mouseDown = isDown;
        }
        if (dup)
            return;

        _events.Enqueue(new MacroEvent
        {
            T = _clock.Elapsed.TotalMilliseconds,
            IsKey = false,
            Up = !isDown,
            X = x,
            Y = y
        });
        if (isDown) Interlocked.Increment(ref _clickCount);
    }
}
