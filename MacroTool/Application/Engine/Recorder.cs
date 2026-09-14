using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using MacroTool.Application.Interop;

namespace MacroTool.Application.Engine;

public sealed class Recorder
{
    public const double TrailingFilterWindowMs = 300;

    private readonly ConcurrentQueue<MacroEvent> _events = new();
    private readonly HashSet<int> _downKeys = [];
    private bool _mouseDown;
    private readonly object _gate = new();
    private readonly Stopwatch _clock = new();
    private Native.HookProc? _kbdProc;
    private Native.HookProc? _mouseProc;
    private Thread? _thread;
    private uint _hookThreadId;
    private double _durationMs;
    private int _clickCount;
    private int _keyCount;

    public int ClickCount => Volatile.Read(ref _clickCount);

    public int KeyCount => Volatile.Read(ref _keyCount);

    public double ElapsedMs => _clock.Elapsed.TotalMilliseconds;

    public void Start()
    {
        if (_thread is not null)
            return;
        _clock.Start();
        _thread = new Thread(Run) { IsBackground = true, Name = "macro-recorder" };
        _thread.Start();
    }

    public MacroTimeline Stop(bool dropTrailingMouse)
    {
        double stopT = _clock.Elapsed.TotalMilliseconds;
        if (_thread is not null)
        {
            Native.PostThreadMessage(_hookThreadId, Native.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
            _thread.Join(3000);
            _thread = null;
        }

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

    private void Run()
    {
        _hookThreadId = Native.GetCurrentThreadId();
        _kbdProc = KbdHook;
        _mouseProc = MouseHook;
        var hKbd = Native.SetWindowsHookEx(Native.WH_KEYBOARD_LL, _kbdProc, Native.GetModuleHandle(null), 0);
        var hMouse = Native.SetWindowsHookEx(Native.WH_MOUSE_LL, _mouseProc, Native.GetModuleHandle(null), 0);
        while (Native.GetMessage(out _, IntPtr.Zero, 0, 0) > 0)
        {
        }
        if (hKbd != IntPtr.Zero) Native.UnhookWindowsHookEx(hKbd);
        if (hMouse != IntPtr.Zero) Native.UnhookWindowsHookEx(hMouse);
        _durationMs = _clock.Elapsed.TotalMilliseconds;
    }

    private IntPtr KbdHook(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var s = Marshal.PtrToStructure<Native.KBDLLHOOKSTRUCT>(lParam);
            ProcessKey((int)s.vkCode, (int)(s.scanCode & 0xFF), (s.flags & Native.LLKHF_EXTENDED) != 0, (s.flags & Native.LLKHF_UP) != 0);
        }
        return Native.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
    }

    private IntPtr MouseHook(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var s = Marshal.PtrToStructure<Native.MSLLHOOKSTRUCT>(lParam);
            ProcessMouse(wParam.ToInt32(), s.pt.X, s.pt.Y);
        }
        return Native.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
    }

    internal void ProcessKey(int vk, int scan, bool ext, bool up)
    {
        if (vk == Native.VK_F10 || vk == Native.VK_F11 || vk == Native.VK_F12)
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

    internal void ProcessMouse(int msg, int x, int y)
    {
        if (msg is not (Native.WM_LBUTTONDOWN or Native.WM_LBUTTONUP))
            return;

        bool isDown = msg == Native.WM_LBUTTONDOWN;
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
