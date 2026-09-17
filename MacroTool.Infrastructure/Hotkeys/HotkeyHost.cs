using MacroTool.Application.Ports;
using MacroTool.Infrastructure.Interop;

namespace MacroTool.Infrastructure.Hotkeys;

public sealed class HotkeyHost : IGlobalHotkeys
{
    private const int IdF10 = 10;
    private const int IdF11 = 11;
    private const int IdF12 = 12;

    private readonly ManualResetEventSlim _ready = new(false);
    private Thread? _thread;
    private uint _threadId;
    private int _disposed;

    public Action? OnF10 { get; set; }

    public Action? OnF11 { get; set; }

    public Action? OnF12 { get; set; }

    public HotkeyStatus Status { get; private set; } = new(false, false, "F12", true);

    public event Action? StatusChanged;

    public void Start()
    {
        if (_thread is not null)
            return;
        _thread = new Thread(ThreadMain) { IsBackground = true, Name = "macro-hotkeys" };
        _thread.Start();
        _ready.Wait(3000);
    }

    private void ThreadMain()
    {
        _threadId = Native.GetCurrentThreadId();
        Native.PeekMessage(out _, IntPtr.Zero, 0, 0, Native.PM_NOREMOVE);

        bool f10 = Native.RegisterHotKey(IntPtr.Zero, IdF10, 0, Native.VK_F10);
        bool f11 = Native.RegisterHotKey(IntPtr.Zero, IdF11, 0, Native.VK_F11);
        bool f12 = Native.RegisterHotKey(IntPtr.Zero, IdF12, 0, Native.VK_F12);
        string exitKeyLabel = "F12";
        if (!f12)
        {
            f12 = Native.RegisterHotKey(IntPtr.Zero, IdF12, Native.MOD_CONTROL, Native.VK_F12);
            exitKeyLabel = f12 ? "Ctrl+F12" : "(F12 被占用)";
        }

        Status = new HotkeyStatus(f10, f11, exitKeyLabel, f12);
        _ready.Set();
        StatusChanged?.Invoke();

        while (Native.GetMessage(out var msg, IntPtr.Zero, 0, 0) > 0)
        {
            if (msg.message != Native.WM_HOTKEY)
                continue;
            switch (msg.wParam.ToInt32())
            {
                case IdF10:
                    OnF10?.Invoke();
                    break;
                case IdF11:
                    OnF11?.Invoke();
                    break;
                case IdF12:
                    OnF12?.Invoke();
                    break;
            }
        }

        if (f10) Native.UnregisterHotKey(IntPtr.Zero, IdF10);
        if (f11) Native.UnregisterHotKey(IntPtr.Zero, IdF11);
        if (f12) Native.UnregisterHotKey(IntPtr.Zero, IdF12);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;
        if (_thread is not null && _thread.IsAlive)
        {
            _ready.Wait(3000);
            Native.PostThreadMessage(_threadId, Native.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
            _thread.Join(3000);
        }
        _thread = null;
    }
}
