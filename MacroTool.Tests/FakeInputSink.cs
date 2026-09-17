using MacroTool.Application.Ports;

namespace MacroTool.Tests;

internal sealed class FakeInputSink : IInputSink
{
    public List<(int Scan, bool Up, bool Ext)> KeyCalls { get; } = [];

    public List<(int X, int Y)> MouseDownCalls { get; } = [];

    public int MouseUpCalls { get; private set; }

    public bool Result { get; set; } = true;

    public bool SendKey(int scan, bool up, bool ext)
    {
        KeyCalls.Add((scan, up, ext));
        return Result;
    }

    public bool SendMouseDown(int x, int y)
    {
        MouseDownCalls.Add((x, y));
        return Result;
    }

    public bool SendMouseUp()
    {
        MouseUpCalls++;
        return Result;
    }
}
