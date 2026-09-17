namespace MacroTool.Application.Ports;

public interface IInputSink
{
    bool SendKey(int scan, bool up, bool ext);

    bool SendMouseDown(int x, int y);

    bool SendMouseUp();
}
