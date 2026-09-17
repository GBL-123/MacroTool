using System.Runtime.InteropServices;
using MacroTool.Application.Ports;

namespace MacroTool.Infrastructure.Interop;

public sealed class LowLevelHookCapture : IInputCaptureSource
{
    private Native.HookProc? _kbdProc;
    private Native.HookProc? _mouseProc;
    private Thread? _thread;
    private uint _hookThreadId;
    private int _disposed;

    public event Action<int, int, bool, bool>? KeyCaptured;

    public event Action<bool, int, int>? MouseCaptured;

    public void Start()
    {
        if (_thread is not null)
            return;
        _thread = new Thread(Run) { IsBackground = true, Name = "macro-recorder" };
        _thread.Start();
    }

    public void Stop()
    {
        var thread = _thread;
        if (thread is null)
            return;
        Native.PostThreadMessage(_hookThreadId, Native.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
        thread.Join(3000);
        _thread = null;
    }

    internal static bool TryTranslateLeftButton(int msg, out bool isDown)
    {
        if (msg is Native.WM_LBUTTONDOWN or Native.WM_LBUTTONUP)
        {
            isDown = msg == Native.WM_LBUTTONDOWN;
            return true;
        }
        isDown = false;
        return false;
    }

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
    }

    private IntPtr KbdHook(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var s = Marshal.PtrToStructure<Native.KBDLLHOOKSTRUCT>(lParam);
            KeyCaptured?.Invoke((int)s.vkCode, (int)(s.scanCode & 0xFF), (s.flags & Native.LLKHF_EXTENDED) != 0, (s.flags & Native.LLKHF_UP) != 0);
        }
        return Native.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
    }

    private IntPtr MouseHook(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && TryTranslateLeftButton(wParam.ToInt32(), out bool isDown))
        {
            var s = Marshal.PtrToStructure<Native.MSLLHOOKSTRUCT>(lParam);
            MouseCaptured?.Invoke(isDown, s.pt.X, s.pt.Y);
        }
        return Native.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;
        Stop();
    }
}
