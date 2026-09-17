using MacroTool.Application.Ports;

namespace MacroTool.Infrastructure.Interop;

public sealed class Win32InputSink : IInputSink
{
    public bool SendKey(int scan, bool up, bool ext) => InputSender.SendKey(scan, up, ext);

    public bool SendMouseDown(int x, int y) => InputSender.SendMouseDown(x, y);

    public bool SendMouseUp() => InputSender.SendMouseUp();
}
